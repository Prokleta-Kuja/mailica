using System.Buffers;
using mailica.Entities;
using mailica.Smtp.Commands;

namespace mailica.Smtp;

public class SessionContext : IDisposable
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="serviceProvider">The service provider instance.</param>
    /// <param name="options">The server options.</param>
    /// <param name="endpointDefinition">The endpoint definition.</param>
    public SessionContext(IServiceProvider serviceProvider, ServerOptions options, EndpointDefinition endpointDefinition)
    {
        ServiceProvider = serviceProvider;
        ServerOptions = options;
        EndpointDefinition = endpointDefinition;
        Transaction = new MessageTransaction();
        Properties = new();
    }

    /// <summary>
    /// The service provider instance. 
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the options that the server was created with.
    /// </summary>
    public ServerOptions ServerOptions { get; }

    /// <summary>
    /// Gets the endpoint definition.
    /// </summary>
    public EndpointDefinition EndpointDefinition { get; }

    /// <summary>
    /// Gets the pipeline to read from and write to.
    /// </summary>
    public SecurableDuplexPipe? Pipe { get; set; }

    /// <summary>
    /// Gets the current transaction.
    /// </summary>
    public MessageTransaction Transaction { get; }

    /// <summary>
    /// The user that was authenticated.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Returns a value indicating whether or nor the current session is authenticated.
    /// </summary>
    public bool IsAuthenticated => User != null;

    /// <summary>
    /// Returns the number of athentication attempts.
    /// </summary>
    public int AuthenticationAttempts { get; set; }

    /// <summary>
    /// Gets a value indicating whether a quit has been requested.
    /// </summary>
    public bool IsQuitRequested { get; set; }

    /// <summary>
    /// Returns a set of propeties for the current session.
    /// </summary>
    public Dictionary<string, object> Properties { get; }

    /////////////////////////////////////////////////////////

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
        // TODO: dispose dbcontext here
        return;
    }
}