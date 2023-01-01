using System.Diagnostics;

namespace mailica.Entities;

[DebuggerDisplay("Id={AddressId} Pattern={Pattern}")]
public class Address
{
    /*
        Prefix ^user\..*   0 or more after dot (dot excaped \.)
        Prefix ^user\..+   1 or more after dot (dot excaped \.)
        Suffix .*\.slave$  0 or more before dot (dot excaped \.)
        Suffix .+\.slave$  1 or more before dot (dot excaped \.)
        Exact ^user$
    */
    public int AddressId { get; set; }
    public int DomainId { get; set; }
    public required string Pattern { get; set; }
    public string? Description { get; set; }
    public bool IsStatic { get; set; }
    public DateTime? Disabled { get; set; }

    public Domain? Domain { get; set; }
    public virtual List<User> Users { get; set; } = new();
}