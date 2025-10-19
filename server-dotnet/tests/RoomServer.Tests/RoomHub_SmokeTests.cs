using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using RoomServer.Models;
using Xunit;

namespace RoomServer.Tests;

public class RoomHub_SmokeTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private const string RoomId = "room-test01";

    [Fact]
    public async Task JoinBroadcastsPresence()
    {
        await using var connectionA = BuildConnection();
        await using var connectionB = BuildConnection();

        var joinReceived = new TaskCompletionSource<RoomEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        connectionA.On<RoomEvent>("event", evt =>
        {
            if (evt.Payload.Kind == "ENTITY.JOIN" &&
                evt.Payload.Data.TryGetProperty("entity", out var entity) &&
                entity.GetProperty("id").GetString() == "E-Bob")
            {
                joinReceived.TrySetResult(evt);
            }
        });

        await connectionA.StartAsync();
        await connectionA.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-Alice",
            Kind = "human",
            DisplayName = "Alice"
        });

        await connectionB.StartAsync();
        await connectionB.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-Bob",
            Kind = "agent",
            DisplayName = "Bot"
        });

        var joinEvent = await joinReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        joinEvent.Payload.Kind.Should().Be("ENTITY.JOIN");
        joinEvent.RoomId.Should().Be(RoomId);
    }

    [Fact]
    public async Task SendMessageIsBroadcastToRoom()
    {
        await using var connectionA = BuildConnection();
        await using var connectionB = BuildConnection();

        var messageReceived = new TaskCompletionSource<MessageModel>(TaskCreationOptions.RunContinuationsAsynchronously);

        connectionB.On<MessageModel>("message", message =>
        {
            if (message.From == "E-Alice")
            {
                messageReceived.TrySetResult(message);
            }
        });

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        await connectionA.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-Alice",
            Kind = "human",
            DisplayName = "Alice"
        });

        await connectionB.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-Bob",
            Kind = "agent",
            DisplayName = "Bot"
        });

        await connectionA.InvokeAsync("SendToRoom", RoomId, new MessageModel
        {
            From = "E-Alice",
            Channel = "room",
            Type = "chat",
            Payload = new { text = "Olá!" }
        });

        var message = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        message.RoomId.Should().Be(RoomId);
        message.Type.Should().Be("chat");
        message.Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task LeaveBroadcastsEvent()
    {
        await using var connectionA = BuildConnection();
        await using var connectionB = BuildConnection();

        var leaveReceived = new TaskCompletionSource<RoomEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        connectionA.On<RoomEvent>("event", evt =>
        {
            if (evt.Payload.Kind == "ENTITY.LEAVE" &&
                evt.Payload.Data.TryGetProperty("entityId", out var entity) &&
                entity.GetString() == "E-Bob")
            {
                leaveReceived.TrySetResult(evt);
            }
        });

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        await connectionA.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-Alice",
            Kind = "human",
            DisplayName = "Alice"
        });

        await connectionB.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-Bob",
            Kind = "agent",
            DisplayName = "Bot"
        });

        await connectionB.InvokeAsync("Leave", RoomId, "E-Bob");

        var leaveEvent = await leaveReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        leaveEvent.Payload.Kind.Should().Be("ENTITY.LEAVE");
        leaveEvent.RoomId.Should().Be(RoomId);
    }

    [Fact]
    public async Task DisconnectPublishesLeaveEvent()
    {
        await using var connectionA = BuildConnection();
        await using var connectionB = BuildConnection();

        var leaveReceived = new TaskCompletionSource<RoomEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        connectionA.On<RoomEvent>("event", evt =>
        {
            if (evt.Payload.Kind == "ENTITY.LEAVE" &&
                evt.Payload.Data.TryGetProperty("entityId", out var entity) &&
                entity.GetString() == "E-Bob")
            {
                leaveReceived.TrySetResult(evt);
            }
        });

        await connectionA.StartAsync();
        await connectionB.StartAsync();

        await connectionA.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-Alice",
            Kind = "human",
            DisplayName = "Alice"
        });

        await connectionB.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-Bob",
            Kind = "agent",
            DisplayName = "Bot"
        });

        await connectionB.StopAsync();

        var leaveEvent = await leaveReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        leaveEvent.Payload.Kind.Should().Be("ENTITY.LEAVE");
        leaveEvent.RoomId.Should().Be(RoomId);
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

    private sealed record RoomEvent(string Id, string RoomId, string Type, EventPayload Payload, DateTime Ts);

    private sealed record EventPayload(string Kind, JsonElement Data);
}
