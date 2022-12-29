using System.Buffers;
using System.Net;
using mailica.Entities;
using mailica.Services;
using mailica.Smtp.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace mailica.Smtp;

public class SessionContext : IDisposable
{
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
        Transaction = new MessageTransaction();
        Properties = new();

        var dbFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        Db = dbFactory.CreateDbContext();

        var cache = serviceProvider.GetRequiredService<IMemoryCache>();
        Cache = cache;
    }


    /////////////////////////////////////////////////////////

    public async Task<bool> ShouldBlockConnectionFrom(IPEndPoint ip)
    {
        await Task.CompletedTask;
        if (ip.Port == 587) // Outgoing
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
            return IncurInfraction(failedAuthCountKey);

        var dbUser = await Db.Users.FirstOrDefaultAsync(u => u.Name.Equals(user, StringComparison.InvariantCultureIgnoreCase), token);
        if (dbUser == null)
            return IncurInfraction(failedAuthCountKey);

        if (DovecotHasher.Verify(dbUser.Salt, dbUser.Hash, password))
            return IncurInfraction(failedAuthCountKey);

        Cache.Remove(failedAuthCountKey);
        User = dbUser;
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
        // TODO: verify sender
        await Task.CompletedTask;
        return MailboxFilterResult.Yes;
    }
    public async Task<MailboxFilterResult> CanDeliverToAsync(EmailAddress to, EmailAddress @from, CancellationToken cancellationToken = default)
    {
        // TODO: verify recipient, called for every one
        await Task.CompletedTask;
        return MailboxFilterResult.Yes;
    }
    public Task<Response> SaveAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        // TODO: save or send email
        return Task.FromResult(Response.Ok);
    }


    public void Dispose()
    {
        Pipe?.Dispose();
        Db?.Dispose();
        return;
    }
}