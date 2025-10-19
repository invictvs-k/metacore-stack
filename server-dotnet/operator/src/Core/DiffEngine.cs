using RoomOperator.Abstractions;

namespace RoomOperator.Core;

public sealed class DiffEngine
{
    private readonly ILogger<DiffEngine> _logger;
    
    public DiffEngine(ILogger<DiffEngine> logger)
    {
        _logger = logger;
    }
    
    public ReconcileDiff CalculateDiff(RoomSpec spec, RoomState state)
    {
        var diff = new ReconcileDiff();
        
        // Calculate entity differences
        var desiredEntityIds = spec.Spec.Entities.Select(e => e.Id).ToHashSet();
        var currentEntityIds = state.Entities.Select(e => e.Id).ToHashSet();
        
        diff.ToJoin = desiredEntityIds.Except(currentEntityIds).ToList();
        diff.ToKick = currentEntityIds.Except(desiredEntityIds).ToList();
        diff.ToEnsure = desiredEntityIds.Intersect(currentEntityIds).ToList();
        
        // Calculate artifact differences
        var desiredArtifactNames = spec.Spec.Artifacts.Select(a => a.Name).ToHashSet();
        var currentArtifactNames = state.Artifacts.Select(a => a.Name).ToHashSet();
        
        diff.ToSeed = spec.Spec.Artifacts
            .Where(a => !string.IsNullOrEmpty(a.SeedFrom))
            .Select(a => a.Name)
            .ToList();
            
        diff.ToPromote = spec.Spec.Artifacts
            .Where(a => a.PromoteAfterSeed)
            .Select(a => a.Name)
            .ToList();
            
        diff.ToDeleteArtifacts = currentArtifactNames.Except(desiredArtifactNames).ToList();
        
        // Calculate policy differences
        if (!string.IsNullOrEmpty(spec.Spec.Policies.DmVisibilityDefault))
        {
            diff.ToApply.Add($"policy:dmVisibilityDefault={spec.Spec.Policies.DmVisibilityDefault}");
        }
        
        _logger.LogInformation(
            "Diff calculated: ToJoin={ToJoin}, ToKick={ToKick}, ToSeed={ToSeed}, ToPromote={ToPromote}, ToDeleteArtifacts={ToDelete}",
            diff.ToJoin.Count, diff.ToKick.Count, diff.ToSeed.Count, diff.ToPromote.Count, diff.ToDeleteArtifacts.Count);
        
        return diff;
    }
    
    public double CalculateChangeRatio(ReconcileDiff diff, RoomState state)
    {
        var totalCurrent = state.Entities.Count + state.Artifacts.Count;
        if (totalCurrent == 0) return 0;
        
        var totalChanges = diff.ToKick.Count + diff.ToDeleteArtifacts.Count;
        return (double)totalChanges / totalCurrent;
    }
}
