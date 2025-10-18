using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RoomServer.Models;

namespace RoomServer.Services;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, EntityInfo>> _rooms = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<(string RoomId, string EntityId), byte>> _connectionIndex = new();

    public EntityInfo AddEntity(string roomId, EntityInfo entity, string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Id);

        entity.RoomId = roomId;
        entity.JoinedAt = DateTime.UtcNow;

        var room = _rooms.GetOrAdd(roomId, _ => new());
        room[entity.Id] = entity;

        var connections = _connectionIndex.GetOrAdd(connectionId, _ => new());
        connections[(roomId, entity.Id)] = 0;

        return entity;
    }

    public EntityInfo? RemoveEntity(string roomId, string entityId, string? connectionId = null)
    {
        var entity = RemoveEntityFromRoom(roomId, entityId);

        if (connectionId is not null)
        {
            RemoveConnectionEntry(connectionId, roomId, entityId);
        }
        else
        {
            RemoveConnectionEntryFromAll(roomId, entityId);
        }

        return entity;
    }

    public IReadOnlyCollection<EntityInfo> GetEntities(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            return room.Values.ToList();
        }

        return Array.Empty<EntityInfo>();
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<EntityInfo>> GetAll()
        => _rooms.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyCollection<EntityInfo>)kvp.Value.Values.ToList());

    public IReadOnlyList<(string RoomId, EntityInfo Entity)> RemoveConnection(string connectionId)
    {
        if (!_connectionIndex.TryRemove(connectionId, out var entries))
        {
            return Array.Empty<(string, EntityInfo)>();
        }

        List<(string RoomId, EntityInfo Entity)> removed = new();
        foreach (var ((roomId, entityId), _) in entries)
        {
            var entity = RemoveEntityFromRoom(roomId, entityId);
            if (entity is not null)
            {
                removed.Add((roomId, entity));
            }
        }

        return removed;
    }

    public bool RoomExists(string roomId) => _rooms.ContainsKey(roomId);

    private EntityInfo? RemoveEntityFromRoom(string roomId, string entityId)
    {
        if (_rooms.TryGetValue(roomId, out var room) && room.TryRemove(entityId, out var entity))
        {
            if (room.IsEmpty)
            {
                _rooms.TryRemove(roomId, out _);
            }

            return entity;
        }

        return null;
    }

    private void RemoveConnectionEntry(string connectionId, string roomId, string entityId)
    {
        if (_connectionIndex.TryGetValue(connectionId, out var entries))
        {
            entries.TryRemove((roomId, entityId), out _);
            if (entries.IsEmpty)
            {
                _connectionIndex.TryRemove(connectionId, out _);
            }
        }
    }

    private void RemoveConnectionEntryFromAll(string roomId, string entityId)
    {
        foreach (var connectionId in _connectionIndex.Keys)
        {
            RemoveConnectionEntry(connectionId, roomId, entityId);
        }
    }
}
