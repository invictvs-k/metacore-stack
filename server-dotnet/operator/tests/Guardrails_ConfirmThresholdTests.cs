using Microsoft.Extensions.Logging;
using Moq;
using RoomOperator.Abstractions;
using RoomOperator.Core;

namespace RoomOperator.Tests;

public class Guardrails_ConfirmThresholdTests
{
  [Fact]
  public void Apply_Requires_Confirm_When_ChangeThresholdExceeded()
  {
    // Arrange
    var config = new GuardrailsConfig
    {
      ChangeThreshold = 0.5,
      RequireConfirmHeader = true,
      MaxEntitiesKickPerCycle = 10,
      MaxArtifactsDeletePerCycle = 10
    };

    var mockLogger = new Mock<ILogger<Guardrails>>();
    var guardrails = new Guardrails(config, mockLogger.Object);

    var diff = new ReconcileDiff
    {
      ToKick = new List<string> { "E-1", "E-2", "E-3", "E-4", "E-5", "E-6" },
      ToDeleteArtifacts = new List<string>()
    };

    var state = new RoomState
    {
      Entities = Enumerable.Range(1, 10).Select(i => new EntityState { Id = $"E-{i}" }).ToList(),
      Artifacts = new List<ArtifactState>()
    };

    // Act - without confirm
    var resultWithoutConfirm = guardrails.Check(diff, state, confirmProvided: false);

    // Assert
    Assert.False(resultWithoutConfirm.Passed);
    Assert.Contains(resultWithoutConfirm.Violations, v => v.Contains("X-Confirm:true"));
  }

  [Fact]
  public void Apply_Succeeds_When_ChangeThresholdExceeded_And_ConfirmProvided()
  {
    // Arrange
    var config = new GuardrailsConfig
    {
      ChangeThreshold = 0.5,
      RequireConfirmHeader = true,
      MaxEntitiesKickPerCycle = 10,
      MaxArtifactsDeletePerCycle = 10
    };

    var mockLogger = new Mock<ILogger<Guardrails>>();
    var guardrails = new Guardrails(config, mockLogger.Object);

    var diff = new ReconcileDiff
    {
      ToKick = new List<string> { "E-1", "E-2", "E-3", "E-4", "E-5", "E-6" },
      ToDeleteArtifacts = new List<string>()
    };

    var state = new RoomState
    {
      Entities = Enumerable.Range(1, 10).Select(i => new EntityState { Id = $"E-{i}" }).ToList(),
      Artifacts = new List<ArtifactState>()
    };

    // Act - with confirm
    var resultWithConfirm = guardrails.Check(diff, state, confirmProvided: true);

    // Assert
    Assert.True(resultWithConfirm.Passed);
    Assert.Contains(resultWithConfirm.Warnings, w => w.Contains("High change ratio"));
  }

  [Fact]
  public void Apply_Fails_When_KickLimitExceeded()
  {
    // Arrange
    var config = new GuardrailsConfig
    {
      MaxEntitiesKickPerCycle = 3,
      MaxArtifactsDeletePerCycle = 10,
      ChangeThreshold = 1.0
    };

    var mockLogger = new Mock<ILogger<Guardrails>>();
    var guardrails = new Guardrails(config, mockLogger.Object);

    var diff = new ReconcileDiff
    {
      ToKick = new List<string> { "E-1", "E-2", "E-3", "E-4", "E-5" },
      ToDeleteArtifacts = new List<string>()
    };

    var state = new RoomState
    {
      Entities = Enumerable.Range(1, 10).Select(i => new EntityState { Id = $"E-{i}" }).ToList(),
      Artifacts = new List<ArtifactState>()
    };

    // Act
    var result = guardrails.Check(diff, state, confirmProvided: true);

    // Assert
    Assert.False(result.Passed);
    Assert.Contains(result.Violations, v => v.Contains("Kick count"));
  }
}
