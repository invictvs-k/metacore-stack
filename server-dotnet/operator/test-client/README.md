# RoomOperator Test Client

A comprehensive test client for validating RoomOperator-RoomServer integration. This client executes various test scenarios to verify functionality, error handling, and performance.

## Features

- ✅ **Basic Flow Testing**: Complete lifecycle validation (create room → add entities → seed artifacts → update policies)
- ✅ **Error Handling**: Test invalid specs, error recovery, and idempotency
- ✅ **Stress Testing**: Performance testing with large entity sets and rapid operations
- ✅ **Detailed Logging**: Color-coded output with timestamps and structured data
- ✅ **Configurable**: Environment-based configuration for different test environments

## Prerequisites

- Node.js 20+ (ESM support required)
- Running RoomOperator instance (default: http://localhost:8080)
- Running RoomServer instance (default: http://localhost:5000)
- Optional: Authentication token for secured endpoints

## Installation

```bash
cd server-dotnet/operator/test-client
npm install
```

## Configuration

Configure via environment variables:

```bash
# RoomOperator URL
export OPERATOR_URL=http://localhost:8080

# RoomServer URL
export ROOMSERVER_URL=http://localhost:5000

# Authentication token (if required)
export ROOM_AUTH_TOKEN=your-token-here

# Test room ID
export TEST_ROOM_ID=room-test-integration

# Enable verbose logging
export VERBOSE=true
```

Or edit `config.js` directly for default values.

## Usage

### Run All Tests

```bash
npm run test:all
```

### Run Individual Scenarios

**Basic Flow (Happy Path):**
```bash
npm run test:basic
```

**Error Cases:**
```bash
npm run test:error
```

**Stress Test:**
```bash
npm run test:stress
```

### Run with Custom Configuration

```bash
OPERATOR_URL=http://localhost:8080 \
ROOMSERVER_URL=http://localhost:5000 \
VERBOSE=true \
npm run test:basic
```

## Test Scenarios

### 1. Basic Flow (`scenarios/basic-flow.js`)

Tests the complete reconciliation lifecycle:

- ✓ Preflight checks (health endpoints)
- ✓ Create empty room
- ✓ Add entities incrementally
- ✓ Seed artifacts (with file references)
- ✓ Update policies
- ✓ Verify final state
- ✓ Cleanup

**Expected Output:**
```
═══════════════════════════════════════════════════
  Basic Flow Scenario - Happy Path
═══════════════════════════════════════════════════

═══ Step 1: Preflight Checks ═══
[+0.123s] INFO    Checking operator health...
[+0.245s] SUCCESS Operator is healthy

═══ Step 2: Create Empty Room ═══
[+1.123s] SUCCESS Room created successfully
[+2.345s] SUCCESS Idempotency verified

...

Test Summary:
  ✓ Passed: 12
  ✗ Failed: 0
  Success Rate: 100.0%
```

### 2. Error Cases (`scenarios/error-cases.js`)

Tests error handling and validation:

- ✓ Invalid spec structure → 400 Bad Request
- ✓ Invalid room ID format → 400 Bad Request
- ✓ Missing required fields → Error handling
- ✓ Dry run mode (X-Dry-Run header)
- ✓ Idempotency verification

**Expected Behaviors:**
- Invalid specs rejected with appropriate error codes
- Dry run doesn't modify state
- Repeated operations are idempotent

### 3. Stress Test (`scenarios/stress-test.js`)

Tests performance and load handling:

- ✓ Rapid spec applications (5 operations)
- ✓ Large entity sets (20+ entities)
- ✓ Convergence time measurement
- ✓ Performance metrics reporting

**Metrics Tracked:**
- Total operations
- Success rate
- Average/min/max duration
- Convergence time

**Expected Output:**
```
═══════════════════════════════════════════════════
  Performance Metrics
═══════════════════════════════════════════════════

Total Operations:      8
Successful:            8
Failed:                0
Success Rate:          100.0%
Total Duration:        4523ms
Average Duration:      565.38ms
Min Duration:          234ms
Max Duration:          1245ms
```

## Directory Structure

```
test-client/
├── config.js                    # Configuration and test data
├── index.js                     # Main entry point
├── package.json                 # Dependencies and scripts
├── README.md                    # This file
├── scenarios/
│   ├── basic-flow.js           # Happy path testing
│   ├── error-cases.js          # Error handling tests
│   └── stress-test.js          # Performance tests
└── utils/
    ├── http-client.js          # HTTP client wrapper (axios)
    ├── logger.js               # Logging utility
    └── message-builder.js      # RoomSpec builder
```

## Test Data

Default test configuration includes:

**Entities:**
- `E-orchestrator-test` - Orchestrator with full permissions
- `E-agent-test-1` - Agent with read capabilities
- `E-agent-test-2` - Agent with no capabilities

**Artifacts:**
- `test-document-1` - Sample document
- `test-config` - JSON configuration file

**Policies:**
```json
{
  "dmVisibilityDefault": "team",
  "allowResourceCreation": false,
  "maxArtifactsPerEntity": 100
}
```

## Customization

### Adding Custom Scenarios

Create a new file in `scenarios/`:

```javascript
#!/usr/bin/env node

import { config, logger, HttpClient, MessageBuilder } from '../index.js';

class MyCustomScenario {
  constructor() {
    this.client = new HttpClient(config, logger);
    this.testsPassed = 0;
    this.testsFailed = 0;
  }

  async run() {
    logger.section('My Custom Scenario');
    
    try {
      await this.test1_MyTest();
      
      const success = logger.summary(this.testsPassed, this.testsFailed);
      process.exit(success ? 0 : 1);
    } catch (error) {
      logger.error('Scenario failed', error);
      process.exit(1);
    }
  }

  async test1_MyTest() {
    logger.step(1, 'My Test');
    
    // Your test logic here
    const spec = MessageBuilder.buildMinimalSpec(config.testRoom.roomId);
    const result = await this.client.applySpec(spec);
    
    if (result.success) {
      logger.success('Test passed');
      this.testsPassed++;
    } else {
      logger.error('Test failed', result.error);
      this.testsFailed++;
    }
  }
}

if (import.meta.url === `file://${process.argv[1]}`) {
  const scenario = new MyCustomScenario();
  scenario.run();
}

export default MyCustomScenario;
```

Add to `package.json`:
```json
{
  "scripts": {
    "test:custom": "node scenarios/my-custom-scenario.js"
  }
}
```

### Modifying Test Data

Edit `config.js` to customize:
- Entity definitions
- Artifact specifications
- Policy defaults
- Execution parameters (delays, retries)

## Troubleshooting

### Connection Refused Errors

**Problem:**
```
ERROR   Request error: connect ECONNREFUSED 127.0.0.1:8080
```

**Solution:**
1. Verify RoomOperator is running: `curl http://localhost:8080/health`
2. Check OPERATOR_URL environment variable
3. Ensure correct port number

### Authentication Failures

**Problem:**
```
ERROR   Failed to apply spec: 401 Unauthorized
```

**Solution:**
1. Set `ROOM_AUTH_TOKEN` environment variable
2. Verify token validity and scopes
3. Check token isn't expired

### Tests Timing Out

**Problem:**
Tests hang without completing

**Solution:**
1. Increase timeout in `config.js`:
   ```javascript
   operator: {
     timeout: 60000, // 60 seconds
   }
   ```
2. Check RoomOperator logs for errors
3. Verify RoomServer is responsive

### Idempotency Tests Failing

**Problem:**
```
WARN    Different results between applies
```

**Solution:**
1. This is expected if room state isn't being properly tracked
2. Check operator logs for reconciliation details
3. Verify operator's diff calculation is working
4. May need longer delays between operations

## Integration with CI/CD

### GitHub Actions Example

```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  integration-test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0'
      
      - name: Start RoomServer
        run: |
          cd server-dotnet/src/RoomServer
          dotnet run &
          sleep 10
      
      - name: Start RoomOperator
        run: |
          cd server-dotnet/operator
          dotnet run &
          sleep 10
      
      - name: Install test client dependencies
        run: |
          cd server-dotnet/operator/test-client
          npm install
      
      - name: Run integration tests
        run: |
          cd server-dotnet/operator/test-client
          npm run test:all
```

## Best Practices

1. **Always cleanup**: Tests should remove created resources
2. **Use unique IDs**: Avoid conflicts with other tests
3. **Check prerequisites**: Verify services are healthy before running tests
4. **Handle timeouts**: Services might be slow under load
5. **Log extensively**: Use verbose mode for debugging
6. **Test idempotency**: Verify operations can be safely retried

## API Documentation

See [ROOMOPERATOR_ROOMSERVER_INTEGRATION.md](../../../docs/ROOMOPERATOR_ROOMSERVER_INTEGRATION.md) for complete API reference and communication patterns.

## Contributing

When adding new test scenarios:
1. Follow existing patterns in `scenarios/`
2. Use utilities from `utils/` for consistency
3. Add comprehensive logging
4. Update this README
5. Add npm script to `package.json`

## Support

- **Documentation**: `/docs/ROOMOPERATOR_ROOMSERVER_INTEGRATION.md`
- **GitHub Issues**: https://github.com/invictvs-k/metacore-stack/issues
- **Operator Docs**: `/docs/room-operator.md`

---

**Version**: 1.0.0  
**Last Updated**: 2025-10-19
