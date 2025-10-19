# Quick Start Guide - Observability & Execution Fix

## What Was Fixed

This implementation addresses all issues mentioned in the problem statement related to observability and test execution in the Dashboard and Integration API.

### Key Improvements

1. **Real-time Observability** ✅
   - Events update continuously without changing routes/tabs
   - SSE connections with heartbeat and exponential backoff reconnection
   - Pause/resume and auto-scroll controls
   - Event windowing (last 2000 events)

2. **Test Execution** ✅
   - Start/stop functionality
   - Real-time stdout/stderr streaming by runId
   - Artifact persistence in `.artifacts/integration/{timestamp}/runs/{runId}/`
   - Result.json with complete metadata
   - Cross-platform runner (Windows/Linux)

3. **Command Execution** ✅
   - Command catalog loaded from file
   - JSON Schema parameter validation
   - Clear feedback on validation errors
   - Transparent proxy to RoomOperator

4. **Dynamic Configuration** ✅
   - Edit and validate configuration
   - Hot reload detection (no API restart needed)
   - Connection testing for all endpoints
   - Actionable validation errors

5. **Resilience** ✅
   - SSE with exponential backoff (1.5x, max 30s)
   - Heartbeat every 10s
   - Proper cleanup on disconnect
   - Detailed error messages

## Quick Start

### 1. Install Dependencies

```bash
# Integration API
cd tools/integration-api
npm install

# Dashboard
cd apps/operator-dashboard
npm install
```

### 2. Start Services

```bash
# Terminal 1: Integration API
cd tools/integration-api
npm run dev

# Terminal 2: Dashboard
cd apps/operator-dashboard
npm run dev
```

### 3. Access Dashboard

Open http://localhost:5173 in your browser.

## Testing the Implementation

### Overview Page
1. Navigate to Overview (/)
2. Verify health checks show status for RoomServer and RoomOperator
3. Click "Refresh" to manually update health status
4. Click "Run All Tests" to execute all test scenarios
5. Observe quick stats (events received, services healthy, etc.)

### Events Page
1. Navigate to Events (/events)
2. Verify events appear in real-time
3. Click "Pause" - events should stop appearing
4. Click "Resume" - events should continue
5. Toggle "Auto-scroll" - page should/shouldn't scroll to new events
6. Try filtering by RoomServer/RoomOperator
7. Click "Clear Events" to reset

### Tests Page
1. Navigate to Tests (/tests)
2. Select a test scenario from dropdown
3. Click "Run Test"
4. Observe:
   - Logs streaming in real-time
   - Exit code displayed when complete
   - Artifacts directory path shown
   - runId displayed
5. Try running "All Tests"
6. Run another test without refreshing page

### Commands Page
1. Navigate to Commands (/commands)
2. Verify command catalog loads
3. Select "Load MCP Providers" command
4. Edit parameters JSON (try both valid and invalid)
5. Click "Execute Command"
6. Observe:
   - Validation errors for invalid params
   - Execution result for valid params

### Settings Page
1. Navigate to Settings (/settings)
2. Click "Test Connections"
3. Verify connection test results show for:
   - RoomServer
   - RoomOperator
   - MCP Status
4. Edit configuration JSON
5. Click "Save"
6. Verify success message
7. Open another tab and change config file externally
8. Wait for hot reload notification to appear
9. Click "Reload" to sync

## Validation Against Requirements

### From Problem Statement Section 3 (API Contracts)

✅ **3.1 Config**
- GET /api/config returns full config
- PUT /api/config validates and saves
- GET /api/config/version returns version + checksum
- Validates URLs, port, runner, paths

✅ **3.2 Events (SSE)**
- GET /api/events/roomserver, /roomoperator, /combined
- Headers: text/event-stream, no-cache, keep-alive
- Heartbeat: `: ping` every 10s
- Retry hint: `retry: 5000`
- Typed events with source and timestamp

✅ **3.3 Tests**
- GET /api/tests lists scenarios
- POST /api/tests/run returns { runId, artifactsDir, logPath }
- GET /api/tests/stream/:runId emits started, log, done, error events
- GET /api/tests/runs/:runId returns result.json
- Cross-platform runner with shell: true
- Artifacts in .artifacts/integration/{timestamp}/runs/{runId}/

✅ **3.4 Commands**
- GET /api/commands reads catalog from file
- POST /api/commands/execute validates params with Ajv
- Returns statusCode and body transparently
- Validation errors are actionable

✅ **3.5 MCP**
- GET /api/mcp/status proxies to RoomServer
- Returns error if unavailable

### From Problem Statement Section 5 (Frontend)

✅ **5.1 Observability (Events)**
- Consumes /api/events/combined via SSE
- Continuous updates without route change
- Filter by source
- Pause/resume functionality
- Auto-scroll with toggle
- Sliding window (2000 events)

✅ **5.2 Tests**
- Lists scenarios via GET /api/tests
- Run/Run All opens SSE stream
- Shows logs incrementally
- Displays exit code and status
- Links to artifacts directory
- Multiple runs without reload

✅ **5.3 Commands**
- Loads catalog via GET /api/commands
- Shows description and schema
- JSON editor with pre-validation
- Execute button calls POST /api/commands/execute
- Shows response and HTTP status
- Clear validation errors

✅ **5.4 Settings**
- Form generated from config structure
- Load, Save, Reset defaults actions
- Test connections (pings all services)
- Hot reload detection via checksum polling

✅ **5.5 Overview**
- Health checks (ping + status MCP)
- Recent runs (via test state)
- Quick actions (Run all, Clean artifacts)
- Poll every 10s for updates

## Architecture Decisions

### Why SSE over WebSocket?
- Simpler protocol (one-way communication sufficient)
- Built-in reconnection in EventSource
- Works through most firewalls/proxies
- No additional dependencies needed

### Why Exponential Backoff?
- Prevents hammering server on failures
- Standard resilience pattern
- Configurable via dashboard settings

### Why Event Windowing?
- Prevents memory leaks from unlimited event accumulation
- 2000 events is ~10-20 minutes of high activity
- User can clear manually if needed

### Why JSON Schema for Commands?
- Standard validation format
- Already used in command catalog spec
- Provides detailed error messages
- No custom validation logic needed

## Troubleshooting

**Events not appearing:**
- Check RoomServer/RoomOperator are running
- Verify baseUrls in config are correct
- Check browser console for SSE errors
- Try refreshing the page

**Tests not running:**
- Verify test scenarios exist in configured path
- Check runner script is executable
- Look for errors in Integration API logs
- Ensure paths use forward slashes

**Commands failing:**
- Verify RoomOperator is accessible
- Check command catalog file exists
- Validate params match schema
- Review command endpoint configuration

**Config not saving:**
- Check validation errors in response
- Verify URLs are valid
- Ensure port is in valid range (1-65535)
- Check file permissions on config file

## What's Next

The implementation is complete and ready for testing. Follow the validation checklist in VALIDATION_CHECKLIST.md for comprehensive testing.

Key files to review:
- `tools/integration-api/src/routes/*.ts` - API implementations
- `apps/operator-dashboard/src/pages/*.tsx` - UI pages
- `apps/operator-dashboard/src/hooks/useSSE.ts` - SSE hook
- `server-dotnet/operator/commands/commands.catalog.json` - Command catalog
