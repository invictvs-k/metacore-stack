# Test Client Setup and Execution

## Prerequisites

The test client requires Node.js dependencies to be installed before tests can run.

### Installation

```bash
# Navigate to test-client directory
cd server-dotnet/operator/test-client

# Install dependencies
npm install
```

This will install:
- `axios` - HTTP client for making requests to RoomServer and RoomOperator

## Running Tests

### From the Dashboard

1. Start the Integration API:
   ```bash
   cd tools/integration-api
   npm run dev
   ```

2. Start the Dashboard:
   ```bash
   cd apps/operator-dashboard
   npm run dev
   ```

3. Navigate to http://localhost:5173/tests

4. Select a test scenario and click "Run Test"

### From Command Line

```bash
cd server-dotnet/operator/test-client

# Run specific test
npm run test:basic
npm run test:error
npm run test:stress

# Run all tests
npm run test:all
```

## Available Test Scenarios

- **basic-flow** - Basic RoomServer and RoomOperator interaction
- **basic-flow-enhanced** - Enhanced basic flow with additional validations
- **error-cases** - Error handling and edge cases
- **stress-test** - Load and performance testing
- **mcp/** - MCP (Model Context Protocol) integration tests

## Troubleshooting

### "Cannot find package 'axios'" Error

**Problem:** Test execution fails with module not found errors.

**Solution:** Install test-client dependencies:
```bash
cd server-dotnet/operator/test-client
npm install
```

### "Run not found" Error

**Problem:** The dashboard shows "Run not found" error.

**Root Cause:** Usually occurs when:
1. Test-client dependencies are not installed
2. The test runner script doesn't have execute permissions
3. The scenarios path is incorrect in the configuration

**Solution:**
1. Ensure dependencies are installed (see above)
2. Check `configs/dashboard.settings.json` for correct `scenariosPath`
3. Verify scenarios exist in `server-dotnet/operator/test-client/scenarios/`

### Exit Code 1

**Problem:** Tests complete with exit code 1.

**Possible Causes:**
- RoomServer or RoomOperator not running
- Services not accessible at configured URLs
- Test assertions failing

**Solution:**
1. Verify RoomServer is running on the configured port (default: 40801)
2. Verify RoomOperator is running on the configured port (default: 40802)
3. Check test logs in `.artifacts/integration/` for specific errors

## Configuration

Test client configuration is in `configs/dashboard.settings.json`:

```json
{
  "testClient": {
    "runner": "scripts/run-test-client.sh",
    "scenariosPath": "server-dotnet/operator/test-client/scenarios",
    "artifactsDir": ".artifacts/integration"
  }
}
```

- **runner**: Script that runs the tests (not used when running via dashboard)
- **scenariosPath**: Path to test scenario files relative to repository root
- **artifactsDir**: Directory where test artifacts and logs are stored

## Artifacts

Test results are stored in `.artifacts/integration/{timestamp}/runs/{runId}/`:

- `test-client.log` - Complete test execution log
- `result.json` - Test metadata (runId, status, exitCode, timestamps, artifactsPath)

## Development

### Adding New Test Scenarios

1. Create a new `.js` file in `server-dotnet/operator/test-client/scenarios/`
2. Follow the existing pattern from other scenarios
3. Use the http-client utility for making requests
4. The new scenario will automatically appear in the dashboard

Example:
```javascript
// scenarios/my-new-test.js
import { testRoomServerConnection } from '../utils/http-client.js';

console.log('Running my new test...');

try {
  await testRoomServerConnection();
  console.log('✅ Test passed');
  process.exit(0);
} catch (error) {
  console.error('❌ Test failed:', error.message);
  process.exit(1);
}
```

### Testing Locally

Run individual scenarios directly:
```bash
cd server-dotnet/operator/test-client
node scenarios/basic-flow.js
```

Environment variables that can be set:
- `OPERATOR_URL` - RoomOperator base URL (default from config)
- `ROOMSERVER_URL` - RoomServer base URL (default from config)
- `ARTIFACTS_DIR` - Where to store artifacts (automatically set by Integration API)
