# Artifact Persistence E2E Testing Guide

## Overview

This guide describes the comprehensive end-to-end testing framework for artifact persistence in the Metacore Stack. The testing suite validates that artifact data persistence works correctly from creation through cleanup, including versioning, promotion, and filesystem consistency.

## Test Objectives

The artifact persistence tests ensure:

1. **Create**: Artifacts can be created via API with proper metadata
2. **List**: Artifacts can be listed and filtered correctly
3. **Download**: Artifacts can be downloaded with content integrity
4. **Version**: Artifact versioning works correctly in `.ai-flow/`
5. **Update**: Artifact updates increment versions properly
6. **Promote**: Artifacts can be promoted from entity to room workspace
7. **Manifests**: Manifests accurately reflect all changes
8. **Filesystem**: Data consistency in storage layer
9. **Cleanup**: Removal operations work correctly
10. **Evidence**: All operations are documented with evidence

## Test Structure

### Unit Tests

**File**: `tests/e2e/artifact-persistence-unit.test.mjs`

Validates artifact persistence logic without requiring a running server:

- Schema validation and compliance
- Manifest structure correctness
- Versioning logic
- Hash calculation (SHA256)
- Path construction (room/entity workspaces)
- Metadata handling
- Parent relationship tracking
- ISO datetime format validation

**Run unit tests:**

```bash
npm run test:artifact-unit
```

### Integration Tests

**File**: `tests/e2e/artifact-persistence.test.mjs`

Comprehensive E2E test that exercises the full artifact API:

- Creates artifacts in entity workspace
- Lists and verifies artifacts
- Downloads and validates content integrity
- Updates artifacts and verifies version increments
- Promotes artifacts to room workspace
- Validates parent relationships
- Documents filesystem structure
- Collects evidence for each step

**Run integration tests:**

```bash
# Requires RoomServer to be running
npm run test:artifact-e2e
```

### Automated Test Runner

**File**: `tests/e2e/run-artifact-persistence-test.sh`

Fully automated integration test runner that:

1. Checks prerequisites (Node.js, .NET SDK)
2. Builds RoomServer
3. Starts RoomServer with health checks
4. Runs E2E tests
5. Collects and verifies artifacts
6. Generates comprehensive test report
7. Cleans up services

**Run automated suite:**

```bash
cd tests/e2e
./run-artifact-persistence-test.sh
```

## Test Artifacts

All test runs generate artifacts in `tests/e2e/.artifacts/artifact-persistence/`:

- **Evidence files** (`evidence-*.json`): Complete trace of all operations
- **Summary files** (`summary-*.txt`): Human-readable test summary
- **Test reports** (`test-report-*.md`): Comprehensive markdown reports
- **Server logs** (`server.log`): RoomServer execution logs
- **Build logs** (`build.log`): Build output

## Artifact Manifest Schema

Artifacts follow the schema defined in `schemas/artifact-manifest.schema.json`:

```json
{
  "name": "string", // Artifact filename
  "type": "string", // MIME type or logical type
  "path": "string", // Relative path in .ai-flow/
  "size": "integer", // Size in bytes
  "sha256": "string", // SHA256 hash (64 hex chars)
  "origin": {
    "room": "string", // Room ID (pattern: room-[A-Za-z0-9_-]{6,})
    "entity": "string", // Entity ID
    "workspace": "string", // "room" or "entity"
    "port": "string" // Optional: source port
  },
  "version": "integer", // Version number (starts at 1)
  "parents": ["string"], // Optional: parent artifact hashes
  "metadata": {}, // Optional: custom metadata
  "ts": "string" // ISO 8601 timestamp
}
```

## Filesystem Structure

Artifacts are persisted in the `.ai-flow/` directory with the following structure:

```
.ai-flow/
└── runs/
    └── {roomId}/
        ├── artifacts/              # Room workspace artifacts
        │   ├── {artifact1.txt}
        │   ├── {artifact2.md}
        │   └── manifest.json       # Manifest for room artifacts
        └── entities/
            └── {entityId}/
                └── artifacts/      # Entity workspace artifacts
                    ├── {draft1.txt}
                    ├── {draft2.md}
                    └── manifest.json  # Manifest for entity artifacts
```

### Manifest File Format

The `manifest.json` file contains an array of all artifact manifests in that workspace:

```json
[
  {
    "name": "intro_refined.md",
    "type": "doc/markdown",
    "path": ".ai-flow/runs/room-abc123/artifacts/intro_refined.md",
    "size": 1280,
    "sha256": "0123456789abcdef...",
    "origin": {
      "room": "room-abc123",
      "entity": "E-AGENT-1",
      "port": "text.generate",
      "workspace": "room"
    },
    "version": 1,
    "ts": "2025-10-17T12:02:10Z",
    "metadata": {
      "note": "refinado com guidance=clareza"
    }
  }
]
```

## API Endpoints

### Create Artifact

**Entity Workspace:**

```http
POST /rooms/{roomId}/entities/{entityId}/artifacts
Content-Type: multipart/form-data
X-Entity-Id: {entityId}

Fields:
  - spec: JSON artifact spec (name, type, metadata)
  - data: File content
  - parents: Optional JSON array of parent hashes
  - port: Optional source port
```

**Room Workspace:**

```http
POST /rooms/{roomId}/artifacts
Content-Type: multipart/form-data
X-Entity-Id: {entityId}

Fields: (same as entity workspace)
```

### List Artifacts

```http
GET /rooms/{roomId}/artifacts?prefix=&type=&entity=&since=&limit=&offset=
X-Entity-Id: {entityId}

GET /rooms/{roomId}/entities/{entityId}/artifacts
X-Entity-Id: {entityId}
```

