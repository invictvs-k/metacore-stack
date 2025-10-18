using System;

namespace RoomServer.Models;

public class EntityInfo
{
    public string Id { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string RoomId { get; set; } = default!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
