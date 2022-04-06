using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mailica.Entities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;

namespace mailica.Sync;

public class SyncInstance
{
    readonly SemaphoreSlim _check = new(1);
    readonly CancellationTokenSource _mainCts;
    readonly ILogger<SyncInstance> _logger;
    DateTime? _lastSeen;
    public SyncInstance(ILogger<SyncInstance> logger, int jobId, Credential from, List<SyncRule> rules)
    {
        JobId = jobId;
        From = from;
        Rules = rules;
        _mainCts = new();
        _logger = logger;
    }

    public int JobId { get; }
    public Credential From { get; }
    public SyncDestination? ToCatchAll { get; set; }
    public List<SyncRule> Rules { get; }

    public async Task Start()
    {
        // var client = new ImapClient();
        // await client.ConnectAsync(HOST, PORT, false);
        // await client.AuthenticateAsync(_user, PASS);

        // client.Inbox.Open(FolderAccess.ReadOnly);
        // client.Inbox.CountChanged += Count;
        // await client.IdleAsync(_mainCts.Token);
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
            Console.WriteLine($"{From.Username} Start {DateTime.Now:hh:mm:ss}");

            using var client = new ImapClient();
            await client.ConnectAsync(From.Host, From.Port, false);
            await client.AuthenticateAsync(From.Username, From.Password); // TODO: decrypt

            var order = new[] { OrderBy.Arrival };
            var query = _lastSeen.HasValue
                ? SearchQuery.DeliveredAfter(_lastSeen.Value)
                : SearchQuery.All;

            client.Inbox.Open(FolderAccess.ReadOnly);
            var uids = await client.Inbox.SortAsync(query, order, _mainCts?.Token ?? default);
            foreach (var uid in uids)
            {
                var message = await client.Inbox.GetMessageAsync(uid);
                Console.WriteLine($"{From.Username} Message {message.Date}");
                _lastSeen = message.Date.UtcDateTime;
            }

            await Task.Delay(TimeSpan.FromMinutes(1));

            Console.WriteLine($"{From.Username} Stop {DateTime.Now:hh:mm:ss}");
            _check.Release();
        }
        else
        {
            Console.WriteLine($"{From.Username} Can't sync");
            return;
        }
    }
    public void Stop()
    {
        Console.WriteLine("Stopping runner");
        if (!_mainCts.IsCancellationRequested)
            _mainCts.Cancel();
        _mainCts.Dispose();
    }
}