using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using Serilog.Events;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Storage;

namespace mailica;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(C.IsDebug ? LogEventLevel.Debug : LogEventLevel.Information)
                .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

        try
        {
            // var keyBytes = File.ReadAllBytes("cert.key");
            // var keyString = File.ReadAllText("cert.key");
            // using var privateKey = ECDsa.Create();
            // // privateKey.ImportECPrivateKey(keyBytes.AsSpan(), out _);
            // privateKey.ImportFromPem(keyString);
            var cert = X509Certificate2.CreateFromPemFile("cert.crt", "cert.key");
            var smtpOpt = new SmtpServerOptionsBuilder()
                .ServerName("gund.ica.hr") // Zamijeni sa pravim imenom iz dns-a
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

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            builder.Services.AddTransient<IUserAuthenticator, Auth1>();
            builder.Services.AddTransient<IMailboxFilter, Filter1>();
            builder.Services.AddTransient<IMessageStore, Store1>();

            var app = builder.Build();

            var smtpServer = new SmtpServer.SmtpServer(smtpOpt, app.Services);
            _ = smtpServer.StartAsync(CancellationToken.None);

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }

    }
}

