using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

namespace mailica.Services;

public class SmtpFilterService : IMailboxFilter
{
    public Task<MailboxFilterResult> CanAcceptFromAsync(ISessionContext context, IMailbox @from, int size, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Filter 1 CanAcceptFromAsync");
        if (String.Equals(@from.Host, "test.com"))
        {
            return Task.FromResult(MailboxFilterResult.NoPermanently);
        }
        return Task.FromResult(MailboxFilterResult.Yes);

    }

    public Task<MailboxFilterResult> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox @from, CancellationToken token)
    {
        // ovaj kurac se poziva koliko god primatelja ima, pa ako otkantaš jednog, otkantao si sve? možda samo mimekit

        // Open Relay - do not accept mails for domain not responsible for
        if (to.Host == "ica.hr")
            return Task.FromResult(MailboxFilterResult.Yes);

        // Permanently - mailbox name not allowed - nije tvoja domena
        // Temporary - mailbox unavailable - nema mailboxa
        return Task.FromResult(MailboxFilterResult.NoTemporarily);
    }
}