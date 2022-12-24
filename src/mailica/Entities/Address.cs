namespace mailica.Entities;

public class Address
{
    public int AddressId { get; set; }
    public int DomainId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsStatic { get; set; }
    public DateTime? Disabled { get; set; }

    public Domain? Domain { get; set; }
    public virtual List<User> Users { get; set; } = new();
}