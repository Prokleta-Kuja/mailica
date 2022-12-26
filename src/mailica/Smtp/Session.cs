using System.IO.Pipelines;
using mailica.Smtp.Commands;

namespace mailica.Smtp;

class Session
{
    readonly StateMachine _stateMachine;
    readonly SessionContext _context;
    readonly CommandFactory _commandFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="context">The session context.</param>
    internal Session(SessionContext context)
    {
        _context = context;
        _stateMachine = new StateMachine(_context);
        _commandFactory = new();
    }

    /// <summary>
    /// Handles the SMTP session.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task which performs the operation.</returns>
    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await OutputGreetingAsync(cancellationToken).ConfigureAwait(false);

        await ExecuteAsync(_context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute the command handler against the specified session context.
    /// </summary>
    /// <param name="context">The session context to execute the command handler against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task which asynchronously performs the execution.</returns>
    async Task ExecuteAsync(SessionContext context, CancellationToken cancellationToken)
    {
        var retries = _context.ServerOptions.MaxRetryCount;

        while (retries-- > 0 && context.IsQuitRequested == false && cancellationToken.IsCancellationRequested == false)
        {
            try
            {
                var command = await ReadCommandAsync(context, cancellationToken).ConfigureAwait(false);

                if (command == null)
                {
                    return;
                }

                if (_stateMachine.TryAccept(command, out var errorResponse) == false)
                {
                    throw new ResponseException(errorResponse!);
                }

                if (await ExecuteAsync(command, context, cancellationToken).ConfigureAwait(false))
                {
                    _stateMachine.Transition(context);
                }

                retries = _context.ServerOptions.MaxRetryCount;
            }
            catch (ResponseException responseException) when (responseException.IsQuitRequested)
            {
                if (context.Pipe != null)
                    await context.Pipe.Output.WriteReplyAsync(responseException.Response, cancellationToken).ConfigureAwait(false);

                context.IsQuitRequested = true;
            }
            catch (ResponseException responseException)
            {
                var response = CreateErrorResponse(responseException.Response, retries);

                if (context.Pipe != null)
                    await context.Pipe.Output.WriteReplyAsync(response, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (context.Pipe != null)
                    await context.Pipe.Output.WriteReplyAsync(new Response(ReplyCode.ServiceClosingTransmissionChannel, "The session has be cancelled."), CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    async ValueTask<Command> ReadCommandAsync(SessionContext context, CancellationToken cancellationToken)
    {
        var timeout = new CancellationTokenSource(context.ServerOptions.CommandWaitTimeout);

        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

        try
        {
            Command? command = null;

            if (context.Pipe != null)
                await context.Pipe.Input.ReadLineAsync(
                    buffer =>
                    {
                        var parser = new Parser(_commandFactory);

                        if (parser.TryMake(ref buffer, out command, out var errorResponse) == false)
                        {
                            throw new ResponseException(errorResponse!);
                        }

                        return Task.CompletedTask;
                    },
                    cancellationTokenSource.Token).ConfigureAwait(false);

            return command!;
        }
        catch (OperationCanceledException)
        {
            if (timeout.IsCancellationRequested)
            {
                throw new ResponseException(new Response(ReplyCode.ServiceClosingTransmissionChannel, "Timeout whilst waiting for input."), true);
            }

            throw new ResponseException(new Response(ReplyCode.ServiceClosingTransmissionChannel, "The session has be cancelled."), true);
        }
        finally
        {
            timeout.Dispose();
            cancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Create an error response.
    /// </summary>
    /// <param name="response">The original response to wrap with the error message information.</param>
    /// <param name="retries">The number of retries remaining before the session is terminated.</param>
    /// <returns>The response that wraps the original response with the additional error information.</returns>
    static Response CreateErrorResponse(Response response, int retries)
    {
        return new Response(response.ReplyCode, $"{response.Message}, {retries} retry(ies) remaining.");
    }

    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="context">The execution context to operate on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task which asynchronously performs the execution.</returns>
    static async Task<bool> ExecuteAsync(Command command, SessionContext context, CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(context, cancellationToken);
        return result;
    }

    /// <summary>
    /// Output the greeting.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task which performs the operation.</returns>
    ValueTask<FlushResult> OutputGreetingAsync(CancellationToken cancellationToken)
    {
        _context.Pipe?.Output.WriteLine($"220 {_context.ServerOptions.ServerName} v1.1 ESMTP ready");

        return _context.Pipe!.Output.FlushAsync(cancellationToken);
    }
}