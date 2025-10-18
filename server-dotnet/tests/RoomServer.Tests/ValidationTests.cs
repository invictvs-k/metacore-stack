using System;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using RoomServer.Models;

namespace RoomServer.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("room-abc123", true)]
    [InlineData("room-ABC_XYZ-123", true)]
    [InlineData("room-12345", false)] // Too short (< 6 chars)
    [InlineData("room", false)] // Missing dash and ID
    [InlineData("abc-123456", false)] // Wrong prefix
    public void RoomId_Validation_WorksCorrectly(string roomId, bool shouldBeValid)
    {
        ValidationHelper.IsValidRoomId(roomId).Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("E-A", true)]
    [InlineData("E-ABCD1234", true)]
    [InlineData("E-Test_Entity-123", true)]
    [InlineData("E-", false)] // No ID after prefix
    [InlineData("Entity-123", false)] // Wrong prefix
    [InlineData("E123", false)] // Missing dash
    public void EntityId_Validation_WorksCorrectly(string entityId, bool shouldBeValid)
    {
        ValidationHelper.IsValidEntityId(entityId).Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("test", true)]
    [InlineData("test.port", true)]
    [InlineData("test.port.sub", true)]
    [InlineData("a", true)]
    [InlineData("Test", false)] // Must be lowercase
    [InlineData("test-port", false)] // No dashes allowed
    [InlineData("1test", false)] // Must start with letter
    public void PortId_Validation_WorksCorrectly(string portId, bool shouldBeValid)
    {
        ValidationHelper.IsValidPortId(portId).Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("human", true)]
    [InlineData("agent", true)]
    [InlineData("npc", true)]
    [InlineData("orchestrator", true)]
    [InlineData("Human", true)] // Case insensitive
    [InlineData("AGENT", true)] // Case insensitive
    [InlineData("robot", false)]
    [InlineData("", false)]
    public void EntityKind_Validation_WorksCorrectly(string kind, bool shouldBeValid)
    {
        ValidationHelper.IsValidEntityKind(kind).Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("ENTITY.JOIN", true)]
    [InlineData("ROOM.STATE", true)]
    [InlineData("MY.CUSTOM.EVENT", true)]
    [InlineData("SIMPLE", true)]
    [InlineData("entity.join", false)] // Must be uppercase
    [InlineData("Entity.Join", false)] // Must be uppercase
    [InlineData("ENTITY_JOIN", false)] // Must use dots
    public void EventKind_Validation_WorksCorrectly(string kind, bool shouldBeValid)
    {
        ValidationHelper.IsValidEventKind(kind).Should().Be(shouldBeValid);
    }

    [Fact]
    public void ChatPayload_WithText_IsValid()
    {
        var payload = JsonDocument.Parse("{\"text\":\"Hello World\"}").RootElement;
        ValidationHelper.ValidateChatPayload(payload, out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ChatPayload_WithoutText_IsInvalid()
    {
        var payload = JsonDocument.Parse("{\"message\":\"Hello World\"}").RootElement;
        ValidationHelper.ValidateChatPayload(payload, out var error).Should().BeFalse();
        error.Should().NotBeNull();
    }

    [Fact]
    public void ChatPayload_NonJson_IsInvalid()
    {
        ValidationHelper.ValidateChatPayload(null!, out var errorNull).Should().BeFalse();
        errorNull.Should().Be("Chat payload is required");

        ValidationHelper.ValidateChatPayload("text", out var errorPrimitive).Should().BeFalse();
        errorPrimitive.Should().Be("Chat payload must be a JSON object");
    }

    [Fact]
    public void CommandPayload_WithTarget_IsValid()
    {
        var payload = JsonDocument.Parse("{\"target\":\"E-TARGET\",\"port\":\"test.port\",\"inputs\":{}}").RootElement;
        ValidationHelper.ValidateCommandPayload(payload, out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void CommandPayload_WithoutTarget_IsInvalid()
    {
        var payload = JsonDocument.Parse("{\"port\":\"test.port\",\"inputs\":{}}").RootElement;
        ValidationHelper.ValidateCommandPayload(payload, out var error).Should().BeFalse();
        error.Should().NotBeNull();
    }

    [Fact]
    public void CommandPayload_NonJson_IsInvalid()
    {
        ValidationHelper.ValidateCommandPayload(null!, out var errorNull).Should().BeFalse();
        errorNull.Should().Be("Command payload is required");

        ValidationHelper.ValidateCommandPayload(42, out var errorPrimitive).Should().BeFalse();
        errorPrimitive.Should().Be("Command payload must be a JSON object");
    }

    [Fact]
    public void EventPayload_WithValidKind_IsValid()
    {
        var payload = JsonDocument.Parse("{\"kind\":\"ENTITY.JOIN\",\"data\":{}}").RootElement;
        ValidationHelper.ValidateEventPayload(payload, out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void EventPayload_WithInvalidKind_IsInvalid()
    {
        var payload = JsonDocument.Parse("{\"kind\":\"entity.join\",\"data\":{}}").RootElement;
        ValidationHelper.ValidateEventPayload(payload, out var error).Should().BeFalse();
        error.Should().NotBeNull();
    }

    [Fact]
    public void EventPayload_NonJson_IsInvalid()
    {
        ValidationHelper.ValidateEventPayload(null!, out var errorNull).Should().BeFalse();
        errorNull.Should().Be("Event payload is required");

        ValidationHelper.ValidateEventPayload(true, out var errorPrimitive).Should().BeFalse();
        errorPrimitive.Should().Be("Event payload must be a JSON object");
    }

    [Fact]
    public void ArtifactPayload_WithManifest_IsValid()
    {
        var payload = JsonDocument.Parse("{\"manifest\":{\"name\":\"test.txt\"}}").RootElement;
        ValidationHelper.ValidateArtifactPayload(payload, out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ArtifactPayload_WithoutManifest_IsInvalid()
    {
        var payload = JsonDocument.Parse("{\"data\":{}}").RootElement;
        ValidationHelper.ValidateArtifactPayload(payload, out var error).Should().BeFalse();
        error.Should().NotBeNull();
    }

    [Fact]
    public void ArtifactPayload_NonJson_IsInvalid()
    {
        ValidationHelper.ValidateArtifactPayload(null!, out var errorNull).Should().BeFalse();
        errorNull.Should().Be("Artifact payload is required");

        ValidationHelper.ValidateArtifactPayload(3.14, out var errorPrimitive).Should().BeFalse();
        errorPrimitive.Should().Be("Artifact payload must be a JSON object");
    }
}
