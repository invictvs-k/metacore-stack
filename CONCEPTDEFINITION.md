# 🧠 Metacore Stack — Functional Specification

### (Version 1.0)

---

## 1. Overview

The Metacore Stack is a collaborative execution environment that allows **humans and AI agents** to coexist, interact, and work on **shared artifacts** in a coordinated and persistent manner.

The core idea is simple:

> "A Room is a living space where entities (human or artificial) enter, interact, produce and transform artifacts, using connected resources, with full governance and traceability."

The system is **language and AI technology agnostic**.  
A Python agent, a human in a browser, and a .NET orchestrator can coexist in the same Room — all acting through standardized interfaces and protocols.

---

## 2. Room Concept

### What it is

A **Room** is the logical and execution environment where work happens.  
Think of it as a **"collaborative game server"**:

- has a **lifecycle** (`init → active → paused → ended`),
- maintains **resources, entities, artifacts, and policies**,
- and remains alive until terminated.

### Function

The Room:

- manages the **global state** (who is present, which resources are active, which artifacts exist);
- propagates **messages and events in real time** among members;
- stores and versions **produced artifacts**;
- applies **security and governance policies**;
- records **telemetry and history** of everything that occurred.

### Example

Imagine a Room called `room-ai-workflow`.  
In it are:

- Marcelo (human),
- the Agent `TextRefiner`,
- and the Orchestrator `StageManager`.

Marcelo sends a Markdown file.  
The `TextRefiner` reads it, improves clarity, and saves a new version.  
The `StageManager` detects the `ARTIFACT.ADDED` event and triggers the next task.  
All of this occurs **within the Room**, with automatic logs and versioning.

---

## 3. Entities

### What they are

**Entities** are Room members.  
They represent both **human persons** and **AI agents**, **automated processes**, or **NPCs (reactive entities)**.

Each entity:

- has an **ID**, a **type** (`human`, `agent`, `npc`, `orchestrator`),
- possesses **capabilities** (ports/functions it can execute),
- obeys **policies** (who can command it, what it can access),
- and may have its **own workspace** (its "work desk").

### Function

Entities are **the actors**.  
Everything that happens in the Room originates from an Entity —  
every message, artifact, or action has a `from` and, optionally, a `to`.

### Example

```json
{
  "id": "E-AGENT-1",
  "kind": "agent",
  "display_name": "TextRefiner",
  "capabilities": ["text.generate", "review"],
  "visibility": "room",
  "policy": { "allow_commands_from": "orchestrator" }
}
```

This agent accepts commands to generate and review texts, and only the orchestrator can give direct instructions to it.

---

## 4. Workspaces and Artifacts

### What they are

**Workspaces** are the "work desks".  
There are two levels:

- **Room Workspace**: shared space, visible to all.
- **Entity Workspace**: private space, visible only to its owner (unless promoted).

**Artifacts** are files, texts, data, or outputs created by entities.  
Each artifact has a **manifest** (`artifact-manifest.json`) with:

- name, type (e.g., `doc/markdown`, `app/json`);
- origin (room, entity, port);
- SHA256 hash and versioning;
- metadata and timestamp.

### Function

Workspaces enable:

- controlled isolation;
- transparent versioning;
- reconstruction and audit of results.

### Flow example

1. Marcelo (E-H1) uploads `input.txt`.
2. The Agent `TextRefiner` generates `output_refined.txt`.
3. The Orchestrator reads the event and sends the result for review.
4. All files remain on the Room's "desk", versioned and traceable.

---

## 5. Messaging and Communication

### What it is

The **Room Bus** is the real-time messaging system.  
Based on **SignalR (WebSocket)**, it connects all entities and propagates messages of type:

- `chat` — free/human communication;
- `command` — formal execution instruction;
- `event` — system or entity event;
- `artifact` — notification about new or changed artifact.

### Function

It's the **heart of the Room**.  
Everything that happens is communicated via messages —  
this allows humans, agents, and orchestrators to share the same channel.

### Example (`command` message)

```json
{
  "id": "01J97KXK7J0ZC9D02T4X9Q4S7X",
  "roomId": "room-ai-workflow",
  "channel": "room",
  "from": "E-ORC",
  "type": "command",
  "payload": {
    "target": "E-AGENT-1",
    "port": "text.generate",
    "inputs": { "text": "Optimize this text." }
  }
}
```

The agent `E-AGENT-1` receives the message and executes the `text.generate` port.

---

## 6. Ports and Capabilities

### What they are

**Ports** are standardized function contracts — they define what an entity _can do_.

Example:

- `text.generate` — receives text and parameters, returns new version.
- `review` — analyzes and provides feedback.
- `plan` — creates task plan.
- `search.web` — performs search via MCP resource.

### Function

Ports transform agents and humans into **interchangeable modules**.  
Any entity can announce its ports and be called by another component.

### Example (adapter)

A `text.generate` can be implemented by:

- a local agent via OpenAI API,
- a human manually reviewing text,
- an external service plugged in via MCP.

All follow the same input/output contract.

---

## 7. Resources and MCP

### What they are

**Resources** are external tools available in the Room.  
They can be:

- Git repositories,
- HTTP APIs,
- search engines,
- databases,
- conversion tools, etc.

