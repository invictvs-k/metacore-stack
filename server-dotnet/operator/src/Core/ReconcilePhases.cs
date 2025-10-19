using RoomOperator.Abstractions;

namespace RoomOperator.Core;

public sealed class ReconcilePhases
{
    private readonly IRoomClient _roomClient;
    private readonly IArtifactsClient _artifactsClient;
    private readonly IPoliciesClient _policiesClient;
    private readonly IMcpClient _mcpClient;
    private readonly DiffEngine _diffEngine;
    private readonly Guardrails _guardrails;
    private readonly RetryPolicyFactory _retryPolicy;
    private readonly AuditLog _auditLog;
    private readonly ILogger<ReconcilePhases> _logger;
    private readonly string _operatorVersion;
    
    public ReconcilePhases(
        IRoomClient roomClient,
        IArtifactsClient artifactsClient,
        IPoliciesClient policiesClient,
        IMcpClient mcpClient,
        DiffEngine diffEngine,
        Guardrails guardrails,
        RetryPolicyFactory retryPolicy,
        AuditLog auditLog,
        ILogger<ReconcilePhases> logger,
        string operatorVersion)
    {
        _roomClient = roomClient;
        _artifactsClient = artifactsClient;
        _policiesClient = policiesClient;
        _mcpClient = mcpClient;
        _diffEngine = diffEngine;
        _guardrails = guardrails;
        _retryPolicy = retryPolicy;
        _auditLog = auditLog;
        _logger = logger;
        _operatorVersion = operatorVersion;
    }
    
    public async Task<ReconcileResult> ExecuteAsync(RoomSpec spec, bool dryRun, bool confirm, CancellationToken ct)
    {
        var result = new ReconcileResult
        {
            StartTime = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        try
        {
            // PLANNING phase
            result.LastCompletedPhase = ReconcilePhase.PLANNING;
            var state = await _roomClient.GetStateAsync(spec.Spec.RoomId, ct);
            var diff = _diffEngine.CalculateDiff(spec, state);
            result.Diff = diff;
            
            _auditLog.LogEvent("reconcile.planning", result.CorrelationId, _operatorVersion, spec.Metadata.Version);
            _logger.LogInformation("PLANNING phase completed");
            
            // PRE_CHECKS phase
            result.LastCompletedPhase = ReconcilePhase.PRE_CHECKS;
            var guardrailsResult = _guardrails.Check(diff, state, confirm);
            
            if (!guardrailsResult.Passed)
            {
                result.Success = false;
                result.Errors.AddRange(guardrailsResult.Violations);
                return result;
            }
            
            result.Warnings.AddRange(guardrailsResult.Warnings);
            _auditLog.LogEvent("reconcile.pre_checks", result.CorrelationId, _operatorVersion, spec.Metadata.Version);
            _logger.LogInformation("PRE_CHECKS phase completed");
            
            if (dryRun)
            {
                result.Success = true;
                _logger.LogInformation("DRY RUN completed");
                return result;
            }
            
            // APPLY phase
            result.LastCompletedPhase = ReconcilePhase.APPLY;
            await ApplyChangesAsync(spec, diff, state, result, ct);
            
            _auditLog.LogEvent("reconcile.apply", result.CorrelationId, _operatorVersion, spec.Metadata.Version);
            _logger.LogInformation("APPLY phase completed");
            
            // VERIFY phase
            result.LastCompletedPhase = ReconcilePhase.VERIFY;
            await VerifyAsync(spec, result, ct);
            
            _auditLog.LogEvent("reconcile.verify", result.CorrelationId, _operatorVersion, spec.Metadata.Version);
            _logger.LogInformation("VERIFY phase completed");
            
            result.Success = result.Errors.Count == 0;
            result.PartialSuccess = result.Warnings.Count > 0 && result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconciliation failed");
            result.Success = false;
            result.Errors.Add(ex.Message);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }
        
        return result;
    }
    
    private async Task ApplyChangesAsync(RoomSpec spec, ReconcileDiff diff, RoomState state, ReconcileResult result, CancellationToken ct)
    {
        // Apply in deterministic order: Entities → Artifacts → Policies
        
        // 1. Join new entities
        foreach (var entityId in diff.ToJoin)
        {
            try
            {
                var entity = spec.Spec.Entities.First(e => e.Id == entityId);
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _roomClient.JoinEntityAsync(spec.Spec.RoomId, entity, ct);
                }, ct);
                
                _auditLog.LogEvent("entity.joined", result.CorrelationId, _operatorVersion, spec.Metadata.Version,
                    new Dictionary<string, object> { ["entityId"] = entityId });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to join entity {EntityId}", entityId);
                result.Warnings.Add($"Failed to join entity {entityId}: {ex.Message}");
                result.PartialSuccess = true;
            }
        }
        
