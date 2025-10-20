---
title: RoomOperator Documentation
status: active
owners: []
tags: [architecture, ops, spec]
last_review: 2025-10-20
links: []
---

# RoomOperator Documentation

## Overview

**RoomOperator** is an external reconciliation controller that manages declarative room state. It reads a `RoomSpec` (YAML), observes the current `RoomState` via REST/SignalR, and continuously reconciles differences through **idempotent, transactional phases**.

### Key Principles

1. **Single Source of Truth**: `RoomSpec` defines the desired state. Everything not in the spec is removed.
2. **Strong Idempotency**: Repeated reconciliations produce no additional side effects.
3. **Deterministic Execution**: Fixed order: Entities → Artifacts → Policies → Resources.
4. **Partial Failure Tolerance**: Failures in one item don't abort the entire cycle.
5. **Observability**: Comprehensive audit logs, metrics, and health states.

---

## Architecture

### Components

```
┌─────────────────────────────────────────────────────────┐
│                      RoomOperator                        │
│                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌───────────┐ │
│  │  HTTP API    │    │ Reconcile    │    │  Clients  │ │
│  │              │───▶│  Service     │───▶│           │ │
│  │ /apply       │    │              │    │ Room      │ │
│  │ /status      │    │ DiffEngine   │    │ Artifacts │ │
│  │ /health      │    │ Guardrails   │    │ Policies  │ │
│  │ /metrics     │    │ RetryPolicy  │    │ MCP       │ │
│  │ /audit       │    │ AuditLog     │    │           │ │
│  └──────────────┘    └──────────────┘    └───────────┘ │
│                                                          │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │  RoomServer   │
                    │  (SignalR/    │
                    │   REST API)   │
                    └───────────────┘
```

### Reconciliation Phases

The operator executes reconciliations in five deterministic phases:

| Phase         | Purpose                                                  |
|---------------|----------------------------------------------------------|
| **PLANNING**  | Calculate diffs (toJoin, toKick, toSeed, etc.)         |
| **PRE_CHECKS**| Validate RBAC, guardrails, file existence               |
| **APPLY**     | Execute changes with retry and exponential backoff      |
| **VERIFY**    | Revalidate state and measure convergence               |
| **ROLLBACK**  | (Optional) Revert changes on critical failures         |

---

## Configuration

### appsettings.json

```json
{
  "RoomServer": {
    "BaseUrl": "http://localhost:5000",
    "HubPath": "/hub"
  },
  "Auth": {
    "TokenType": "Bearer",
    "Token": "${ROOM_AUTH_TOKEN}",
    "RequiredScopes": [
      "room:entities:write",
      "room:artifacts:write",
      "room:policies:write",
      "room:resources:read"
    ],
    "ValidateScopes": true
  },
  "Operator": {
    "Version": "1.0.0",
    "Features": {
      "Resources": false
    },
    "Reconciliation": {
      "IntervalSeconds": 2,
      "ParallelRooms": 1,
      "RateLimitPerSecond": 8,
      "StateConsistency": {
        "Mode": "active",
        "StaleTolerance": "5s",
        "ReadBeforeApply": true
      },
      "Guardrails": {
        "MaxEntitiesKickPerCycle": 5,
        "MaxArtifactsDeletePerCycle": 10,
        "ChangeThreshold": 0.5,
        "RequireConfirmHeader": true
      },
      "ConvergenceTracking": {
        "Enabled": true,
        "MaxCyclesUntilConverged": 10
      }
    },
    "Retry": {
      "MaxAttempts": 3,
      "InitialDelayMs": 100,
      "MaxDelayMs": 5000,
      "JitterFactor": 0.2
    }
  },
  "HttpApi": {
    "Port": 8080
  }
}
```

### Environment Variables

- `ROOM_AUTH_TOKEN`: Bearer token for Room Server authentication

---

## RoomSpec Format

### Example: ai-lab.room.yaml

```yaml
apiVersion: v1
kind: RoomSpec
metadata:
  name: ai-lab
  version: 4
spec:
  roomId: ai-lab-01
  
  entities:
    - id: E-orchestrator-main
      kind: orchestrator
      displayName: Lab Orchestrator
      visibility: team
      capabilities:
        - execute_commands
        - manage_artifacts
      policy:
        allow_commands_from: orchestrator
        sandbox_mode: true
        
    - id: E-agent-researcher
      kind: agent
      displayName: Research Agent
      visibility: team
      capabilities:
        - read_artifacts
      policy:
        allow_commands_from: orchestrator
        
  artifacts:
    - name: project-plan
      type: document
      workspace: shared
      tags:
        - planning
      seedFrom: ./seeds/project-plan.md
      promoteAfterSeed: true
      
  policies:
    dmVisibilityDefault: team
    allowResourceCreation: false
    maxArtifactsPerEntity: 100
```

