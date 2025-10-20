# Development Setup Guide

This guide explains how to run RoomServer, RoomOperator, Integration API, and Operator Dashboard in development mode.

## Prerequisites

- .NET 8.0 SDK
- Node.js 18+ and npm
- Visual Studio Code (recommended) or your preferred IDE

## Quick Start - All Services

### Option 1: Using VS Code Launch Configurations (Recommended)

1. Open the workspace in VS Code
2. Press `F5` or go to Run and Debug
3. Select **"Full Stack (Development)"** from the dropdown
4. Click the green play button

This will start all services:
- RoomServer on `http://127.0.0.1:40801`
- RoomOperator on `http://127.0.0.1:40802`
- Integration API on `http://127.0.0.1:40901`
- Operator Dashboard on `http://localhost:5173`

### Option 2: Manual Start

#### 1. RoomServer

```bash
cd server-dotnet/src/RoomServer
dotnet run --environment Development
```

The server will start on `http://127.0.0.1:40801`

#### 2. RoomOperator

```bash
cd server-dotnet/operator
dotnet run --environment Development
```

The operator will start on `http://127.0.0.1:40802`

#### 3. Integration API

```bash
cd tools/integration-api
npm install
npm run dev
```

The API will start on `http://127.0.0.1:40901`

#### 4. Test Client Dependencies

**IMPORTANT:** Before running tests, install test-client dependencies:

```bash
cd server-dotnet/operator/test-client
npm install
```

#### 5. Operator Dashboard

```bash
cd apps/operator-dashboard
npm install
npm run dev
```

The dashboard will be available at `http://localhost:5173`

## Development Configuration

### RoomServer Configuration

Development settings are in `server-dotnet/src/RoomServer/appsettings.Development.json`:

- **Port**: 40801
- **Environment**: Development
- **Logging**: Debug level
- **MCP Servers**: Configured for local WebSocket connections

### RoomOperator Configuration

Development settings are in `server-dotnet/operator/appsettings.Development.json`:

- **Port**: 40802
- **Environment**: Development
- **Logging**: Debug level
- **RoomServer URL**: `http://127.0.0.1:40801`
- **Auth Token**: `dev-token-operator` (for development only)
- **Scope Validation**: Disabled (for easier development)
- **Reconciliation Interval**: 5 seconds (slower for debugging)
- **Guardrails**: Relaxed (RequireConfirmHeader: false)

**Key Development Features:**
- Auth validation disabled for easier testing
- Slower reconciliation interval for observability
- Debug logging enabled
- Confirm headers not required for operations

### Integration API Configuration

Configuration in `configs/dashboard.settings.json`:

- **Port**: 40901
- **RoomServer URL**: `http://127.0.0.1:40801`
- **RoomOperator URL**: `http://127.0.0.1:40802`
- **Test Runner**: `scripts/run-test-client.sh`
- **Artifacts Directory**: `.artifacts/integration`

### Dashboard Configuration

Vite development server on port 5173, configured to proxy API requests to Integration API on port 40901.

## VS Code Launch Configurations

The workspace includes several launch configurations in `.vscode/launch.json`:

### Individual Services

- **RoomServer (Development)**: Launch RoomServer alone
- **RoomOperator (Development)**: Launch RoomOperator alone
- **Integration API (Development)**: Launch Integration API alone
- **Operator Dashboard (Development)**: Launch Dashboard alone

### Compound Configurations

- **Full Stack (Development)**: Launch all services together
- **Backend Only (Development)**: Launch RoomServer, RoomOperator, and Integration API (no Dashboard)

## Testing the Setup

### 1. Verify Services Are Running

Open these URLs in your browser:

- RoomServer: http://127.0.0.1:40801/health
- RoomOperator: http://127.0.0.1:40802/health  
- Integration API: http://127.0.0.1:40901/api/config
- Dashboard: http://localhost:5173

### 2. Check Dashboard Health

1. Navigate to http://localhost:5173
2. Go to the Overview page
3. All services should show as "healthy" (green checkmark)

### 3. Test Event Streaming

1. Go to the Events page
2. You should see events streaming in real-time from both RoomServer and RoomOperator

### 4. Run a Test

1. Go to the Tests page
2. Select a test scenario
3. Click "Run Test"
4. Logs should stream in real-time
5. Exit code should be displayed when complete

## Troubleshooting

### Port Already in Use

