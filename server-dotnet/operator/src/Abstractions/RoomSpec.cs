using YamlDotNet.Serialization;

namespace RoomOperator.Abstractions;

public sealed class RoomSpec
{
  [YamlMember(Alias = "apiVersion")]
  public string ApiVersion { get; set; } = "v1";

  [YamlMember(Alias = "kind")]
  public string Kind { get; set; } = "RoomSpec";

  [YamlMember(Alias = "metadata")]
  public SpecMetadata Metadata { get; set; } = new();

  [YamlMember(Alias = "spec")]
  public RoomSpecData Spec { get; set; } = new();
}

public sealed class SpecMetadata
{
  [YamlMember(Alias = "name")]
  public string Name { get; set; } = default!;

  [YamlMember(Alias = "version")]
  public int Version { get; set; }
}

public sealed class RoomSpecData
{
  [YamlMember(Alias = "roomId")]
  public string RoomId { get; set; } = default!;

  [YamlMember(Alias = "entities")]
  public List<EntitySpec> Entities { get; set; } = new();

  [YamlMember(Alias = "artifacts")]
  public List<ArtifactSeedSpec> Artifacts { get; set; } = new();

  [YamlMember(Alias = "policies")]
  public GlobalPolicies Policies { get; set; } = new();

  [YamlMember(Alias = "resources")]
  public List<ResourceSpec> Resources { get; set; } = new();
}

public sealed class EntitySpec
{
  [YamlMember(Alias = "id")]
  public string Id { get; set; } = default!;

  [YamlMember(Alias = "kind")]
  public string Kind { get; set; } = default!;

  [YamlMember(Alias = "displayName")]
  public string? DisplayName { get; set; }

  [YamlMember(Alias = "visibility")]
  public string Visibility { get; set; } = "team";

  [YamlMember(Alias = "ownerUserId")]
  public string? OwnerUserId { get; set; }

  [YamlMember(Alias = "capabilities")]
  public List<string> Capabilities { get; set; } = new();

  [YamlMember(Alias = "policy")]
  public EntityPolicy Policy { get; set; } = new();
}

public sealed class EntityPolicy
{
  [YamlMember(Alias = "allow_commands_from")]
  public string AllowCommandsFrom { get; set; } = "any";

  [YamlMember(Alias = "sandbox_mode")]
  public bool SandboxMode { get; set; }

  [YamlMember(Alias = "env_whitelist")]
  public List<string> EnvWhitelist { get; set; } = new();

  [YamlMember(Alias = "scopes")]
  public List<string> Scopes { get; set; } = new();
}

public sealed class ArtifactSeedSpec
{
  [YamlMember(Alias = "name")]
  public string Name { get; set; } = default!;

  [YamlMember(Alias = "type")]
  public string Type { get; set; } = default!;

  [YamlMember(Alias = "workspace")]
  public string Workspace { get; set; } = default!;

  [YamlMember(Alias = "tags")]
  public List<string> Tags { get; set; } = new();

  [YamlMember(Alias = "seedFrom")]
  public string? SeedFrom { get; set; }

  [YamlMember(Alias = "promoteAfterSeed")]
  public bool PromoteAfterSeed { get; set; }

  [YamlMember(Alias = "dependsOn")]
  public List<string> DependsOn { get; set; } = new();
}

public sealed class GlobalPolicies
{
  [YamlMember(Alias = "dmVisibilityDefault")]
  public string DmVisibilityDefault { get; set; } = "team";

  [YamlMember(Alias = "allowResourceCreation")]
  public bool AllowResourceCreation { get; set; }

  [YamlMember(Alias = "maxArtifactsPerEntity")]
  public int MaxArtifactsPerEntity { get; set; } = 100;
}

public sealed class ResourceSpec
{
  [YamlMember(Alias = "name")]
  public string Name { get; set; } = default!;

  [YamlMember(Alias = "type")]
  public string Type { get; set; } = default!;

  [YamlMember(Alias = "config")]
  public Dictionary<string, object> Config { get; set; } = new();

  [YamlMember(Alias = "dependsOn")]
  public List<string> DependsOn { get; set; } = new();

  [YamlMember(Alias = "onMissingDependency")]
  public string OnMissingDependency { get; set; } = "skip";
}
