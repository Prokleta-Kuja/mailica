using System.Diagnostics;
using mailica.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace mailica.Entities;

public partial class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Address>(e =>
        {
            e.HasKey(e => e.AddressId);
            e.HasOne(e => e.Domain).WithMany(e => e.Addresses).HasForeignKey(e => e.DomainId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(e => e.Name).IsUnique();
        });

        builder.Entity<Domain>(e =>
        {
            e.HasKey(e => e.DomainId);
            e.HasIndex(e => e.Name);
        });

        builder.Entity<Server>(e =>
        {
            e.HasKey(e => e.ServerId);
        });

        builder.Entity<User>(e =>
        {
            e.HasKey(e => e.UserId);
            e.HasMany(e => e.Domains).WithMany(e => e.Users);
        });

        // SQLite conversions
        if (Database.IsSqlite())
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
    public async ValueTask InitializeDefaults(IDataProtectionProvider dpProvider)
    {
        if (!Debugger.IsAttached)
            return;

        var serverProtector = dpProvider.CreateProtector(nameof(Server));
        Servers.Add(new Server
        {
            Name = "Home",
            Description = "Test",
            Host = "abcd.ica.hr",
            Port = 587,
            IsSecure = true,
            Username = "home",
            Password = serverProtector.Protect("P@ssw0rd"),
        });

        var master = DovecotHasher.Hash("master");
        var masterUser = new User { Name = "master", IsMaster = true, Description = "Master user", Salt = master.Salt, Hash = master.Hash, Password = DovecotHasher.Password(master.Salt, master.Hash), };
        Users.Add(masterUser);

        var slave = DovecotHasher.Hash("slave");
        var slaveUser = new User { Name = "slave", Quota = 1024, Description = "Slave user", Salt = slave.Salt, Hash = slave.Hash, Password = DovecotHasher.Password(slave.Salt, slave.Hash), };
        Users.Add(slaveUser);

        var ica = new Domain { Name = "ica.hr", };
        ica.Users.Add(masterUser);
        ica.Users.Add(slaveUser);
        Domains.Add(ica);

        var mailMaster = new Address { Name = "master", Description = "Master main", Domain = ica, IsStatic = true, };
        var mailSlave = new Address { Name = "slave", Description = "Slave main", Domain = ica, IsStatic = true, };
        var mailSales = new Address { Name = @"slave\..+@te\.st", Description = "Slave wildcard after", Domain = ica, };
        Addresses.AddRange(mailMaster, mailSlave, mailSales);

        await SaveChangesAsync();
    }
}