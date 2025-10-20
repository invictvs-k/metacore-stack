using System;
using System.Text.Json.Serialization;
using RoomServer.Models;

namespace RoomServer.Services;

public abstract record RoomEventStreamItem
{
  [JsonPropertyName("id")]
  public required string Id { get; init; }

  [JsonPropertyName("roomId")]
  public required string RoomId { get; init; }

  [JsonPropertyName("ts")]
  public required DateTimeOffset Timestamp { get; init; }

  [JsonPropertyName("type")]
  public abstract string Type { get; }
}

public sealed record RoomEventStreamNotification : RoomEventStreamItem
{
  public override string Type => "event";

  [JsonPropertyName("payload")]
  public required RoomEventPayload Payload { get; init; }
}

public sealed record RoomEventPayload
{
  [JsonPropertyName("kind")]
  public required string Kind { get; init; }

  [JsonPropertyName("data")]
  public required object Data { get; init; }
}

public sealed record RoomEventStreamMessage : RoomEventStreamItem
{
  public override string Type => "message";

  [JsonPropertyName("payload")]
  public required MessageModel Payload { get; init; }
}
