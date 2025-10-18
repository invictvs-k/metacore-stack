using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RoomServer.Models;

namespace RoomServer.Services;

public sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, EntitySession> _byConnection = new();
    private readonly ConcurrentDictionary<(string RoomId, string EntityId), string> _index = new();

    public void Add(EntitySession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(session.ConnectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(session.RoomId);
        ArgumentNullException.ThrowIfNull(session.Entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(session.Entity.Id);

        _byConnection[session.ConnectionId] = session;
        _index[(session.RoomId, session.Entity.Id)] = session.ConnectionId;
    }

    public EntitySession? GetByConnection(string connectionId)
        => _byConnection.TryGetValue(connectionId, out var session) ? session : null;

    public EntitySession? GetByRoomAndEntity(string roomId, string entityId)
    {
        if (_index.TryGetValue((roomId, entityId), out var connectionId))
        {
            return GetByConnection(connectionId);
        }

        return null;
    }

    public IReadOnlyList<EntitySession> ListByRoom(string roomId)
        => _byConnection.Values.Where(s => s.RoomId == roomId).ToList();

    public EntitySession? RemoveByConnection(string connectionId)
    {
        if (_byConnection.TryRemove(connectionId, out var session))
        {
            _index.TryRemove((session.RoomId, session.Entity.Id), out _);
            return session;
        }

        return null;
    }

    public bool RemoveByRoomAndEntity(string roomId, string entityId)
    {
        if (_index.TryRemove((roomId, entityId), out var connectionId))
        {
            return _byConnection.TryRemove(connectionId, out _);
        }

        return false;
    }
}