        // 2. Kick extra entities
        foreach (var entityId in diff.ToKick)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _roomClient.KickEntityAsync(spec.Spec.RoomId, entityId, ct);
                }, ct);
                
                _auditLog.LogEvent("entity.kicked", result.CorrelationId, _operatorVersion, spec.Metadata.Version,
                    new Dictionary<string, object> { ["entityId"] = entityId });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kick entity {EntityId}", entityId);
                result.Warnings.Add($"Failed to kick entity {entityId}: {ex.Message}");
                result.PartialSuccess = true;
            }
        }
        
        // 3. Seed artifacts
        foreach (var artifactName in diff.ToSeed)
        {
            try
            {
                var artifactSpec = spec.Spec.Artifacts.First(a => a.Name == artifactName);
                if (string.IsNullOrEmpty(artifactSpec.SeedFrom))
                    continue;
                
                // Check if file exists and read it
                var seedPath = Path.Combine(Directory.GetCurrentDirectory(), artifactSpec.SeedFrom);
                if (!File.Exists(seedPath))
                {
                    result.Warnings.Add($"Seed file not found: {seedPath}");
                    diff.Blocked.Add(artifactName);
                    continue;
                }
                
                var content = await File.ReadAllBytesAsync(seedPath, ct);
                var localHash = Clients.ArtifactsClient.BuildFingerprint(artifactSpec, content);
                var remoteHash = await _artifactsClient.GetArtifactHashAsync(spec.Spec.RoomId, artifactName, ct);
                
                // Only write if hash differs
                if (localHash != remoteHash)
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        await _artifactsClient.SeedArtifactAsync(spec.Spec.RoomId, artifactSpec, content, ct);
                    }, ct);
                    
                    _auditLog.LogEvent("artifact.seeded", result.CorrelationId, _operatorVersion, spec.Metadata.Version,
                        new Dictionary<string, object> { ["artifactName"] = artifactName, ["hash"] = localHash });
                }
                else
                {
                    _logger.LogDebug("Artifact {Name} hash unchanged, skipping", artifactName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed artifact {Name}", artifactName);
                result.Warnings.Add($"Failed to seed artifact {artifactName}: {ex.Message}");
                result.PartialSuccess = true;
            }
        }
        
        // 4. Promote artifacts
        foreach (var artifactName in diff.ToPromote)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _artifactsClient.PromoteArtifactAsync(spec.Spec.RoomId, artifactName, ct);
                }, ct);
                
                _auditLog.LogEvent("artifact.promoted", result.CorrelationId, _operatorVersion, spec.Metadata.Version,
                    new Dictionary<string, object> { ["artifactName"] = artifactName });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to promote artifact {Name}", artifactName);
                result.Warnings.Add($"Failed to promote artifact {artifactName}: {ex.Message}");
                result.PartialSuccess = true;
            }
        }
        
        // 5. Delete artifacts
        foreach (var artifactName in diff.ToDeleteArtifacts)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _artifactsClient.DeleteArtifactAsync(spec.Spec.RoomId, artifactName, ct);
                }, ct);
                
                _auditLog.LogEvent("artifact.deleted", result.CorrelationId, _operatorVersion, spec.Metadata.Version,
                    new Dictionary<string, object> { ["artifactName"] = artifactName });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete artifact {Name}", artifactName);
                result.Warnings.Add($"Failed to delete artifact {artifactName}: {ex.Message}");
                result.PartialSuccess = true;
            }
        }
        
        // 6. Apply policies
        // Always apply dmVisibilityDefault policy, using default value if null or empty
        var dmVisibilityDefaultValue = string.IsNullOrEmpty(spec.Spec.Policies.DmVisibilityDefault)
            ? Constants.DefaultDmVisibility
            : spec.Spec.Policies.DmVisibilityDefault;
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _policiesClient.ApplyPolicyAsync(spec.Spec.RoomId, "dmVisibilityDefault", 
                    dmVisibilityDefaultValue, ct);
            }, ct);
            
            _auditLog.LogEvent("policy.applied", result.CorrelationId, _operatorVersion, spec.Metadata.Version,
                new Dictionary<string, object> { ["policyName"] = "dmVisibilityDefault" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply policy dmVisibilityDefault");
            result.Warnings.Add($"Failed to apply policy: {ex.Message}");
            result.PartialSuccess = true;
        }
    }
    
    private async Task VerifyAsync(RoomSpec spec, ReconcileResult result, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("State revalidated before VERIFY");
            var finalState = await _roomClient.GetStateAsync(spec.Spec.RoomId, ct);
            var finalDiff = _diffEngine.CalculateDiff(spec, finalState);
            
            var hasChanges = finalDiff.ToJoin.Any() || finalDiff.ToKick.Any() || 
                           finalDiff.ToSeed.Any() || finalDiff.ToDeleteArtifacts.Any();
            
            if (hasChanges)
            {
                result.Warnings.Add("State not fully converged after reconciliation");
            }
            else
            {
                _logger.LogInformation("State converged successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify final state");
            result.Warnings.Add("Verification failed");
        }
    }
}
