
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using tester;

FillGmailUser1();
FillGmailCompany();

// using var vanjskiMailbox = new ImapClient();
// using var nutarnjiMailbox = new ImapClient();

// vanjskiMailbox.Connect("dovecot.gunda", 143, false);
// nutarnjiMailbox.Connect("dovecot.gunda", 143, false);

// vanjskiMailbox.Authenticate("user1", "P@ssw0rd");
// nutarnjiMailbox.Authenticate("user2", "P@ssw0rd");

// // The Inbox folder is always available on all IMAP servers...
// var vanjskiInbox = vanjskiMailbox.Inbox;
// var nutarnjiInbox = nutarnjiMailbox.Inbox;

// vanjskiInbox.Open(FolderAccess.ReadWrite);
// nutarnjiInbox.Open(FolderAccess.ReadWrite);

// var query = SearchQuery.DeliveredAfter(DateTime.Parse("2013-01-12"));

// foreach (var uid in vanjskiInbox.Search(query))
// {
//     var message = vanjskiInbox.GetMessage(uid);
//     nutarnjiInbox.Append(message, MessageFlags.None);
//     vanjskiInbox.AddFlags(uid, MessageFlags.Deleted, true);
// }

// vanjskiInbox.Expunge();

// vanjskiMailbox.Disconnect(true);
// nutarnjiMailbox.Disconnect(true);

// Console.WriteLine("Hello, World!");

static void FillGmailUser1()
{
    using var client = GetClient("gmail-user1");
    var messages = new List<MimeMessage>{
        GetMessage(C.BoxGmailUser1),
        GetMessage(C.BoxGmailUser1),
        GetMessage(C.BoxGmailUser1),
        GetMessage(C.BoxGmailUser1),
        GetMessage(C.BoxGmailUser1),
        GetMessage(C.BoxGmailUser1),
    };
    var inbox = client.Inbox;
    foreach (var message in messages)
        inbox.Append(message);
    client.Disconnect(true);
}

static void FillGmailCompany()
{
    using var client = GetClient("gmail-company");
    var messages = new List<MimeMessage>{
        GetMessage(C.BoxSales),
        GetMessage(C.BoxSales,C.BoxSupport),
        GetMessage(C.BoxUser1),
        GetMessage(C.BoxUser2),
        GetMessage(C.BoxSales),
        GetMessage(C.BoxSales,C.BoxSupport),
        GetMessage(C.BoxUser1),
        GetMessage(C.BoxUser2),
    };
    var inbox = client.Inbox;
    foreach (var message in messages)
        inbox.Append(message);
    client.Disconnect(true);
}

static MimeMessage GetMessage(params MailboxAddress[] addresses)
{
    var message = new MimeMessage();
    message.From.Add(C.BoxSender);
    message.To.AddRange(addresses);
    message.Subject = Guid.NewGuid().ToString();
    message.Body = new TextPart("plain") { Text = C.Lipsum };
    return message;
}

static ImapClient GetClient(string username)
{
    var client = new ImapClient();
    client.Connect("dovecot.gunda", 143, false);
    client.Authenticate(username, "P@ssw0rd");
    return client;
}