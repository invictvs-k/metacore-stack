# Port Configuration Guide

This document describes the standardized port configuration for all components in the Metacore Stack.

## Port Assignments

| Component          | Port  | URL                            | Configuration File                              |
| ------------------ | ----- | ------------------------------ | ----------------------------------------------- |
| RoomServer         | 40801 | http://localhost:40801         | server-dotnet/src/RoomServer/appsettings.json  |
| RoomOperator       | 40802 | http://localhost:40802         | server-dotnet/operator/appsettings.json         |
| Integration API    | 40901 | http://localhost:40901         | configs/dashboard.settings.json                 |
| Dashboard UI       | 5173  | http://localhost:5173          | apps/operator-dashboard/vite.config.ts          |

## Configuration Files

### RoomServer (server-dotnet/src/RoomServer/appsettings.json)

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:40801"
      }
    }
  }
}
```

### RoomOperator (server-dotnet/operator/appsettings.json)

```json
{
  "RoomServer": {
    "BaseUrl": "http://localhost:40801",
    "HubPath": "/hub"
  },
  "HttpApi": {
    "Port": 40802
  }
}
```

### Test Client (server-dotnet/operator/test-client/config.js)

```javascript
export default {
  operator: {
    baseUrl: process.env.OPERATOR_URL || 'http://localhost:40802',
  },
  roomServer: {
    baseUrl: process.env.ROOMSERVER_URL || 'http://localhost:40801',
  }
}
```

### Dashboard Settings (configs/dashboard.settings.json)

```json
{
  "roomServer": {
    "baseUrl": "http://127.0.0.1:40801"
  },
  "roomOperator": {
    "baseUrl": "http://127.0.0.1:40802"
  },
  "integrationApi": {
    "port": 40901
  }
}
```

### Dashboard Vite Config (apps/operator-dashboard/vite.config.ts)

```typescript
export default defineConfig({
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:40901',
        changeOrigin: true
      }
    }
  }
})
```

## Running the Components

### Start RoomServer

```bash
cd server-dotnet/src/RoomServer
dotnet run
# Now listening on: http://localhost:40801
```

### Start RoomOperator

```bash
cd server-dotnet/operator
dotnet run
# Now listening on: http://0.0.0.0:40802
# Connecting to RoomServer at http://localhost:40801
```

### Start Integration API

```bash
cd tools/integration-api
npm install
npm run dev
# Integration API listening on port 40901
```

### Start Dashboard

```bash
cd apps/operator-dashboard
npm install
npm run dev
# Dashboard running at http://localhost:5173
```

### Run Test Client

```bash
cd server-dotnet/operator/test-client
npm install
npm run test:all
# Uses OPERATOR_URL=http://localhost:40802
# Uses ROOMSERVER_URL=http://localhost:40801
```

## Environment Variables

You can override the default ports using environment variables:

```bash
# For RoomOperator
export OPERATOR_URL=http://localhost:40802

# For RoomServer  
export ROOMSERVER_URL=http://localhost:40801

# For authentication (optional)
export ROOM_AUTH_TOKEN=your-token-here
```

## Integration Scripts

All integration test scripts have been updated to use the new ports:

- `server-dotnet/operator/scripts/run-integration-test.sh`
- `server-dotnet/operator/scripts/run-tests.sh`
- `server-dotnet/operator/scripts/run-operator.sh`

## Migration Notes

### Changed Ports

- **RoomServer**: 5000 → 40801
- **RoomOperator**: 8080 → 40802

### Why These Ports?

The ports 40801/40802 were chosen because:

1. They were already used in the Test configurations (appsettings.Test.json)
2. They match the Dashboard's expectations
3. They avoid conflicts with common development ports (3000, 5000, 8080)
4. They're in a high port range that typically doesn't require admin privileges

## Troubleshooting

### Port Already in Use

If you get errors about ports being in use:

```bash
# Check what's using the port
lsof -ti:40801
lsof -ti:40802

# Kill the process
kill $(lsof -t -i:40801)
kill $(lsof -t -i:40802)
```

### RoomOperator Can't Connect to RoomServer

1. Verify RoomServer is running: `curl http://localhost:40801/health`
2. Check the logs for connection errors
3. Ensure no firewall is blocking the port

### Test Client Failures

1. Verify both RoomServer and RoomOperator are running
2. Check environment variables: `echo $OPERATOR_URL $ROOMSERVER_URL`
3. Use `VERBOSE=true npm run test:basic` for detailed logs
