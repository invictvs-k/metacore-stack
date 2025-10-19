using System;
using System.Text.Json.Serialization;
using NUlid;

namespace RoomServer.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageType
{
  Chat,
  Command,
  Event,
  Artifact
}

public class MessageModel
{
  public string Id { get; set; } = Ulid.NewUlid().ToString();
  public string RoomId { get; set; } = default!;
  public string Channel { get; set; } = "room";
  public string From { get; set; } = default!;
  public string? To { get; set; }
  public string Type { get; set; } = "chat";
  public object Payload { get; set; } = default!;
  public DateTime Ts { get; set; } = DateTime.UtcNow;
  public string? CorrelationId { get; set; }
}
