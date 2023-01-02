using System.Buffers;
using System.Net;
using System.Text.RegularExpressions;
using mailica.Entities;
using mailica.Services;
using mailica.Smtp.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace mailica.Smtp;

public class SessionContext : IDisposable
{
    Guid ContextId { get; } = Guid.NewGuid();
    public IServiceProvider ServiceProvider { get; }
    public AppDbContext Db { get; }
    public IMemoryCache Cache { get; }
    public ServerOptions ServerOptions { get; }
    public EndpointDefinition EndpointDefinition { get; }
    public IPEndPoint? RemoteEndpoint { get; set; }
    public SecurableDuplexPipe? Pipe { get; set; }
    public MessageTransaction Transaction { get; }
    public User? User { get; private set; }
    public bool IsAuthenticated => User != null;
    public int AuthenticationAttempts { get; set; }
    public bool IsQuitRequested { get; set; }
    public Dictionary<string, object> Properties { get; }
    public SessionContext(IServiceProvider serviceProvider, ServerOptions options, EndpointDefinition endpointDefinition)
    {
        ServiceProvider = serviceProvider;
        ServerOptions = options;
        EndpointDefinition = endpointDefinition;
        Transaction = new MessageTransaction(endpointDefinition.AuthenticationRequired);
        Properties = new();

        var dbFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        Db = dbFactory.CreateDbContext();

        var cache = serviceProvider.GetRequiredService<IMemoryCache>();
        Cache = cache;
    }
    public void Log(string message, object? properties = null) => Db.Logs.Add(new(ContextId, message, properties));

    public async Task<bool> ShouldBlockConnectionFrom(IPEndPoint ip)
    {
        await Task.CompletedTask;
        if (Transaction.Outgoing)
        {
            var failedAuthCountKey = C.Cache.FailedAuthCount(ip);
            if (Cache.TryGetValue<int>(failedAuthCountKey, out var result))
                return result >= 8; // TODO: move to config
        }

        return false;
    }
    public async Task<bool> AuthenticateAsync(string? user, string? password, CancellationToken token)
    {
        if (RemoteEndpoint == null)
            return false;

        var failedAuthCountKey = C.Cache.FailedAuthCount(RemoteEndpoint);
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            Log("Authentication failed, no username and/or password provided");
            return IncurInfraction(failedAuthCountKey);
        }

        var userLower = user.ToLower();
        var dbUser = await Db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Name.Equals(userLower) && !u.Disabled.HasValue, token);
        if (dbUser == null)
        {
            Log($"Authentication failed, invalid user", new { user });
            return IncurInfraction(failedAuthCountKey);
        }

        if (!DovecotHasher.Verify(dbUser.Salt, dbUser.Hash, password))
        {
            Log($"Authentication failed, invalid password", new { user });
            return IncurInfraction(failedAuthCountKey);
        }

        Cache.Remove(failedAuthCountKey);
        User = dbUser;
        Log($"Authenticated", new { user });
        return true;

        bool IncurInfraction(string failedAuthCountKey)
        {
            var failedAuthCount = Cache.GetOrCreate<int>(failedAuthCountKey, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                e.SlidingExpiration = TimeSpan.FromMinutes(5);
                return default;
            });
            Cache.Set(failedAuthCountKey, ++failedAuthCount);
            return false;
        }
    }
    public async Task<MailboxFilterResult> CanAcceptFromAsync(EmailAddress @from, int size, CancellationToken cancellationToken = default)
    {
        if (Transaction.Outgoing)
        {
            if (User == null)
            {
                Log("Cannot send without authentication");
                return MailboxFilterResult.NoPermanently;
            }

            var domain = await Db.Domains
                .AsNoTracking()
                .Where(d => d.Name.Equals(@from.Host.ToLower()) && !d.Disabled.HasValue)
                .Include(d => d.Addresses.Where(a => a.Users.Contains(User) && !a.Disabled.HasValue))
                .SingleOrDefaultAsync(cancellationToken);
            if (domain == null)
            {
                Log("Unsupported domain", new { domain = @from.Host });
                return MailboxFilterResult.NoPermanently;
            }

            var authorizedToSend = false;
            foreach (var address in domain.Addresses)
            {
                if (address.IsStatic)
                    authorizedToSend = address.Pattern.Equals(@from.User, StringComparison.InvariantCultureIgnoreCase);
                else
                    authorizedToSend = Regex.IsMatch(@from.User, address.Pattern, RegexOptions.IgnoreCase);

                if (authorizedToSend)
                    break;
            }

            if (authorizedToSend)
            {
                Transaction.FromUser = User;
                return MailboxFilterResult.Yes;
            }
            else
            {
                Log("User not authorized to send as", new { address = @from.ToString(), user = User.Name });
                return MailboxFilterResult.NoTemporarily;
            }
        }

        // No checking for outside senders for now
        return MailboxFilterResult.Yes;
    }
    public async Task<MailboxFilterResult> CanDeliverToAsync(EmailAddress to, EmailAddress @from, CancellationToken cancellationToken = default)
    {
        if (Transaction.Outgoing) // No checking for internal senders
            return MailboxFilterResult.Yes;

        var domain = await Db.Domains
              .AsNoTracking()
              .Where(d => d.Name.Equals(to.Host.ToLower()) && !d.Disabled.HasValue)
              .Include(d => d.Addresses.Where(a => !a.Disabled.HasValue))
              .ThenInclude(da => da.Users)
              .SingleOrDefaultAsync(cancellationToken);
        if (domain == null)
        {
            Log("Not authorized to receive for domain", new { domain = to.Host });
            return MailboxFilterResult.NoPermanently;
        }
        foreach (var address in domain.Addresses)
            if (address.IsStatic)
            {
                if (address.Pattern.Equals(to.User, StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var user in address.Users)
                        Transaction.ToUsers.TryAdd(user.UserId, user);
                    Log("Recipient address matched", new { recipient = to.ToString(), address = address.Pattern });
                    return MailboxFilterResult.Yes;
                }
            }
            else if (Regex.IsMatch(to.User, address.Pattern, RegexOptions.IgnoreCase))
            {
                foreach (var user in address.Users)
                    Transaction.ToUsers.TryAdd(user.UserId, user);
                Log("Recipient address matched", new { recipient = to.ToString(), pattern = address.Pattern });
                return MailboxFilterResult.Yes;
            }

        Log("Recipient address not matched", new { recipient = to.ToString() });
        return MailboxFilterResult.NoPermanently;
    }
    public async Task<Response> SaveAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        // TODO: save or send email
        Console.WriteLine("Message pushed.");
        await using var stream = new MemoryStream();

        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            await stream.WriteAsync(memory, cancellationToken);
        }

        stream.Position = 0;

        using var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
        using var fs = File.OpenWrite($"{DateTime.UtcNow.Ticks}.eml");
        await message.WriteToAsync(fs, cancellationToken);
        return Response.Ok;
    }


    public void Dispose()
    {
        Pipe?.Dispose();
        if (Db != null)
        {
            if (Db.ChangeTracker.HasChanges())
                Db.SaveChanges();
            Db.Dispose();
        }
    }
}