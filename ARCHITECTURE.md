# Diagramas de Arquitetura - Metacore Stack

Este documento contém diagramas visuais para ajudar a entender a arquitetura do sistema.

## Arquitetura Geral

```
┌─────────────────────────────────────────────────────────────────────┐
│                           CLIENTS                                   │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐│
│  │ Web Browser │  │  Python     │  │   Node.js   │  │    CLI     ││
│  │   (Next.js) │  │   Agent     │  │   Agent     │  │   Client   ││
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └─────┬──────┘│
│         │                 │                 │                │       │
│         └─────────────────┴─────────────────┴────────────────┘       │
│                                  │                                   │
│                          SignalR WebSocket                           │
│                                  │                                   │
└──────────────────────────────────┼───────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      ROOM HOST (.NET 8)                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ RoomHub (SignalR)                                             │ │
│  │  • Join/Leave                                                 │ │
│  │  • SendToRoom                                                 │ │
│  │  • ListTools / CallTool                                       │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────────┐  │
│  │ SessionStore │  │ ArtifactStore│  │  McpRegistry            │  │
│  │              │  │              │  │  (MCP Bridge)           │  │
│  │ • Entities   │  │ • Workspaces │  │  • McpClient x N        │  │
│  │ • Rooms      │  │ • Manifests  │  │  • ResourceCatalog      │  │
│  └──────────────┘  └──────────────┘  └─────────────────────────┘  │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────────┐  │
│  │ PermissionSvc│  │ PolicyEngine │  │  RoomEventPublisher     │  │
│  └──────────────┘  └──────────────┘  └─────────────────────────┘  │
│                                                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               │ JSON-RPC 2.0 (WebSocket)
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      MCP SERVERS (TypeScript)                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐  │
│  │  web.search      │  │  http.request    │  │  [Extensível]   │  │
│  │  :8081           │  │  :8082           │  │  :808X          │  │
│  │                  │  │                  │  │                 │  │
│  │ • tools/list     │  │ • tools/list     │  │ • tools/list    │  │
│  │ • tool/call      │  │ • tool/call      │  │ • tool/call     │  │
│  └──────────────────┘  └──────────────────┘  └─────────────────┘  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Fluxo de Mensagens em uma Sala

```
┌──────────────┐                                            ┌──────────────┐
│  Entity A    │                                            │  Entity B    │
│  (Human)     │                                            │  (AI Agent)  │
└──────┬───────┘                                            └──────┬───────┘
       │                                                           │
       │ 1. Join("room-123", {id: "E-A", kind: "human"})          │
       │──────────────────────────────────────────────────────────>│
       │                                                           │
       │ <────────────────── ENTITY.JOIN Event ──────────────────>│
       │                     (broadcast to all)                    │
       │                                                           │
       │                      2. Join("room-123", ...)             │
       │ <─────────────────────────────────────────────────────────│
       │                                                           │
       │ <────────────────── ENTITY.JOIN Event ──────────────────>│
       │                     (Entity B joined)                     │
       │                                                           │
       │ 3. SendToRoom({type: "chat", payload: {text: "Hello"}})  │
       │──────────────────────────────────────────────────────────>│
       │                                                           │
       │ <─────────────── Message Broadcast ─────────────────────>│
       │                  (received by all)                        │
       │                                                           │
       │              4. SendToRoom({type: "command",              │
       │                 payload: {target: "E-B", port: "..."}})   │
       │──────────────────────────────────────────────────────────>│
       │                                                           │
       │                                      5. Execute Command   │
       │ <────────────────────────────────────────────────────────│
       │                                                           │
```

## Fluxo de Artefatos

```
┌─────────────┐                                        ┌──────────────┐
│   Entity    │                                        │  Room Host   │
└──────┬──────┘                                        └──────┬───────┘
       │                                                      │
       │ 1. POST /artifact/entity-E1/draft.md                │
       │      Content: "My draft document"                   │
       │─────────────────────────────────────────────────────>│
       │                                                      │
       │                                   2. Calculate SHA256│
       │                                      Create Manifest│
       │                                      Save to disk   │
       │                                                      │
       │ 3. Return ArtifactManifest                          │
       │ <─────────────────────────────────────────────────────
       │    {hash: "abc123...", workspace: "entity-E1", ...} │
       │                                                      │
       │ 4. Broadcast ARTIFACT.ADDED                         │
       │ <─────────────────────────────────────────────────────
       │                                                      │
       │ 5. POST /artifact/promote                           │
       │    {from: "entity-E1", name: "draft.md"}            │
       │─────────────────────────────────────────────────────>│
       │                                                      │
       │                                  6. Copy to room WS  │
       │                                     Update Manifest │
       │                                                      │
       │ 7. Return promoted manifest                         │
       │ <─────────────────────────────────────────────────────
       │    {workspace: "room-123", ...}                     │
       │                                                      │
