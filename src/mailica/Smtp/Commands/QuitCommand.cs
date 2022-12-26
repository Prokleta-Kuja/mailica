namespace mailica.Smtp.Commands;

public class QuitCommand : Command
{
    public const string Command = "QUIT";

    /// <summary>
    /// Constructor.
    /// </summary>
    public QuitCommand() : base(Command) { }

    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="context">The execution context to operate on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns true if the command executed successfully such that the transition to the next state should occurr, false 
    /// if the current state is to be maintained.</returns>
    internal override async Task<bool> ExecuteAsync(SessionContext context, CancellationToken cancellationToken)
    {
        context.IsQuitRequested = true;

        if (context.Pipe != null)
            await context.Pipe.Output.WriteReplyAsync(Response.ServiceClosingTransmissionChannel, cancellationToken).ConfigureAwait(false);

        return true;
    }
}