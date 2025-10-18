using System;
using System.Text.Json.Serialization;

namespace RoomServer.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntityKind
{
    Human,
    Agent,
    Npc,
    Orchestrator
}

public sealed class EntitySpec
{
    public string Id { get; set; } = default!;            // E-*
    public string Kind { get; set; } = default!;          // human|agent|npc|orchestrator
    public string? DisplayName { get; set; }
    public string Visibility { get; set; } = "team";      // public|team|owner
    public string? OwnerUserId { get; set; }              // required if visibility==owner
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public PolicySpec Policy { get; set; } = new();
}
