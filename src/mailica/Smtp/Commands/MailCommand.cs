namespace mailica.Smtp.Commands;
public enum MailboxFilterResult { Yes = 0, NoTemporarily = 1, NoPermanently = 2, SizeLimitExceeded = 3 }
public class MailCommand : Command
{
    public const string Command = "MAIL";

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="parameters">The list of extended (ESMTP) parameters.</param>
    public MailCommand(EmailAddress address, IReadOnlyDictionary<string, string> parameters) : base(Command)
    {
        Address = address;
        Parameters = parameters;
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
        if (context.EndpointDefinition.AuthenticationRequired && context.IsAuthenticated == false)
        {
            if (context.Pipe != null)
                await context.Pipe.Output.WriteReplyAsync(Response.AuthenticationRequired, cancellationToken).ConfigureAwait(false);
            return false;
        }

        context.Transaction.Reset();
        context.Transaction.Parameters = Parameters;

        // check if a size has been defined
        var size = GetMessageSize();

        // check against the server supplied maximum
        if (context.ServerOptions.MaxMessageSize > 0 && size > context.ServerOptions.MaxMessageSize)
        {
            if (context.Pipe != null)
                await context.Pipe.Output.WriteReplyAsync(Response.SizeLimitExceeded, cancellationToken).ConfigureAwait(false);
            return false;
        }
        if (context.Pipe != null)
            switch (await context.CanAcceptFromAsync(Address, size, cancellationToken).ConfigureAwait(false))
            {
                case MailboxFilterResult.Yes:
                    context.Transaction.From = Address;
                    await context.Pipe.Output.WriteReplyAsync(Response.Ok, cancellationToken).ConfigureAwait(false);
                    return true;

                case MailboxFilterResult.NoTemporarily:
                    await context.Pipe.Output.WriteReplyAsync(Response.MailboxUnavailable, cancellationToken).ConfigureAwait(false);
                    return false;

                case MailboxFilterResult.NoPermanently:
                    await context.Pipe.Output.WriteReplyAsync(Response.MailboxNameNotAllowed, cancellationToken).ConfigureAwait(false);
                    return false;

                case MailboxFilterResult.SizeLimitExceeded:
                    await context.Pipe.Output.WriteReplyAsync(Response.SizeLimitExceeded, cancellationToken).ConfigureAwait(false);
                    return false;
            }

        throw new ResponseException(Response.TransactionFailed);
    }

    /// <summary>
    /// Gets the estimated message size supplied from the ESMTP command extension.
    /// </summary>
    /// <returns>The estimated message size that was supplied by the client.</returns>
    int GetMessageSize()
    {
        if (Parameters.TryGetValue("SIZE", out var value) == false)
        {
            return 0;
        }

        return int.TryParse(value, out var size) == false ? 0 : size;
    }

    /// <summary>
    /// Gets the address that the mail is from.
    /// </summary>
    public EmailAddress Address { get; }

    /// <summary>
    /// The list of extended mail parameters.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }
}