### Field Reference

#### Entities

- **id**: Unique entity identifier (e.g., `E-agent-1`)
- **kind**: `human`, `agent`, `npc`, or `orchestrator`
- **visibility**: `public`, `team`, or `owner`
- **ownerUserId**: Required when `visibility=owner`
- **capabilities**: List of granted capabilities
- **policy**: Entity-specific policies

#### Artifacts

- **name**: Unique artifact name
- **type**: Artifact type (document, dataset, etc.)
- **workspace**: Workspace name
- **tags**: List of tags
- **seedFrom**: Path to seed file (relative to operator)
- **promoteAfterSeed**: Auto-promote after seeding
- **dependsOn**: List of dependency IDs

#### Policies

- **dmVisibilityDefault**: Default DM visibility (`team`, `owner`, `public`)
- **allowResourceCreation**: Allow dynamic resource creation
- **maxArtifactsPerEntity**: Limit per entity

---

## API Reference

### POST /apply

Apply a RoomSpec.

**Headers:**
- `X-Dry-Run: true` - Perform validation without applying changes
- `X-Confirm: true` - Required when change threshold exceeded

**Request Body:**
```json
{
  "spec": {
    "apiVersion": "v1",
    "kind": "RoomSpec",
    "metadata": {
      "name": "test-room",
      "version": 1
    },
    "spec": {
      "roomId": "test-room-01",
      "entities": [...],
      "artifacts": [...],
      "policies": {...}
    }
  }
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "partialSuccess": false,
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "phase": "VERIFY",
  "diff": {
    "toJoin": ["E-agent-1"],
    "toKick": [],
    "toSeed": ["artifact-1"],
    "toPromote": [],
    "blocked": []
  },
  "warnings": [],
  "duration": 1.23
}
```

### GET /status

Get operator status.

**Response:**
```json
{
  "version": "1.0.0",
  "health": "Healthy",
  "rooms": [
    {
      "roomId": "test-room-01",
      "isReconciling": false,
      "lastReconcile": "2025-10-19T02:30:00Z",
      "pendingDiff": null,
      "blocked": [],
      "cyclesSinceConverged": 0
    }
  ],
  "queuedRequests": 0,
  "timestamp": "2025-10-19T02:35:00Z"
}
```

### GET /status/rooms/{roomId}

Get room-specific status.

### GET /health

Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-19T02:35:00Z"
}
```

### GET /metrics

Prometheus metrics endpoint.

**Metrics exposed:**
- `room_operator_reconcile_attempts_total`
- `room_operator_reconcile_successes_total`
- `room_operator_reconcile_failures_total`
- `room_operator_reconcile_duration_seconds`
- `room_operator_queued_requests`
- `room_operator_active_reconciliations`

### GET /audit

Get audit log entries.

**Query Parameters:**
- `count`: Number of entries (default: 100)
- `correlationId`: Filter by correlation ID

---

## Idempotency & Fingerprinting

### Artifact Fingerprinting

Artifacts are only written if their fingerprint differs from the remote:

```csharp
string BuildFingerprint(ArtifactSeedSpec a, byte[] fileContent) =>
    Sha256($"{a.Name}|{a.Type}|{a.Workspace}|{string.Join(",", a.Tags)}|{Convert.ToBase64String(fileContent)}");
