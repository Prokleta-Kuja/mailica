using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace authica
{
    public class Program
    {
        static bool _shouldStart = true;
        static IHost _instance = null!;
        public static async Task Main(string[] args)
        {
            await RegexTest();
            return;
            InitializeDirectories();
            await InitializeDb(args);

            while (_shouldStart)
            {
                _shouldStart = false;
                _instance = CreateHostBuilder(args).Build();
                _instance.Run();
            }
        }
        public static void Shutdown(bool restart = false)
        {
            _shouldStart = restart;
            _instance.StopAsync();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        static void InitializeDirectories()
        {
            // var appdata = new DirectoryInfo(C.Paths.AppData);
            // appdata.Create();
        }

        static async Task InitializeDb(string[] args)
        {
            await Task.CompletedTask;
            // var dbFile = new FileInfo(C.Paths.AppDataFor("app.db"));

            // var opt = new DbContextOptionsBuilder<AppDbContext>();
            // opt.UseSqlite(C.Paths.AppDbConnectionString);

            // var db = new AppDbContext(opt.Options);
            // if (db.Database.GetMigrations().Any())
            //     await db.Database.MigrateAsync();
            // else
            //     await db.Database.EnsureCreatedAsync();

            // // Seed
            // if (!db.Users.Any())
            // {
            //     await db.InitializeDefaults(new PasswordHashingService());
            // }
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