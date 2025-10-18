using System;

namespace RoomServer.Models;

public sealed class PolicySpec
{
    public string AllowCommandsFrom { get; set; } = "any";   // owner|orchestrator|any
    public bool SandboxMode { get; set; }
    public string[] EnvWhitelist { get; set; } = Array.Empty<string>();
}
