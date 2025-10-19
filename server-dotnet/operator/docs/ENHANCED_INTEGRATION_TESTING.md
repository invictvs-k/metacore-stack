# Enhanced Integration Test System

This document describes the enhanced integration test orchestration system for RoomServer, RoomOperator, and Test Client validation.

## Overview

The enhanced integration test system provides comprehensive validation with:

- **Automated Service Orchestration**: Automatic startup, readiness checks, and shutdown
- **Artifact Collection**: Structured logging and metrics in `.artifacts/integration/{timestamp}/`
- **Performance Metrics**: P50/P95 latency, success rates, and detailed timings
- **Trace Logging**: NDJSON-formatted event traces for analysis and replay
- **Configurable Scenarios**: Support for multiple test scenarios with timeout controls
- **Port Management**: Automatic port selection when defaults are occupied
- **Comprehensive Reporting**: JSON metrics and human-readable reports

## Quick Start

### Run Enhanced Integration Tests

```bash
cd server-dotnet/operator/scripts
./run-integration-enhanced.sh
```

This will:
1. Build the .NET solution
2. Start RoomServer with health checks
3. Start RoomOperator with readiness validation
4. Execute test scenarios with full tracing
5. Collect logs and generate reports
6. Clean up all services

### Custom Configuration

Override defaults with environment variables:

```bash
# Use custom ports
ROOMSERVER_PORT=40902 \
ROOMOPERATOR_PORT=8081 \
./run-integration-enhanced.sh

# Run specific scenarios
TEST_SCENARIOS=basic-flow,stress-test \
SCENARIO_TIMEOUT=180 \
./run-integration-enhanced.sh

# Adjust timeouts
READINESS_TIMEOUT=90 \
SCENARIO_TIMEOUT=240 \
./run-integration-enhanced.sh
```

## Configuration Options

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ROOMSERVER_HOST` | `127.0.0.1` | RoomServer bind address |
| `ROOMSERVER_PORT` | `40901` | RoomServer port (auto-selects if occupied) |
| `ROOMOPERATOR_PORT` | `8080` | RoomOperator port (auto-selects if occupied) |
| `TEST_SCENARIOS` | `basic-flow,error-cases` | Comma-separated scenario list |
| `LOG_LEVEL` | `debug` | Logging verbosity |
| `READINESS_TIMEOUT` | `60` | Service readiness timeout (seconds) |
| `SCENARIO_TIMEOUT` | `120` | Per-scenario timeout (seconds) |
| `ROOM_AUTH_TOKEN` | `test-token` | Authentication token (if required) |

### Available Test Scenarios

- **basic-flow** (or `basic`): Complete happy path with entities, artifacts, and policies
- **error-cases** (or `error`): Error handling and validation tests
- **stress-test** (or `stress`): Performance and load testing

## Artifact Structure

After each test run, artifacts are organized in `.artifacts/integration/{timestamp}/`:

```
.artifacts/integration/20251019-121530/
├── logs/
│   ├── build.log                    # .NET build output
│   ├── roomserver.log               # RoomServer runtime logs
│   ├── roomoperator.log             # RoomOperator runtime logs
│   ├── npm-install.log              # Test client dependency installation
│   ├── test-client-basic-flow.log   # Basic flow scenario output
│   ├── test-client-error-cases.log  # Error cases scenario output
│   └── test-client-stress-test.log  # Stress test scenario output
└── results/
    ├── metrics.json                 # Comprehensive metrics (JSON)
    ├── report.txt                   # Human-readable report
    ├── trace.ndjson                 # Structured trace events (NDJSON)
    ├── tests_passed.txt             # Number of passed tests
    └── tests_failed.txt             # Number of failed tests
