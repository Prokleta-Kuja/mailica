namespace mailica.Entities;

public class Server
{
    public int ServerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Host { get; set; }
    public int Port { get; set; }
    public bool IsSecure { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}