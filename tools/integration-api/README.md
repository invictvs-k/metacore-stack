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
- `PUT /api/config` - Update configuration
- `GET /api/config/version` - Get configuration version/checksum

### Events

- `GET /api/events/roomserver` - SSE stream of RoomServer events
- `GET /api/events/roomoperator` - SSE stream of RoomOperator events
- `GET /api/events/combined` - Combined SSE stream

### Tests

- `GET /api/tests` - List available test scenarios
- `POST /api/tests/run` - Execute a test scenario (body: `{ scenarioId: string | 'all' }`)
- `GET /api/tests/runs/:runId` - Get test run metadata
- `GET /api/tests/stream/:runId` - SSE stream of test logs

### Commands

- `GET /api/commands` - Get command catalog
- `POST /api/commands/execute` - Execute a command (body: `{ commandId: string, params: any }`)

### MCP

- `GET /api/mcp/status` - Get MCP status from RoomServer

### Health

- `GET /health` - Health check endpoint

## Environment Variables

The API reads configuration from `configs/dashboard.settings.json`. Default values are used if the file is missing.

## Artifacts

Test run artifacts are stored in `.artifacts/integration/{date}/runs/{runId}/`:
- `test-client.log` - Test execution logs
- `result.json` - Test run metadata
- Other test-generated files

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
