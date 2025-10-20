using RoomOperator.Abstractions;
using RoomOperator.Clients;
using System.Text;

namespace RoomOperator.Tests;

public class Artifacts_FingerprintTests
{
  [Fact]
  public void GivenSeededArtifact_WhenHashUnchanged_ThenFingerprintMatches()
  {
    // Arrange
    var spec = new ArtifactSeedSpec
    {
      Name = "test-artifact",
      Type = "document",
      Workspace = "shared",
      Tags = new List<string> { "tag1", "tag2" }
    };

    var content = Encoding.UTF8.GetBytes("test content");

    // Act
    var fingerprint1 = ArtifactsClient.BuildFingerprint(spec, content);
    var fingerprint2 = ArtifactsClient.BuildFingerprint(spec, content);

    // Assert
    Assert.Equal(fingerprint1, fingerprint2);
  }

  [Fact]
  public void GivenSeededArtifact_WhenContentDiffers_ThenFingerprintDiffers()
  {
    // Arrange
    var spec = new ArtifactSeedSpec
    {
      Name = "test-artifact",
      Type = "document",
      Workspace = "shared",
      Tags = new List<string> { "tag1" }
    };

    var content1 = Encoding.UTF8.GetBytes("content version 1");
    var content2 = Encoding.UTF8.GetBytes("content version 2");

    // Act
    var fingerprint1 = ArtifactsClient.BuildFingerprint(spec, content1);
    var fingerprint2 = ArtifactsClient.BuildFingerprint(spec, content2);

    // Assert
    Assert.NotEqual(fingerprint1, fingerprint2);
  }

  [Fact]
  public void GivenSeededArtifact_WhenTagsDiffer_ThenFingerprintDiffers()
  {
    // Arrange
    var spec1 = new ArtifactSeedSpec
    {
      Name = "test-artifact",
      Type = "document",
      Workspace = "shared",
      Tags = new List<string> { "tag1" }
    };

    var spec2 = new ArtifactSeedSpec
    {
      Name = "test-artifact",
      Type = "document",
      Workspace = "shared",
      Tags = new List<string> { "tag1", "tag2" }
    };

    var content = Encoding.UTF8.GetBytes("same content");

    // Act
    var fingerprint1 = ArtifactsClient.BuildFingerprint(spec1, content);
    var fingerprint2 = ArtifactsClient.BuildFingerprint(spec2, content);

    // Assert
    Assert.NotEqual(fingerprint1, fingerprint2);
  }
}
