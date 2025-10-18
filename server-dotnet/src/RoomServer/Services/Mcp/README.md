# MCP Bridge - Model Context Protocol Integration

## Overview

The MCP Bridge module enables the Room Server to connect to external TypeScript-based MCP (Model Context Protocol) servers via WebSocket using JSON-RPC 2.0. This allows external tools and resources to be dynamically discovered and made available to entities in a room.

## Architecture

### Components

1. **McpClient** (`Services/Mcp/McpClient.cs`)
   - WebSocket JSON-RPC 2.0 client
   - Maintains 1:1 connection with a single MCP server
   - Automatic reconnection with exponential backoff
   - Thread-safe request/response handling

2. **McpRegistry** (`Services/Mcp/McpRegistry.cs`)
   - Manages multiple McpClient instances
   - Initializes connections on startup
   - Aggregates tools from all servers

3. **ResourceCatalog** (`Services/Mcp/ResourceCatalog.cs`)
   - Central catalog of all available tools
   - Supports resolution by full key (`serverId:toolId`) or short ID
   - Filters tools based on visibility policies

4. **PolicyEngine** (`Services/Mcp/PolicyEngine.cs`)
   - Validates listing and calling permissions
   - Implements visibility and allowedEntities policies
   - Rate limiting support (MVP implementation)

5. **RoomHub.Resources** (`Hubs/RoomHub.Resources.cs`)
   - SignalR Hub methods: `ListTools` and `CallTool`
   - Emits `RESOURCE.CALLED` and `RESOURCE.RESULT` events
   - Standardized error handling

## Configuration

Configure MCP servers in `appsettings.json`:

```json
{
  "McpServers": [
    {
      "id": "web.search@local",
      "url": "ws://localhost:8081",
      "visibility": "room"
    },
    {
      "id": "http.request@local",
      "url": "ws://localhost:8082",
      "visibility": "room"
    }
  ],
  "McpDefaults": {
    "rateLimit": {
      "perMinute": 60
    },
    "scopes": ["net:*"],
    "allowedEntities": "public"
  }
}
```

### Configuration Options

- **McpServers**: Array of MCP server configurations
  - `id`: Unique identifier for the server (e.g., "web.search@local")
  - `url`: WebSocket URL (e.g., "ws://localhost:8081")
  - `visibility`: Default visibility for tools from this server ("public", "room", "entity")

- **McpDefaults**: Default policies for all tools
  - `rateLimit.perMinute`: Maximum calls per minute (default: 60)
  - `scopes`: Array of allowed scopes (e.g., ["net:*"])
  - `allowedEntities`: Who can call tools ("public", "team", "owner")

## SignalR Hub Methods

### ListTools(roomId: string)

Lists all tools visible to the caller in the specified room.

**Returns:** `CatalogItemDto[]`

```typescript
interface CatalogItemDto {
  key: string;           // Full key: "serverId:toolId"
  toolId: string;        // Short tool ID
  serverId: string;      // Server ID
  title?: string;        // Tool title
  description?: string;  // Tool description
  policy?: ToolPolicy;   // Policy information
}
```

### CallTool(roomId: string, toolIdOrKey: string, args: JsonElement)

Calls a tool with the specified arguments.

**Parameters:**
- `roomId`: Room identifier
- `toolIdOrKey`: Full key ("serverId:toolId") or short ID ("toolId")
- `args`: JSON arguments for the tool

**Returns:** `ToolCallResultDto`

```typescript
interface ToolCallResultDto {
  ok: boolean;
  rawOutput?: string;    // JSON string of the result
  error?: string;        // Error message if ok=false
  code?: number;         // Error code if ok=false
}
```

## Events

### RESOURCE.CALLED

Emitted when a tool is invoked.

```json
{
  "kind": "RESOURCE.CALLED",
  "data": {
    "toolId": "web.search",
    "serverId": "web.search@local",
    "key": "web.search@local:web.search",
    "callerEntityId": "E-USER-123",
    "args": { /* tool arguments */ }
  }
}
```

### RESOURCE.RESULT

Emitted when a tool call completes (success or failure).

```json
{
  "kind": "RESOURCE.RESULT",
  "data": {
    "ok": true,
    "toolId": "web.search",
    "serverId": "web.search@local",
    "key": "web.search@local:web.search",
    "callerEntityId": "E-USER-123",
    "output": { /* tool result */ }
  }
}
```

## Error Handling

All errors are returned as `HubException` with standardized error codes:

- **TOOL_NOT_FOUND**: Tool not found in catalog
- **PERM_DENIED**: Permission denied by policy
- **RATE_LIMITED**: Rate limit exceeded
- **MCP_ERROR**: MCP server returned an error
- **MCP_UNAVAILABLE**: MCP server is not connected

Example error format:
```json
{
  "error": "TOOL_NOT_FOUND",
  "message": "Tool not found: unknown-tool",
  "code": -32000
}
```

## Policies

### Visibility

Controls who can see the tool in listings:
- `public`: Visible to everyone
- `room`: Visible to room members (default)
- `entity`: Private/hidden

### AllowedEntities

Controls who can call the tool:
- `public`: Anyone can call
- `team`: Room members can call
- `owner`: Only tool owner can call (MVP: not implemented)

## Development

### Running Tests

```bash
cd server-dotnet
dotnet test --filter "FullyQualifiedName~McpBridge"
```

### Testing with Mock MCP Server

To test the integration with a real MCP server:

1. Start an MCP server on `ws://localhost:8081` that implements:
   - `tools/list` method
   - `tool/call` method

2. Update `appsettings.Development.json` with the server configuration

3. Run the Room Server:
   ```bash
   cd server-dotnet/src/RoomServer
   dotnet run
   ```

4. Connect via SignalR and call `ListTools` and `CallTool`

## Future Enhancements

1. **Rate Limiting**: Implement actual rate limiting in PolicyEngine
2. **Ownership**: Track tool ownership for "owner" policy
3. **Caching**: Implement more sophisticated caching strategies
4. **Metrics**: Add metrics for tool usage and performance
5. **Tool Discovery**: Support dynamic tool discovery/registration
6. **Security**: Add authentication/authorization for MCP servers
7. **Health Checks**: Monitor MCP server health and availability

## JSON-RPC 2.0 Protocol

The MCP Bridge uses JSON-RPC 2.0 over WebSocket.

### Request Format
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": {}
}
```

### Response Format (Success)
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "id": "web.search",
        "title": "Web Search",
        "description": "Search the web",
        "inputSchema": { /* JSON Schema */ },
        "outputSchema": { /* JSON Schema */ },
        "policy": {
          "visibility": "room",
          "allowedEntities": "public",
          "scopes": ["net:*"],
          "rateLimit": { "perMinute": 60 }
        }
      }
    ]
  }
}
```

### Response Format (Error)
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32000,
    "message": "Server error"
  }
}
```