```

This ensures:
1. Unchanged artifacts are not rewritten
2. Content, metadata, and tags all affect the fingerprint
3. Reconciliation is truly idempotent

### Entity JOIN/KICK Compatibility

- **JOIN**: Ignores 409 Conflict (entity already exists)
- **KICK**: Ignores 404 Not Found (entity already removed)

This ensures operations can be retried safely.

---

## Guardrails

### Safety Limits

Configured per cycle:

- `MaxEntitiesKickPerCycle`: Max entities to remove (default: 5)
- `MaxArtifactsDeletePerCycle`: Max artifacts to delete (default: 10)
- `ChangeThreshold`: Max change ratio before requiring confirmation (default: 0.5)

### Change Threshold

If more than 50% of entities/artifacts are being removed, `X-Confirm:true` header is required.

**Example:**
- Current: 10 entities
- To Remove: 6 entities
- Change Ratio: 60%
- Result: **Requires X-Confirm:true**

---

## State Consistency

### Anti-Stale Protection

Before applying changes, the operator revalidates room state:

```
1. Read state (with TTL cache)
2. Calculate diff
3. Validate guardrails
4. REVALIDATE state
5. Apply changes
```

This prevents race conditions and stale state issues.

---

## Retry & Error Handling

### Retry Policy

Exponential backoff with jitter:

- **MaxAttempts**: 3
- **InitialDelayMs**: 100
- **MaxDelayMs**: 5000
- **JitterFactor**: 0.2

### Partial Success

If some operations fail:
- Reconciliation continues for remaining items
- Result marked as `partialSuccess: true`
- Warnings logged
- Next cycle retries failed items

---

## Audit & Observability

### Audit Log Format

```json
{
  "type": "event",
  "action": "entity.joined",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-10-19T02:30:00Z",
  "operatorVersion": "1.0.0",
  "specVersion": 4,
  "metadata": {
    "entityId": "E-agent-1"
  }
}
```

### Log Types

- **command**: User-initiated actions
- **event**: System events (joins, kicks, seeds, etc.)

---

## DevOps Decision Matrix

| Scenario                | Description                        | Requires X-Confirm | Example             |
|-------------------------|------------------------------------|--------------------|---------------------|
| **Addition**            | Add new entities/artifacts         | ❌                  | Add E-NEW           |
| **Update**              | Change visibility/policies         | ❌                  | visibility→private  |
| **Removal ≤ 50%**       | Remove few items                   | ❌                  | Remove 2 entities   |
| **Removal > 50%**       | Remove half or more                | ✅                  | 10 → 4 entities     |
| **Critical Policy**     | Remove essential policy            | ✅                  | Remove dmVisibility |

---

## Usage Examples

### Basic Reconciliation

```bash
# Start operator
cd server-dotnet/operator
export ROOM_AUTH_TOKEN="your-token-here"
dotnet run

# Apply spec
curl -X POST http://localhost:8080/apply \
  -H "Content-Type: application/json" \
  -d @spec-request.json

# Check status
curl http://localhost:8080/status

# Get audit log
curl http://localhost:8080/audit?count=50
```

### Dry Run

```bash
curl -X POST http://localhost:8080/apply \
  -H "X-Dry-Run: true" \
  -H "Content-Type: application/json" \
  -d @spec-request.json
```

### High-Impact Changes

```bash
curl -X POST http://localhost:8080/apply \
  -H "X-Confirm: true" \
  -H "Content-Type: application/json" \
  -d @spec-request.json
```

### Monitor Metrics

```bash
# Prometheus scrape endpoint
curl http://localhost:8080/metrics
```

---

## Testing

### Run Tests

```bash
cd server-dotnet/operator/tests
dotnet test
```

### Test Categories

1. **Idempotency Tests**: Verify repeated operations have no side effects
2. **Fingerprint Tests**: Ensure hash consistency
3. **Guardrails Tests**: Validate safety thresholds
4. **Validation Tests**: Spec structure validation

---

## Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY bin/Release/net8.0/publish/ .
ENTRYPOINT ["dotnet", "RoomOperator.dll"]
```

### Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: room-operator
spec:
  replicas: 1
  selector:
    matchLabels:
      app: room-operator
  template:
    metadata:
      labels:
        app: room-operator
    spec:
      containers:
      - name: operator
        image: room-operator:1.0.0
        ports:
        - containerPort: 8080
        env:
        - name: ROOM_AUTH_TOKEN
          valueFrom:
            secretKeyRef:
              name: room-secrets
              key: auth-token
        - name: RoomServer__BaseUrl
          value: "http://room-server:5000"
```

---

## Troubleshooting

### Common Issues

**Problem**: Reconciliation fails with "Guardrails check failed"
- **Solution**: Check change ratio. If > 50%, add `X-Confirm:true` header.

**Problem**: Artifacts not seeding
- **Solution**: Verify `seedFrom` path is correct and file exists.

**Problem**: MCP resources pending
- **Solution**: Check `Features:Resources` is enabled and MCP service is available.

**Problem**: Authentication failures
- **Solution**: Verify `ROOM_AUTH_TOKEN` is set and has required scopes.

---

## Best Practices

1. **Version Control Specs**: Keep RoomSpecs in git
2. **Dry Run First**: Always test with `X-Dry-Run:true`
3. **Monitor Metrics**: Set up Prometheus/Grafana
4. **Audit Regularly**: Review audit logs for anomalies
5. **Small Changes**: Iterate incrementally
6. **Test Locally**: Validate specs before production

---

## Roadmap

- [ ] Multi-room parallel reconciliation
- [ ] Webhook notifications
- [ ] GitOps integration
- [ ] Advanced RBAC with scope validation
- [ ] Automatic rollback on critical failures
- [ ] Spec history and versioning API

---

## Support

For issues, feature requests, or questions:
- GitHub Issues: https://github.com/invictvs-k/metacore-stack/issues
- Documentation: /docs/room-operator.md
