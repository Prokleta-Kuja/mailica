using mailica.Entities;

namespace mailica.QueryParams;
public class CredentialCol
{
    public const string IsValid = nameof(Credential.IsValid);
    public const string Host = nameof(Credential.Host);
    public const string Username = nameof(Credential.Username);
    public const string Type = nameof(Credential.Type);
    public const string Disabled = nameof(Credential.Disabled);
}