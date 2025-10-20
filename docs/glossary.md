# Glossary

**Last Updated:** 2025-10-20

This glossary defines key terms and concepts used throughout the Metacore Stack project.

---

## A

### ADR (Architecture Decision Record)
A document that captures an important architectural decision made along with its context and consequences. See `docs/_adr/` for examples.

### Artifact
Output files generated during test execution, including logs, traces, and result metadata. Stored in `.artifacts/integration/{timestamp}/runs/{runId}/`.

### AJV
A JSON schema validator library used to validate configuration files, command parameters, and data structures against JSON schemas.

---

## C

### Command Catalog
A JSON file (`commands.catalog.json`) that defines available commands that can be executed on the RoomOperator, including their parameters and JSON schema validation rules.

### CORS (Cross-Origin Resource Sharing)
HTTP security mechanism that allows the dashboard (running on one origin) to access the API (running on another origin). Configured to allow localhost:5173 for development.

---

## D

### Dashboard
The Operator Dashboard - a web-based UI for monitoring and controlling RoomServer, RoomOperator, and Test Client. Built with React and TypeScript.

---

## E

### Entity
A participant or object within a Room that has state and can receive messages. Examples include users, bots, or AI agents.

### Event Stream
A continuous flow of events from RoomServer or RoomOperator delivered via Server-Sent Events (SSE). Used for real-time monitoring.

---

## I

### Integration API
Backend service (Express/TypeScript) that provides a unified API for the Operator Dashboard. Handles configuration, event proxying, test execution, and command orchestration.

### Integration Test
Automated test that validates the interaction between multiple components (e.g., RoomOperator and RoomServer). Executed via the test-client.

---

## J

### JSON Schema
A vocabulary that allows you to annotate and validate JSON documents. Used throughout the project for configuration validation, command parameter validation, and data contracts.

---

## M

### MCP (Model Context Protocol)
Protocol for integrating AI models with the RoomServer. Provides a standardized way for language models to interact with room state and operations.

### Message
A unit of communication sent between entities within a room or between components in the system.

---

## R

### Room
A logical space where entities can interact, communicate, and share state. The core abstraction of the Metacore Stack.

### RoomOperator
A .NET service that manages room lifecycle, orchestrates operations, and provides a control API for room management. Runs on port 40802.

### RoomServer
A .NET SignalR-based service that hosts rooms, manages real-time communication, and coordinates entity interactions. Runs on port 40801.

### Run ID (runId)
A unique identifier (UUID) assigned to each test execution. Used to isolate test runs and organize artifacts.

---

## S

### Scenario
A predefined test case that exercises specific functionality. Defined in the test-client's `scenarios/` directory.

### Schema
See JSON Schema. Also refers to the canonical data models defined in the `schemas/` directory (Room, Entity, Message, etc.).

### SSE (Server-Sent Events)
A web technology that allows servers to push real-time updates to clients over HTTP. Used extensively for event streaming and test log streaming.

---

## T

### Test Client
A Node.js-based tool for executing integration test scenarios against RoomOperator and RoomServer. Located in `server-dotnet/operator/test-client/`.

---

## Z

### Zustand
A lightweight state management library for React. Used in the Operator Dashboard for global state (theme, runId, event history).

---

## Port Reference

Standard port assignments across the system:

- **40801**: RoomServer (SignalR + HTTP)
- **40802**: RoomOperator (HTTP API)
- **40901**: Integration API (HTTP API)
- **5173**: Operator Dashboard (Vite dev server)

See [PORT_CONFIGURATION.md](../PORT_CONFIGURATION.md) for detailed port configuration.

---

## Acronyms

- **ADR**: Architecture Decision Record
- **API**: Application Programming Interface
- **CORS**: Cross-Origin Resource Sharing
- **HTTP**: Hypertext Transfer Protocol
- **JSON**: JavaScript Object Notation
- **MCP**: Model Context Protocol
- **MVP**: Minimum Viable Product
- **NDJSON**: Newline Delimited JSON
- **REST**: Representational State Transfer
- **SSE**: Server-Sent Events
- **UI**: User Interface
- **UUID**: Universally Unique Identifier

---

## Related Documentation

- [Concept Definition](../CONCEPTDEFINITION.md): Core architectural concepts
- [Integration Guide](./ROOMOPERATOR_ROOMSERVER_INTEGRATION.md): Component integration details
- [Testing Guide](./TESTING.md): Testing methodology and tools
- [Table of Contents](./TOC.md): Complete documentation index
