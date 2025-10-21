# End-to-End Tests

This directory contains end-to-end tests for the Metacore Stack.

## Test Suites

### Basic Flow Test

**File**: `basic-flow.test.mjs`

Tests basic SSE functionality and health endpoint.

```bash
npm run test:e2e
```

### Artifact Persistence Tests

**Files**:

- `artifact-persistence-unit.test.mjs` - Unit tests for artifact logic
- `artifact-persistence.test.mjs` - Integration tests requiring running server
- `run-artifact-persistence-test.sh` - Automated test runner with server setup

**Unit Tests** (no server required):

```bash
npm run test:artifact-unit
```

**Integration Tests** (requires RoomServer):

```bash
# Option 1: Manual server setup
# Terminal 1: Start RoomServer
cd server-dotnet/src/RoomServer
dotnet run

# Terminal 2: Run tests
npm run test:artifact-e2e

# Option 2: Automated setup
cd tests/e2e
./run-artifact-persistence-test.sh
```

**What the tests validate**:

- ✅ Artifact creation via API
- ✅ Artifact listing and filtering
- ✅ Artifact download and content integrity
- ✅ Version increments on updates
- ✅ Artifact promotion (entity → room workspace)
- ✅ Manifest consistency
- ✅ Parent relationship tracking
- ✅ Filesystem data persistence
- ✅ Cleanup operations
- ✅ Schema compliance

See [Artifact Persistence Testing Guide](../../docs/ARTIFACT_PERSISTENCE_TESTING.md) for detailed documentation.

## Test Artifacts

Test runs generate artifacts in `.artifacts/` subdirectories:

```
tests/e2e/.artifacts/
└── artifact-persistence/
    ├── evidence-*.json        # Detailed operation traces
    ├── summary-*.txt          # Human-readable summaries
    ├── test-report-*.md       # Comprehensive test reports
    ├── server.log             # RoomServer logs
    └── build.log              # Build output
```

## Running All Tests

```bash
# Run all test suites (schemas, contracts, artifact unit tests)
npm test

# Run all tests including smoke and contract tests
npm run test:all
```

## Prerequisites

- Node.js 20+
- .NET SDK 8.0+
- Dependencies installed: `npm install`

## Troubleshooting

### Tests fail with "Cannot find module"

Ensure dependencies are installed:

```bash
npm install
cd ../../schemas && npm install
```

### Server connection errors

Ensure RoomServer is running:

```bash
cd server-dotnet/src/RoomServer
dotnet run
```

Check server is listening on port 40801:

```bash
curl http://localhost:40801/health
```

### Permission errors on test scripts

Make scripts executable:

```bash
chmod +x tests/e2e/*.sh
```

## CI/CD Integration

These tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run E2E Tests
  run: |
    npm install
    npm run test:artifact-unit

- name: Run Integration Tests
  run: |
    cd tests/e2e
    ./run-artifact-persistence-test.sh
```

## Contributing

When adding new E2E tests:

1. Follow the existing test structure
2. Use descriptive test names
3. Collect evidence for verification
4. Update this README
5. Add to package.json scripts
6. Document in main test guide

## Related Documentation

- [Artifact Persistence Testing Guide](../../docs/ARTIFACT_PERSISTENCE_TESTING.md)
- [Testing Guide](../../docs/TESTING.md)
- [Integration Testing](../../server-dotnet/operator/docs/ENHANCED_INTEGRATION_TESTING.md)
