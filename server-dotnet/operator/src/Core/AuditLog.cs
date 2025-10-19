using RoomOperator.Abstractions;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Metacore.Shared.Channels;

namespace RoomOperator.Core;

public sealed class AuditLog
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();
    private readonly int _maxEntries;
    private readonly ILogger<AuditLog> _logger;
    private readonly ChannelSubscriptionManager<AuditEntry> _subscriptions;

    public AuditLog(ILogger<AuditLog> logger, int maxEntries = 1000)
    {
        _logger = logger;
        _maxEntries = maxEntries;
        _subscriptions = new ChannelSubscriptionManager<AuditEntry>(() => ChannelSettings.CreateSingleReaderOptions());
    }

    public IAsyncEnumerable<AuditEntry> SubscribeAsync(int replayCount = 100, CancellationToken cancellationToken = default)
    {
        return SubscribeAsyncCore(replayCount, cancellationToken);
    }

    private async IAsyncEnumerable<AuditEntry> SubscribeAsyncCore(
        int replayCount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (subscriptionId, reader, _) = _subscriptions.CreateSubscription();

        using var registration = cancellationToken.Register(() => _subscriptions.Complete(subscriptionId));

        try
        {
            if (replayCount > 0)
            {
                var snapshot = _entries.TakeLast(replayCount).ToList();
                foreach (var entry in snapshot)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return entry;
                }
            }

            await foreach (var item in ReadAllAsync(reader, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            _subscriptions.Complete(subscriptionId);
        }
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

        Broadcast(entry);
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

        Broadcast(entry);
    }

    public List<AuditEntry> GetRecent(int count = 100)
    {
        return _entries.TakeLast(count).ToList();
    }
    
    public List<AuditEntry> GetByCorrelation(string correlationId)
    {
        return _entries.Where(e => e.CorrelationId == correlationId).ToList();
    }

    private static async IAsyncEnumerable<AuditEntry> ReadAllAsync(
        ChannelReader<AuditEntry> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var item))
            {
                yield return item;
            }
        }
    }

    private void Broadcast(AuditEntry entry)
    {
        _subscriptions.Broadcast(entry);
    }
}
