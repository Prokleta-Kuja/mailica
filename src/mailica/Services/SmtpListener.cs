// using System.IO.Pipelines;
// using System.Net.Security;
// using System.Net.Sockets;
// using System.Security.Authentication;
// using System.Security.Cryptography.X509Certificates;
// using SmtpServer;
// using SmtpServer.IO;
// using SmtpServer.Net;

// namespace mailica;

// public class SmtpListener : IEndpointListener
// {
//     public const string LocalEndPointKey = "EndpointListener:LocalEndPoint";
//     public const string RemoteEndPointKey = "EndpointListener:RemoteEndPoint";

//     readonly IEndpointDefinition _endpointDefinition;
//     readonly TcpListener _tcpListener;
//     readonly Action _disposeAction;

//     internal SmtpListener(IEndpointDefinition endpointDefinition, TcpListener tcpListener, Action disposeAction)
//     {
//         _endpointDefinition = endpointDefinition;
//         _tcpListener = tcpListener;
//         _disposeAction = disposeAction;
//     }

//     public async Task<ISecurableDuplexPipe> GetPipeAsync(ISessionContext context, CancellationToken cancellationToken)
//     {
//         var tcpClient = await _tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
//         cancellationToken.ThrowIfCancellationRequested();

//         context.Properties.Add(LocalEndPointKey, _tcpListener.LocalEndpoint);
//         context.Properties.Add(RemoteEndPointKey, tcpClient.Client.RemoteEndPoint);

//         var stream = tcpClient.GetStream();
//         stream.ReadTimeout = (int)_endpointDefinition.ReadTimeout.TotalMilliseconds;

//         return new SecurableDuplexPipe(stream, () =>
//         {
//             tcpClient.Close();
//             tcpClient.Dispose();
//         });
//     }

//     public void Dispose()
//     {
//         _tcpListener.Stop();
//         _disposeAction();
//     }
// }
// internal sealed class SecurableDuplexPipe : ISecurableDuplexPipe
// {
//     readonly Action _disposeAction;
//     Stream? _stream;
//     bool _disposed;

//     internal SecurableDuplexPipe(Stream stream, Action disposeAction)
//     {
//         _stream = stream;
//         _disposeAction = disposeAction;

//         Input = PipeReader.Create(_stream);
//         Output = PipeWriter.Create(_stream);
//     }

//     public async Task UpgradeAsync(X509Certificate certificate, SslProtocols protocols, CancellationToken cancellationToken = default)
//     {
//         if (_stream == null)
//             return;

//         var stream = new SslStream(_stream, true);

//         await stream.AuthenticateAsServerAsync(certificate, false, protocols, true).ConfigureAwait(false);

//         _stream = stream;

//         Input = PipeReader.Create(_stream);
//         Output = PipeWriter.Create(_stream);
//     }

//     void Dispose(bool disposing)
//     {
//         if (_disposed == false)
//         {
//             if (disposing)
//             {
//                 _disposeAction();
//                 _stream = null;
//             }

//             _disposed = true;
//         }
//     }

//     public void Dispose()
//     {
//         Dispose(true);
//     }

//     public PipeReader Input { get; private set; }
//     public PipeWriter Output { get; private set; }
//     public bool IsSecure => _stream is SslStream;
// }