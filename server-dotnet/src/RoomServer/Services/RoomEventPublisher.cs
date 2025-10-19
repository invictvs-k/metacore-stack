using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NUlid;
using RoomServer.Hubs;
using RoomServer.Models;
using RoomServer.Services.ArtifactStore;

namespace RoomServer.Services;

public class RoomEventPublisher
{
    private readonly IHubContext<RoomHub> _hubContext;
    private readonly RoomObservabilityService _observability;

    public RoomEventPublisher(IHubContext<RoomHub> hubContext, RoomObservabilityService observability)
    {
        _hubContext = hubContext;
        _observability = observability;
    }

    public async Task PublishAsync(string roomId, string eventType, object data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        var eventPayload = new
        {
            id = Ulid.NewUlid().ToString(),
            roomId,
            type = "event",
            payload = new { kind = eventType, data },
            ts = DateTime.UtcNow
        };

        // Log to events.jsonl
        await _observability.LogEventAsync(roomId, eventType, data);

        // Broadcast to SignalR clients
        await _hubContext.Clients.Group(roomId).SendAsync("event", eventPayload);
    }

    public async Task PublishArtifactMessageAsync(string roomId, ArtifactManifest manifest, string from = "E-SERVER", string channel = "room")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentNullException.ThrowIfNull(manifest);

        var message = new MessageModel
        {
            Id = Ulid.NewUlid().ToString(),
            RoomId = roomId,
            Channel = channel,
            From = from,
            Type = "artifact",
            Payload = new { manifest },
            Ts = DateTime.UtcNow
        };

        // Track artifact in stats
        _observability.TrackArtifact(roomId);

        await _hubContext.Clients.Group(roomId).SendAsync("message", message);
    }
}
