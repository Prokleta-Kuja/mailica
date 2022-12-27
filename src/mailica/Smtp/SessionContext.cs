using System.Buffers;
using System.Net;
using mailica.Entities;
using mailica.Smtp.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace mailica.Smtp;

public class SessionContext : IDisposable
{
    public SessionContext(IServiceProvider serviceProvider, ServerOptions options, EndpointDefinition endpointDefinition)
    {
        ServiceProvider = serviceProvider;
        ServerOptions = options;
        EndpointDefinition = endpointDefinition;
        Transaction = new MessageTransaction();
        Properties = new();

        var dbFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        Db = dbFactory.CreateDbContext();
    }

    public IServiceProvider ServiceProvider { get; }
    public AppDbContext Db { get; set; }
    public ServerOptions ServerOptions { get; }
    public EndpointDefinition EndpointDefinition { get; }
    public SecurableDuplexPipe? Pipe { get; set; }
    public MessageTransaction Transaction { get; }
    public User? User { get; private set; }
    public bool IsAuthenticated => User != null;
    public int AuthenticationAttempts { get; set; }
    public bool IsQuitRequested { get; set; }
    public Dictionary<string, object> Properties { get; }

    /////////////////////////////////////////////////////////

    public async Task<bool> CanReceiveFromIp(IPEndPoint ip)
    {
        // TODO: implement
        // if port 25 then incoming if 587 then outgoing
        return await Task.FromResult(false);
    }
    public Task<bool> AuthenticateAsync(string? user, string? password, CancellationToken token)
    {
        Console.WriteLine("Auth, User={0} Password={1}", user, password);
        // TODO: set User 
        return Task.FromResult(true);
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