```

### Artifacts are automatically excluded from Git (`.gitignore`)

## Metrics Collection

### metrics.json Structure

```json
{
  "timestamp": "2025-10-19T12:15:30Z",
  "configuration": {
    "roomserver_host": "127.0.0.1",
    "roomserver_port": 40901,
    "roomoperator_port": 8080,
    "test_scenarios": "basic-flow,error-cases",
    "readiness_timeout": 60,
    "scenario_timeout": 120
  },
  "services": {
    "roomserver": {
      "startup_time_ms": 3245,
      "port": 40901,
      "pid": 12345
    },
    "roomoperator": {
      "startup_time_ms": 2890,
      "port": 8080,
      "pid": 12346
    }
  },
  "scenarios": {
    "basic-flow": {
      "passed": 12,
      "failed": 0,
      "duration_ms": 15234
    },
    "error-cases": {
      "passed": 8,
      "failed": 0,
      "duration_ms": 8456
    }
  },
  "summary": {
    "total_duration_s": 45,
    "tests_total": 20,
    "tests_passed": 20,
    "tests_failed": 0,
    "success_rate": 100.0,
    "status": "SUCCESS"
  }
}
```

### Trace Events (trace.ndjson)

NDJSON format for structured event tracing:

```jsonl
{"timestamp":"2025-10-19T12:15:35.123Z","timestamp_ms":1697718935123,"elapsed_ms":0,"type":"checkpoint","name":"scenario_start","scenario":"basic-flow"}
{"timestamp":"2025-10-19T12:15:35.456Z","timestamp_ms":1697718935456,"elapsed_ms":333,"type":"operation","operation":"getOperatorHealth"}
{"timestamp":"2025-10-19T12:15:35.489Z","timestamp_ms":1697718935489,"elapsed_ms":366,"type":"http_request","method":"GET","url":"/health"}
{"timestamp":"2025-10-19T12:15:35.523Z","timestamp_ms":1697718935523,"elapsed_ms":400,"type":"http_response","method":"GET","url":"/health","status":200,"duration_ms":34}
{"timestamp":"2025-10-19T12:15:35.525Z","timestamp_ms":1697718935525,"elapsed_ms":402,"type":"assertion","name":"operator_health","passed":true}
{"timestamp":"2025-10-19T12:15:35.800Z","timestamp_ms":1697718935800,"elapsed_ms":677,"type":"metric","metric":"apply_spec_duration","value":245,"unit":"ms"}
```

Event Types:
- `checkpoint`: Test phase markers
- `operation`: High-level operation start
- `http_request`: HTTP request sent
- `http_response`: HTTP response received (includes duration)
- `assertion`: Test assertion result
- `metric`: Custom metric value
- `error`: Error occurrence
- `summary`: Final summary (last event)

## Performance Metrics

The test client automatically calculates and reports:

- **Latency Statistics**:
  - Min/Max/Average response times
  - P50 (median) latency
  - P95 latency
- **Request Metrics**:
  - Total HTTP requests
  - Total HTTP responses
  - Error count
- **Test Metrics**:
  - Tests passed/failed
  - Success rate percentage
  - Scenario duration

Example output:

```
═══════════════════════════════════════════════════
  Performance Metrics
═══════════════════════════════════════════════════

  Total Duration:       15234ms
  HTTP Requests:        24
  HTTP Responses:       24
  Errors:               0

  Latency Statistics:
    Count:              24
    Min:                28.00ms
    Max:                456.00ms
    Average:            123.45ms
    P50:                98.00ms
    P95:                234.00ms
```

## Exit Codes

- `0`: All tests passed successfully
- `1`: One or more tests failed or service startup failed

## Troubleshooting

### Port Already in Use

The script automatically finds available ports if defaults are occupied.

### Service Startup Failure

Check the service logs in `.artifacts/integration/{timestamp}/logs/`.

### Test Timeout

Increase scenario timeout with `SCENARIO_TIMEOUT` environment variable.

## Related Documentation

- [Test Client README](../test-client/README.md)
- [Original Integration Script](../scripts/run-integration-test.sh)

---

**Version**: 1.0.0  
**Last Updated**: 2025-10-19