### Download Artifact

```http
GET /rooms/{roomId}/artifacts/{name}?download=true
X-Entity-Id: {entityId}

GET /rooms/{roomId}/entities/{entityId}/artifacts/{name}
X-Entity-Id: {entityId}
```

### Promote Artifact

Promotes an artifact from entity workspace to room workspace:

```http
POST /rooms/{roomId}/artifacts/promote
Content-Type: application/json
X-Entity-Id: {entityId}

{
  "fromEntity": "E-001",
  "name": "draft.txt",
  "as": "final.txt",           // Optional: rename during promotion
  "metadata": {}               // Optional: additional metadata
}
```

## Versioning Behavior

1. **Initial Creation**: First artifact with a given name gets `version: 1`
2. **Updates**: Subsequent writes to the same artifact name increment the version
3. **Latest Only**: List operations return only the latest version of each artifact
4. **Version History**: All versions are stored in the manifest.json file
5. **Promotion**: Promoted artifacts get a new version in the room workspace

## Parent Tracking

Artifacts can track their lineage through parent relationships:

- **Parents Array**: List of SHA256 hashes of parent artifacts
- **Promotion**: When promoting, the source artifact's hash is added to parents
- **Lineage**: Enables tracking of artifact derivation chains

## Test Scenarios

### Scenario 1: Basic Creation and Retrieval

1. Create artifact in entity workspace
2. List artifacts in entity workspace
3. Download artifact and verify content
4. Verify SHA256 hash matches

### Scenario 2: Versioning

1. Create initial artifact (v1)
2. Update same artifact (v2)
3. Verify version increment
4. Verify both versions in manifest
5. Verify list returns only latest

### Scenario 3: Promotion

1. Create artifact in entity workspace
2. Promote to room workspace
3. Verify artifact exists in both workspaces
4. Verify parent relationship in promoted artifact
5. Verify different versions in each workspace

### Scenario 4: Metadata and Custom Fields

1. Create artifact with custom metadata
2. Verify metadata is preserved
3. Update with different metadata
4. Verify metadata changes are tracked

### Scenario 5: Cleanup

1. Create multiple artifacts
2. Delete specific artifacts
3. Verify removal from filesystem
4. Verify manifest consistency
5. Verify no residual files

## Evidence Collection

Each test run collects evidence including:

- **Operation Timestamps**: When each operation occurred
- **Request/Response Data**: API calls and responses
- **File Hashes**: SHA256 verification for content integrity
- **Version Numbers**: Tracking version increments
- **Parent Relationships**: Lineage verification
- **Filesystem State**: Directory structure snapshots
- **Manifest Contents**: Before/after comparisons

## Running Tests

### Quick Start

```bash
# Run unit tests only (no server required)
npm run test:artifact-unit

# Run with automated server setup
cd tests/e2e
./run-artifact-persistence-test.sh

# Manual setup (more control)
# Terminal 1: Start RoomServer
cd server-dotnet/src/RoomServer
dotnet run

# Terminal 2: Run tests
npm run test:artifact-e2e
```

### Prerequisites

- Node.js 20+
- .NET SDK 8.0+
- RoomServer built and running (for E2E tests)
- Active room and entity session (for E2E tests)

### Configuration

Tests can be configured via environment variables:

```bash
# Override RoomServer URL (default: http://localhost:40801)
export ROOM_SERVER_URL=http://localhost:8080

# Run tests
npm run test:artifact-e2e
```

## Troubleshooting

### Tests Fail with "Session Not Found"

Ensure you have:

1. Created a room via the API
2. Created an entity and established a session
3. Updated TEST_ROOM_ID and TEST_ENTITY_ID in the test script

### Filesystem Verification Fails

Check:

1. RoomServer has write permissions to the working directory
2. `.ai-flow/` directory exists in RoomServer's content root
3. Manifest files are being created correctly

### Hash Mismatches

Verify:

1. Content is not being modified during transfer
2. Encoding is consistent (UTF-8)
3. Line ending conversions are not occurring

## Continuous Integration

To integrate these tests into CI/CD:

```yaml
# Example GitHub Actions workflow
- name: Run Artifact Persistence Tests
  run: |
    cd tests/e2e
    ./run-artifact-persistence-test.sh

- name: Upload Test Artifacts
  uses: actions/upload-artifact@v3
  with:
    name: test-evidence
    path: tests/e2e/.artifacts/artifact-persistence/
```

## Best Practices

1. **Run Unit Tests First**: Always run unit tests before E2E tests
2. **Check Evidence**: Review evidence files after test runs
3. **Clean Between Runs**: Ensure clean state between test executions
4. **Use Unique IDs**: Generate unique room/entity IDs for each test run
5. **Verify Cleanup**: Always verify cleanup operations complete successfully
6. **Document Changes**: Update tests when changing artifact schemas
7. **Version Test Data**: Keep test artifacts in version control

## Related Documentation

- [Artifact Manifest Schema](../schemas/artifact-manifest.schema.json)
- [RoomServer API Documentation](./ROOMOPERATOR_ROOMSERVER_INTEGRATION.md)
- [Testing Guide](./TESTING.md)
- [Integration Testing](../server-dotnet/operator/docs/ENHANCED_INTEGRATION_TESTING.md)

## Future Enhancements

Planned improvements to the testing framework:

- [ ] Concurrent access testing
- [ ] Large file handling tests
- [ ] Workspace permission tests
- [ ] Cleanup stress tests
- [ ] Performance benchmarking
- [ ] Filesystem corruption recovery
- [ ] Migration testing between versions

---

_Last Updated: 2025-10-21_
