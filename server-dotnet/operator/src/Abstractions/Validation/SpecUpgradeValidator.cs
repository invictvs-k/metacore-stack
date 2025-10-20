namespace RoomOperator.Abstractions.Validation;

public sealed class SpecUpgradeValidator
{
  public UpgradeValidationResult ValidateUpgrade(RoomSpec currentSpec, RoomSpec newSpec)
  {
    var result = new UpgradeValidationResult
    {
      CurrentVersion = currentSpec.Metadata.Version,
      NewVersion = newSpec.Metadata.Version
    };

    if (newSpec.Metadata.Version <= currentSpec.Metadata.Version)
    {
      result.AddError($"New version ({newSpec.Metadata.Version}) must be greater than current version ({currentSpec.Metadata.Version})");
      return result;
    }

    if (newSpec.Spec.RoomId != currentSpec.Spec.RoomId)
    {
      result.AddError("RoomId cannot be changed");
      return result;
    }

    // Check for breaking changes
    CheckBreakingEntityChanges(currentSpec, newSpec, result);
    CheckBreakingArtifactChanges(currentSpec, newSpec, result);

    return result;
  }

  private void CheckBreakingEntityChanges(RoomSpec current, RoomSpec newSpec, UpgradeValidationResult result)
  {
    var currentEntityIds = current.Spec.Entities.Select(e => e.Id).ToHashSet();
    var newEntityIds = newSpec.Spec.Entities.Select(e => e.Id).ToHashSet();

    var removed = currentEntityIds.Except(newEntityIds).ToList();
    if (removed.Count > 0)
    {
      result.AddWarning($"Entities will be removed: {string.Join(", ", removed)}");
    }
  }

  private void CheckBreakingArtifactChanges(RoomSpec current, RoomSpec newSpec, UpgradeValidationResult result)
  {
    var currentArtifactNames = current.Spec.Artifacts.Select(a => a.Name).ToHashSet();
    var newArtifactNames = newSpec.Spec.Artifacts.Select(a => a.Name).ToHashSet();

    var removed = currentArtifactNames.Except(newArtifactNames).ToList();
    if (removed.Count > 0)
    {
      result.AddWarning($"Artifacts will be removed: {string.Join(", ", removed)}");
    }
  }
}

public sealed class UpgradeValidationResult
{
  public int CurrentVersion { get; set; }
  public int NewVersion { get; set; }
  public List<string> Errors { get; } = new();
  public List<string> Warnings { get; } = new();

  public bool IsValid => Errors.Count == 0;

  public void AddError(string message) => Errors.Add(message);
  public void AddWarning(string message) => Warnings.Add(message);
}
