using mailica.Entities;

namespace mailica.Sync;

public class SyncDestination
{
    public SyncDestination(Credential credential, string? folder)
    {
        Credential = credential;
        Folder = folder;
    }

    public Credential Credential { get; set; }
    public string? Folder { get; set; }

    public override bool Equals(object? obj)
    {
        return obj != null && obj is SyncDestination destination && destination.Credential.CredentialId == Credential.CredentialId;
    }

    public override int GetHashCode()
    {
        return Credential.CredentialId.GetHashCode();
    }
}