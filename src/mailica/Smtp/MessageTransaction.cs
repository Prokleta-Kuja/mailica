using System.Collections.ObjectModel;

namespace mailica.Smtp;

public class MessageTransaction
{
    /// <summary>
    /// Reset the current transaction.
    /// </summary>
    public void Reset()
    {
        From = null;
        To = new();
        Parameters = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    }

    public bool Outgoing { get; set; }

    /// <summary>
    /// Gets or sets the mailbox that is sending the message.
    /// </summary>
    public EmailAddress? From { get; set; }

    /// <summary>
    /// Gets or sets the collection of mailboxes that the message is to be delivered to.
    /// </summary>
    public List<EmailAddress> To { get; set; } = new();

    /// <summary>
    /// The list of parameters that were supplied by the client.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; set; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}