using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mailica.Entities;
using mailica.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace mailica
{
    public class Program
    {
        static bool _shouldStart = true;
        static IHost _instance = null!;
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
                InitializeDirectories();

                while (_shouldStart)
                {
                    _shouldStart = false;
                    _instance = CreateHostBuilder(args).Build();
                    await InitializeDb();
                    _instance.Run();
                }

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
        public static void Shutdown(bool restart = false)
        {
            _shouldStart = restart;
            _instance.StopAsync();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        static void InitializeDirectories()
        {
            var appdata = new DirectoryInfo(C.Paths.AppData);
            appdata.Create();
        }

        static async Task InitializeDb()
        {
            var dbFactory = _instance.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = dbFactory.CreateDbContext();

            if (db.Database.GetMigrations().Any())
                await db.Database.MigrateAsync();
            else
                await db.Database.EnsureCreatedAsync();

            // Seed
            if (!db.Credentials.Any())
            {
                var hasher = _instance.Services.GetRequiredService<IPasswordHasher>();
                var dpProvider = _instance.Services.GetRequiredService<IDataProtectionProvider>();
                await db.InitializeDefaults(hasher, dpProvider);
            }
        }
        static async Task RegexTest()
        {
            await Task.CompletedTask;
            var sw = new Stopwatch();
            var reg2 = new Regex(@"user\..*@te\.st", RegexOptions.IgnoreCase); // 0 or more
            var reg1 = new Regex(@"user\..+@te\.st", RegexOptions.IgnoreCase | RegexOptions.Compiled); // 1 or more
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
}