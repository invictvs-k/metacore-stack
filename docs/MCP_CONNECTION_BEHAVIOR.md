---
title: MCP Connection Behavior in RoomServer
status: active
owners: []
tags: [architecture, implementation, mcp]
last_review: 2025-10-20
links: []
---

# MCP Connection Behavior in RoomServer

## Overview

The RoomServer's MCP (Model Context Protocol) connection management has been refactored to support **lazy loading**, **on-demand connection**, and **graceful error handling** without impacting the core functionality of the room system.

## Key Principles

1. **Lazy Loading**: MCP providers are not connected automatically at startup. They remain in an `idle` state until explicitly loaded via RoomOperator commands.

2. **Non-Blocking**: MCP connection failures do not prevent RoomServer from starting or operating. The health check endpoint (`/health`) is independent of MCP status.

3. **Rate-Limited Logging**: Error logs for MCP connection issues are rate-limited (1 log per provider per 60 seconds) to prevent log spam.

4. **State Machine**: Each MCP provider has a clear state: `idle`, `connecting`, `connected`, or `error`.

## Architecture

### Components

#### MCPConnectionManager

The core component managing MCP provider connections:

- **State Management**: Tracks each provider's state, connection attempts, and errors
- **Connection Logic**: Implements exponential backoff with jitter for retries
- **Monitoring**: Automatically attempts reconnection if a connection is lost
- **Rate Limiting**: Prevents excessive logging during connection failures

#### McpRegistryHostedService

Hosted service that initializes the connection manager:

- Reads provider configurations from `appsettings.json`
- Supports `Mcp:LazyLoad` configuration flag (default: `true`)
- Does not block application startup

#### MCP Admin Endpoints

RoomServer exposes the following endpoints:

- `POST /admin/mcp/load`: Load and connect to MCP providers
- `GET /admin/mcp/status`: Get administrative MCP status (same as public endpoint)

#### MCP Status Endpoint

- `GET /status/mcp`: Public read-only endpoint returning provider states

## Configuration

### RoomServer (appsettings.json)

```json
{
  "Mcp": {
    "LazyLoad": true
  },
  "McpServers": [
    {
      "id": "example-mcp",
      "url": "ws://localhost:5099",
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

### RoomOperator (appsettings.json)

```json
{
  "RoomServer": {
    "BaseUrl": "http://localhost:40801"
  },
  "Mcp": {
    "Providers": [
      {
        "id": "example-mcp",
        "url": "ws://127.0.0.1:5099",
        "visibility": "room"
      }
    ]
  }
}
```

## Usage

### Starting Without MCP

RoomServer can start and operate normally without any MCP providers configured:

```bash
dotnet run
```

The server will:
- Start successfully
- Report health as `healthy`
- Show no providers in `/status/mcp`

### Loading MCP Providers

To load MCP providers at runtime, use the RoomOperator:

```bash
# Via RoomOperator endpoint
curl -X POST http://localhost:40802/mcp/load \
  -H "Content-Type: application/json" \
  -d '{
    "providers": [
      {
        "id": "example-mcp",
        "url": "ws://localhost:5099",
        "visibility": "room"
      }
    ]
  }'
```

Or directly via RoomServer:

```bash
curl -X POST http://localhost:40801/admin/mcp/load \
  -H "Content-Type: application/json" \
  -d '{
    "providers": [
      {
        "id": "example-mcp",
        "url": "ws://localhost:5099",
        "visibility": "room"
      }
    ]
  }'
```

### Querying MCP Status

Check the status of MCP providers:

```bash
# Via RoomServer
curl http://localhost:40801/status/mcp

