using System;
using System.Linq;
using System.Threading.Tasks;
using mailica.Entities;
using mailica.Services;
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
                e.HasMany(p => p.OutgoingRules).WithOne(p => p.Credential).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<IncomingRule>(e =>
            {
                e.HasKey(p => p.IncomingRuleId);
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
        public async ValueTask InitializeDefaults(IPasswordHasher hasher)
        {
            // await SaveChangesAsync();
            await Task.CompletedTask;
        }
    }
}