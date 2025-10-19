using System;
using System.Text.Json.Serialization;

namespace RoomServer.Models;

public sealed class PolicySpec
{
  [JsonPropertyName("allow_commands_from")]
  public string AllowCommandsFrom { get; set; } = "any";   // owner|orchestrator|any

  [JsonPropertyName("sandbox_mode")]
  public bool SandboxMode { get; set; }

  [JsonPropertyName("env_whitelist")]
  public string[] EnvWhitelist { get; set; } = Array.Empty<string>();

  [JsonPropertyName("scopes")]
  public string[] Scopes { get; set; } = Array.Empty<string>();

  [JsonPropertyName("rateLimit")]
  public RateLimitSpec? RateLimit { get; set; }
}

public sealed class RateLimitSpec
{
  [JsonPropertyName("perMinute")]
  public int PerMinute { get; set; }
}