# Via RoomOperator
curl http://localhost:40802/mcp/status
```

**Response Example:**

```json
{
  "providers": [
    {
      "id": "example-mcp",
      "state": "connecting",
      "attempts": 2,
      "lastChangeAt": 1634567890123,
      "lastError": "Connection refused",
      "nextRetryAt": 1634567895123
    }
  ]
}
```

## State Machine

### States

1. **idle**: Provider is configured but not yet connecting
2. **connecting**: Provider is actively attempting to connect
3. **connected**: Provider is successfully connected and operational
4. **error**: Provider failed to connect after maximum retry attempts

### Transitions

```
idle --[load command]--> connecting
connecting --[success]--> connected
connecting --[failure, retry]--> connecting
connecting --[max retries]--> error
connected --[disconnect]--> connecting
error --[manual retry/reload]--> connecting
```

## Error Handling

### Connection Failures

When a provider fails to connect:

1. **Exponential Backoff**: Retry delays increase exponentially (500ms, 1s, 2s, 4s, ...)
2. **Max Attempts**: After 5 attempts, the provider enters the `error` state
3. **Rate-Limited Logging**: Errors are logged at most once per 60 seconds per provider
4. **Health Independence**: RoomServer remains healthy regardless of MCP status

### Log Spam Prevention

Instead of continuous error logs, the system:
- Logs state transitions only
- Rate-limits error messages (60-second window)
- Provides detailed status via API endpoints

## Integration Testing

### Running Tests

Execute the full integration test suite:

```bash
./scripts/run-integration.sh
```

This will:
1. Start RoomServer in test mode (port 40801)
2. Start RoomOperator in test mode (port 40802)
3. Run test scenarios
4. Collect artifacts in `.artifacts/integration/{timestamp}/`
5. Generate reports (JSON and JUnit XML)

### Test Scenarios

1. **01-no-mcp**: Validates operation without MCP
2. **02-load-mcp**: Tests loading providers on-demand
3. **03-mcp-unavailable**: Tests error handling for unavailable providers
4. **04-status**: Tests status query endpoints

### Test Artifacts

Results are saved to `.artifacts/integration/{timestamp}/`:
```
.artifacts/integration/20241019-123456/
├── logs/
│   ├── roomserver.log
│   ├── roomoperator.log
│   └── test-client.log
└── results/
    ├── report.json
    ├── junit.xml
    └── mcp-status-final.json
```

## Migration Guide

### From Old Behavior

**Before**: MCP providers connected automatically at startup, causing:
- Startup delays if providers were unavailable
- Continuous error logs
- Potential health check failures

**After**: MCP providers are loaded on-demand:
- Instant startup regardless of MCP availability
- Clean logs with rate limiting
- Health check always succeeds (independent of MCP)

### Configuration Changes

No breaking changes to existing configurations. Simply add:

```json
{
  "Mcp": {
    "LazyLoad": true
  }
}
```

To revert to old behavior (not recommended):

```json
{
  "Mcp": {
    "LazyLoad": false
  }
}
```

## Troubleshooting

### Provider Stuck in "connecting" State

Check the logs and status endpoint for detailed error information:

```bash
curl http://localhost:40801/status/mcp
```

Common causes:
- Provider endpoint is unreachable
- Network firewall blocking WebSocket connections
- Provider not running

### No Providers Showing Up

Verify providers were loaded:

```bash
# Check if load command was sent
curl -X POST http://localhost:40802/mcp/load -d '...'

# Verify status
curl http://localhost:40801/status/mcp
```

### Rate Limit Not Working

Logs should show state transitions only. If you see repeated errors:
1. Check the `lastChangeAt` timestamps in status
2. Verify logs show "State transition" messages
3. Ensure logging level is not set to Debug for MCP components

## API Reference

### POST /admin/mcp/load

Load and connect to MCP providers.

**Request:**
```json
{
  "providers": [
    {
      "id": "string",
      "url": "string (WebSocket URL)",
      "visibility": "string (optional)"
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "message": "MCP providers loading initiated",
  "count": 1
}
```

### GET /status/mcp

Get current status of all MCP providers.

**Response (200 OK):**
```json
{
  "providers": [
    {
      "id": "string",
      "state": "idle | connecting | connected | error",
      "attempts": "number",
      "lastChangeAt": "number (timestamp)",
      "lastError": "string (optional)",
      "nextRetryAt": "number (timestamp, optional)"
    }
  ]
}
```

### GET /health

Health check endpoint (independent of MCP status).

**Response (200 OK):**
```json
{
  "status": "healthy"
}
```

## Best Practices

1. **Always use lazy loading** in production to prevent startup delays
2. **Monitor MCP status** via the status endpoint or RoomOperator
3. **Load providers early** in the application lifecycle if they're critical
4. **Handle provider failures gracefully** in client applications
5. **Use the integration test suite** to validate changes

## References

- RoomServer Source: `server-dotnet/src/RoomServer/`
- RoomOperator Source: `server-dotnet/operator/`
- Integration Tests: `server-dotnet/operator/test-client/scenarios/mcp/`
- Orchestration Scripts: `scripts/`
