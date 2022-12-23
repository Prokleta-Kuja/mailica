using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using mailica.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        InitializeDirectories();

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
            var cert = X509Certificate2.CreateFromPemFile("../../certs/cert.crt", "../../certs/cert.key"); // Učitaj iz env?
            var smtpOpt = new SmtpServerOptionsBuilder()
                .ServerName("abcd.ica.hr") // Zamijeni sa pravim imenom iz dns-a
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

            builder.Services.AddTransient<IUserAuthenticator, Auth1>();
            builder.Services.AddTransient<IMailboxFilter, Filter1>();
            builder.Services.AddTransient<IMessageStore, Store1>();

            var app = builder.Build();
            await InitializeDb(app.Services);

            var smtpServer = new SmtpServer.SmtpServer(smtpOpt, app.Services);
            _ = smtpServer.StartAsync(CancellationToken.None);

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
    static void InitializeDirectories()
    {
        var appdata = new DirectoryInfo(C.Paths.AppData);
        appdata.Create();
    }

    static async Task InitializeDb(IServiceProvider provider)
    {
        var dbFactory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();

        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        // Seed
        if (C.IsDebug && !db.Domains.Any())
        {
            var dpProvider = provider.GetRequiredService<IDataProtectionProvider>();
            await db.InitializeDefaults(dpProvider);
        }
    }
    static async Task RegexTest()
    {
        await Task.CompletedTask;
        var sw = new Stopwatch();
        var reg2 = new Regex(@"user\..*@te\.st", RegexOptions.IgnoreCase); // 0 or more after dot
        var reg1 = new Regex(@"user\..+@te\.st", RegexOptions.IgnoreCase | RegexOptions.Compiled); // 1 or more after dot
        sw.Start();
        var is0 = reg1.IsMatch("User@te.st");
        var is1 = reg1.IsMatch("user-a@te.st");
        var is2 = reg1.IsMatch("usera@te.st");
        var is3 = reg1.IsMatch("usera@te.sT");
        var is4 = reg1.IsMatch("user.abc@te.st");
        var is5 = reg1.IsMatch("nije@te.st");
        var is6 = reg1.IsMatch("user.abc@te.st");
        var is7 = reg1.IsMatch("user.abc-d@te.st");
        var is8 = reg1.IsMatch("user.abc.e@te.st");
        var is9 = reg1.IsMatch("user.abc+@te.st");
        Console.WriteLine(sw.ElapsedMilliseconds);
        sw.Stop();
    }
}

