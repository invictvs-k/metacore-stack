# RoomOperator

A declarative reconciliation controller for managing room state.

## Overview

RoomOperator is an external .NET 8 console application that reconciles room state against declarative `RoomSpec` YAML files. It ensures the room's actual state matches the desired state through continuous, idempotent reconciliation cycles.

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Access to a RoomServer instance
- Authentication token (if required)

### Running Locally

```bash
# Navigate to operator directory
cd server-dotnet/operator

# Set authentication token (if required)
export ROOM_AUTH_TOKEN="your-token-here"

# Run the operator
dotnet run
```

The operator will start on `http://localhost:8080` by default.

### Configuration

Edit `appsettings.json` to configure:
- RoomServer connection
- Authentication settings
- Reconciliation parameters
- Guardrails and safety limits

See full configuration reference in `/docs/room-operator.md`.

## API Endpoints

| Endpoint                  | Method | Description                        |
|---------------------------|--------|------------------------------------|
| `/apply`                  | POST   | Apply a RoomSpec                   |
| `/status`                 | GET    | Get operator status                |
| `/status/rooms/{roomId}`  | GET    | Get room-specific status           |
| `/health`                 | GET    | Health check                       |
| `/metrics`                | GET    | Prometheus metrics                 |
| `/audit`                  | GET    | Audit log entries                  |

## RoomSpec Example

```yaml
apiVersion: v1
kind: RoomSpec
metadata:
  name: my-room
  version: 1
spec:
  roomId: my-room-01
  
  entities:
    - id: E-agent-1
      kind: agent
      displayName: Agent 1
      visibility: team
      
  artifacts:
    - name: document-1
      type: document
      workspace: shared
      seedFrom: ./seeds/doc1.md
      
  policies:
    dmVisibilityDefault: team
```

## Testing

Run the test suite:

```bash
cd tests
dotnet test
```

## Features

✅ **Idempotent Reconciliation**: Repeating reconciliations produces no side effects
✅ **Deterministic Execution**: Fixed order (Entities → Artifacts → Policies)
✅ **Guardrails**: Safety limits for bulk changes
✅ **Retry with Backoff**: Exponential backoff with jitter
✅ **Audit Logging**: Complete audit trail of all operations
✅ **Prometheus Metrics**: Full observability
✅ **State Validation**: Anti-stale protection
✅ **Partial Success**: Continues on individual failures

## Architecture

```
RoomOperator
├── Abstractions/       # Core models and contracts
├── Clients/            # API clients (Room, Artifacts, MCP, Policies)
├── Core/               # Reconciliation engine
│   ├── DiffEngine
│   ├── ReconcilePhases
│   ├── Guardrails
│   ├── RetryPolicy
│   └── AuditLog
└── HttpApi/            # REST endpoints and metrics
```

## Documentation

Full documentation available at `/docs/room-operator.md`:
- Architecture details
- Configuration reference
- API reference
- Usage examples
- DevOps decision matrix
- Troubleshooting guide

## Building

```bash
# Build operator
dotnet build

# Build with tests
dotnet build ../RoomServer.sln
```

## Docker

```bash
# Build image
docker build -t room-operator:latest .

# Run container
docker run -p 8080:8080 \
  -e ROOM_AUTH_TOKEN="your-token" \
  -e RoomServer__BaseUrl="http://room-server:5000" \
  room-operator:latest
```

## Support

- Documentation: `/docs/room-operator.md`
- Issues: https://github.com/invictvs-k/metacore-stack/issues
