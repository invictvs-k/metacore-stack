using System.Text.Json;
using System.Threading.Tasks;
using RoomServer.Models;

namespace RoomServer.Services.Mcp;

/// <summary>
/// Represents a client for communicating with a single MCP (Model Context Protocol) server via WebSocket.
/// </summary>
public interface IMcpClient
{
    /// <summary>
    /// Gets the unique identifier for this MCP server.
    /// </summary>
    string ServerId { get; }

    /// <summary>
    /// Gets a value indicating whether the client is currently connected to the MCP server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the MCP server asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync();

    /// <summary>
    /// Disconnects from the MCP server.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Lists all available tools from the MCP server.
    /// This result is cached after the first successful call.
    /// </summary>
    /// <returns>An array of tool specifications.</returns>
    Task<ToolSpec[]> ListToolsAsync();

    /// <summary>
    /// Calls a specific tool on the MCP server with the provided input.
    /// </summary>
    /// <param name="toolId">The ID of the tool to call.</param>
    /// <param name="input">The input arguments for the tool.</param>
    /// <returns>The raw JSON result from the tool execution.</returns>
    Task<JsonElement> CallToolRawAsync(string toolId, JsonElement input);
}
