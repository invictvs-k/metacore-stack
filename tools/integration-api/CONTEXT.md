# Context Card: Integration API

> **Location:** `tools/integration-api`  
> **Type:** Service  
> **Status:** Active

## Overview

**Purpose:**
Backend service providing a unified API for the Operator Dashboard. Manages configuration persistence, proxies event streams from RoomServer and RoomOperator via SSE, executes test scenarios with live log streaming, and orchestrates command execution with JSON Schema validation.

**Key Responsibilities:**
- Configuration management with hot-reload support
- Real-time event streaming via SSE from multiple sources
- Test scenario execution with artifact collection and log streaming
- Command catalog management and execution
- Health check proxying for system monitoring
- MCP status proxying

## Quick Start

### Prerequisites
- Node.js 18+
- RoomServer running on port 40801
- RoomOperator running on port 40802
- Test client dependencies installed

### How to Run

```bash
# Development
npm install
npm run dev
# API available at http://localhost:40901

# Production
npm run build
npm start

# Tests
npm test
```

### Configuration

**Environment Variables:**
None required. Configuration is read from `configs/dashboard.settings.json`.

**Configuration Files:**
- `configs/dashboard.settings.json`: Main configuration (port, service URLs, test runner settings)
- Default values used if file is missing

## Architecture

### Inputs

**API Endpoints:**
- `GET /api/config`: Get current configuration
- `PUT /api/config`: Update configuration
- `POST /api/tests/run`: Execute test scenario
- `POST /api/commands/execute`: Execute command

**Events/Messages Consumed:**
- SSE from `http://127.0.0.1:40801/events`: RoomServer events
- SSE from `http://127.0.0.1:40802/events`: RoomOperator events

**Files Read:**
- `configs/dashboard.settings.json`: Configuration
- `server-dotnet/operator/commands/commands.catalog.json`: Command catalog
- `.artifacts/integration/{timestamp}/runs/{runId}/result.json`: Test results

**Dependencies (External):**
- RoomServer (port 40801): Event source and health checks
- RoomOperator (port 40802): Event source and health checks
- Test Client: Test scenario execution

### Outputs

**API Endpoints:**
- `GET /api/config`: Returns configuration JSON
- `GET /api/config/version`: Returns configuration checksum
- `GET /api/events/roomserver`: SSE stream of RoomServer events
- `GET /api/events/roomoperator`: SSE stream of RoomOperator events
- `GET /api/events/combined`: Combined SSE stream
- `GET /api/tests`: List of test scenarios
- `POST /api/tests/run`: Initiates test execution, returns runId
- `GET /api/tests/runs/:runId`: Test run metadata
- `GET /api/tests/stream/:runId`: SSE stream of test logs
- `GET /api/commands`: Command catalog
- `POST /api/commands/execute`: Command execution result
- `GET /api/mcp/status`: MCP status (proxied)
- `GET /api/health/*`: Health check results
- `GET /health`: Service health

**Events/Messages Published:**
- SSE events: `message`, `connected`, `disconnected`, `error` (event streams)
- SSE events: `started`, `log`, `done`, `error` (test streams)

**Files Written:**
- `.artifacts/integration/{timestamp}/runs/{runId}/test-client.log`: Test logs
- `.artifacts/integration/{timestamp}/runs/{runId}/result.json`: Test metadata
- Test-generated artifacts in same directory

**Side Effects:**
- HTTP requests to RoomServer and RoomOperator
- Process execution for test runner
- File system writes for artifacts

### Data Flow

```
Dashboard → Express Routes → Services → External Systems
                ↓                          ↓
           SSE Streams  ←  Event Proxying  
                ↓
         Test Execution → Child Process → Artifacts
```

## Internal Structure

### Key Directories
- `src/`: Main application source
  - `routes/`: Express route handlers (config, events, tests, commands, mcp)
  - `services/`: Business logic (config, tests)
  - `types/`: TypeScript type definitions

### Key Files
- `src/index.ts`: Main entry point, Express server setup
- `src/routes/config.ts`: Configuration endpoints
- `src/routes/events.ts`: SSE event streaming endpoints
- `src/routes/tests.ts`: Test execution endpoints
- `src/routes/commands.ts`: Command execution endpoints
- `src/routes/mcp.ts`: MCP status proxy
- `src/services/config.ts`: Configuration persistence and validation
- `src/services/tests.ts`: Test runner service

### Technology Stack
- **Language/Runtime:** TypeScript 5.0, Node 18+
- **Framework:** Express
- **Key Libraries:**
  - `eventsource`: SSE client for proxying events
  - `axios`: HTTP client for proxying requests
  - `ajv`: JSON Schema validation

## Dependencies

