namespace mailica.Entities;

public class User
{
    public int UserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsMaster { get; set; }
    public int? Quota { get; set; }
    public required string Salt { get; set; }
    public required string Hash { get; set; }
    public required string Password { get; set; }
    public DateTime? Disabled { get; set; }

    public virtual List<Domain> Domains { get; set; } = new();
    public virtual List<Address> Addresses { get; set; } = new();
}