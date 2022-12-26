namespace mailica.Smtp.Commands;

public class EhloCommand : Command
{
    public const string Command = "EHLO";

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="domainOrAddress">The domain name or address literal.</param>
    public EhloCommand(string domainOrAddress) : base(Command)
    {
        DomainOrAddress = domainOrAddress;
    }

    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="context">The execution context to operate on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns true if the command executed successfully such that the transition to the next state should occurr, false 
    /// if the current state is to be maintained.</returns>
    internal override async Task<bool> ExecuteAsync(SessionContext context, CancellationToken cancellationToken)
    {
        if (context.Pipe == null)
            return false;

        var output = new[] { GetGreeting(context) }.Union(GetExtensions(context)).ToArray();

        for (var i = 0; i < output.Length - 1; i++)
        {
            context.Pipe.Output.WriteLine($"250-{output[i]}");
        }

        context.Pipe.Output.WriteLine($"250 {output[^1]}");

        await context.Pipe.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Returns the greeting to display to the remote host.
    /// </summary>
    /// <param name="context">The session context.</param>
    /// <returns>The greeting text to display to the remote host.</returns>
    protected virtual string GetGreeting(SessionContext context)
    {
        return $"{context.ServerOptions.ServerName} Hello {DomainOrAddress}, haven't we met before?";
    }

    /// <summary>
    /// Returns the list of extensions that are current for the context.
    /// </summary>
    /// <param name="context">The session context.</param>
    /// <returns>The list of extensions that are current for the context.</returns>
    protected virtual IEnumerable<string> GetExtensions(SessionContext context)
    {
        yield return "PIPELINING";
        yield return "8BITMIME";
        yield return "SMTPUTF8";

        if (context.Pipe?.IsSecure == false && context.EndpointDefinition.ServerCertificate != null)
        {
            yield return "STARTTLS";
        }

        if (context.ServerOptions.MaxMessageSize > 0)
        {
            yield return $"SIZE {context.ServerOptions.MaxMessageSize}";
        }

        if (IsPlainLoginAllowed(context))
        {
            yield return "AUTH PLAIN LOGIN";
        }

        static bool IsPlainLoginAllowed(SessionContext context)
        {
            if (context.Pipe == null)
                return false;

            return context.Pipe.IsSecure || context.EndpointDefinition.AllowUnsecureAuthentication;
        }
    }

    /// <summary>
    /// Gets the domain name or address literal.
    /// </summary>
    public string DomainOrAddress { get; }
}