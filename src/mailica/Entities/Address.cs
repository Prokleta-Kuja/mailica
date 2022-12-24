using System.Diagnostics;

namespace mailica.Entities;

[DebuggerDisplay("Id={AddressId} Pattern={Pattern}")]
public class Address
{
    public int AddressId { get; set; }
    public int DomainId { get; set; }
    public required string Pattern { get; set; }
    public string? Description { get; set; }
    public bool IsStatic { get; set; }
    public DateTime? Disabled { get; set; }

    public Domain? Domain { get; set; }
    public virtual List<User> Users { get; set; } = new();
}