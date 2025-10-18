using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NUlid;
using RoomServer.Hubs;

namespace RoomServer.Services;

public class RoomEventPublisher
{
    private readonly IHubContext<RoomHub> _hubContext;

    public RoomEventPublisher(IHubContext<RoomHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishAsync(string roomId, string eventType, object data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        return _hubContext.Clients.Group(roomId).SendAsync("event", new
        {
            id = Ulid.NewUlid().ToString(),
            roomId,
            type = "event",
            payload = new { kind = eventType, data },
            ts = DateTime.UtcNow
        });
    }
}
