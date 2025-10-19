using RoomOperator.Abstractions;
using RoomOperator.Abstractions.Validation;

namespace RoomOperator.Tests;

public class Policies_MinimalTests
{
    [Fact]
    public void GivenMissingPolicy_WhenReconcile_ThenApplyDefault()
    {
        // Arrange
        var validator = new RoomSpecValidator();
        
        var spec = new RoomSpec
        {
            Metadata = new SpecMetadata { Name = "test", Version = 1 },
            Spec = new RoomSpecData
            {
                RoomId = "test-room",
                Policies = new GlobalPolicies
                {
                    DmVisibilityDefault = "" // Empty, should warn
                }
            }
        };
        
        // Act
        var result = validator.Validate(spec);
        
        // Assert
        Assert.True(result.IsValid); // Still valid, but should have warning
        Assert.Contains(result.Warnings, w => w.Contains("dmVisibilityDefault"));
    }
    
    [Fact]
    public void GivenPolicySet_WhenValidate_ThenAccepted()
    {
        // Arrange
        var validator = new RoomSpecValidator();
        
        var spec = new RoomSpec
        {
            Metadata = new SpecMetadata { Name = "test", Version = 1 },
            Spec = new RoomSpecData
            {
                RoomId = "test-room",
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
        Assert.DoesNotContain(result.Warnings, w => w.Contains("dmVisibilityDefault"));
    }
}
