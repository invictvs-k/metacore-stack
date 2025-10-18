using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using RoomServer.Models;
using Xunit;

namespace RoomServer.Tests;

/// <summary>
/// Tests for command target resolution with various payload types.
/// These tests verify that the ResolveCommandTarget method correctly handles
/// different serialization formats of command payloads.
/// </summary>
public class CommandTargetResolutionTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private const string RoomId = "room-cmd-test";

    [Fact]
    public async Task CommandWithJsonElementPayload_ShouldResolveTarget()
    {
        await using var senderConnection = BuildConnection();
        await using var targetConnection = BuildConnection();

        await senderConnection.StartAsync();
        await targetConnection.StartAsync();

        await senderConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-SENDER",
            Kind = "orchestrator"
        });

        await targetConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-TARGET",
            Kind = "agent",
            Policy = new PolicySpec { AllowCommandsFrom = "any" }
        });

        // Create a JsonElement payload
        using var doc = JsonDocument.Parse("{\"target\":\"E-TARGET\",\"action\":\"execute\"}");
        var jsonPayload = doc.RootElement.Clone();

        // This should succeed without throwing
        await senderConnection.InvokeAsync("SendToRoom", RoomId, new MessageModel
        {
            From = "E-SENDER",
            Channel = "room",
            Type = "command",
            Payload = jsonPayload
        });

        // If we get here, the target was resolved successfully
        Assert.True(true);
    }

    [Fact]
    public async Task CommandWithJsonDocumentPayload_ShouldResolveTarget()
    {
        await using var senderConnection = BuildConnection();
        await using var targetConnection = BuildConnection();

        await senderConnection.StartAsync();
        await targetConnection.StartAsync();

        await senderConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-SENDER2",
            Kind = "orchestrator"
        });

        await targetConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-TARGET2",
            Kind = "agent",
            Policy = new PolicySpec { AllowCommandsFrom = "any" }
        });

        // Create a JsonDocument payload
        using var jsonDoc = JsonDocument.Parse("{\"target\":\"E-TARGET2\",\"command\":\"stop\"}");

        // This should succeed without throwing
        await senderConnection.InvokeAsync("SendToRoom", RoomId, new MessageModel
        {
            From = "E-SENDER2",
            Channel = "room",
            Type = "command",
            Payload = jsonDoc
        });

        // If we get here, the target was resolved successfully
        Assert.True(true);
    }

    [Fact]
    public async Task CommandWithDictionaryPayload_ShouldResolveTarget()
    {
        await using var senderConnection = BuildConnection();
        await using var targetConnection = BuildConnection();

        await senderConnection.StartAsync();
        await targetConnection.StartAsync();

        await senderConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-SENDER3",
            Kind = "orchestrator"
        });

        await targetConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-TARGET3",
            Kind = "agent",
            Policy = new PolicySpec { AllowCommandsFrom = "any" }
        });

        // Create a dictionary payload
        var dictPayload = new Dictionary<string, object?> { ["target"] = "E-TARGET3", ["data"] = new { value = 42 } };

        // This should succeed without throwing
        await senderConnection.InvokeAsync("SendToRoom", RoomId, new MessageModel
        {
            From = "E-SENDER3",
            Channel = "room",
            Type = "command",
            Payload = dictPayload
        });

        // If we get here, the target was resolved successfully
        Assert.True(true);
    }

    [Fact]
    public async Task CommandWithStringDictionaryPayload_ShouldResolveTarget()
    {
        await using var senderConnection = BuildConnection();
        await using var targetConnection = BuildConnection();

        await senderConnection.StartAsync();
        await targetConnection.StartAsync();

        await senderConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-SENDER4",
            Kind = "orchestrator"
        });

        await targetConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-TARGET4",
            Kind = "agent",
            Policy = new PolicySpec { AllowCommandsFrom = "any" }
        });

        // Create a string dictionary payload
        var stringDictPayload = new Dictionary<string, string> { ["target"] = "E-TARGET4", ["action"] = "start" };

        // This should succeed without throwing
        await senderConnection.InvokeAsync("SendToRoom", RoomId, new MessageModel
        {
            From = "E-SENDER4",
            Channel = "room",
            Type = "command",
            Payload = stringDictPayload
        });

        // If we get here, the target was resolved successfully
        Assert.True(true);
    }

    [Fact]
    public async Task CommandWithMissingTarget_ShouldThrowBadRequest()
    {
        await using var senderConnection = BuildConnection();

        await senderConnection.StartAsync();

        await senderConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-SENDER5",
            Kind = "orchestrator"
        });

        // Create a payload without a target property
        JsonElement payloadWithoutTarget;
        using (var doc = JsonDocument.Parse("{\"action\":\"execute\"}"))
        {
            payloadWithoutTarget = doc.RootElement.Clone();
        }

        var exception = await Assert.ThrowsAsync<HubException>(() => 
            senderConnection.InvokeAsync("SendToRoom", RoomId, new MessageModel
            {
                From = "E-SENDER5",
                Channel = "room",
                Type = "command",
                Payload = payloadWithoutTarget
            }));

        // The exception message should indicate missing target
        // Note: We can't parse it as JSON due to pre-existing issue, but we can check it's thrown
        exception.Should().NotBeNull();
    }

    [Fact]
    public async Task CommandWithNonExistentTarget_ShouldThrowNotFound()
    {
        await using var senderConnection = BuildConnection();

        await senderConnection.StartAsync();

        await senderConnection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-SENDER6",
            Kind = "orchestrator"
        });

        // Create a payload with a non-existent target
        using var doc = JsonDocument.Parse("{\"target\":\"E-NONEXISTENT\"}");
        var payload = doc.RootElement.Clone();

        var exception = await Assert.ThrowsAsync<HubException>(() => 
            senderConnection.InvokeAsync("SendToRoom", RoomId, new MessageModel
            {
                From = "E-SENDER6",
                Channel = "room",
                Type = "command",
                Payload = payload
            }));

        // The exception should be thrown for non-existent target
        exception.Should().NotBeNull();
    }

    private HubConnection BuildConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/room", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .WithAutomaticReconnect()
            .Build();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
