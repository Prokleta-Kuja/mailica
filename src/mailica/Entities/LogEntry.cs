using System.Diagnostics;
using System.Text.Json;

namespace mailica.Entities;

[DebuggerDisplay("{Message}")]
public class LogEntry
{
    public LogEntry() { }
    public LogEntry(Guid context, string message, object? properties)
    {
        Timestamp = DateTime.UtcNow;
        Context = context;
        Message = message;
        if (properties != null)
            Properties = JsonSerializer.Serialize(properties);
    }
    public int LogEntryId { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid Context { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Properties { get; set; }
}