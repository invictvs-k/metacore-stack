using RoomOperator.Abstractions;
using System.Collections.Concurrent;

namespace RoomOperator.Core;

public sealed class AuditLog
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();
    private readonly int _maxEntries;
    private readonly ILogger<AuditLog> _logger;
    
    public AuditLog(ILogger<AuditLog> logger, int maxEntries = 1000)
    {
        _logger = logger;
        _maxEntries = maxEntries;
    }
    
    public void LogEvent(string action, string correlationId, string operatorVersion, int specVersion, Dictionary<string, object>? metadata = null)
    {
        var entry = new AuditEntry
        {
            Type = "event",
            Action = action,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            OperatorVersion = operatorVersion,
            SpecVersion = specVersion,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
        
        _entries.Enqueue(entry);
        
        // Keep only the last N entries
        while (_entries.Count > _maxEntries)
        {
            _entries.TryDequeue(out _);
        }
        
        _logger.LogInformation(
            "Audit: {Type} {Action} (correlation={CorrelationId}, spec_v={SpecVersion})",
            entry.Type, entry.Action, entry.CorrelationId, entry.SpecVersion);
    }
    
    public void LogCommand(string action, string correlationId, string operatorVersion, int specVersion, Dictionary<string, object>? metadata = null)
    {
        var entry = new AuditEntry
        {
            Type = "command",
            Action = action,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            OperatorVersion = operatorVersion,
            SpecVersion = specVersion,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
        
        _entries.Enqueue(entry);
        
        while (_entries.Count > _maxEntries)
        {
            _entries.TryDequeue(out _);
        }
        
        _logger.LogInformation(
            "Audit: {Type} {Action} (correlation={CorrelationId}, spec_v={SpecVersion})",
            entry.Type, entry.Action, entry.CorrelationId, entry.SpecVersion);
    }
    
    public List<AuditEntry> GetRecent(int count = 100)
    {
        return _entries.TakeLast(count).ToList();
    }
    
    public List<AuditEntry> GetByCorrelation(string correlationId)
    {
        return _entries.Where(e => e.CorrelationId == correlationId).ToList();
    }
}
