using System.Security.Cryptography.X509Certificates;

namespace mailica.Smtp;

public class ServerOptions
{
    public ServerOptions(string serverName, X509Certificate2 cert, params int[] ports)
    {
        ServerName = serverName;
        if (!ports.Any())
            ports = new int[] { 25, 587 };

        foreach (var port in ports)
            Endpoints.Add(new EndpointDefinition(port, cert));
    }
    /// <summary>
    /// Gets or sets the maximum size of a message.
    /// </summary>
    public int MaxMessageSize { get; set; }

    /// <summary>
    /// The maximum number of retries before quitting the session.
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// The maximum number of authentication attempts.
    /// </summary>
    public int MaxAuthenticationAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the SMTP server name.
    /// </summary>
    public string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the endpoint to listen on.
    /// </summary>
    internal List<EndpointDefinition> Endpoints { get; set; } = new();

    /// <summary>
    /// The timeout to use when waiting for a command from the client.
    /// </summary>
    public TimeSpan CommandWaitTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The size of the buffer that is read from each call to the underlying network client.
    /// </summary>
    public int NetworkBufferSize { get; set; } = 128;
}