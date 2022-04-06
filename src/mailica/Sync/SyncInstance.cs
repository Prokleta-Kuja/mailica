using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using mailica.Entities;
using mailica.Enums;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace mailica.Sync;

public class SyncInstance : IDisposable
{
    readonly SemaphoreSlim _check = new(1);
    readonly CancellationTokenSource _mainCts;
    readonly ILogger<SyncInstance> _logger;
    readonly IDataProtector _protector;
    readonly string _pass;
    readonly System.Timers.Timer _timer;
    DateTime? _lastSeen;
    public SyncInstance(ILogger<SyncInstance> logger, IDataProtectionProvider dpProvider, int jobId, Credential from, List<SyncRule> rules)
    {
        JobId = jobId;
        From = from;
        Rules = rules;
        _mainCts = new();
        _logger = logger;
        _protector = dpProvider.CreateProtector(nameof(Credential));
        _pass = _protector.Unprotect(from.Password);
        _timer = new(TimeSpan.FromSeconds(5).TotalMilliseconds); // TODO: add job sync
        _timer.Elapsed += TimerElapsed;
        _timer.AutoReset = false;
        Status = SyncStatus.Scheduled;
    }


    public int JobId { get; }
    public Credential From { get; }
    public SyncDestination? ToCatchAll { get; set; }
    public List<SyncRule> Rules { get; }
    public SyncStatus Status { get; private set; }

    void TimerElapsed(object? sender, ElapsedEventArgs e) { _ = SyncAsync().ConfigureAwait(false); }
    void CountChanged(object? sender, EventArgs e) { _ = SyncAsync().ConfigureAwait(false); }
    public void Start()
    {
        _timer.Start();
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
        if (!_check.Wait(0))
        {
            _logger.LogDebug($"{From.Username} Can't sync");
            return;
        }
        else
        {
            try
            {
                Status = SyncStatus.Syncing;
                _logger.LogDebug($"{From.Username} Start {DateTime.Now:hh:mm:ss}");

                using var client = new ImapClient();
                await client.ConnectAsync(From.Host, From.Port, false);
                await client.AuthenticateAsync(From.Username, _pass);

                var order = new[] { OrderBy.Arrival };
                var query = _lastSeen.HasValue
                    ? SearchQuery.DeliveredAfter(_lastSeen.Value)
                    : SearchQuery.All;

                client.Inbox.Open(FolderAccess.ReadOnly);
                var uids = await client.Inbox.SortAsync(query, order, _mainCts?.Token ?? default);
                foreach (var uid in uids)
                {
                    var message = await client.Inbox.GetMessageAsync(uid);
                    _logger.LogDebug($"{From.Username} Message {message.Subject}");
                    _lastSeen = message.Date.UtcDateTime;
                }

                await Task.Delay(TimeSpan.FromSeconds(15)); // TODO: Remove this after test

                _logger.LogDebug($"{From.Username} Stop {DateTime.Now:hh:mm:ss}");
                _timer.Start();
                Status = SyncStatus.Scheduled;
            }
            catch (Exception)
            {
                // TODO: retry logic
            }
            finally
            {
                _check.Release();
            }
        }
    }
    public void Stop()
    {
        Console.WriteLine("Stopping runner");
        if (!_mainCts.IsCancellationRequested)
            _mainCts.Cancel();
        _timer.Stop();
        Dispose();
    }

    public void Dispose()
    {
        _mainCts.Dispose();
        _timer.Dispose();
    }
}