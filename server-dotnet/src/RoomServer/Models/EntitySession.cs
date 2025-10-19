using System;

namespace RoomServer.Models;

public sealed class EntitySession
{
  public string ConnectionId { get; set; } = default!;
  public string RoomId { get; set; } = default!;
  public EntitySpec Entity { get; set; } = default!;
  public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
  public string? UserId { get; set; }
}
