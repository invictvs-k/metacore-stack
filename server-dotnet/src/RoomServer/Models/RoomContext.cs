using System;
using System.Collections.Concurrent;

namespace RoomServer.Models;

public sealed class RoomContext
{
  public string RoomId { get; set; } = default!;
  public RoomState State { get; set; } = RoomState.Init;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime? LastStateChange { get; set; }
}

public sealed class RoomContextStore
{
  private readonly ConcurrentDictionary<string, RoomContext> _rooms = new();

  public RoomContext GetOrCreate(string roomId)
  {
    return _rooms.GetOrAdd(roomId, key => new RoomContext { RoomId = key });
  }

  public RoomContext? Get(string roomId)
  {
    return _rooms.TryGetValue(roomId, out var context) ? context : null;
  }

  public void UpdateState(string roomId, RoomState newState)
  {
    var context = GetOrCreate(roomId);
    context.State = newState;
    context.LastStateChange = DateTime.UtcNow;
  }

  public bool Remove(string roomId)
  {
    return _rooms.TryRemove(roomId, out _);
  }
}
