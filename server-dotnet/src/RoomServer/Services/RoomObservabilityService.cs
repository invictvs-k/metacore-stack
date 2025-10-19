using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RoomServer.Models;

namespace RoomServer.Services;

public sealed class RoomObservabilityService : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _rootPath;
    private readonly ConcurrentDictionary<string, RoomRunStats> _roomStats = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    public RoomObservabilityService(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _rootPath = Path.Combine(environment.ContentRootPath, ".ai-flow", "runs");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task LogEventAsync(string roomId, string eventType, object data, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        var roomDir = GetRoomDirectory(roomId);
        Directory.CreateDirectory(roomDir);

        var eventsFile = Path.Combine(roomDir, "events.jsonl");
        var eventEntry = new
        {
            ts = DateTime.UtcNow,
            type = eventType,
            data
        };

        var jsonLine = JsonSerializer.Serialize(eventEntry, SerializerOptions);

        var fileLock = _fileLocks.GetOrAdd(eventsFile, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync(ct);
        try
        {
            await File.AppendAllLinesAsync(eventsFile, new[] { jsonLine }, ct);
        }
        finally
        {
            fileLock.Release();
        }

        // Update stats
        UpdateStats(roomId, eventType);
    }

    public async Task WriteRoomRunSummaryAsync(string roomId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);

        var roomDir = GetRoomDirectory(roomId);
        if (!Directory.Exists(roomDir))
        {
            return;
        }

        if (!_roomStats.TryGetValue(roomId, out var stats))
        {
            stats = new RoomRunStats { RoomId = roomId };
        }

        stats.EndedAt = DateTime.UtcNow;
        stats.DurationMs = stats.StartedAt.HasValue
            ? (long)(stats.EndedAt.Value - stats.StartedAt.Value).TotalMilliseconds
            : 0;

        var summaryFile = Path.Combine(roomDir, "room-run.json");
        var summary = new
        {
            roomId,
            startedAt = stats.StartedAt,
            endedAt = stats.EndedAt,
            durationMs = stats.DurationMs,
            entities = stats.Entities.ToList(),
            stats.MessageCount,
            stats.ArtifactCount,
            stats.CommandCount,
            stats.ErrorCount,
            eventCounts = stats.EventCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(summaryFile, json, ct);

        // Clean up stats
        _roomStats.TryRemove(roomId, out _);
    }

    public void TrackEntityJoin(string roomId, string entityId)
    {
        var stats = _roomStats.GetOrAdd(roomId, _ => new RoomRunStats
        {
            RoomId = roomId,
            StartedAt = DateTime.UtcNow
        });

        stats.Entities.Add(entityId);
    }

    public void TrackMessage(string roomId, string messageType)
    {
        if (_roomStats.TryGetValue(roomId, out var stats))
        {
            stats.MessageCount++;

            if (string.Equals(messageType, "command", StringComparison.OrdinalIgnoreCase))
            {
                stats.CommandCount++;
            }
        }
    }

    public void TrackArtifact(string roomId)
    {
        if (_roomStats.TryGetValue(roomId, out var stats))
        {
            stats.ArtifactCount++;
        }
    }

    public void TrackError(string roomId)
    {
        if (_roomStats.TryGetValue(roomId, out var stats))
        {
            stats.ErrorCount++;
        }
    }

    private void UpdateStats(string roomId, string eventType)
    {
        if (!_roomStats.TryGetValue(roomId, out var stats))
        {
            return;
        }

        stats.EventCounts.AddOrUpdate(eventType, 1, (_, count) => count + 1);
    }

    private string GetRoomDirectory(string roomId)
    {
        return Path.Combine(_rootPath, roomId);
    }

    public void Dispose()
    {
        foreach (var semaphore in _fileLocks.Values)
        {
            semaphore.Dispose();
        }
    }

    private sealed class RoomRunStats
    {
        public string RoomId { get; set; } = default!;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public long DurationMs { get; set; }
        public HashSet<string> Entities { get; } = new();
        public int MessageCount { get; set; }
        public int ArtifactCount { get; set; }
        public int CommandCount { get; set; }
        public int ErrorCount { get; set; }
        public ConcurrentDictionary<string, int> EventCounts { get; } = new();
    }
}
