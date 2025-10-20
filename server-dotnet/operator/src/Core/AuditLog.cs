using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Metacore.Shared.Channels;
using RoomOperator.Abstractions;

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
    _subscriptions = new ChannelSubscriptionManager<AuditEntry>(
        () => ChannelSettings.CreateSingleReaderOptions(
            fullMode: BoundedChannelFullMode.DropOldest));
  }

  public IAsyncEnumerable<AuditEntry> SubscribeAsync(int replayCount = 100, CancellationToken cancellationToken = default)
  {
    Func<CancellationToken, IAsyncEnumerable<AuditEntry>>? replayFactory = replayCount > 0
        ? ct => ReplayEntriesAsync(replayCount, ct)
        : null;

    return _subscriptions.SubscribeAsync(replayFactory, cancellationToken);
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

  private async IAsyncEnumerable<AuditEntry> ReplayEntriesAsync(
      int replayCount,
      [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var snapshot = _entries.TakeLast(replayCount).ToList();

    foreach (var entry in snapshot)
    {
      cancellationToken.ThrowIfCancellationRequested();
      yield return entry;
    }
  }

  private void Broadcast(AuditEntry entry)
  {
    _subscriptions.Broadcast(entry);
  }
}
