using System.Collections.ObjectModel;
using mailica.Entities;

namespace mailica.Smtp;

public class MessageTransaction
{
    public MessageTransaction(bool outgoing)
    {
        Outgoing = outgoing;
    }
    public void Reset()
    {
        From = null;
        FromUser = null;
        To = new();
        ToUsers.Clear();
        Parameters = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    }

    public bool Outgoing { get; set; }
    public EmailAddress? From { get; set; }
    public User? FromUser { get; set; }
    public List<EmailAddress> To { get; set; } = new();
    public Dictionary<int, User> ToUsers { get; set; } = new();
    public IReadOnlyDictionary<string, string> Parameters { get; set; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}