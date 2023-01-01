using System.Diagnostics;
using System.Text.RegularExpressions;
using mailica.Entities;
using mailica.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using Serilog.Events;

namespace mailica;

public class Program
{
    public static string CertCrt { get; private set; } = string.Empty;
    public static string CertKey { get; private set; } = string.Empty;

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
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();
            builder.Services.AddSmtp();
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
            builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();
            builder.Services.AddDbContextFactory<AppDbContext>(builder =>
            {
                builder.UseSqlite(C.Paths.AppDbConnectionString);
                if (C.IsDebug)
                {
                    builder.EnableSensitiveDataLogging();
                    builder.LogTo(message => Debug.WriteLine(message), new[] { RelationalEventId.CommandExecuted });
                }
            });

            var app = builder.Build();
            await Initialize(app.Services);

            // app.UseSmtp();
            //////////////
            var cts = new CancellationTokenSource();
            var cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPemFile(C.Paths.CertCrt, C.Paths.CertKey);
            var opt = new Smtp.ServerOptions("abcd.ica.hr", cert);
            var server = new Smtp.Server(opt, app.Services);
            _ = server.StartAsync(cts.Token);
            /////////////
            app.UseForwardedHeaders();
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

    static async Task Initialize(IServiceProvider provider)
    {
        if (C.IsDebug)
        {
            Directory.CreateDirectory(C.Paths.MailData);
            Directory.CreateDirectory(C.Paths.ConfigData);
            Directory.CreateDirectory(C.Paths.CertData);
        }

        if (string.IsNullOrWhiteSpace(C.Hostname))
            throw new Exception("You must specify HOSTNAME environment variable");

        if (string.IsNullOrWhiteSpace(C.MasterUser) || string.IsNullOrWhiteSpace(C.MasterSecret))
            throw new Exception("You must specify MASTER_USER & MASTER_SECRET environment variables");

        if (!File.Exists(C.Paths.CertCrt) || !File.Exists(C.Paths.CertKey))
            throw new Exception($"Could not load certs from {C.Paths.CertData}");

        if (!Directory.GetFiles(C.Paths.ConfigData).Any())
            DovecotConfiguration.Initial();

        var dbFactory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();

        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        // Demo data
        if (C.IsDebug && !db.Domains.Any())
        {
            var dpProvider = provider.GetRequiredService<IDataProtectionProvider>();
            await db.InitializeDefaults(dpProvider);
        }
    }
}

