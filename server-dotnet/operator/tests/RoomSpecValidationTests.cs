using RoomOperator.Abstractions;
using RoomOperator.Abstractions.Validation;

namespace RoomOperator.Tests;

public class RoomSpecValidationTests
{
    [Fact]
    public void ValidSpec_PassesValidation()
    {
        // Arrange
        var validator = new RoomSpecValidator();
        var spec = new RoomSpec
        {
            Metadata = new SpecMetadata
            {
                Name = "test-room",
                Version = 1
            },
            Spec = new RoomSpecData
            {
                RoomId = "test-room-01",
                Entities = new List<EntitySpec>
                {
                    new() { Id = "E-agent-1", Kind = "agent", DisplayName = "Test Agent" }
                },
                Policies = new GlobalPolicies
                {
                    DmVisibilityDefault = "team"
                }
            }
        };
        
        // Act
        var result = validator.Validate(spec);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
    
    [Fact]
    public void MissingRoomId_FailsValidation()
    {
        // Arrange
        var validator = new RoomSpecValidator();
        var spec = new RoomSpec
        {
            Metadata = new SpecMetadata { Name = "test", Version = 1 },
            Spec = new RoomSpecData
            {
                RoomId = ""
            }
        };
        
        // Act
        var result = validator.Validate(spec);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("RoomId"));
    }
    
    [Fact]
    public void DuplicateEntityIds_FailsValidation()
    {
        // Arrange
        var validator = new RoomSpecValidator();
        var spec = new RoomSpec
        {
            Metadata = new SpecMetadata { Name = "test", Version = 1 },
            Spec = new RoomSpecData
            {
                RoomId = "test-room",
                Entities = new List<EntitySpec>
                {
                    new() { Id = "E-agent-1", Kind = "agent" },
                    new() { Id = "E-agent-1", Kind = "agent" }
                }
            }
        };
        
        // Act
        var result = validator.Validate(spec);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate entity Id"));
    }
    
    [Fact]
    public void InvalidEntityKind_FailsValidation()
    {
        // Arrange
        var validator = new RoomSpecValidator();
        var spec = new RoomSpec
        {
            Metadata = new SpecMetadata { Name = "test", Version = 1 },
            Spec = new RoomSpecData
            {
                RoomId = "test-room",
                Entities = new List<EntitySpec>
                {
                    new() { Id = "E-invalid-1", Kind = "invalid_kind" }
                }
            }
        };
        
        // Act
        var result = validator.Validate(spec);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Invalid entity kind"));
    }
    
    [Fact]
    public void OwnerVisibilityWithoutUserId_FailsValidation()
    {
        // Arrange
        var validator = new RoomSpecValidator();
        var spec = new RoomSpec
        {
            Metadata = new SpecMetadata { Name = "test", Version = 1 },
            Spec = new RoomSpecData
            {
                RoomId = "test-room",
                Entities = new List<EntitySpec>
                {
                    new() { Id = "E-human-1", Kind = "human", Visibility = "owner" }
                }
            }
        };
        
        // Act
        var result = validator.Validate(spec);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("requires OwnerUserId"));
    }
}
