using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using mailica.Entities;
using mailica.Enums;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace mailica.Sync;

public class SyncInstance : IDisposable
{
    readonly SemaphoreSlim _check = new(1);
    readonly CancellationTokenSource _mainCts;
    readonly ILogger<SyncInstance> _logger;
    readonly IDataProtector _protector;
    readonly string _pass;
    readonly System.Timers.Timer _timer;
    double _every;
    int _failCount;
    DateTime? _lastSynced;
    public SyncInstance(ILogger<SyncInstance> logger, IDataProtectionProvider dpProvider, int jobId, Credential from, List<SyncRule> rules)
    {
        JobId = jobId;
        From = from;
        Rules = rules;
        _mainCts = new();
        _logger = logger;
        _protector = dpProvider.CreateProtector(nameof(Credential));
        _pass = _protector.Unprotect(from.Password);
        _timer = new();
        _timer.Elapsed += TimerElapsed;
        _timer.AutoReset = false;
    }
    public int JobId { get; }
    public Credential From { get; }
    public SyncDestination? ToCatchAll { get; set; }
    public List<SyncRule> Rules { get; }
    public SyncStatus Status { get; private set; }
    public SyncHistory History { get; set; }

    void TimerElapsed(object? sender, ElapsedEventArgs e) { _ = SyncAsync().ConfigureAwait(false); }
    void CountChanged(object? sender, EventArgs e) { _ = SyncAsync().ConfigureAwait(false); }
    public void Start(TimeSpan every, double randomMS)
    {
        _every = every.TotalMilliseconds;
        _timer.Interval = _every + randomMS;
        _timer.Start();
        Status = SyncStatus.Scheduled;
    }

    async Task IdleAsync()
    {
        // var client = new ImapClient();
        // await client.ConnectAsync(HOST, PORT, false);
        // await client.AuthenticateAsync(_user, PASS);

        // client.Inbox.Open(FolderAccess.ReadOnly);
        // client.Inbox.CountChanged += CountChanged;
        // await client.IdleAsync(_mainCts.Token);
        await Task.CompletedTask;
    }
    async Task SyncAsync()
    {
        if (_mainCts.Token.IsCancellationRequested)
        {
            _logger.LogDebug("{FromUsername} Can't sync - cancellation requested", From.Username);
            return;
        }

        if (!_check.Wait(0))
        {
            _logger.LogDebug("{FromUsername} Can't sync - sync already in progress", From.Username);
            return;
        }
        else
        {
            try
            {
                Status = SyncStatus.Syncing;
                _logger.LogDebug("{FromUsername} Start", From.Username);

                using var client = new ImapClient();
                await client.ConnectAsync(From.Host, From.Port, false);
                await client.AuthenticateAsync(From.Username, _pass);

                var order = new[] { OrderBy.Arrival };
                var query = _lastSynced.HasValue
                    ? SearchQuery.DeliveredAfter(_lastSynced.Value)
                    : SearchQuery.All;

                client.Inbox.Open(FolderAccess.ReadOnly);
                var uids = await client.Inbox.SortAsync(query, order, _mainCts.Token);
                foreach (var uid in uids)
                {
                    if (_mainCts.Token.IsCancellationRequested)
                        break;

                    var message = await client.Inbox.GetMessageAsync(uid);
                    _logger.LogDebug("{FromUsername} Message {Subject}", From.Username, message.Subject);
                    await ProcessMessageAsync(message);
                }

                _logger.LogDebug("{FromUsername} Stop", From.Username);
                _failCount = 0;
                _timer.Interval = _every;
                if (!_mainCts.Token.IsCancellationRequested)
                {
                    _timer.Start();
                    Status = SyncStatus.Scheduled;
                }
                else
                    Status = SyncStatus.Stopped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{FromUsername} failed to sync {Count}", From.Username, ++_failCount);
                if (_failCount >= 10)
                    Status = SyncStatus.Errored;
                else
                {
                    Status = SyncStatus.Retry;
                    _timer.Start();
                }
            }
            finally
            {
                _check.Release();
            }
        }
    }

    async Task ProcessMessageAsync(MimeMessage message)
    {
        var destinationAddresses = new HashSet<string>();
        foreach (var to in message.To.Mailboxes)
            destinationAddresses.Add(to.Address);
        foreach (var cc in message.Cc.Mailboxes)
            destinationAddresses.Add(cc.Address);
        foreach (var bcc in message.Bcc.Mailboxes)
            destinationAddresses.Add(bcc.Address);

        var destinations = new HashSet<SyncDestination>();
        foreach (var rule in Rules)
            if (destinationAddresses.Any(a => rule.Matches(a)))
                foreach (var destination in rule.Destinations)
                    destinations.Add(destination);

        if (_mainCts.Token.IsCancellationRequested)
        {
            _logger.LogDebug("{FromUsername} Message processing aborted", From.Username);
            return;
        }

        if (destinations.Any())
            foreach (var destination in destinations)
                await DeliverAsync(message, destination);
        else if (ToCatchAll != null)
            await DeliverAsync(message, ToCatchAll);

        _lastSynced = message.Date.UtcDateTime;
    }

    async Task DeliverAsync(MimeMessage message, SyncDestination destination)
    {
        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(destination.Credential.Host, destination.Credential.Port, SecureSocketOptions.Auto, _mainCts.Token);
            var unprotectedPass = _protector.Unprotect(destination.Credential.Password);
            await client.AuthenticateAsync(destination.Credential.Username, unprotectedPass, _mainCts.Token);

            IMailFolder? folder = null;
            if (string.IsNullOrWhiteSpace(destination.Folder))
                folder = client.Inbox;
            else
            {
                var separator = destination.Folder[0];
                var actual = destination.Folder[1..];
                try
                {
                    folder = client.GetFolder(actual);
                }
                catch (FolderNotFoundException)
                {
                    var currentDir = client.GetFolder(client.PersonalNamespaces[0]);
                    var parts = actual.Split(separator);
                    var queue = new Queue<string>(parts);

                    while (queue.TryDequeue(out var part))
                    {
                        currentDir = currentDir.Create(part, true);
                        currentDir.Subscribe();
                    }

                    folder = currentDir;
                }
            }

            await folder.AppendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("{FromUsername} Message delivery to {ToUsername} aborted", From.Username, destination.Credential.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{FromUsername} Could not deliver to {ToUsername}", destination.Credential.Username);

            var undelivered = C.Paths.Undelivered(destination.Credential.CredentialId);
            Directory.CreateDirectory(undelivered);
            var filename = $"{Guid.NewGuid}.eml";
            await message.WriteToAsync(Path.Combine(undelivered, filename));
        }
    }

    public void Stop()
    {
        Console.WriteLine("Stopping runner");
        _timer.Stop();
        if (!_mainCts.IsCancellationRequested)
            _mainCts.Cancel();
        Dispose();
    }

    public void Dispose()
    {
        _mainCts.Dispose();
        _timer.Dispose();
    }
}