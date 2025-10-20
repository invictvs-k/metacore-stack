# Integration API

Backend service for the Operator Dashboard. Provides a unified API for managing and monitoring RoomServer, RoomOperator, and Test Client components.

## Features

- **Configuration Management**: Persist and hot-reload dashboard settings
- **Event Streaming**: SSE endpoints for real-time events from RoomServer and RoomOperator
- **Test Execution**: Run test scenarios with artifact collection and live log streaming
- **Command Execution**: Execute RoomOperator commands from catalog
- **MCP Status**: Proxy for RoomServer MCP status

## Installation

```bash
npm install
```

### Test Client Dependencies

The test execution feature requires the test-client dependencies to be installed:

```bash
cd ../../server-dotnet/operator/test-client
npm install
```

This installs the `axios` HTTP client required by test scenarios. See `TEST_SETUP.md` in the repository root for complete test setup instructions.

## Development

```bash
npm run dev
```

This starts the API server with hot reload on port 40901 (configurable in `configs/dashboard.settings.json`).

## Build

```bash
npm run build
```

## Production

```bash
npm start
```

## API Endpoints

### Configuration

- `GET /api/config` - Get current configuration
- `PUT /api/config` - Update configuration (with validation)
- `GET /api/config/version` - Get configuration version/checksum for hot reload detection

### Events (SSE)

- `GET /api/events/roomserver` - SSE stream of RoomServer events
- `GET /api/events/roomoperator` - SSE stream of RoomOperator events
- `GET /api/events/combined` - Combined SSE stream from both sources

**SSE Protocol:**
- Heartbeat: `: ping` every 10s to keep connections alive
- Reconnect hint: `retry: 5000` (configurable)
- Event types: `message`, `connected`, `disconnected`, `error`

### Tests

- `GET /api/tests` - List available test scenarios
- `POST /api/tests/run` - Execute a test scenario (body: `{ scenarioId?: string, all?: boolean }`)
- `GET /api/tests/runs/:runId` - Get test run metadata/results
- `GET /api/tests/stream/:runId` - SSE stream of test logs

**SSE Events for Test Streaming:**
- `started` - Test execution started (with runId and artifactsDir)
- `log` - Incremental log output (stdout/stderr chunks)
- `done` - Test execution completed (with exit code)
- `error` - Test execution error

### Commands

- `GET /api/commands` - Get command catalog from `server-dotnet/operator/commands/commands.catalog.json`
- `POST /api/commands/execute` - Execute a command with JSON Schema validation (body: `{ commandId: string, params: any }`)

### MCP

- `GET /api/mcp/status` - Proxy to RoomServer MCP status endpoint

### Health Checks

- `GET /api/health/roomserver` - Check RoomServer health (proxied)
- `GET /api/health/roomoperator` - Check RoomOperator health (proxied)
- `GET /api/health/all` - Check all services health in one request

### Health

- `GET /health` - Health check endpoint

## Environment Variables

The API reads configuration from `configs/dashboard.settings.json`. Default values are used if the file is missing.

## Artifacts

Test run artifacts are stored in `.artifacts/integration/{timestamp}/runs/{runId}/`:
- `test-client.log` - Test execution logs (stdout/stderr)
- `result.json` - Test run metadata (runId, exitCode, status, timestamps, artifactsPath)
- Other test-generated files

## SSE Resilience

The API implements robust SSE handling:

- **Heartbeat**: Comment-based ping (`: ping`) every 10s to keep connections alive
- **Retry Hint**: Sends `retry: 5000` to guide client reconnection with exponential backoff
- **Backpressure**: Proper cleanup on client disconnect
- **Error Handling**: Sends typed error events with actionable messages
- **Reconnection**: Clients reconnect with exponential backoff (1.5x multiplier, max 30s)

## Test Runner

The test runner supports cross-platform execution:

- Uses `shell: true` for Windows/Linux compatibility
- Captures stdout/stderr in real-time
- Streams logs via SSE with minimal latency (500ms polling)
- Persists artifacts with timestamp and runId structure
- Saves complete result metadata as JSON

## Troubleshooting

### Port Already in Use

Change the port in `configs/dashboard.settings.json`:

```json
{
  "integrationApi": {
    "port": 40902
  }
}
```

### Test Runner Not Found

Ensure the runner script exists and is executable:

```bash
chmod +x scripts/run-test-client.sh
```

For Windows, ensure Node.js and npm are in PATH.

### SSE Connection Issues

Check that:
1. RoomServer and RoomOperator are running at configured baseUrls
2. Base URLs in config are correct and accessible
3. No CORS issues (only localhost:5173 is allowed by default)
4. Firewall allows connections on configured ports

### Command Validation Errors

Verify that:
1. Parameters match the schema in `server-dotnet/operator/commands/commands.catalog.json`
2. All required parameters are provided
3. Parameter types match the schema (string, number, boolean, array, object)

## Architecture

```
src/
├── index.ts              # Main entry point
├── types/
│   └── index.ts          # TypeScript type definitions
├── services/
│   ├── config.ts         # Configuration management
│   └── tests.ts          # Test execution service
└── routes/
    ├── config.ts         # Config endpoints
    ├── events.ts         # SSE event endpoints
    ├── tests.ts          # Test execution endpoints
    ├── commands.ts       # Command execution endpoints
    └── mcp.ts            # MCP status endpoints
```
