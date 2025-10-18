using System;
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
/// Smoke tests for MCP Bridge functionality.
/// These tests verify the basic functionality without requiring actual MCP servers.
/// </summary>
public class McpBridge_SmokeTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private const string RoomId = "room-mcp-test";

    [Fact]
    public async Task ListTools_WithNoMcpServers_ReturnsEmptyArray()
    {
        await using var connection = BuildConnection();
        await connection.StartAsync();

        await connection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-TEST",
            Kind = "human",
            DisplayName = "Tester"
        });

        // ListTools should work even with no MCP servers configured
        var tools = await connection.InvokeAsync<CatalogItemDto[]>("ListTools", RoomId);

        tools.Should().NotBeNull();
        tools.Should().BeEmpty();
    }

    [Fact]
    public async Task ListTools_RequiresValidSession()
    {
        await using var connection = BuildConnection();
        await connection.StartAsync();

        // Try to list tools without joining the room first
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
        {
            await connection.InvokeAsync<CatalogItemDto[]>("ListTools", RoomId);
        });

        exception.Should().NotBeNull();
        exception.Message.Should().Contain("AUTH_REQUIRED");
    }

    [Fact]
    public async Task ListTools_RequiresMatchingRoomId()
    {
        await using var connection = BuildConnection();
        await connection.StartAsync();

        await connection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-TEST",
            Kind = "human",
            DisplayName = "Tester"
        });

        // Try to list tools for a different room
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
        {
            await connection.InvokeAsync<CatalogItemDto[]>("ListTools", "different-room");
        });

        exception.Should().NotBeNull();
        exception.Message.Should().Contain("PERM_DENIED");
    }

    [Fact]
    public async Task CallTool_RequiresValidSession()
    {
        await using var connection = BuildConnection();
        await connection.StartAsync();

        var args = JsonDocument.Parse("{}").RootElement;

        // Try to call a tool without joining the room first
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
        {
            await connection.InvokeAsync<ToolCallResultDto>("CallTool", RoomId, "test-tool", args);
        });

        exception.Should().NotBeNull();
        exception.Message.Should().Contain("AUTH_REQUIRED");
    }

    [Fact]
    public async Task CallTool_WithNonexistentTool_ReturnsToolNotFound()
    {
        await using var connection = BuildConnection();
        await connection.StartAsync();

        await connection.InvokeAsync("Join", RoomId, new EntitySpec
        {
            Id = "E-TEST",
            Kind = "human",
            DisplayName = "Tester"
        });

        var args = JsonDocument.Parse("{}").RootElement;

        // Try to call a tool that doesn't exist
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
        {
            await connection.InvokeAsync<ToolCallResultDto>("CallTool", RoomId, "nonexistent-tool", args);
        });

        exception.Should().NotBeNull();
        exception.Message.Should().Contain("TOOL_NOT_FOUND");
    }

    [Fact]
    public void PolicyEngine_CanList_AllowsNonEntityVisibility()
    {
        // This test verifies PolicyEngine behavior directly
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomServer.Services.Mcp.PolicyEngine>();
        var policyEngine = new RoomServer.Services.Mcp.PolicyEngine(logger);

        var session = new EntitySession
        {
            ConnectionId = "test-conn",
            RoomId = RoomId,
            Entity = new EntitySpec
            {
                Id = "E-TEST",
                Kind = "human",
                DisplayName = "Tester"
            },
            JoinedAt = DateTime.UtcNow
        };

        // Tool with "room" visibility should be listable
        var roomVisibilityTool = new CatalogItem(
            ServerId: "test-server",
            ToolId: "test-tool",
            Key: "test-server:test-tool",
            Spec: new ToolSpec(
                id: "test-tool",
                title: "Test Tool",
                description: "A test tool",
                inputSchema: null,
                outputSchema: null,
                policy: new ToolPolicy(
                    visibility: "room",
                    allowedEntities: "public",
                    scopes: null,
                    rateLimit: null
                )
            )
        );

        var canList = policyEngine.CanList(session, roomVisibilityTool);
        canList.Should().BeTrue();

        // Tool with "entity" visibility should NOT be listable
        var entityVisibilityTool = roomVisibilityTool with
        {
            Spec = roomVisibilityTool.Spec with
            {
                policy = new ToolPolicy(
                    visibility: "entity",
                    allowedEntities: "public",
                    scopes: null,
                    rateLimit: null
                )
            }
        };

        var cannotList = policyEngine.CanList(session, entityVisibilityTool);
        cannotList.Should().BeFalse();
    }

    [Fact]
    public void PolicyEngine_CanCall_AllowsPublicAndTeam()
    {
        // This test verifies PolicyEngine behavior directly
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomServer.Services.Mcp.PolicyEngine>();
        var policyEngine = new RoomServer.Services.Mcp.PolicyEngine(logger);

        var session = new EntitySession
        {
            ConnectionId = "test-conn",
            RoomId = RoomId,
            Entity = new EntitySpec
            {
                Id = "E-TEST",
                Kind = "human",
                DisplayName = "Tester"
            },
            JoinedAt = DateTime.UtcNow
        };

        // Tool with "public" allowedEntities should be callable
        var publicTool = new CatalogItem(
            ServerId: "test-server",
            ToolId: "test-tool",
            Key: "test-server:test-tool",
            Spec: new ToolSpec(
                id: "test-tool",
                title: "Test Tool",
                description: "A test tool",
                inputSchema: null,
                outputSchema: null,
                policy: new ToolPolicy(
                    visibility: "room",
                    allowedEntities: "public",
                    scopes: null,
                    rateLimit: null
                )
            )
        );

        var canCallPublic = policyEngine.CanCall(session, publicTool);
        canCallPublic.Should().BeTrue();

        // Tool with "team" allowedEntities should be callable
        var teamTool = publicTool with
        {
            Spec = publicTool.Spec with
            {
                policy = new ToolPolicy(
                    visibility: "room",
                    allowedEntities: "team",
                    scopes: null,
                    rateLimit: null
                )
            }
        };

        var canCallTeam = policyEngine.CanCall(session, teamTool);
        canCallTeam.Should().BeTrue();

        // Tool with "owner" allowedEntities should NOT be callable (MVP limitation)
        var ownerTool = publicTool with
        {
            Spec = publicTool.Spec with
            {
                policy = new ToolPolicy(
                    visibility: "room",
                    allowedEntities: "owner",
                    scopes: null,
                    rateLimit: null
                )
            }
        };

        var cannotCallOwner = policyEngine.CanCall(session, ownerTool);
        cannotCallOwner.Should().BeFalse();
    }

    [Fact]
    public void ResourceCatalog_Resolve_WorksWithFullKeyAndShortId()
    {
        // This test verifies ResourceCatalog behavior directly
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomServer.Services.Mcp.ResourceCatalog>();
        var catalog = new RoomServer.Services.Mcp.ResourceCatalog(logger);

        var mockClient = new MockMcpClient("test-server");
        var spec = new ToolSpec(
            id: "test-tool",
            title: "Test Tool",
            description: "A test tool",
            inputSchema: null,
            outputSchema: null,
            policy: null
        );

        catalog.Register("test-server", spec, mockClient);

        // Resolve by full key
        var (itemByKey, clientByKey) = catalog.Resolve("test-server:test-tool");
        itemByKey.Should().NotBeNull();
        itemByKey.Key.Should().Be("test-server:test-tool");
        clientByKey.Should().Be(mockClient);

        // Resolve by short tool ID
        var (itemById, clientById) = catalog.Resolve("test-tool");
        itemById.Should().NotBeNull();
        itemById.Key.Should().Be("test-server:test-tool");
        clientById.Should().Be(mockClient);
    }

    [Fact]
    public void ResourceCatalog_Resolve_ThrowsOnNotFound()
    {
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomServer.Services.Mcp.ResourceCatalog>();
        var catalog = new RoomServer.Services.Mcp.ResourceCatalog(logger);

        // Try to resolve a non-existent tool
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            catalog.Resolve("nonexistent-tool");
        });

        exception.Should().NotBeNull();
        exception.Message.Should().Contain("Tool not found");
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

    // Mock implementation of IMcpClient for testing
    private class MockMcpClient : RoomServer.Services.Mcp.IMcpClient
    {
        public MockMcpClient(string serverId)
        {
            ServerId = serverId;
        }

        public string ServerId { get; }
        public bool IsConnected => true;

        public Task ConnectAsync() => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;

        public Task<ToolSpec[]> ListToolsAsync()
        {
            return Task.FromResult(Array.Empty<ToolSpec>());
        }

        public Task<JsonElement> CallToolRawAsync(string toolId, JsonElement input)
        {
            throw new NotImplementedException("Mock client does not implement CallToolRawAsync");
        }
    }
}