```

## Integração MCP (Model Context Protocol)

```
┌─────────────┐                  ┌──────────────┐           ┌────────────────┐
│   Entity    │                  │  Room Host   │           │  MCP Server    │
│  (Agent)    │                  │  McpClient   │           │  (web.search)  │
└──────┬──────┘                  └──────┬───────┘           └───────┬────────┘
       │                                │                           │
       │ 1. ListTools("room-123")      │                           │
       │──────────────────────────────>│                           │
       │                                │                           │
       │                                │ 2. Aggregate tools        │
       │                                │    from all MCP servers   │
       │                                │                           │
       │ 3. Return catalog              │                           │
       │ <──────────────────────────────│                           │
       │  [{key: "web.search@local:search", ...}]                  │
       │                                │                           │
       │ 4. CallTool("room-123",        │                           │
       │    "search", {q: "AI agents"}) │                           │
       │──────────────────────────────>│                           │
       │                                │                           │
       │                                │ 5. RESOURCE.CALLED event  │
       │ <──────────────────────────────│                           │
       │                                │                           │
       │                                │ 6. JSON-RPC 2.0 Request   │
       │                                │  {method: "tool/call"...} │
       │                                │──────────────────────────>│
       │                                │                           │
       │                                │                    7. Execute search
       │                                │                       Return results
       │                                │ 8. JSON-RPC Response      │
       │                                │ <──────────────────────────
       │                                │  {result: {...}}          │
       │                                │                           │
       │ 9. Return result               │                           │
       │ <──────────────────────────────│                           │
       │  {ok: true, rawOutput: "..."}  │                           │
       │                                │                           │
       │                                │ 10. RESOURCE.RESULT event │
       │ <──────────────────────────────│                           │
       │                                │                           │
```

## Estrutura de Dados em Disco

```
metacore-stack/
├── data/
│   └── workspaces/
│       ├── room-{roomId}/
│       │   ├── file1.txt
│       │   ├── file2.md
│       │   └── artifact-manifest.json
│       │       {
│       │         "artifacts": [
│       │           {
│       │             "name": "file1.txt",
│       │             "sha256": "abc123...",
│       │             "origin": {
│       │               "roomId": "room-123",
│       │               "entityId": "E-A",
│       │               "timestamp": "2025-10-18T10:00:00Z"
│       │             }
│       │           }
│       │         ]
│       │       }
│       │
│       └── entity-{entityId}/
│           ├── draft.txt
│           └── artifact-manifest.json
│
└── logs/
    ├── events.jsonl (planejado)
    └── room-run.json (planejado)
```

## Políticas e Governança

```
┌────────────────────────────────────────────────────────────────┐
│                        Policy Layers                           │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  1. ENTITY POLICY                                              │
│     • Who can send commands to this entity?                    │
│       - "any" (anyone)                                         │
│       - "orchestrator" (only orchestrators)                    │
│       - "E-SPECIFIC-ID" (specific entity)                      │
│                                                                │
│  2. VISIBILITY POLICY                                          │
│     • Who can see this entity/resource?                        │
│       - "public" (everyone)                                    │
│       - "team" (room members)                                  │
│       - "owner" (only owner)                                   │
│                                                                │
│  3. MCP TOOL POLICY                                            │
│     • Scopes: ["net:github.com", "net:*.openai.com"]          │
│     • Rate Limit: {perMinute: 30}                              │
│     • Allowed Entities: "public" | "team" | "owner"           │
│                                                                │
│  4. WORKSPACE POLICY                                           │
│     • Who can access workspace?                                │
│       - Room workspace: all room members                       │
│       - Entity workspace: only owner or permitted entities     │
│                                                                │
└────────────────────────────────────────────────────────────────┘

Validation Flow:
┌──────────┐      ┌──────────────────┐      ┌───────────────┐
│ Request  │─────>│ PermissionService│─────>│ PolicyEngine  │
│          │      │ • CanSendCommand │      │ • Validate    │
│          │      │ • CanAccessWS    │      │ • Rate Limit  │
└──────────┘      └──────────────────┘      └───────────────┘
                           │                         │
                           ▼                         ▼
                     ┌─────────┐             ┌──────────┐
                     │ Allowed │             │ Denied   │
                     └─────────┘             └──────────┘
```

## Ciclo de Vida de uma Sala

```
         ┌──────────┐
         │   INIT   │  (Sala não existe ainda)
         └────┬─────┘
              │
              │ First entity joins
              ▼
         ┌──────────┐
         │  ACTIVE  │  (Entidades conectadas, trabalho em progresso)
         └────┬─────┘
              │
              ├─────────────────┐
              │                 │
              │ Pause signal    │ End signal
              ▼                 ▼
         ┌──────────┐      ┌──────────┐
         │  PAUSED  │      │  ENDED   │ (Sala finalizada, só leitura)
         └────┬─────┘      └──────────┘
              │
              │ Resume signal
              ▼
         ┌──────────┐
         │  ACTIVE  │
         └──────────┘

Nota: Implementação de estados PAUSED/ENDED planejada para futuras versões.
Atualmente, salas são criadas implicitamente no primeiro Join e existem
enquanto houver entidades conectadas.
```
