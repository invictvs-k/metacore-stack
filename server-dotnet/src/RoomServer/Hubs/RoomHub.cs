using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NUlid;
using RoomServer.Models;
using RoomServer.Services;

namespace RoomServer.Hubs;

public class RoomHub : Hub
{
    private readonly RoomManager _manager;
    private readonly RoomEventPublisher _events;
    private readonly ILogger<RoomHub> _logger;

    public RoomHub(RoomManager manager, RoomEventPublisher events, ILogger<RoomHub> logger)
    {
        _manager = manager;
        _events = events;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var removed = _manager.RemoveConnection(Context.ConnectionId);
        foreach (var (roomId, entity) in removed)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await _events.PublishAsync(roomId, "ENTITY.LEAVE", new { entityId = entity.Id });
            await PublishRoomState(roomId);
            _logger.LogInformation("[{RoomId}] {EntityId} disconnected ({Kind})", roomId, entity.Id, entity.Kind);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Join(string roomId, EntityInfo entity)
    {
        var stored = _manager.AddEntity(roomId, entity, Context.ConnectionId);

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await _events.PublishAsync(roomId, "ENTITY.JOIN", new { entity = stored });
        await PublishRoomState(roomId);

        _logger.LogInformation("[{RoomId}] {EntityId} joined ({Kind})", roomId, stored.Id, stored.Kind);
    }

    public async Task Leave(string roomId, string entityId)
    {
        var removed = _manager.RemoveEntity(roomId, entityId, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

        if (removed is not null)
        {
            await _events.PublishAsync(roomId, "ENTITY.LEAVE", new { entityId = removed.Id });
            await PublishRoomState(roomId);
            _logger.LogInformation("[{RoomId}] {EntityId} left ({Kind})", roomId, removed.Id, removed.Kind);
        }
    }

    public async Task SendToRoom(string roomId, MessageModel message)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.RoomId = roomId;
        message.Ts = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(message.Id))
        {
            message.Id = Ulid.NewUlid().ToString();
        }

        await Clients.Group(roomId).SendAsync("message", message);
        _logger.LogInformation("[{RoomId}] {From} → {Channel} :: {Type}", roomId, message.From, message.Channel, message.Type);
    }

    public Task<IReadOnlyCollection<EntityInfo>> ListEntities(string roomId)
        => Task.FromResult(_manager.GetEntities(roomId));

    private Task PublishRoomState(string roomId)
    {
        var entities = _manager.GetEntities(roomId);
        return _events.PublishAsync(roomId, "ROOM.STATE", new { entities });
    }
}
