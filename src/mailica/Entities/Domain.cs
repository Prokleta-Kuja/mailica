using System.Diagnostics;

namespace mailica.Entities;

[DebuggerDisplay("{Name}")]
public class Domain
{
    public int DomainId { get; set; }
    public required string Name { get; set; }
    public required string Host { get; set; }
    public int Port { get; set; }
    public bool IsSecure { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public DateTime? Disabled { get; set; }

    public virtual List<Address> Addresses { get; set; } = new();
    public virtual List<User> Users { get; set; } = new();
}