If you get a "port already in use" error:

```bash
# Check what's using the port (example for port 40801)
lsof -i :40801  # macOS/Linux
netstat -ano | findstr :40801  # Windows

# Kill the process or change the port in configuration
```

### RoomOperator Can't Connect to RoomServer

1. Verify RoomServer is running on port 40801
2. Check `appsettings.Development.json` for correct URL
3. Look at RoomOperator logs for connection errors

### Test Client Missing Dependencies

If you get "Cannot find package 'axios'" error:

```bash
cd server-dotnet/operator/test-client
npm install
```

### Integration API Not Starting

```bash
cd tools/integration-api
rm -rf node_modules package-lock.json
npm install
npm run dev
```

### Dashboard Build Errors

```bash
cd apps/operator-dashboard
rm -rf node_modules package-lock.json
npm install
npm run dev
```

## Development Workflow

### Making Changes to RoomServer/RoomOperator

1. Make your changes in the code
2. Stop the running service (Ctrl+C or stop in VS Code)
3. Restart the service (dotnet run or F5 in VS Code)
4. Changes will be reflected immediately

### Making Changes to Integration API

The API runs with `ts-node` in watch mode, so changes are reflected automatically without restart.

### Making Changes to Dashboard

Vite provides hot module replacement (HMR), so changes appear instantly in the browser.

### Running Tests During Development

From the dashboard:
1. Navigate to Tests page
2. Select scenario
3. Click "Run Test"
4. Watch logs stream in real-time

From command line:
```bash
cd server-dotnet/operator/test-client
npm run test:basic
# or
npm run test:error
# or
npm run test:all
```

## Environment Variables

### RoomServer

- `ASPNETCORE_ENVIRONMENT`: Set to "Development"
- `ASPNETCORE_URLS`: `http://127.0.0.1:40801`

### RoomOperator

- `ASPNETCORE_ENVIRONMENT`: Set to "Development"
- `ASPNETCORE_URLS`: `http://127.0.0.1:40802`
- `ROOM_AUTH_TOKEN`: Optional, defaults to config value

### Integration API

- `NODE_ENV`: Set to "development"
- `PORT`: 40901

## Debugging

### .NET Services (RoomServer/RoomOperator)

1. Set breakpoints in VS Code
2. Press F5 and select the appropriate launch configuration
3. Breakpoints will be hit when code executes

### Integration API (Node.js)

1. Set breakpoints in VS Code
2. Press F5 and select "Integration API (Development)"
3. Breakpoints will be hit when API endpoints are called

### Dashboard (React)

1. Use browser DevTools
2. Open Sources tab
3. Set breakpoints in TypeScript/JavaScript code
4. Or use React DevTools extension

## Logs

### RoomServer Logs

Visible in the terminal where RoomServer is running, or in VS Code Debug Console.

Log level: Debug (configured in appsettings.Development.json)

### RoomOperator Logs

Visible in the terminal where RoomOperator is running, or in VS Code Debug Console.

Log level: Debug (configured in appsettings.Development.json)

### Integration API Logs

Visible in the terminal where Integration API is running.

Uses `morgan` for HTTP request logging in dev mode.

### Dashboard Logs

- Browser console for React app logs
- Network tab for API requests
- Events page for real-time service events

## Production vs Development

### Key Differences

| Feature | Development | Production |
|---------|------------|------------|
| Auth Validation | Disabled | Enabled |
| Logging Level | Debug | Information/Warning |
| CORS | Permissive | Restricted |
| Reconciliation Interval | 5s | 2s |
| Guardrails | Relaxed | Strict |
| Error Details | Full stack traces | Sanitized messages |

### Switching to Production

To run in production mode:

```bash
# RoomServer
ASPNETCORE_ENVIRONMENT=Production dotnet run

# RoomOperator  
ASPNETCORE_ENVIRONMENT=Production dotnet run

# Integration API
NODE_ENV=production npm start

# Dashboard
npm run build
npm run preview
```

## Next Steps

- Read [TEST_SETUP.md](./TEST_SETUP.md) for testing documentation
- Read [QUICKSTART_GUIDE.md](./QUICKSTART_GUIDE.md) for usage guide
- Read [VALIDATION_CHECKLIST.md](./VALIDATION_CHECKLIST.md) for feature validation

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review logs for error messages
3. Check GitHub issues
4. Create a new issue with detailed information