They are exposed via **MCP (Model Context Protocol)** —  
an open standard that allows connecting tools via WebSocket/JSON-RPC.

### Function

Resources expand the Room's "reach" —  
Entities can query data, send requests, and seek knowledge outside the environment, with security and control.

### Example

An MCP Server `web.search` (in TypeScript) exposes:

```json
{
  "tools": [
    {
      "id": "web.search",
      "title": "Search the Web",
      "inputSchema": { "q": "string", "limit": "number" },
      "outputSchema": { "items": "array" }
    }
  ]
}
```

The Entity in the Room calls:

```json
{ "toolId": "web.search", "args": { "q": "cognitive agents", "limit": 3 } }
```

And receives a list of results.  
Everything recorded, versioned, and audited.

---

## 8. Orchestrators and Tasks

### What they are

**Orchestrators** are special Entities that possess coordination "scripts" — called **Tasks**.  
These scripts define:

- **sequential or conditional commands**;
- **dependencies between tasks**;
- **human validation checkpoints**;
- **expected results**.

### Function

They transform the Room into a **programmable execution environment**.  
Instead of writing a rigid code flow, you write JSON that describes the work — and the Orchestrator executes it.

### Example (simplified Task Script)

```json
{
  "name": "Refine Document",
  "steps": [
    {
      "task": "generate_text",
      "target": "E-AGENT-1",
      "port": "text.generate",
      "inputs": { "text": "draft.md", "guidance": "clarity and fluency" },
      "output": "refined.md"
    },
    {
      "task": "review",
      "target": "E-HUMAN-1",
      "port": "review",
      "inputs": { "artifact": "refined.md" },
      "checkpoint": "await_approval"
    }
  ]
}
```

The Orchestrator executes step by step, awaiting confirmations and publishing events (`TASK.START`, `TASK.END`, `CHECKPOINT.REACHED`).

---

## 9. Policies and Governance

### What they are

**Policies** are security and governance rules applied in real time:

- who can send commands to whom,
- which resources each entity can access,
- how many times it can use a tool (rate-limit),
- what can be logged or masked (PII).

### Function

They ensure **control and compliance**, without blocking work fluidity.  
They are enforced by the Room Host, and recorded in manifests and telemetry logs.

### Example

```json
"policy": {
  "allow_commands_from": "orchestrator",
  "scopes": ["net:github.com", "net:*.openai.com"],
  "rateLimit": { "perMinute": 30 }
}
```

---

## 10. Telemetry and History

### What it is

Every event generated in the Room is recorded in:

- `events.jsonl` — continuous event log;
- `room-run.json` — consolidated summary (entities, artifacts, duration);
- and optionally sent via **OpenTelemetry** for real-time observability.

### Function

Enables:

- complete traceability (who did what, when, and with what);
- audit and replay of past executions;
- learning and flow adjustment.

### Example (log line)

```json
{
  "ts": "2025-10-17T12:10:03Z",
  "event": "RESOURCE.CALLED",
  "room": "room-ai-workflow",
  "entity": "E-AGENT-1",
  "tool": "web.search",
  "args": { "q": "Azure AI" }
}
```

---

## 11. Potential and Extensions

### a) Universal meta-platform

Being based on **protocols**, the Room can integrate:

- Python agents (LangGraph, Agno, AutoGen);
- .NET orchestrators (Orleans);
- UI/Web apps (Next.js);
- MCP resources written in any language.

### b) Hybrid environments

A Room can be open to multiple humans and agents simultaneously, becoming a **collaborative cognitive workspace** — hybrid human+AI.

### c) Reusability

Each **Stage** of a larger project is just an **encapsulated Room**, with defined input and output, allowing reuse as modules in broader pipelines.

### d) Future applications

- **Persistent cognitive meetings**: human teams + AIs working with continuous context.
- **AI-oriented development environments**: agents refactoring, testing, and versioning code in real time.
- **Multi-agent assistants in specific domains**: legal, medical, educational, creative.
- **Autonomous governance**: dynamic policies adapting to context and entity performance.

---

## 12. Complete functional example

1. **Room created**  
   `POST /rooms → {"id":"room-abc123","state":"active"}`
2. **Entities enter**
   - Human (E-H1) via UI.
   - AI Agent (E-A1) via WS.
   - Orchestrator (E-ORC) via script.

3. **Artifact sent**  
   Marcelo sends `original_text.md`.
4. **Orchestrator sends command**  
   `E-ORC` → `E-A1` (`port=text.generate`, input: `original_text.md`).
5. **Agent produces output**  
   `E-A1` saves `refined_text.md`.
6. **Orchestrator requests review**  
   `E-ORC` → `E-H1` (`port=review`, input: `refined_text.md`).
7. **Marcelo approves**  
   Artifact is marked as final.
8. **Room finalizes**  
   All artifacts, events, and manifests are saved.  
   `room-run.json` summarizes the session history.

---

## 13. Strategic Potential

The Meta-platform is a **universal framework for cognitive collaboration**.

It's not "just another AI chat system".  
It's a **living work infrastructure**, where:

- each project can become a network of rooms and stages;
- each room can be reopened, audited, and reused;
- and each entity (human or AI) is interoperable via contracts.

In other words:

> The Living Room is what transforms AI work from something episodic (prompts and responses) into something continuous, governed, and evolutionary.

---
