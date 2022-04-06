using System.Threading;
using System.Threading.Tasks;
using mailica.Sync;
using Microsoft.Extensions.Hosting;

namespace mailica.Services;

public class SyncService : IHostedService
{
    readonly SyncManager _syncManager;

    public SyncService(SyncManager syncManager)
    {
        _syncManager = syncManager;
    }

    public Task StartAsync(CancellationToken cancellationToken) => _syncManager.RestartAsync();

    public Task StopAsync(CancellationToken cancellationToken) => _syncManager.StopAsync();
}