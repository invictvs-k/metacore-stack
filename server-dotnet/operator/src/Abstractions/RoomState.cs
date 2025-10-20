namespace RoomOperator.Abstractions;

public sealed class RoomState
{
  public string RoomId { get; set; } = default!;
  public List<EntityState> Entities { get; set; } = new();
  public List<ArtifactState> Artifacts { get; set; } = new();
  public Dictionary<string, object> Policies { get; set; } = new();
  public List<ResourceState> Resources { get; set; } = new();
  public DateTime LastUpdated { get; set; }
}

public sealed class EntityState
{
  public string Id { get; set; } = default!;
  public string Kind { get; set; } = default!;
  public string? DisplayName { get; set; }
  public string Visibility { get; set; } = "team";
  public string? OwnerUserId { get; set; }
  public List<string> Capabilities { get; set; } = new();
  public bool IsConnected { get; set; }
}

public sealed class ArtifactState
{
  public string Name { get; set; } = default!;
  public string Type { get; set; } = default!;
  public string Workspace { get; set; } = default!;
  public List<string> Tags { get; set; } = new();
  public string? ContentHash { get; set; }
  public bool IsPromoted { get; set; }
}

public sealed class ResourceState
{
  public string Name { get; set; } = default!;
  public string Type { get; set; } = default!;
  public string Status { get; set; } = "PENDING";
  public Dictionary<string, object> Config { get; set; } = new();
}
