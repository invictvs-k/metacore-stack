# Operator Dashboard

Web-based control and observability dashboard for RoomServer, RoomOperator, and Test Client components.

## Features

- **System Overview**: Real-time health checks and status monitoring with quick actions
- **Event Streaming**: Live SSE streams from RoomServer and RoomOperator with pause/resume and auto-scroll
- **Test Execution**: Run integration tests with live log streaming per runId and artifact links
- **Command Orchestration**: Execute RoomOperator commands with JSON Schema validation
- **Configuration Management**: Edit, validate, and hot-reload dashboard settings with connection testing
- **Responsive UI**: Built with React, TypeScript, and TailwindCSS
- **SSE Resilience**: Exponential backoff reconnection with heartbeat monitoring

## Prerequisites

- Node.js 18+
- Integration API running on port 40901
- RoomServer running on port 40801
- RoomOperator running on port 40802

## Installation

```bash
npm install
```

## Development

```bash
npm run dev
```

The dashboard will be available at http://localhost:5173

## Build

```bash
npm run build
```

Build output will be in the `dist/` directory.

## Production

```bash
npm run preview
```

Serves the production build for testing.

## Architecture

### Pages

- **Overview** (`/`) - System health with live checks, quick stats, and actions (Run All Tests, Clean Artifacts)
- **Events** (`/events`) - Real-time event streaming with filtering, pause/resume, auto-scroll toggle, and windowing (2000 events)
- **Tests** (`/tests`) - Test scenario execution with SSE log streaming, exit codes, and artifact directory links
- **Commands** (`/commands`) - Command catalog and execution with parameter validation
- **Settings** (`/settings`) - Configuration editor with validation, hot reload detection, and connection testing
- **About** (`/about`) - Documentation and version info

### Key Components

```
src/
├── pages/           # Page components
├── components/      # Reusable UI components
├── hooks/          # Custom React hooks (useSSE, useConfig, useTestRunner)
├── store/          # Zustand state management
├── types/          # TypeScript type definitions
└── utils/          # Utility functions
```

### Hooks

#### `useSSE(url, onMessage, enabled, options)`
Manages Server-Sent Events connections with:
- Automatic reconnection with exponential backoff
- Support for multiple event types (started, log, done, error, etc.)
- Heartbeat monitoring
- Configurable retry intervals and backoff multipliers

#### `useConfig()`
Provides access to dashboard configuration with:
- Automatic refresh every 10 seconds
- Update capabilities with validation
- Mutation tracking

#### `useTestRunner()`
Manages test scenario execution and result streaming with:
- Scenario listing via SWR
- Test execution with runId tracking
- Run metadata retrieval

### State Management

Uses Zustand for lightweight global state:
- Theme preference (light/dark/system)
- Current run ID for test execution tracking
- Event history with windowing (keeps last 2000 events)

## Configuration

The dashboard reads configuration from the Integration API (`/api/config`). Configuration can be edited through the Settings page.

Default configuration structure:

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

## API Proxy

The development server proxies API requests to the Integration API:

```javascript
// vite.config.ts
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:40901',
      changeOrigin: true
    }
  }
}
```

## Technologies

- **React 18** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **TailwindCSS** - Styling
- **React Router** - Client-side routing
- **Zustand** - State management
- **SWR** - Data fetching and caching
- **Lucide React** - Icons

## Browser Support

Modern browsers with EventSource (SSE) support:
- Chrome/Edge 90+
- Firefox 88+
- Safari 14+

## Development Tips

### SSE Resilience
The dashboard implements robust SSE handling:
- Exponential backoff reconnection (1.5x multiplier, max 30s)
- Multiple event type support
- Automatic cleanup on unmount
- Heartbeat detection

### Hot Reload Detection
Configuration changes are detected via checksum polling every 5 seconds. When a change is detected, a notification appears prompting the user to reload.

### Test Execution
- Tests run with unique runId for isolation
- Logs stream in real-time via SSE with typed events
- Exit codes and artifact directories are displayed on completion
- Multiple tests can run sequentially without page reload

### Event Management
- **Pause/Resume**: Stop receiving events without closing SSE connection
- **Auto-scroll**: Toggle automatic scrolling to latest events
- **Windowing**: Keeps only last 2000 events in memory to prevent performance issues
- **Filtering**: Filter by source (all/roomserver/roomoperator)

### Command Validation
Commands use JSON Schema for parameter validation before execution. Validation errors show which parameters are missing or invalid with actionable feedback.

### Connection Testing
The Settings page includes a "Test Connections" button that:
- Pings RoomServer and RoomOperator baseUrls
- Checks MCP status endpoint availability
- Displays detailed results with error messages

## Troubleshooting

### API Connection Failed
Ensure the Integration API is running on port 40901:
```bash
cd tools/integration-api
npm run dev
```

### SSE Connection Errors
Check that RoomServer and RoomOperator are accessible at configured URLs.

### Build Errors
Clear cache and reinstall:
```bash
rm -rf node_modules dist
npm install
```

## License

MIT
