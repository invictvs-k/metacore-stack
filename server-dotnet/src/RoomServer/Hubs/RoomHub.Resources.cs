using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RoomServer.Models;
using RoomServer.Services;
using RoomServer.Services.Mcp;

namespace RoomServer.Hubs;

/// <summary>
/// Partial class for RoomHub containing MCP resource-related methods.
/// </summary>
public partial class RoomHub
{
  /// <summary>
  /// Lists all tools available in the room that are visible to the caller.
  /// </summary>
  public Task<CatalogItemDto[]> ListTools(string roomId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(roomId);

    var session = _sessions.GetByConnection(Context.ConnectionId)
        ?? throw ErrorFactory.HubUnauthorized("AUTH_REQUIRED", "session not found");

    if (!string.Equals(session.RoomId, roomId, StringComparison.Ordinal))
    {
      throw ErrorFactory.HubForbidden("PERM_DENIED", "cannot list tools for different room");
    }

    var visibleTools = _connectionManager.Catalog.ListVisible(session, _policyEngine);

    var dtos = visibleTools.Select(item => new CatalogItemDto(
        key: item.Key,
        toolId: item.ToolId,
        serverId: item.ServerId,
        title: item.Spec.title,
        description: item.Spec.description,
        policy: item.Spec.policy
    )).ToArray();

    _logger.LogInformation("[{RoomId}] {EntityId} listed {Count} tools", roomId, session.Entity.Id, dtos.Length);

    return Task.FromResult(dtos);
  }

  /// <summary>
  /// Calls a tool with the specified arguments.
  /// Validates permissions, emits events, and returns the result.
  /// </summary>
  public async Task<ToolCallResultDto> CallTool(string roomId, string toolIdOrKey, JsonElement args)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
    ArgumentException.ThrowIfNullOrWhiteSpace(toolIdOrKey);

    var session = _sessions.GetByConnection(Context.ConnectionId)
        ?? throw ErrorFactory.HubUnauthorized("AUTH_REQUIRED", "session not found");

    if (!string.Equals(session.RoomId, roomId, StringComparison.Ordinal))
    {
      throw ErrorFactory.HubForbidden("PERM_DENIED", "cannot call tools in different room");
    }

    // Resolve tool
    CatalogItem item;
    IMcpClient client;
    try
    {
      (item, client) = _connectionManager.Catalog.Resolve(toolIdOrKey);
    }
    catch (InvalidOperationException)
    {
      throw ErrorFactory.HubNotFound("TOOL_NOT_FOUND", $"Tool not found: {toolIdOrKey}");
    }

    // Validate permissions
    if (!_policyEngine.CanCall(session, item))
    {
      throw ErrorFactory.HubForbidden("PERM_DENIED", "You are not allowed to call this tool");
    }

    // Validate rate limit
    if (!_policyEngine.CheckRateLimit(session, item))
    {
      throw new HubException(JsonSerializer.Serialize(new
      {
        error = "RATE_LIMITED",
        message = "Rate limit exceeded for this tool",
        code = -32001
      }));
    }

    // Check if client is connected
    if (!client.IsConnected)
    {
      throw new HubException(JsonSerializer.Serialize(new
      {
        error = "MCP_UNAVAILABLE",
        message = "MCP server is not connected",
        code = -32002
      }));
    }

    // Emit RESOURCE.CALLED event
    await _events.PublishAsync(roomId, "RESOURCE.CALLED", new
    {
      toolId = item.ToolId,
      serverId = item.ServerId,
      key = item.Key,
      callerEntityId = session.Entity.Id,
      args
    });

    _logger.LogInformation("[{RoomId}] {EntityId} calling tool {Key}", roomId, session.Entity.Id, item.Key);

    try
    {
      // Execute the tool call
      var result = await client.CallToolRawAsync(item.ToolId, args);
      var rawOutput = result.GetRawText();

      // Emit RESOURCE.RESULT event (success)
      await _events.PublishAsync(roomId, "RESOURCE.RESULT", new
      {
        ok = true,
        toolId = item.ToolId,
        serverId = item.ServerId,
        key = item.Key,
        callerEntityId = session.Entity.Id,
        output = result
      });

      _logger.LogInformation("[{RoomId}] Tool {Key} completed successfully", roomId, item.Key);

      return new ToolCallResultDto(
          ok: true,
          rawOutput: rawOutput,
          error: null,
          code: null
      );
    }
    catch (McpServerException ex)
    {
      // MCP server returned an error
      await _events.PublishAsync(roomId, "RESOURCE.RESULT", new
      {
        ok = false,
        toolId = item.ToolId,
        serverId = item.ServerId,
        key = item.Key,
        callerEntityId = session.Entity.Id,
        error = ex.Message,
        code = ex.ErrorCode
      });

      _logger.LogWarning(ex, "[{RoomId}] Tool {Key} returned error", roomId, item.Key);

      throw new HubException(JsonSerializer.Serialize(new
      {
        error = "MCP_ERROR",
        message = ex.Message,
        code = ex.ErrorCode
      }));
    }
    catch (Exception ex)
    {
      // Other errors (network, etc.)
      await _events.PublishAsync(roomId, "RESOURCE.RESULT", new
      {
        ok = false,
        toolId = item.ToolId,
        serverId = item.ServerId,
        key = item.Key,
        callerEntityId = session.Entity.Id,
        error = ex.Message
      });

      _logger.LogError(ex, "[{RoomId}] Error calling tool {Key}", roomId, item.Key);

      throw new HubException(JsonSerializer.Serialize(new
      {
        error = "MCP_UNAVAILABLE",
        message = "Failed to communicate with MCP server",
        code = -32002
      }));
    }
  }
}
