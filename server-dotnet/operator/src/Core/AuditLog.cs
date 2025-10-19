using RoomOperator.Abstractions;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace RoomOperator.Core;

public sealed class AuditLog
{
    private const int ChannelCapacity = 256;
    private static readonly BoundedChannelOptions SubscriberChannelOptions = new(ChannelCapacity)
    {
        SingleReader = true,
        AllowSynchronousContinuations = false,
        FullMode = BoundedChannelFullMode.DropOldest
    };

    private readonly ConcurrentQueue<AuditEntry> _entries = new();
    private readonly int _maxEntries;
    private readonly ILogger<AuditLog> _logger;
    private readonly ConcurrentDictionary<Guid, ChannelWriter<AuditEntry>> _subscribers = new();

    public AuditLog(ILogger<AuditLog> logger, int maxEntries = 1000)
    {
        _logger = logger;
        _maxEntries = maxEntries;
    }

    public IAsyncEnumerable<AuditEntry> SubscribeAsync(int replayCount = 100, CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<AuditEntry>(SubscriberChannelOptions);

        var subscriptionId = Guid.NewGuid();
        _subscribers[subscriptionId] = channel.Writer;

        if (replayCount > 0)
        {
            var snapshot = _entries.TakeLast(replayCount).ToList();
            foreach (var entry in snapshot)
            {
                if (!channel.Writer.TryWrite(entry))
                {
                    CompleteSubscription(subscriptionId);
                    return ReadAllAsync(channel.Reader, subscriptionId, default, cancellationToken);
                }
            }
        }

        var registration = cancellationToken.Register(() => CompleteSubscription(subscriptionId));

        return ReadAllAsync(channel.Reader, subscriptionId, registration, cancellationToken);
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

    private async IAsyncEnumerable<AuditEntry> ReadAllAsync(
        ChannelReader<AuditEntry> reader,
        Guid subscriptionId,
        CancellationTokenRegistration registration,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    yield return item;
                }
            }
        }
        finally
        {
            registration.Dispose();
            CompleteSubscription(subscriptionId);
        }
    }

    private void Broadcast(AuditEntry entry)
    {
        foreach (var kvp in _subscribers)
        {
            var subscriptionId = kvp.Key;
            var writer = kvp.Value;

            if (!writer.TryWrite(entry))
            {
                CompleteSubscription(subscriptionId);
            }
        }
    }

    private void CompleteSubscription(Guid subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var writer))
        {
            writer.TryComplete();
        }
    }
}
