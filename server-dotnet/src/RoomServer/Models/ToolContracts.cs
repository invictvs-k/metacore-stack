using System.Text.Json;

namespace RoomServer.Models;

// DTO for deserializing the response of 'tools/list' from an MCP Server
public record ToolSpec(
    string id,
    string? title,
    string? description,
    JsonElement? inputSchema,
    JsonElement? outputSchema,
    ToolPolicy? policy);

public record ToolPolicy(
    string? visibility,
    string? allowedEntities,
    string[]? scopes,
    RateLimit? rateLimit);

public record RateLimit(int perMinute);

// Internal catalog model, combining server and tool data
public record CatalogItem(
    string ServerId,     // ex: "web.search@local"
    string ToolId,       // ex: "web.search"
    string Key,          // Unique global key. Format: $"{ServerId}:{ToolId}"
    ToolSpec Spec);      // The original tool specification

// DTOs for SignalR Hub
public record CatalogItemDto(
    string key,
    string toolId,
    string serverId,
    string? title,
    string? description,
    ToolPolicy? policy);

public record ToolCallResultDto(
    bool ok,
    string? rawOutput,
    string? error,
    int? code);

// DTOs for internal communication
public record ToolCallRequest(
    string RoomId,
    string ToolId,
    JsonElement Args,
    string CallerEntityId);

public record ToolCallResult(
    bool Ok,
    JsonElement? Output,
    string? ErrorMessage,
    int? ErrorCode);

// Configuration models
public record McpServerConfig(
    string id,
    string url,
    string? visibility);

public record McpDefaultsConfig(
    RateLimit? rateLimit,
    string[]? scopes,
    string? allowedEntities);
