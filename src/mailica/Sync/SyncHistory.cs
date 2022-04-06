using System;
using System.Collections.Generic;
using System.Linq;

namespace mailica.Sync;
class SyncHistory
{
    readonly int _maxCount;
    readonly Queue<SyncLog> _queue;

    public SyncHistory(int maxCount)
    {
        _maxCount = maxCount;
        _queue = new Queue<SyncLog>(maxCount);
    }

    public void Append(string entry)
    {
        if (_queue.Count == _maxCount)
            _queue.Dequeue();

        var log = new SyncLog(entry);
        _queue.Enqueue(log);
    }

    public IEnumerable<SyncLog> GetEntries()
    {
        return _queue.Reverse();
    }
}
record SyncLog
{
    public SyncLog(string entry)
    {
        At = DateTime.UtcNow;
        Entry = entry;
    }

    public DateTime At { get; }
    public string Entry { get; }
}