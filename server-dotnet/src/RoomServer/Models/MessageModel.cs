using System;
using NUlid;

namespace RoomServer.Models;

public class MessageModel
{
    public string Id { get; set; } = Ulid.NewUlid().ToString();
    public string RoomId { get; set; } = default!;
    public string Channel { get; set; } = "room";
    public string From { get; set; } = default!;
    public string Type { get; set; } = "chat";
    public object Payload { get; set; } = default!;
    public DateTime Ts { get; set; } = DateTime.UtcNow;
}
