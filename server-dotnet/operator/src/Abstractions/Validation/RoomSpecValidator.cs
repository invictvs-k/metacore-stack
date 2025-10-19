namespace RoomOperator.Abstractions.Validation;

public sealed class RoomSpecValidator
{
    public ValidationResult Validate(RoomSpec spec)
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrWhiteSpace(spec.Spec.RoomId))
        {
            result.AddError("RoomId is required");
        }
        
        if (spec.Metadata.Version <= 0)
        {
            result.AddError("Spec version must be positive");
        }
        
        // Validate entities
        var entityIds = new HashSet<string>();
        foreach (var entity in spec.Spec.Entities)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                result.AddError("Entity Id is required");
            }
            else if (!entityIds.Add(entity.Id))
            {
                result.AddError($"Duplicate entity Id: {entity.Id}");
            }
            
            if (!IsValidEntityKind(entity.Kind))
            {
                result.AddError($"Invalid entity kind: {entity.Kind}");
            }
            
            if (entity.Visibility == "owner" && string.IsNullOrWhiteSpace(entity.OwnerUserId))
            {
                result.AddError($"Entity {entity.Id} with visibility=owner requires OwnerUserId");
            }
        }
        
        // Validate artifacts
        var artifactNames = new HashSet<string>();
        foreach (var artifact in spec.Spec.Artifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.Name))
            {
                result.AddError("Artifact Name is required");
            }
            else if (!artifactNames.Add(artifact.Name))
            {
                result.AddError($"Duplicate artifact name: {artifact.Name}");
            }
            
            if (string.IsNullOrWhiteSpace(artifact.Type))
            {
                result.AddError($"Artifact {artifact.Name} requires Type");
            }
            
            if (string.IsNullOrWhiteSpace(artifact.Workspace))
            {
                result.AddError($"Artifact {artifact.Name} requires Workspace");
            }
        }
        
        // Validate mandatory policies
        if (string.IsNullOrWhiteSpace(spec.Spec.Policies.DmVisibilityDefault))
        {
            result.AddWarning($"dmVisibilityDefault not set, will default to '{Constants.DefaultDmVisibility}'");
        }
        
        return result;
    }
    
    private bool IsValidEntityKind(string kind)
    {
        return kind.ToLowerInvariant() switch
        {
            "human" => true,
            "agent" => true,
            "npc" => true,
            "orchestrator" => true,
            _ => false
        };
    }
}

public sealed class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public bool IsValid => Errors.Count == 0;
    
    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);
}
