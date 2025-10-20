# Dashboard React for Control and Observability

Comprehensive control and observability dashboard for RoomServer, RoomOperator, and Test Client components.

## Overview

This implementation provides a full-stack solution for monitoring and managing the Metacore Stack:

- **Integration API** - Express/TypeScript backend providing unified REST and SSE endpoints
- **Operator Dashboard** - React/TypeScript frontend with real-time monitoring capabilities

## Quick Start

### Prerequisites

- Node.js 18+
- npm or pnpm
- RoomServer running on port 40801
- RoomOperator running on port 40802

### Installation

Install all dependencies:

```bash
npm run install:all
```

Or install individually:

```bash
# Integration API
cd tools/integration-api
npm install

# Dashboard
cd apps/operator-dashboard
npm install
```

### Development

Start both API and Dashboard in parallel:

```bash
npm run dev:parallel
```

Or run individually:

```bash
# Terminal 1: Integration API
npm run api:dev

# Terminal 2: Dashboard
npm run dashboard:dev
```

Access the dashboard at: http://localhost:5173

## Architecture

### Integration API (`tools/integration-api/`)

Express-based REST API that serves as the integration hub.

**Port**: 40901

**Features**:
- Configuration management with persistence
- SSE event streaming from RoomServer/RoomOperator
- Test scenario execution with artifact collection
- Command catalog and execution
- MCP status proxy

**Key Endpoints**:
- `GET /api/config` - Get configuration
- `PUT /api/config` - Update configuration
- `GET /api/events/combined` - Combined SSE stream
- `POST /api/tests/run` - Execute test scenarios
- `GET /api/commands` - Get command catalog
- `POST /api/commands/execute` - Execute commands

See [Integration API README](tools/integration-api/README.md) for details.

### Operator Dashboard (`apps/operator-dashboard/`)

React-based web interface for system control and monitoring.

**Port**: 5173 (dev server)

**Pages**:
- **Overview** - System health and status
- **Events** - Real-time event streaming with filtering
- **Tests** - Test execution and results
- **Commands** - Command orchestration
- **Settings** - Configuration editor
- **About** - Documentation and info

See [Dashboard README](apps/operator-dashboard/README.md) for details.

## Configuration

Configuration is managed through `configs/dashboard.settings.json`:

```json
{
  "version": 1,
  "roomServer": {
    "baseUrl": "http://127.0.0.1:40801",
    "events": { "type": "sse", "path": "/events" }
  },
  "roomOperator": {
    "baseUrl": "http://127.0.0.1:40802",
    "events": { "type": "sse", "path": "/events" }
  },
  "testClient": {
    "runner": "scripts/run-test-client.sh",
    "scenariosPath": "server-dotnet/operator/test-client/scenarios",
    "artifactsDir": ".artifacts/integration"
  },
  "integrationApi": {
    "port": 40901,
    "logLevel": "info"
  },
  "ui": {
    "theme": "system",
    "refreshInterval": 5000
  }
}
```

Configuration can be edited through the dashboard Settings page or by directly modifying the JSON file.

## Service Ports

| Service            | Port  | Description                      |
| ------------------ | ----- | -------------------------------- |
| RoomServer         | 40801 | MCP event emitter and status     |
| RoomOperator       | 40802 | Command executor                 |
| Integration API    | 40901 | REST API and event hub           |
| Operator Dashboard | 5173  | Web UI (Vite dev server)         |

## Build for Production

Build both projects:

```bash
npm run build:all
```

Or build individually:

```bash
# Build API
npm run build:api

# Build Dashboard
npm run build:dashboard
```

## Type Checking

Check TypeScript types for all projects:

```bash
npm run typecheck:all
```

## Project Structure

```
metacore-stack/
├── apps/
│   └── operator-dashboard/     # React dashboard frontend
│       ├── src/
│       │   ├── pages/         # Page components
│       │   ├── hooks/         # Custom hooks
│       │   ├── store/         # Zustand store
│       │   └── types/         # TypeScript types
│       └── README.md
├── tools/
│   └── integration-api/       # Express backend
│       ├── src/
│       │   ├── routes/        # API routes
│       │   ├── services/      # Business logic
│       │   └── types/         # TypeScript types
│       └── README.md
├── configs/
│   └── dashboard.settings.json # Configuration file
├── server-dotnet/
│   ├── operator/              # RoomOperator
│   │   └── test-client/       # Test scenarios
│   └── src/
│       └── RoomServer/        # RoomServer
└── README.md
```

## Features

### Real-time Event Streaming

The dashboard connects to RoomServer and RoomOperator via Server-Sent Events (SSE) for real-time monitoring:

- Automatic reconnection on connection loss
- Event filtering by source
- Event history with 100-event buffer
- Color-coded event sources

### Test Execution

Execute integration tests directly from the dashboard:

- Select individual scenarios or run all tests
- Live log streaming during execution
- Artifact collection and persistence
- Exit code and status reporting
- Test result history

Test artifacts are stored in `.artifacts/integration/{date}/runs/{runId}/`:
- `test-client.log` - Complete test logs
- `result.json` - Test metadata
- Other test-generated files

### Command Orchestration

Execute RoomOperator commands with dynamic parameter validation:

- Command catalog with descriptions
- JSON schema-based parameter validation
- Real-time execution feedback
- Command history

### Configuration Management

Edit dashboard settings through the web interface:

- JSON editor with syntax highlighting
- Configuration validation
- Hot reload on save
- Version tracking with checksums

## Development Workflow

1. **Start Services**:
   ```bash
   # Terminal 1: RoomServer
   cd server-dotnet/src/RoomServer
   dotnet run

   # Terminal 2: RoomOperator
   cd server-dotnet/operator
   dotnet run
   ```

2. **Start Dashboard Stack**:
   ```bash
   npm run dev:parallel
   ```

3. **Access Dashboard**:
   Open http://localhost:5173

4. **Monitor & Control**:
   - View real-time events on the Events page
   - Execute tests on the Tests page
   - Run commands on the Commands page
   - Adjust configuration on the Settings page

## Technologies

### Backend
- **Express** - Web framework
- **TypeScript** - Type safety
- **SSE** - Real-time event streaming
- **uuid** - Unique ID generation
- **chokidar** - File watching

### Frontend
- **React 18** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool
- **TailwindCSS** - Styling
- **React Router** - Routing
- **Zustand** - State management
- **SWR** - Data fetching
- **Lucide React** - Icons

## Troubleshooting

### Integration API won't start
- Check that port 40901 is available
- Verify Node.js version (18+)
- Ensure dependencies are installed: `cd tools/integration-api && npm install`

### Dashboard can't connect to API
- Verify Integration API is running on port 40901
- Check browser console for errors
- Ensure CORS is properly configured

### SSE events not streaming
- Verify RoomServer and RoomOperator are running
- Check service URLs in configuration
- Look for connection errors in browser Network tab

### Tests won't execute
- Verify test-client dependencies are installed
- Check test scenarios exist in configured path
- Ensure test runner script has execute permissions

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.

## License

MIT
