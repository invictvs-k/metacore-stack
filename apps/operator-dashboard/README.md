# Operator Dashboard

Web-based control and observability dashboard for RoomServer, RoomOperator, and Test Client components.

## Features

- **System Overview**: Real-time health checks and status monitoring
- **Event Streaming**: Live SSE streams from RoomServer and RoomOperator
- **Test Execution**: Run integration tests with live logs and artifact collection
- **Command Orchestration**: Execute RoomOperator commands with dynamic parameters
- **Configuration Management**: Edit and persist dashboard settings
- **Responsive UI**: Built with React, TypeScript, and TailwindCSS

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

- **Overview** (`/`) - System health and quick stats
- **Events** (`/events`) - Real-time event streaming with filtering
- **Tests** (`/tests`) - Test scenario execution and results
- **Commands** (`/commands`) - Command catalog and execution
- **Settings** (`/settings`) - Configuration editor
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

#### `useSSE(url, onMessage, enabled)`
Manages Server-Sent Events connections with automatic reconnection.

#### `useConfig()`
Provides access to dashboard configuration with update capabilities.

#### `useTestRunner()`
Manages test scenario execution and result streaming.

### State Management

Uses Zustand for lightweight global state:
- Theme preference
- Current run ID
- Event history

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

### Hot Reload
The dashboard automatically reloads when:
- Source files change (Vite HMR)
- Configuration is updated (via Settings page)

### Event Filtering
Use the filter buttons on the Events page to focus on specific sources.

### Test Logs
Test logs are streamed in real-time. The terminal view auto-scrolls to show new output.

### Dark Mode
Theme follows system preference by default. Can be configured in Settings.

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
