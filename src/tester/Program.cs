
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MimeKit;
using tester;

// var spfValidator = new ARSoft.Tools.Net.Spf.SpfValidator();

// var mailIpAddress = IPAddress.Parse("176.222.33.4");
// var domain = "zagreb.hr";
// var senderAddress = "sender@zagreb.hr";

// var result = spfValidator.CheckHost(mailIpAddress, domain, senderAddress);
// var r = result.Result;
// System.Console.WriteLine("ok");

// var x = PasswordHasher.Hash("pass");
// System.Console.WriteLine(PasswordHasher.DovecotHash(x.salt, x.hash));
// System.Console.WriteLine(PasswordHasher.Verify(x.salt, x.hash, "pass"));

// var keyLength = 20;
// var saltLength = 16;
// var rounds = 5000;
// var pass = "pass";
// var saltChars = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Select(c => Convert.ToByte(c)).ToArray();
// var salt = new byte[saltLength];
// for (int i = 0; i < saltLength; i++)
//      salt[i] = saltChars[RandomNumberGenerator.GetInt32(saltLength)];
//     //salt[i] = Convert.ToByte("A006C816.266807B"[i]);
// var key = KeyDerivation.Pbkdf2(
//     password: pass,
//     salt: salt,
//     prf: KeyDerivationPrf.HMACSHA1,
//     iterationCount: rounds,
//     numBytesRequested: keyLength);

// Console.WriteLine($"{{PBKDF2}}$1${Encoding.UTF8.GetString(salt)}${rounds}${Convert.ToHexString(key).ToLower()}");



var sw = new Stopwatch();
sw.Start();
var mail = GetMessage(new MailboxAddress("kita", "kita@ica.hr"), new MailboxAddress("kita2", "kita2@ica.hr"));
mail.From.Clear();
mail.From.Add(new MailboxAddress("Kurac", "slave.1234@ica.hr"));
var client = new SmtpClient();
client.Connect("abcd.ica.hr", 587);
client.Authenticate("slave", "slave");
client.Send(mail);
client.Disconnect(true);
sw.Stop();
System.Console.WriteLine(sw.Elapsed);


// var client = new ImapClient();
// client.Connect("dovecot.gunda", 143, false);
// client.Authenticate("gmail-user1", "P@ssw0rd");

// var folders = client.GetFolders(client.PersonalNamespaces[0]);

// var path = ".Pero1.Zdero2"; // Store folder separator first, so you know what it is
// var actual = path[1..]; // remove first char - same as Substring(1)
// var separator = path[0];

// IMailFolder? folder;
// try
// {
//     folder = client.GetFolder(actual);
// }
// catch (FolderNotFoundException)
// {
//     var currentDir = client.GetFolder(client.PersonalNamespaces[0]);
//     var parts = actual.Split(separator);
//     for (int i = 0; i < parts.Length; i++)
//     {
//         currentDir = currentDir.Create(parts[i], true);
//         currentDir.Subscribe();
//     }

//     folder = currentDir;
// }


// Console.WriteLine(folder.FullName);
/////////////////////////

// var runner1 = new Runner();
// var runner2 = new Runner();

// Task.Run(() => runner1.Run("gmail-user1"));
// Task.Run(() => runner2.Run("user2"));

// await Task.Delay(TimeSpan.FromSeconds(25));

// runner1.Stop();
// runner2.Stop();


// FillGmailUser1();
// FillGmailCompany();

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



// var client = new ImapClient();
// client.Connect("dovecot.gunda", 143, false);
// client.Authenticate("gmail-user1", "P@ssw0rd");

// var cts = new CancellationTokenSource(TimeSpan.FromMinutes(9));
// client.Inbox.Open(FolderAccess.ReadOnly);
// client.Inbox.CountChanged += Count;
// await client.IdleAsync(cts.Token);

// void Count(object? sender, EventArgs e)
// {
//     if (sender is not IImapFolder folder)
//         return;

//     var lastChange = DateTime.UtcNow;
//     System.Console.WriteLine($"{lastChange:hh:mm:ss} -> {folder.Count}");
// }

static void FillGmailUser1()
{
    using var client = GetClient("gmail-user1");
    var messages = new List<MimeMessage>{
        GetMessage(C.BoxGmailUser1),
        // GetMessage(C.BoxGmailUser1),
        // GetMessage(C.BoxGmailUser1),
        // GetMessage(C.BoxGmailUser1),
        // GetMessage(C.BoxGmailUser1),
        // GetMessage(C.BoxGmailUser1),
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