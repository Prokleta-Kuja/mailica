using System.Collections.Generic;
using System.Linq;
using mailica.Entities;
using mailica.Enums;

namespace mailica.Models;

public class CredentialEditModel
{
    public CredentialEditModel(Credential c)
    {
        Type = c.Type;
        Host = c.Host;
        Port = c.Port;
        Username = c.Username;
        Disabled = c.Disabled.HasValue;
    }

    public CredentialType Type { get; set; } = CredentialType.Imap;
    public string? Host { get; set; }
    public int? Port { get; set; } = 143;
    public string? Username { get; set; }
    public string? NewPassword { get; set; }
    public bool Disabled { get; set; }
    public Dictionary<string, string>? Validate(HashSet<string> usernames)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(Host))
            errors.Add(nameof(Host), "Required");

        if (!Port.HasValue)
            errors.Add(nameof(Port), "Required");
        else if (Port <= 0)
            errors.Add(nameof(Port), "Must be greater than 0");
        else if (Port > 65_535)
            errors.Add(nameof(Port), "Must be less than or equal to 65535");

        if (string.IsNullOrWhiteSpace(Username))
            errors.Add(nameof(Username), "Required");
        else if (usernames.Contains(Username))
            errors.Add(nameof(Username), "Already exists");

        return errors.Any() ? errors : null;
    }
}