using Microsoft.Extensions.Logging;
using Moq;
using RoomOperator.Abstractions;
using RoomOperator.Core;

namespace RoomOperator.Tests;

public class Entities_Reconcile_IdempotencyTests
{
  [Fact]
  public void GivenMissingEntities_WhenReconcile_ThenJoinOnce()
  {
    // Arrange
    var mockRoomClient = new Mock<IRoomClient>();
    var mockLogger = new Mock<ILogger<DiffEngine>>();
    var diffEngine = new DiffEngine(mockLogger.Object);

    var spec = new RoomSpec
    {
      Spec = new RoomSpecData
      {
        RoomId = "test-room",
        Entities = new List<EntitySpec>
                {
                    new() { Id = "E-agent-1", Kind = "agent", DisplayName = "Agent 1" }
                }
      }
    };

    var state = new RoomState
    {
      RoomId = "test-room",
      Entities = new List<EntityState>()
    };

    // Act
    var diff = diffEngine.CalculateDiff(spec, state);

    // Assert
    Assert.Single(diff.ToJoin);
    Assert.Contains("E-agent-1", diff.ToJoin);
    Assert.Empty(diff.ToKick);
  }

  [Fact]
  public void GivenExtraEntities_WhenReconcile_ThenKickOnce()
  {
    // Arrange
    var mockLogger = new Mock<ILogger<DiffEngine>>();
    var diffEngine = new DiffEngine(mockLogger.Object);

    var spec = new RoomSpec
    {
      Spec = new RoomSpecData
      {
        RoomId = "test-room",
        Entities = new List<EntitySpec>()
      }
    };

    var state = new RoomState
    {
      RoomId = "test-room",
      Entities = new List<EntityState>
            {
                new() { Id = "E-agent-extra", Kind = "agent" }
            }
    };

    // Act
    var diff = diffEngine.CalculateDiff(spec, state);

    // Assert
    Assert.Empty(diff.ToJoin);
    Assert.Single(diff.ToKick);
    Assert.Contains("E-agent-extra", diff.ToKick);
  }

  [Fact]
  public void GivenMatchingEntities_WhenReconcile_ThenNoDiff()
  {
    // Arrange
    var mockLogger = new Mock<ILogger<DiffEngine>>();
    var diffEngine = new DiffEngine(mockLogger.Object);

    var spec = new RoomSpec
    {
      Spec = new RoomSpecData
      {
        RoomId = "test-room",
        Entities = new List<EntitySpec>
                {
                    new() { Id = "E-agent-1", Kind = "agent" }
                }
      }
    };

    var state = new RoomState
    {
      RoomId = "test-room",
      Entities = new List<EntityState>
            {
                new() { Id = "E-agent-1", Kind = "agent" }
            }
    };

    // Act
    var diff = diffEngine.CalculateDiff(spec, state);

    // Assert
    Assert.Empty(diff.ToJoin);
    Assert.Empty(diff.ToKick);
    Assert.Single(diff.ToEnsure);
  }
}
