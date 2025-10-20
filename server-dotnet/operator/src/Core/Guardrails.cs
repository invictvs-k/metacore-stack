using RoomOperator.Abstractions;

namespace RoomOperator.Core;

public sealed class GuardrailsConfig
{
  public int MaxEntitiesKickPerCycle { get; set; } = 5;
  public int MaxArtifactsDeletePerCycle { get; set; } = 10;
  public double ChangeThreshold { get; set; } = 0.5;
  public bool RequireConfirmHeader { get; set; } = true;
}

public sealed class Guardrails
{
  private readonly GuardrailsConfig _config;
  private readonly ILogger<Guardrails> _logger;

  public Guardrails(GuardrailsConfig config, ILogger<Guardrails> logger)
  {
    _config = config;
    _logger = logger;
  }

  public GuardrailsResult Check(ReconcileDiff diff, RoomState state, bool confirmProvided)
  {
    var result = new GuardrailsResult { Passed = true };

    // Check entity kick limit
    if (diff.ToKick.Count > _config.MaxEntitiesKickPerCycle)
    {
      result.Passed = false;
      result.Violations.Add($"Kick count ({diff.ToKick.Count}) exceeds limit ({_config.MaxEntitiesKickPerCycle})");
    }

    // Check artifact delete limit
    if (diff.ToDeleteArtifacts.Count > _config.MaxArtifactsDeletePerCycle)
    {
      result.Passed = false;
      result.Violations.Add($"Artifact delete count ({diff.ToDeleteArtifacts.Count}) exceeds limit ({_config.MaxArtifactsDeletePerCycle})");
    }

    // Check change threshold
    var changeRatio = CalculateChangeRatio(diff, state);
    if (changeRatio > _config.ChangeThreshold)
    {
      if (_config.RequireConfirmHeader && !confirmProvided)
      {
        result.Passed = false;
        result.Violations.Add($"Change ratio ({changeRatio:P0}) exceeds threshold ({_config.ChangeThreshold:P0}). X-Confirm:true header required.");
      }
      else
      {
        result.Warnings.Add($"High change ratio ({changeRatio:P0}) detected but confirmed");
      }
    }

    if (!result.Passed)
    {
      _logger.LogWarning("Guardrails check failed: {Violations}", string.Join("; ", result.Violations));
    }

    return result;
  }

  private double CalculateChangeRatio(ReconcileDiff diff, RoomState state)
  {
    var totalCurrent = state.Entities.Count + state.Artifacts.Count;
    if (totalCurrent == 0) return 0;

    var totalChanges = diff.ToKick.Count + diff.ToDeleteArtifacts.Count;
    return (double)totalChanges / totalCurrent;
  }
}

public sealed class GuardrailsResult
{
  public bool Passed { get; set; }
  public List<string> Violations { get; set; } = new();
  public List<string> Warnings { get; set; } = new();
}
