---
title: Integration Testing Guide
status: active
owners: []
tags: [testing, howto, integration]
last_review: 2025-10-20
links: []
---

# Integration Testing Guide

Complete guide for testing RoomOperator-RoomServer integration.

## Overview

This guide covers how to test the integration between RoomOperator (reconciliation controller) and RoomServer (SignalR/REST service). The test suite includes automated scenarios for functional testing, error handling, and performance validation.

## Quick Start

### Prerequisites

1. **.NET 8 SDK** - For running RoomServer and RoomOperator
2. **Node.js 20+** - For running the test client
3. **Ports Available**: 5000 (RoomServer), 8080 (RoomOperator)

### Automated Integration Test

Run everything with a single command:

```bash
cd server-dotnet/operator/scripts
./run-integration-test.sh
```

This script will:
1. Build projects
2. Start RoomServer (port 5000)
3. Start RoomOperator (port 8080)
4. Run test client scenarios
5. Clean up processes on completion

### Manual Setup

If you prefer to run components separately:

**Terminal 1 - RoomServer:**
```bash
cd server-dotnet/src/RoomServer
export ASPNETCORE_ENVIRONMENT=Test
dotnet run
```

**Terminal 2 - RoomOperator:**
```bash
cd server-dotnet/operator
export ROOM_AUTH_TOKEN=test-token  # Optional
dotnet run
```

**Terminal 3 - Test Client:**
```bash
cd server-dotnet/operator/test-client
npm install
npm run test:all
```

## Test Scenarios

### 1. Basic Flow (Happy Path)

Tests the complete reconciliation lifecycle.

**Run:**
```bash
npm run test:basic
```

**What it tests:**
- ✓ Health checks (Operator and RoomServer)
- ✓ Room creation with minimal spec
- ✓ Entity joining (3 different entity types)
- ✓ Artifact seeding (documents and configs)
- ✓ Policy updates
- ✓ State verification
- ✓ Idempotency (reapplying same spec)
- ✓ Cleanup (removing all entities)

**Expected Duration:** 10-15 seconds

**Success Criteria:**
- All health checks pass
- Entities joined successfully
- State matches expected configuration
- Idempotency verified (no errors on reapply)
- Cleanup removes all entities

### 2. Error Cases

Tests error handling and validation.

**Run:**
```bash
npm run test:error
```

**What it tests:**
- ✓ Invalid spec structure → 400 Bad Request
- ✓ Invalid room ID format → 400 Bad Request
- ✓ Missing required fields → Error handling
- ✓ Dry run mode (X-Dry-Run header)
- ✓ Idempotency with repeated operations

**Expected Duration:** 8-12 seconds

**Success Criteria:**
- Invalid specs rejected with appropriate HTTP status codes
- Dry run doesn't modify state
- Repeated operations are idempotent

### 3. Stress Test

Tests performance and load handling.

**Run:**
```bash
npm run test:stress
```

**What it tests:**
- ✓ Rapid spec applications (5 sequential operations)
- ✓ Large entity sets (20+ entities in single spec)
- ✓ Convergence time measurement
- ✓ Performance metrics reporting

**Expected Duration:** 15-25 seconds

**Success Criteria:**
- 80%+ success rate on rapid operations
- All entities joined in large set
- Reasonable convergence time (< 10 seconds)
- No memory leaks or crashes

### 4. Run All Scenarios

**Run:**
```bash
npm run test:all
```

Runs all scenarios sequentially. Total duration: ~35-50 seconds.

## Test Client Architecture

```
test-client/
├── index.js                    # Entry point and exports
├── config.js                   # Configuration and test data
├── package.json                # Dependencies and scripts
├── scenarios/
│   ├── basic-flow.js          # Happy path tests
│   ├── error-cases.js         # Error handling tests
│   └── stress-test.js         # Performance tests
└── utils/
    ├── http-client.js         # HTTP client (axios wrapper)
    ├── logger.js              # Logging with colors
    └── message-builder.js     # RoomSpec builder
```

