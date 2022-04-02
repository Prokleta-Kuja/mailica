using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mailica.Entities;
using mailica.Enums;
using mailica.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace authica.Entities
{
    public partial class AppDbContext : DbContext, IDataProtectionKeyContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Credential> Credentials { get; set; } = null!;
        public DbSet<IncomingRule> IncomingRules { get; set; } = null!;
        public DbSet<IncomingRuleCredential> IncomingRuleCredentials { get; set; } = null!;
        public DbSet<Job> Jobs { get; set; } = null!;
        public DbSet<OutgoingRule> OutgoingRules { get; set; } = null!;
        public DbSet<OutgoingRuleAuthorization> OutgoingRuleAuthorizations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Account>(e =>
            {
                e.HasKey(p => p.AccountId);
                e.HasMany(p => p.Authorizations).WithOne(p => p.Account!).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Credential>(e =>
            {
                e.HasKey(p => p.CredentialId);
                e.HasOne(p => p.Job).WithOne(p => p.Credential).HasForeignKey<Job>(p => p.CredentialId);
                e.HasMany(p => p.CatchAllJobs).WithOne(p => p.CatchAllCredential).OnDelete(DeleteBehavior.SetNull);
                e.HasMany(p => p.IncomingRuleCredentials).WithOne(p => p.Credential).OnDelete(DeleteBehavior.Cascade);
                e.HasMany(p => p.OutgoingRules).WithOne(p => p.Credential).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<IncomingRule>(e =>
            {
                e.HasKey(p => p.IncomingRuleId);
                e.HasMany(p => p.IncomingRuleCredentials).WithOne(p => p.IncomingRule).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<IncomingRuleCredential>(e =>
            {
                e.HasKey(p => new { p.IncomingRuleId, p.CredentialId });
            });

            builder.Entity<Job>(e =>
            {
                e.HasKey(p => p.JobId);
                e.HasMany(p => p.IncomingRules).WithOne(p => p.Job).HasForeignKey(p => p.JobId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<OutgoingRule>(e =>
            {
                e.HasKey(p => p.OutgoingRuleId);
                e.HasMany(p => p.Authorizations).WithOne(p => p.OutgoingRule).HasForeignKey(p => p.OutgoingRuleId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<OutgoingRuleAuthorization>(e =>
            {
                e.HasKey(p => new { p.OutgoingRuleId, p.AccountId });
            });

            // SQLite conversions
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var dtProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

                foreach (var property in dtProperties)
                    builder.Entity(entityType.Name).Property(property.Name).HasConversion(new DateTimeToBinaryConverter());

                var decProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));

                foreach (var property in decProperties)
                    builder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();

                var spanProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(TimeSpan) || p.PropertyType == typeof(TimeSpan?));

                foreach (var property in spanProperties)
                    builder.Entity(entityType.Name).Property(property.Name).HasConversion<long>();
            }
        }
        public async ValueTask InitializeDefaults(IPasswordHasher hasher, IDataProtectionProvider dpProvider)
        {
            if (!Debugger.IsAttached)
                return;

            var debugPass = "P@ssw0rd";
            var debugHost = "dovecot.gunda";
            var debugPort = 143;
            var protector = dpProvider.CreateProtector(nameof(Credential));

            var account1 = new Account("user1");
            account1.SetPassword(debugPass, hasher);
            var account2 = new Account("user2");
            account2.SetPassword(debugPass, hasher);
            Accounts.AddRange(account1, account2);

            var gmailUser1 = new Credential(CredentialType.Imap, debugHost, debugPort, "gmail-user1");
            gmailUser1.Password = protector.Protect(debugPass);
            var gmailCompany = new Credential(CredentialType.Imap, debugHost, debugPort, "gmail-company");
            gmailCompany.Password = protector.Protect(debugPass);
            var user1 = new Credential(CredentialType.Imap, debugHost, debugPort, "user1");
            user1.Password = protector.Protect(debugPass);
            var user2 = new Credential(CredentialType.Imap, debugHost, debugPort, "user2");
            user2.Password = protector.Protect(debugPass);
            Credentials.AddRange(gmailUser1, gmailCompany, user1, user2);

            await SaveChangesAsync();

            var personalJob = new Job(gmailUser1.CredentialId, false, TimeSpan.FromMinutes(1));
            personalJob.CatchAllCredentialId = user1.CredentialId; // All mails received on gmail-user1 will be sent to internal user 1
            var companyJob = new Job(gmailCompany.CredentialId, false, TimeSpan.FromMinutes(1));
            Jobs.AddRange(personalJob, companyJob);

            await SaveChangesAsync();

            var incomingRuleSales = new IncomingRule(companyJob.JobId, Regex.Escape("sales@te.st"));
            incomingRuleSales.IncomingRuleCredentials.Add(new() { Credential = user1 });
            incomingRuleSales.IncomingRuleCredentials.Add(new() { Credential = user2 });
            var incomingRuleSupport = new IncomingRule(companyJob.JobId, Regex.Escape("support@te.st"));
            incomingRuleSupport.IncomingRuleCredentials.Add(new() { Credential = user2 });
            var incomingRuleUser1 = new IncomingRule(companyJob.JobId, Regex.Escape("user1@te.st"));
            incomingRuleUser1.IncomingRuleCredentials.Add(new() { Credential = user1 });
            var incomingRuleUser2 = new IncomingRule(companyJob.JobId, Regex.Escape("user2@te.st"));
            incomingRuleUser2.IncomingRuleCredentials.Add(new() { Credential = user2 });
            IncomingRules.AddRange(incomingRuleSales, incomingRuleSupport, incomingRuleUser1, incomingRuleUser2);

            await SaveChangesAsync();
        }
    }
}