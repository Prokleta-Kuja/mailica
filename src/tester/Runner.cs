using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;

namespace tester;

public class Runner
{
    readonly SemaphoreSlim _check = new(1);
    const string HOST = "dovecot.gunda";
    const int PORT = 143;
    const string PASS = "P@ssw0rd";
    string _user = string.Empty;
    CancellationTokenSource? _mainCts;
    DateTime? _lastSeen;
    public async Task Run(string user)
    {
        _user = user;
        _mainCts = new CancellationTokenSource();
        var client = new ImapClient();
        await client.ConnectAsync(HOST, PORT, false);
        await client.AuthenticateAsync(_user, PASS);

        client.Inbox.Open(FolderAccess.ReadOnly);
        client.Inbox.CountChanged += Count;
        await client.IdleAsync(_mainCts.Token);
    }

    void Count(object? sender, EventArgs e)
    {
        if (sender is not IImapFolder folder)
            return;

        Task.Run(() => Sync()).ConfigureAwait(false);
    }
    async Task Sync()
    {
        if (_check.Wait(0))
        {
            Console.WriteLine($"{_user} Start {DateTime.Now:hh:mm:ss}");

            var client = new ImapClient();
            await client.ConnectAsync(HOST, PORT, false);
            await client.AuthenticateAsync(_user, PASS);

            var order = new[] { OrderBy.Arrival };
            var query = _lastSeen.HasValue
            ? SearchQuery.DeliveredAfter(_lastSeen.Value)
            : SearchQuery.All;

            client.Inbox.Open(FolderAccess.ReadOnly);
            var uids = await client.Inbox.SortAsync(query, order, _mainCts?.Token ?? default);
            foreach (var uid in uids)
            {
                var message = await client.Inbox.GetMessageAsync(uid);
                Console.WriteLine($"{_user} Message {message.Date}");
                _lastSeen = message.Date.UtcDateTime;
            }

            await Task.Delay(TimeSpan.FromMinutes(1));

            Console.WriteLine($"{_user} Stop {DateTime.Now:hh:mm:ss}");
            _check.Release();
        }
        else
        {
            Console.WriteLine($"{_user} Can't sync");
            return;

        }
    }
    public void Stop()
    {
        Console.WriteLine("Stopping runner");
        _mainCts?.Cancel();
        _mainCts?.Dispose();
        _mainCts = null;
    }
}