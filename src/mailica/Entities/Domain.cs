namespace mailica.Entities;

public class Domain
{
    public int DomainId { get; set; }
    public required string Name { get; set; }
    public DateTime? Disabled { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new HashSet<Address>();
    public virtual ICollection<User> Users { get; set; } = new HashSet<User>();
}