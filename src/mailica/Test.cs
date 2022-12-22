using System.Buffers;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace mailica;

public class Store1 : MessageStore
{
    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();

        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            await stream.WriteAsync(memory, cancellationToken);
        }

        stream.Position = 0;

        var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
        Console.WriteLine($"Store 1 saving :{message.TextBody}");

        return SmtpResponse.Ok;
    }
}
public class Filter1 : IMailboxFilter, IMailboxFilterFactory
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
        // ovaj kurac se poziva koliko god primatelja ima, pa ako otkantaš jednog, otkantao si sve

        // Open Relay - do not accept mails for domain not responsible for
        if (to.Host == "ica.hr")
            return Task.FromResult(MailboxFilterResult.Yes);

        // Permanently - mailbox name not allowed - nije tvoja domena
        // Temporary - mailbox unavailable - nema mailboxa
        return Task.FromResult(MailboxFilterResult.NoTemporarily);
    }

    public IMailboxFilter CreateInstance(ISessionContext context)
    {
        return new Filter1();
    }
}
public class Auth1 : IUserAuthenticator, IUserAuthenticatorFactory
{
    public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken token)
    {
        Console.WriteLine("Auth 1, User={0} Password={1}", user, password);

        return Task.FromResult(user.Length > 4);
    }

    public IUserAuthenticator CreateInstance(ISessionContext context)
    {
        return new Auth1();
    }
}