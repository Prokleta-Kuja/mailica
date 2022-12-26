namespace mailica.Smtp;

public abstract class PipelineException : Exception { }
public sealed class PipelineCancelledException : PipelineException { }
public sealed class ResponseException : Exception
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="response">The response to raise in the exception.</param>
    public ResponseException(Response response) : this(response, false) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="response">The response to raise in the exception.</param>
    /// <param name="quit">Indicates whether or not the session should terminate.</param>
    public ResponseException(Response response, bool quit)
    {
        Response = response;
        IsQuitRequested = quit;
    }

    /// <summary>
    /// The response to return to the client.
    /// </summary>
    public Response Response { get; }

    /// <summary>
    /// Indicates whether or not the session should terminate.
    /// </summary>
    public bool IsQuitRequested { get; }
}