### Key Components

**HttpClient (`utils/http-client.js`)**
- Axios-based client with interceptors
- Automatic retry with exponential backoff
- Error handling and logging
- Separate clients for Operator and RoomServer

**Logger (`utils/logger.js`)**
- Timestamped output with elapsed time
- Color-coded by severity (INFO, SUCCESS, WARN, ERROR, DEBUG)
- Structured data output (JSON formatting)
- Summary reports with pass/fail counts

**MessageBuilder (`utils/message-builder.js`)**
- Builds valid RoomSpec structures
- Validates spec format
- Provides convenience methods for common specs
- Entity, artifact, and policy builders

## Configuration

### Environment Variables

```bash
# RoomOperator URL
export OPERATOR_URL=http://localhost:8080

# RoomServer URL  
export ROOMSERVER_URL=http://localhost:5000

# Authentication token (optional)
export ROOM_AUTH_TOKEN=your-token

# Test room ID
export TEST_ROOM_ID=room-test-integration

# Enable verbose logging
export VERBOSE=true
```

### Test Data Customization

Edit `test-client/config.js` to customize:

**Entities:**
```javascript
entities: [
  {
    id: 'E-orchestrator-test',
    kind: 'orchestrator',
    displayName: 'Test Orchestrator',
    // ...
  },
  // Add more entities
]
```

**Artifacts:**
```javascript
artifacts: [
  {
    name: 'test-document-1',
    type: 'document',
    workspace: 'shared',
    tags: ['test'],
    content: 'Sample content',
  },
  // Add more artifacts
]
```

**Execution Settings:**
```javascript
execution: {
  delayBetweenOperations: 1000, // ms
  maxRetries: 3,
  retryDelay: 2000, // ms
}
```

## Interpreting Results

### Successful Test Output

```
═══════════════════════════════════════════════════
  Basic Flow Scenario - Happy Path
═══════════════════════════════════════════════════

═══ Step 1: Preflight Checks ═══
[+0.123s] INFO    Checking operator health...
[+0.245s] SUCCESS Operator is healthy
[+0.367s] SUCCESS RoomServer is healthy

═══ Step 2: Create Empty Room ═══
[+1.234s] SUCCESS Room created successfully
[+2.345s] SUCCESS Idempotency verified

...

────────────────────────────────────────────────────────────
Test Summary:
  ✓ Passed: 12
  ✗ Failed: 0
  Success Rate: 100.0%
```

### Failed Test Output

```
[+5.123s] ERROR   Failed to add entities
{
  "operation": "applySpec",
  "status": 500,
  "message": "Internal server error",
  "data": { ... }
}

────────────────────────────────────────────────────────────
Test Summary:
  ✓ Passed: 5
  ✗ Failed: 3
  Success Rate: 62.5%
```

### Performance Metrics

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

## Troubleshooting

### Common Issues

#### 1. Connection Refused

**Symptom:**
```
ERROR   Request error: connect ECONNREFUSED 127.0.0.1:8080
```

**Solutions:**
- Verify RoomOperator is running: `curl http://localhost:8080/health`
- Check if port 8080 is available: `lsof -i :8080`
- Verify OPERATOR_URL environment variable

#### 2. RoomServer Not Responding

**Symptom:**
```
ERROR   No response from server
```

**Solutions:**
- Check RoomServer logs for errors
- Verify port 5000 is accessible: `curl http://localhost:5000`
- Restart RoomServer if needed

#### 3. Authentication Failures

**Symptom:**
```
ERROR   401 Unauthorized
```

**Solutions:**
- Set ROOM_AUTH_TOKEN environment variable
- Verify token isn't expired
- Check required scopes in operator's appsettings.json

#### 4. Tests Timing Out

**Symptom:**
Tests hang without completing

**Solutions:**
- Increase timeout in `config.js`:
  ```javascript
  operator: { timeout: 60000 }
  ```
- Check operator logs for reconciliation loops
- Verify no circular dependencies in specs

