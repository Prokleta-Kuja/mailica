using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace mailica.Smtp;

public class EndpointDefinition
{
    public EndpointDefinition(int port, X509Certificate2 cert)
    {
        Endpoint = new IPEndPoint(IPAddress.Any, port);
        ServerCertificate = cert;
        ReadTimeout = TimeSpan.FromMinutes(2);
        SupportedSslProtocols = SslProtocols.Tls12;
    }
    /// <summary>
    /// The IP endpoint to listen on.
    /// </summary>
    public IPEndPoint Endpoint { get; set; }

    /// <summary>
    /// Indicates whether the endpoint is secure by default.
    /// </summary>
    public bool IsSecure { get; set; }

    /// <summary>
    /// Gets a value indicating whether the client must authenticate in order to proceed.
    /// </summary>
    public bool AuthenticationRequired { get; set; }

    /// <summary>
    /// Gets a value indicating whether authentication should be allowed on an unsecure session.
    /// </summary>
    public bool AllowUnsecureAuthentication { get; set; }

    /// <summary>
    /// The timeout on each individual buffer read.
    /// </summary>
    public TimeSpan ReadTimeout { get; set; }

    /// <summary>
    /// Gets the Server Certificate to use when starting a TLS session.
    /// </summary>
    public X509Certificate ServerCertificate { get; set; }

    /// <summary>
    /// The supported SSL protocols.
    /// </summary>
    public SslProtocols SupportedSslProtocols { get; set; }
}