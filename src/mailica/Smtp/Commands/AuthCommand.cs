using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;

namespace mailica.Smtp.Commands;

public enum AuthenticationMethod { Login, Plain }

public class AuthCommand : Command
{
    public const string Command = "AUTH";

    string? _user;
    string? _password;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="method">The authentication method.</param>
    /// <param name="parameter">The authentication parameter.</param>
    public AuthCommand(AuthenticationMethod method, string? parameter) : base(Command)
    {
        Method = method;
        Parameter = parameter ?? string.Empty;
    }

    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="context">The execution context to operate on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns true if the command executed successfully such that the transition to the next state should occur, false 
    /// if the current state is to be maintained.</returns>
    internal override async Task<bool> ExecuteAsync(SessionContext context, CancellationToken cancellationToken)
    {
        switch (Method)
        {
            case AuthenticationMethod.Plain:
                if (await TryPlainAsync(context, cancellationToken).ConfigureAwait(false) == false)
                {
                    if (context.Pipe != null)
                        await context.Pipe.Output.WriteReplyAsync(Response.AuthenticationFailed, cancellationToken).ConfigureAwait(false);
                    return false;
                }
                break;

            case AuthenticationMethod.Login:
                if (await TryLoginAsync(context, cancellationToken).ConfigureAwait(false) == false)
                {
                    if (context.Pipe != null)
                        await context.Pipe.Output.WriteReplyAsync(Response.AuthenticationFailed, cancellationToken).ConfigureAwait(false);
                    return false;
                }
                break;
        }

        if (await context.AuthenticateAsync(_user, _password, cancellationToken).ConfigureAwait(false) == false)
        {
            var remaining = context.ServerOptions.MaxAuthenticationAttempts - ++context.AuthenticationAttempts;
            var response = new Response(ReplyCode.AuthenticationFailed, $"authentication failed, {remaining} attempt(s) remaining.");

            if (context.Pipe != null)
                await context.Pipe.Output.WriteReplyAsync(response, cancellationToken).ConfigureAwait(false);

            if (remaining <= 0)
                throw new ResponseException(Response.ServiceClosingTransmissionChannel, true);

            return false;
        }

        if (context.Pipe != null)
            await context.Pipe.Output.WriteReplyAsync(Response.AuthenticationSuccessful, cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Attempt a PLAIN login sequence.
    /// </summary>
    /// <param name="context">The execution context to operate on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>true if the PLAIN login sequence worked, false if not.</returns>
    async Task<bool> TryPlainAsync(SessionContext context, CancellationToken cancellationToken)
    {
        var authentication = Parameter;

        if (string.IsNullOrWhiteSpace(authentication) && context.Pipe != null)
        {
            await context.Pipe.Output.WriteReplyAsync(new Response(ReplyCode.ContinueWithAuth, " "), cancellationToken).ConfigureAwait(false);
            authentication = await context.Pipe.Input.ReadLineAsync(Encoding.ASCII, cancellationToken).ConfigureAwait(false);
        }

        if (TryExtractFromBase64(authentication) == false)
        {
            if (context.Pipe != null)
                await context.Pipe.Output.WriteReplyAsync(Response.AuthenticationFailed, cancellationToken).ConfigureAwait(false);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempt to extract the user name and password combination from a single line base64 encoded string.
    /// </summary>
    /// <param name="base64">The base64 encoded string to extract the user name and password from.</param>
    /// <returns>true if the user name and password were extracted from the base64 encoded string, false if not.</returns>
    bool TryExtractFromBase64(string base64)
    {
        var match = Regex.Match(Encoding.UTF8.GetString(Convert.FromBase64String(base64)), "\x0000(?<user>.*)\x0000(?<password>.*)");

        if (match.Success == false)
        {
            return false;
        }

        _user = match.Groups["user"].Value;
        _password = match.Groups["password"].Value;

        return true;
    }

    /// <summary>
    /// Attempt a LOGIN login sequence.
    /// </summary>
    /// <param name="context">The execution context to operate on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>true if the LOGIN login sequence worked, false if not.</returns>
    async Task<bool> TryLoginAsync(SessionContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Parameter) == false)
        {
            _user = Encoding.UTF8.GetString(Convert.FromBase64String(Parameter));
        }
        else
        {
            if (context.Pipe != null)
                await context.Pipe.Output.WriteReplyAsync(new(ReplyCode.ContinueWithAuth, "VXNlcm5hbWU6"), cancellationToken).ConfigureAwait(false);
            if (context.Pipe != null)
                _user = await ReadBase64EncodedLineAsync(context.Pipe.Input, cancellationToken).ConfigureAwait(false);
        }
        // TODO: WTF is this hardcoded shit
        if (context.Pipe != null)
            await context.Pipe.Output.WriteReplyAsync(new(ReplyCode.ContinueWithAuth, "UGFzc3dvcmQ6"), cancellationToken).ConfigureAwait(false);
        if (context.Pipe != null)
            _password = await ReadBase64EncodedLineAsync(context.Pipe.Input, cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Read a Base64 encoded line.
    /// </summary>
    /// <param name="reader">The pipe to read from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decoded Base64 string.</returns>
    static async Task<string> ReadBase64EncodedLineAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var text = await reader.ReadLineAsync(cancellationToken);

        return text == null ? string.Empty : Encoding.UTF8.GetString(Convert.FromBase64String(text));
    }

    /// <summary>
    /// The authentication method.
    /// </summary>
    public AuthenticationMethod Method { get; }

    /// <summary>
    /// The authentication parameter.
    /// </summary>
    public string Parameter { get; }
}