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
}