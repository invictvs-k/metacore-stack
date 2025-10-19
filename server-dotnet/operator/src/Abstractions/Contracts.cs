namespace RoomOperator.Abstractions;

public enum ReconcilePhase
{
    PLANNING,
    PRE_CHECKS,
    APPLY,
    VERIFY,
    ROLLBACK
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public sealed class ReconcileResult
{
    public bool Success { get; set; }
    public bool PartialSuccess { get; set; }
    public ReconcilePhase LastCompletedPhase { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ReconcileDiff? Diff { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

public sealed class ReconcileDiff
{
    public List<string> ToJoin { get; set; } = new();
    public List<string> ToKick { get; set; } = new();
    public List<string> ToEnsure { get; set; } = new();
    public List<string> ToSeed { get; set; } = new();
    public List<string> ToPromote { get; set; } = new();
    public List<string> ToApply { get; set; } = new();
    public List<string> ToDeleteArtifacts { get; set; } = new();
    public List<string> Blocked { get; set; } = new();
}

public sealed class ApplyRequest
{
    public RoomSpec Spec { get; set; } = default!;
    public bool DryRun { get; set; }
    public bool Confirm { get; set; }
}

public sealed class OperatorStatus
{
    public string Version { get; set; } = default!;
    public HealthStatus Health { get; set; }
    public List<RoomStatus> Rooms { get; set; } = new();
    public int QueuedRequests { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class RoomStatus
{
    public string RoomId { get; set; } = default!;
    public string? CurrentPhase { get; set; }
    public bool IsReconciling { get; set; }
    public ReconcileDiff? PendingDiff { get; set; }
    public List<string> Blocked { get; set; } = new();
    public DateTime? LastReconcile { get; set; }
    public int CyclesSinceConverged { get; set; }
}

public sealed class AuditEntry
{
    public string Type { get; set; } = "event";
    public string Action { get; set; } = default!;
    public string CorrelationId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string OperatorVersion { get; set; } = default!;
    public int SpecVersion { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public interface IRoomClient
{
    Task<RoomState> GetStateAsync(string roomId, CancellationToken ct = default);
    Task JoinEntityAsync(string roomId, EntitySpec entity, CancellationToken ct = default);
    Task KickEntityAsync(string roomId, string entityId, CancellationToken ct = default);
}

public interface IArtifactsClient
{
    Task<string?> GetArtifactHashAsync(string roomId, string name, CancellationToken ct = default);
    Task SeedArtifactAsync(string roomId, ArtifactSeedSpec spec, byte[] content, CancellationToken ct = default);
    Task PromoteArtifactAsync(string roomId, string name, CancellationToken ct = default);
    Task DeleteArtifactAsync(string roomId, string name, CancellationToken ct = default);
}

public interface IPoliciesClient
{
    Task ApplyPolicyAsync(string roomId, string policyName, object policyValue, CancellationToken ct = default);
}

public interface IMcpClient
{
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task EnsureResourceAsync(string roomId, ResourceSpec spec, CancellationToken ct = default);
}
