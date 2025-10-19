# Integration Test Scripts

This directory contains scripts for orchestrating integration tests of the RoomOperator-RoomServer system.

## Scripts

### run-integration-enhanced.sh (Recommended)

Enhanced integration test orchestration with comprehensive artifact collection, metrics, and tracing.

**Usage:**
```bash
./run-integration-enhanced.sh
```

**Features:**
- Automatic service startup with readiness checks
- Port conflict resolution (auto-selects free ports)
- Structured artifact collection in `.artifacts/integration/{timestamp}/`
- Performance metrics (P50/P95 latency)
- NDJSON trace logging
- Comprehensive JSON and text reports
- Automatic cleanup on exit

**Environment Variables:**
- `ROOMSERVER_HOST` - Server bind address (default: 127.0.0.1)
- `ROOMSERVER_PORT` - Server port (default: 40901)
- `ROOMOPERATOR_PORT` - Operator port (default: 8080)
- `TEST_SCENARIOS` - Scenarios to run (default: basic-flow,error-cases)
- `READINESS_TIMEOUT` - Service startup timeout in seconds (default: 60)
- `SCENARIO_TIMEOUT` - Per-scenario timeout in seconds (default: 120)
- `LOG_LEVEL` - Logging verbosity (default: debug)

**Examples:**
```bash
# Run with custom ports
ROOMSERVER_PORT=40902 ROOMOPERATOR_PORT=8081 ./run-integration-enhanced.sh

# Run specific scenarios
TEST_SCENARIOS=basic-flow,stress-test ./run-integration-enhanced.sh

# Increase timeouts for CI environments
READINESS_TIMEOUT=120 SCENARIO_TIMEOUT=300 ./run-integration-enhanced.sh
```

See [Enhanced Integration Testing Guide](../docs/ENHANCED_INTEGRATION_TESTING.md) for detailed documentation.

### run-integration-test.sh

Original integration test script with basic orchestration.

**Usage:**
```bash
./run-integration-test.sh [scenario]
```

**Arguments:**
- `scenario` - Test scenario to run: `basic`, `error`, `stress`, or `all` (default: basic)

**Examples:**
```bash
./run-integration-test.sh basic
./run-integration-test.sh all
```

### run-roomserver.sh

Utility script to start RoomServer standalone for testing.

**Usage:**
```bash
./run-roomserver.sh
```

### run-operator.sh

Utility script to start RoomOperator standalone for testing.

**Usage:**
```bash
./run-operator.sh
```

### run-tests.sh

Utility script to run test client scenarios against running services.

**Usage:**
```bash
./run-tests.sh
```

## Artifacts

The enhanced integration test script creates the following artifact structure:

```
.artifacts/integration/{timestamp}/
├── logs/
│   ├── build.log                    # Build output
│   ├── roomserver.log               # RoomServer logs
│   ├── roomoperator.log             # RoomOperator logs
│   └── test-client-*.log            # Test scenario logs
└── results/
    ├── metrics.json                 # Performance metrics
    ├── report.txt                   # Human-readable report
    ├── trace.ndjson                 # Structured trace events
    ├── tests_passed.txt             # Pass count
    └── tests_failed.txt             # Fail count
```

## Exit Codes

All scripts follow standard exit code conventions:
- `0` - Success (all tests passed)
- `1` - Failure (one or more tests failed or service startup failed)

## Requirements

- .NET 8+ SDK
- Node.js 20+
- curl (for health checks)
- jq (for JSON processing - enhanced script only)
- bash 4.0+

## Troubleshooting

### Port Already in Use

The enhanced script automatically finds free ports if defaults are occupied.

### Service Won't Start

Check the service logs in `.artifacts/integration/{timestamp}/logs/`.

### Tests Timeout

Increase timeouts with environment variables:
```bash
READINESS_TIMEOUT=120 SCENARIO_TIMEOUT=300 ./run-integration-enhanced.sh
```

### Missing jq

Install jq for the enhanced script:
```bash
# Ubuntu/Debian
sudo apt-get install jq

# macOS
brew install jq
```

## Related Documentation

- [Enhanced Integration Testing](../docs/ENHANCED_INTEGRATION_TESTING.md) - Comprehensive guide
- [Test Client README](../test-client/README.md) - Test client documentation
- [RoomOperator Integration Guide](../../../docs/ROOMOPERATOR_ROOMSERVER_INTEGRATION.md) - API reference

---

**Last Updated**: 2025-10-19
