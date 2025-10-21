# Quick Start: Artifact Persistence Testing

This quick start guide helps you run the artifact persistence tests immediately.

## Prerequisites Check

```bash
# Check Node.js version (requires 20+)
node --version

# Check .NET SDK (requires 8.0+)
dotnet --version

# Install dependencies
npm install
```

## Option 1: Unit Tests Only (Fastest)

No server required. Tests artifact logic, schema validation, and versioning.

```bash
npm run test:artifact-unit
```

Expected output:

```
✅ Schema loads correctly
✅ Valid artifact manifest passes validation
✅ Invalid manifest fails validation - missing required field
✅ Artifact versioning increments correctly
✅ SHA256 hash calculation
✅ Artifact path construction
✅ Entity workspace path construction
✅ Manifest metadata is optional
✅ Manifest with metadata validates
✅ Parent tracking in manifest
✅ Multiple parents tracking
✅ Workspace must be room or entity
✅ Example artifact from schema validates
✅ Invalid example is rejected
✅ ISO datetime format validation

Test Results: 15 passed, 0 failed
✅ All tests passed!
```

## Option 2: Automated Integration Tests (Recommended)

Automated script that builds, starts server, runs tests, and cleans up.

```bash
cd tests/e2e
./run-artifact-persistence-test.sh
```

The script will:

1. ✅ Build RoomServer
2. ✅ Start RoomServer with health checks
3. ✅ Run E2E tests
4. ✅ Collect evidence
5. ✅ Generate reports
6. ✅ Clean up

Output files in `tests/e2e/.artifacts/artifact-persistence/`:

- `evidence-*.json` - Detailed test traces
- `summary-*.txt` - Human-readable summary
- `test-report-*.md` - Comprehensive report
- `server.log` - Server output
- `build.log` - Build output

## Option 3: Manual Integration Tests (Advanced)

For more control over the testing process.

### Step 1: Start RoomServer

```bash
# Terminal 1
cd server-dotnet/src/RoomServer
dotnet run
```

Wait for: `Now listening on: http://localhost:40801`

### Step 2: Verify Server

```bash
# Terminal 2
curl http://localhost:40801/health
```

Should return 200 OK.

### Step 3: Run Tests

```bash
npm run test:artifact-e2e
```

### Step 4: Stop Server

Press Ctrl+C in Terminal 1.

## What Gets Tested

### Unit Tests Validate:

- ✅ Artifact manifest schema compliance
- ✅ Required vs optional fields
- ✅ SHA256 hash format (64 hex characters)
- ✅ Room ID patterns (`room-[A-Za-z0-9_-]{6,}`)
- ✅ ISO 8601 datetime format
- ✅ Workspace types (room/entity)
- ✅ Version increment logic
- ✅ Path construction for both workspaces
- ✅ Metadata handling (optional)
- ✅ Parent relationship tracking
- ✅ Multiple parents support

### Integration Tests Validate:

- ✅ Create artifacts via API
- ✅ List artifacts with filters
- ✅ Download artifacts
- ✅ Content integrity (SHA256 verification)
- ✅ Version increments on updates
- ✅ Artifact promotion (entity → room)
- ✅ Parent relationships in promoted artifacts
- ✅ Manifest consistency
- ✅ Filesystem structure in .ai-flow/

## Troubleshooting

### "Cannot find module" errors

```bash
# Install all dependencies
npm install

# Install schema dependencies
cd schemas && npm install
```

### "Port 40801 already in use"

```bash
# Find and kill the process
lsof -ti:40801 | xargs kill

# Or use a different port
export ROOM_SERVER_URL=http://localhost:8080
```

### "Permission denied" on scripts

```bash
chmod +x tests/e2e/*.sh
```

### Tests fail with "Session not found"

The integration tests assume:

1. A room exists
2. An entity session is active
3. Authentication headers are set

For now, the tests document expected behavior. Full automation requires:

- Room creation endpoint
- Entity session establishment
- Proper authentication flow

## Next Steps

1. **Review evidence files** in `.artifacts/` directory
2. **Read the full guide** at [docs/ARTIFACT_PERSISTENCE_TESTING.md](../../docs/ARTIFACT_PERSISTENCE_TESTING.md)
3. **Customize tests** for your use case
4. **Add to CI/CD** pipeline

## Common Commands

```bash
# Run all tests (schemas + contracts + artifact unit tests)
npm test

# Run all test suites
npm run test:all

# Run just artifact tests
npm run test:artifact-unit
npm run test:artifact-e2e

# Run with automated setup
cd tests/e2e && ./run-artifact-persistence-test.sh

# View test artifacts
ls -lh tests/e2e/.artifacts/artifact-persistence/

# Read latest summary
cat tests/e2e/.artifacts/artifact-persistence/summary-*.txt | tail -50
```

## Need Help?

- 📖 [Full Testing Documentation](../../docs/ARTIFACT_PERSISTENCE_TESTING.md)
- 📖 [E2E Tests README](./README.md)
- 📖 [Integration Testing Guide](../../docs/TESTING.md)
- 🐛 [Open an issue](https://github.com/invictvs-k/metacore-stack/issues)

## Success Indicators

You'll know the tests are working when you see:

```
✅ 15/15 unit tests passed
✅ Schema validation working
✅ Versioning logic verified
✅ Hash calculation correct
✅ All tests passed!
```

And evidence files are generated in `.artifacts/artifact-persistence/`.

---

**Time to complete**: 2-5 minutes (unit tests) or 5-10 minutes (full suite)
