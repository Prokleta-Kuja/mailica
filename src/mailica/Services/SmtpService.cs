using System.Security.Cryptography.X509Certificates;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Storage;

namespace mailica.Services;

public static class SmtpService
{
    static IServiceProvider? s_provider;
    static SmtpServer.SmtpServer? s_server;
    static CancellationTokenSource s_cts = new();
    static Task? s_serverTask;
    public static void AddSmtp(this IServiceCollection services)
    {
        services.AddTransient<IUserAuthenticator, SmtpAuthService>();
        services.AddTransient<IMailboxFilter, SmtpFilterService>();
        services.AddTransient<IMessageStore, SmtpStoreService>();
    }
    public static void UseSmtp(this WebApplication app)
    {
        s_provider = app.Services;
        Start();

        var appLifetime = s_provider.GetRequiredService<IHostApplicationLifetime>();
        appLifetime.ApplicationStopping.Register(Stop);
    }
    static ISmtpServerOptions BuildOptions()
    {
        var cert = X509Certificate2.CreateFromPemFile(C.Paths.CertCrt, C.Paths.CertKey);
        return new SmtpServerOptionsBuilder()
                .ServerName(C.Hostname)
                .Endpoint(c => c
                    .Port(25)//.IsSecure(false)
                    .AllowUnsecureAuthentication(false)
                    .Certificate(cert)
                )
                .Endpoint(c => c
                    .Port(587)//.IsSecure(true)
                    .AllowUnsecureAuthentication(false)
                    .Certificate(cert)
                )
                .Build();
    }
    static CancellationToken GetToken()
    {
        if (s_cts.IsCancellationRequested)
        {
            s_cts.Dispose();
            s_cts = new();
        }

        return s_cts.Token;
    }
    static void Start()
    {
        if (s_provider == null)
            throw new Exception("SMTP service not initialized via UseSmtp");

        var smtpOpt = BuildOptions();
        var token = GetToken();

        s_server = new SmtpServer.SmtpServer(smtpOpt, s_provider);
        s_serverTask = s_server.StartAsync(token);
    }
    static void Stop()
    {
        Console.WriteLine("SMTP service Stopping...."); // TODO: remove
        s_server?.Shutdown();
        try
        {
            s_server?.ShutdownTask?.Wait();
        }
        catch (AggregateException e)
        {
            e.Handle(exception => exception is OperationCanceledException);
        }
        try
        {
            s_serverTask?.Wait();
        }
        catch (AggregateException e)
        {
            e.Handle(exception => exception is OperationCanceledException);
        }
    }
    static void Restart()
    {
        Stop();
        Start();
    }
}