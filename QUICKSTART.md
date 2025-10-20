# Quick Start Guide - Operator Dashboard

## Overview

The Operator Dashboard is a web-based control and observability platform for the Metacore Stack. It provides real-time monitoring, test execution, and command orchestration capabilities.

## Prerequisites

- Node.js 18 or higher
- npm or pnpm
- Running instances of:
  - RoomServer (port 40801)
  - RoomOperator (port 40802)

## Installation

From the repository root:

```bash
npm run install:all
```

This installs dependencies for:
- Integration API (`tools/integration-api`)
- Operator Dashboard (`apps/operator-dashboard`)

## Running the Dashboard

### Option 1: Start Both Services Together (Recommended)

```bash
npm run dev:parallel
```

This starts:
- Integration API on http://localhost:40901
- Dashboard UI on http://localhost:5173

### Option 2: Start Services Individually

**Terminal 1 - Integration API:**
```bash
cd tools/integration-api
npm run dev
```

**Terminal 2 - Dashboard:**
```bash
cd apps/operator-dashboard
npm run dev
```

## Accessing the Dashboard

Open your browser and navigate to:

```
http://localhost:5173
```

## Dashboard Features

### 1. Overview Page (`/`)
- Health status of RoomServer and RoomOperator
- MCP provider connection status
- Quick statistics dashboard

### 2. Events Page (`/events`)
- Real-time event streaming via SSE
- Filter events by source (RoomServer/RoomOperator)
- Event history with timestamps

### 3. Tests Page (`/tests`)
- List of available test scenarios
- Execute individual or all tests
- Live log streaming during test execution
- Test result history

### 4. Commands Page (`/commands`)
- Browse available commands
- Execute commands with parameters
- View command execution results

### 5. Settings Page (`/settings`)
- Edit dashboard configuration
- Update service URLs
- Configure theme and behavior
- View current configuration

### 6. About Page (`/about`)
- Documentation and resources
- System architecture overview
- Version information

## Configuration

The dashboard configuration is stored in:

```
configs/dashboard.settings.json
```

You can edit this file directly or use the Settings page in the dashboard.

### Default Configuration

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

## Building for Production

Build both projects:

```bash
npm run build:all
```

Individual builds:

```bash
# Build Integration API
npm run build:api

# Build Dashboard
npm run build:dashboard
```

## Type Checking

Check TypeScript types for all projects:

```bash
npm run typecheck:all
```

## Troubleshooting

### Integration API Won't Start

1. Check if port 40901 is available:
   ```bash
   lsof -i :40901
   ```

2. Verify Node.js version:
   ```bash
   node --version  # Should be 18+
   ```

3. Reinstall dependencies:
   ```bash
   cd tools/integration-api
   rm -rf node_modules
   npm install
   ```

### Dashboard Can't Connect to API

1. Verify Integration API is running on port 40901
2. Check browser console for errors
3. Verify CORS settings in Integration API

### No Test Scenarios Found

1. Verify test-client directory exists:
   ```bash
   ls -la server-dotnet/operator/test-client/scenarios
   ```

2. Check configuration path in Settings page

### SSE Events Not Streaming

1. Verify RoomServer and RoomOperator are running
2. Check service URLs in configuration
3. Look for connection errors in browser Network tab

## Development Scripts

### Root Level

| Script              | Description                        |
| ------------------- | ---------------------------------- |
| `npm run install:all` | Install all dependencies         |
| `npm run dev:parallel` | Start both API and Dashboard    |
| `npm run build:all`   | Build all projects              |
| `npm run typecheck:all` | Type check all projects       |

### Integration API

| Script                  | Description                  |
| ----------------------- | ---------------------------- |
| `npm run dev`           | Start with hot reload        |
| `npm run build`         | Build for production         |
| `npm start`             | Run production build         |
| `npm run type-check`    | Check TypeScript types       |

### Operator Dashboard

| Script               | Description                     |
| -------------------- | ------------------------------- |
| `npm run dev`        | Start Vite dev server           |
| `npm run build`      | Build for production            |
| `npm run preview`    | Preview production build        |
| `npm run type-check` | Check TypeScript types          |

## Project Structure

```
metacore-stack/
├── apps/
│   └── operator-dashboard/        # React Dashboard
│       ├── src/
│       │   ├── pages/            # Page components
│       │   ├── hooks/            # Custom React hooks
│       │   ├── store/            # Zustand state
│       │   └── types/            # TypeScript types
│       └── package.json
├── tools/
│   └── integration-api/          # Express API
│       ├── src/
│       │   ├── routes/           # API endpoints
│       │   ├── services/         # Business logic
│       │   └── types/            # TypeScript types
│       └── package.json
├── configs/
│   └── dashboard.settings.json   # Configuration
└── package.json                  # Root package
```

## Service Ports

| Service            | Port  | URL                           |
| ------------------ | ----- | ----------------------------- |
| RoomServer         | 40801 | http://localhost:40801        |
| RoomOperator       | 40802 | http://localhost:40802        |
| Integration API    | 40901 | http://localhost:40901        |
| Operator Dashboard | 5173  | http://localhost:5173         |

## Documentation

For more detailed information, see:

- [Dashboard README](apps/operator-dashboard/README.md)
- [Integration API README](tools/integration-api/README.md)
- [Complete Guide](DASHBOARD_README.md)

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review the detailed documentation
3. Check browser console for errors
4. Verify all services are running

## License

MIT
