
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;

// var message = new MimeMessage();
// message.From.Add(new MailboxAddress("Joey Tribbiani", "joey@friends.com"));
// message.To.Add(new MailboxAddress("Mrs. Chanandler Bong", "chandler@friends.com"));
// message.Subject = "How you doin'?";

// message.Body = new TextPart("plain")
// {
//     Text = @"Hey Chandler,

// I just wanted to let you know that Monica and I were going to go play some paintball, you in?

// -- Joey"
// };

// using (var client = new ImapClient())
// {
//     client.Connect("dovecot.gunda", 143, false);

//     client.Authenticate("user1", "P@ssw0rd");

//     // The Inbox folder is always available on all IMAP servers...
//     var inbox = client.Inbox;

//     Console.WriteLine("Total messages: {0}", inbox.Count);
//     Console.WriteLine("Recent messages: {0}", inbox.Recent);

//     await inbox.AppendAsync(message, MessageFlags.None);

//     // for (int i = 0; i < inbox.Count; i++)
//     // {
//     //     var message = inbox.GetMessage(i);
//     //     Console.WriteLine("Subject: {0}", message.Subject);
//     // }

//     client.Disconnect(true);
// }


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