### Internal Dependencies
- `server-dotnet/operator/test-client`: Test scenario runner
- `server-dotnet/operator/commands/commands.catalog.json`: Command catalog
- `configs/dashboard.settings.json`: Configuration

### External Dependencies
- `express`: Web framework
- `eventsource`: SSE client
- `axios`: HTTP client
- `ajv`: JSON Schema validation
- TypeScript build tooling

**Dependency Graph:**
```
operator-dashboard → integration-api → room-server
                                    ↘  room-operator
                                    ↘  test-client
```

## Testing

### Test Structure
- `tests/unit/`: Unit tests for services and routes (to be added)
- `tests/integration/`: Integration tests (to be added)

### How to Run Tests
```bash
# All tests
npm test

# With coverage
npm run test:coverage
```

### Test Coverage
- Current: Minimal (infrastructure needs to be added)
- Target: 80%+

## Known Limits & Issues

### Performance
- SSE connections held open indefinitely (managed by client reconnection)
- Test runner uses polling (500ms) for log streaming
- Multiple concurrent test runs not fully tested

### Scalability
- Designed for single operator/small team use
- Not load-tested for high concurrency
- File-based artifact storage (not scalable long-term)

### Technical Debt
- Test coverage needs to be added
- Error handling could be more comprehensive
- Artifact cleanup not automated (manual deletion required)

### Compatibility
- Cross-platform test runner execution (Windows/Linux)
- Requires shell support for test execution

## Development Guidelines

### Code Style
- TypeScript strict mode
- ESLint configuration (to be added)
- Follow Express best practices

### Common Patterns
- **Route Handlers**: Keep thin, delegate to services
- **Services**: Encapsulate business logic and external interactions
- **SSE Streaming**: Use proper cleanup and error handling
- **Validation**: Use AJV for JSON Schema validation

### Anti-Patterns
- Don't mix business logic in route handlers: Use services
- Don't leave SSE connections without heartbeat: Send ping comments
- Don't execute commands without validation: Always validate against schema

## Deployment

### Build Process
```bash
npm run build
# Output: dist/
```

### Deployment Steps
1. Build the application: `npm run build`
2. Install production dependencies: `npm ci --production`
3. Start the server: `npm start`
4. Ensure configuration file exists at `configs/dashboard.settings.json`

### Environment-Specific Notes
- **Development:** Hot reload enabled with `npm run dev`
- **Staging:** Use production build, configure ports appropriately
- **Production:** Use process manager (PM2, systemd) for reliability

## Monitoring & Observability

### Logs
- Location: stdout/stderr (captured by process manager)
- Format: Plain text logs with timestamps
- Key log patterns:
  - Server startup: "Integration API listening on port..."
  - SSE connections: "SSE connection established/closed"
  - Test execution: "Test started/completed with exit code..."
  - Errors: "Error: [detailed message]"

### Metrics
- Not currently collected (could add Prometheus/statsd)

### Health Checks
- Endpoint: `GET /health`
- Returns: `{ status: "ok" }` with 200 OK

## Troubleshooting

### Common Issues

#### Issue: Port Already in Use
**Symptoms:**
- Server fails to start with EADDRINUSE error

**Solution:**
```bash
# Change port in configs/dashboard.settings.json
{
  "integrationApi": { "port": 40902 }
}
```

#### Issue: Test Runner Not Found
**Symptoms:**
- Test execution fails with "command not found"

**Solution:**
```bash
# Ensure runner script is executable
chmod +x scripts/run-test-client.sh

# For Windows, ensure Node.js and npm are in PATH
```

#### Issue: SSE Connection Issues
**Symptoms:**
- Events not streaming, connection errors

**Solution:**
- Verify RoomServer and RoomOperator are running at configured URLs
- Check firewall allows connections on configured ports
- Verify no CORS issues (only localhost:5173 allowed by default)

#### Issue: Command Validation Errors
**Symptoms:**
- Command execution fails with validation error

**Solution:**
- Verify parameters match schema in `commands.catalog.json`
- Ensure all required parameters are provided
- Check parameter types match schema

## Useful Links

- **Documentation:**
  - [Integration API README](./README.md)
  - [Operator Dashboard](../../apps/operator-dashboard/README.md)
  - [Test Client README](../../server-dotnet/operator/test-client/README.md)
  
- **Related ADRs:**
  - Check `docs/_adr/` for relevant architectural decisions
  
- **External Resources:**
  - [Express.js Documentation](https://expressjs.com)
  - [Server-Sent Events Specification](https://html.spec.whatwg.org/multipage/server-sent-events.html)

## Owner & Contact

- **Primary Owner:** Development Team
- **Slack Channel:** TBD
- **Repository:** invictvs-k/metacore-stack

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-20 | Initial context card created | AI Agent |

---

**Last Updated:** 2025-10-20  
**Version:** 1.0