#### 5. Idempotency Test Failures

**Symptom:**
```
WARN    Different results between applies
```

**Solutions:**
- This might indicate state tracking issues
- Check operator's diff calculation logic
- Increase delays between operations
- Review operator audit logs

### Debug Mode

Enable verbose logging:

```bash
export VERBOSE=true
npm run test:basic
```

This shows:
- All HTTP requests and responses
- Detailed error messages
- Internal state transitions
- Timing information

### Log Files

When using `run-integration-test.sh`, logs are saved to:
- `/tmp/roomserver.log` - RoomServer output
- `/tmp/operator.log` - RoomOperator output

View logs:
```bash
tail -f /tmp/roomserver.log
tail -f /tmp/operator.log
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  integration-test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0'
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'
      
      - name: Run Integration Tests
        run: |
          cd server-dotnet/operator/scripts
          chmod +x run-integration-test.sh
          ./run-integration-test.sh all
```

### Jenkins

```groovy
pipeline {
    agent any
    
    stages {
        stage('Setup') {
            steps {
                sh 'dotnet --version'
                sh 'node --version'
            }
        }
        
        stage('Integration Test') {
            steps {
                dir('server-dotnet/operator/scripts') {
                    sh 'chmod +x run-integration-test.sh'
                    sh './run-integration-test.sh all'
                }
            }
        }
    }
    
    post {
        always {
            junit '**/test-results/*.xml'
        }
    }
}
```

## Best Practices

### Test Development

1. **Isolation**: Each test should be independent
2. **Cleanup**: Always remove created resources
3. **Idempotency**: Verify operations can be retried
4. **Timeouts**: Set appropriate timeouts for operations
5. **Assertions**: Check actual vs expected state

### Test Execution

1. **Pre-flight**: Always check service health first
2. **Sequential**: Run tests in logical order
3. **Delays**: Add delays between operations for async processing
4. **Retries**: Use retries for flaky operations
5. **Logging**: Log extensively for debugging

### Continuous Testing

1. **Automate**: Use CI/CD for regular test execution
2. **Monitor**: Track test success rates over time
3. **Alert**: Set up alerts for test failures
4. **Report**: Generate and archive test reports
5. **Review**: Regularly review and update tests

## Advanced Topics

### Custom Scenarios

Create new test scenarios in `scenarios/`:

```javascript
#!/usr/bin/env node

import { config, logger, HttpClient, MessageBuilder } from '../index.js';

class MyScenario {
  constructor() {
    this.client = new HttpClient(config, logger);
  }

  async run() {
    logger.section('My Custom Scenario');
    // Your test logic
  }
}

if (import.meta.url === `file://${process.argv[1]}`) {
  new MyScenario().run();
}
```

### Performance Benchmarking

Track metrics over time:

```javascript
const metrics = {
  timestamp: Date.now(),
  scenario: 'basic-flow',
  duration: result.data.duration,
  operations: result.data.diff.toJoin.length,
};

// Save to file or send to monitoring system
```

### Load Testing

For heavy load testing, consider:
- Apache JMeter
- k6 (JavaScript-based)
- Gatling

Example k6 script:

```javascript
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  vus: 10,
  duration: '30s',
};

export default function () {
  let spec = { /* RoomSpec */ };
  let res = http.post('http://localhost:8080/apply', JSON.stringify(spec));
  check(res, { 'status is 200': (r) => r.status === 200 });
}
```

## Documentation

- **Integration Guide**: `/docs/ROOMOPERATOR_ROOMSERVER_INTEGRATION.md`
- **Operator Documentation**: `/docs/room-operator.md`
- **Test Client README**: `/server-dotnet/operator/test-client/README.md`
- **API Reference**: See integration guide for complete API docs

## Support

- **Issues**: https://github.com/invictvs-k/metacore-stack/issues
- **Discussions**: https://github.com/invictvs-k/metacore-stack/discussions

---

**Last Updated**: 2025-10-19  
**Version**: 1.0.0
