namespace mailica.Smtp;

public class ServerOptions
{
    public ServerOptions(string serverName, params EndpointDefinition[] endpoints)
    {
        ServerName = serverName;
        Endpoints = endpoints.ToList();
    }
    /// <summary>
    /// Gets or sets the maximum size of a message.
    /// </summary>
    public int MaxMessageSize { get; set; }

    /// <summary>
    /// The maximum number of retries before quitting the session.
    /// </summary>
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// The maximum number of authentication attempts.
    /// </summary>
    public int MaxAuthenticationAttempts { get; set; }

    /// <summary>
    /// Gets or sets the SMTP server name.
    /// </summary>
    public string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the endpoint to listen on.
    /// </summary>
    internal List<EndpointDefinition> Endpoints { get; set; }

    /// <summary>
    /// The timeout to use when waiting for a command from the client.
    /// </summary>
    public TimeSpan CommandWaitTimeout { get; set; }

    /// <summary>
    /// The size of the buffer that is read from each call to the underlying network client.
    /// </summary>
    public int NetworkBufferSize { get; set; }
}