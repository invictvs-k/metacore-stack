# 🚀 Quick Reference - Metacore Stack

Guia rápido de referência para comandos e conceitos essenciais.

## Comandos Make Principais

```bash
# Setup inicial (apenas primeira vez)
make bootstrap              # Instala dependências globais e locais

# Build
make build                  # Compila .NET + TypeScript

# Desenvolvimento
make run-server            # Roda Room Host (.NET) em http://localhost:5000
make mcp-up                # Sobe MCP servers (web.search, http.request)

# Qualidade de código
make test                  # Roda todos os testes
make lint                  # Roda linters (TypeScript)
make format                # Formata código (.NET + TypeScript)
make schemas-validate      # Valida JSON Schemas

# Docker
make compose-up            # Sobe ambiente completo via Docker Compose
```

## Estrutura do Projeto (Resumo)

```
metacore-stack/
├── server-dotnet/          # Backend .NET 8 + SignalR
├── mcp-ts/                 # MCP servers TypeScript
├── ui/                     # Frontend Next.js (MVP)
├── schemas/                # JSON Schemas (validação)
└── infra/                  # Docker Compose
```

## Endpoints Principais

### SignalR Hub (`ws://localhost:5000/room`)

**Métodos:**
- `Join(roomId, entity)` → Lista de entidades
- `Leave(roomId, entityId)` → void
- `SendToRoom(roomId, message)` → void
- `ListEntities(roomId)` → Lista de entidades
- `ListTools(roomId)` → Catálogo de ferramentas MCP
- `CallTool(roomId, toolId, args)` → Resultado da ferramenta

**Eventos recebidos:**
- `message` - Mensagens da sala
- `ENTITY.JOIN` - Entidade entrou
- `ENTITY.LEAVE` - Entidade saiu
- `ROOM.STATE` - Estado da sala atualizado
- `RESOURCE.CALLED` - Ferramenta MCP chamada
- `RESOURCE.RESULT` - Resultado de ferramenta MCP

### HTTP REST

- `GET /` → "RoomServer alive"
- `GET /health` → Health check
- `POST /artifact/{workspace}/{name}` → Criar artefato
- `GET /artifact/{workspace}/{name}` → Baixar artefato
- `GET /artifact/{workspace}` → Listar artefatos
- `POST /artifact/promote` → Promover artefato

## Modelos de Dados

### EntitySpec
```json
{
  "id": "E-HUMAN-1",
  "kind": "human",
  "display_name": "João Silva",
  "visibility": "team",
  "capabilities": ["review", "approve"],
  "policy": {
    "allow_commands_from": "orchestrator"
  }
}
```

**Tipos de entidade:** `human`, `agent`, `npc`, `orchestrator`  
**Visibilidade:** `public`, `team`, `owner`

### MessageModel
```json
{
  "id": "01J97KXK7J0ZC9D02T4X9Q4S7X",
  "roomId": "room-123",
  "channel": "room",
  "from": "E-HUMAN-1",
  "type": "chat",
  "payload": {
    "text": "Hello, world!"
  },
  "ts": "2025-10-18T10:00:00Z"
}
```

**Tipos de mensagem:** `chat`, `command`, `event`, `artifact`  
**Canais:** `"room"` (broadcast), `"@entityId"` (DM)

### Command Message
```json
{
  "type": "command",
  "payload": {
    "target": "E-AGENT-1",
    "port": "text.generate",
    "inputs": {
      "text": "Optimize this text",
      "guidance": "clarity and fluency"
    }
  }
}
```

### ArtifactManifest
```json
{
  "name": "document.txt",
  "workspace": "room-123",
  "sha256": "abc123...",
  "size_bytes": 1024,
  "mime_type": "text/plain",
  "origin": {
    "roomId": "room-123",
    "entityId": "E-AGENT-1",
    "port": "text.generate",
    "timestamp": "2025-10-18T10:00:00Z"
  }
}
```

## Fluxos Comuns

### Conectar e Entrar em uma Sala

```typescript
// 1. Conectar ao Hub SignalR
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/room")
  .build();

await connection.start();

// 2. Entrar na sala
const entities = await connection.invoke("Join", "room-123", {
  id: "E-USER-1",
  kind: "human",
  display_name: "João",
  visibility: "team",
  capabilities: [],
  policy: { allow_commands_from: "any" }
});

// 3. Escutar mensagens
connection.on("message", (msg) => {
  console.log("Received:", msg);
});

// 4. Enviar mensagem
await connection.invoke("SendToRoom", "room-123", {
  from: "E-USER-1",
  type: "chat",
  channel: "room",
  payload: { text: "Hello!" }
});
```

### Chamar Ferramenta MCP

```typescript
// 1. Listar ferramentas disponíveis
const tools = await connection.invoke("ListTools", "room-123");
console.log(tools);
// [{key: "web.search@local:search", toolId: "search", ...}]

// 2. Chamar ferramenta
const result = await connection.invoke("CallTool", "room-123", "search", {
  q: "AI agents",
  limit: 5
});

console.log(result);
// {ok: true, rawOutput: "[{...}]"}
```

### Criar e Promover Artefato

```bash
# 1. Criar artefato no workspace da entidade
curl -X POST http://localhost:5000/artifact/entity-E1/draft.md \
  -H "Content-Type: text/markdown" \
  --data-binary @draft.md

# 2. Promover para workspace da sala
curl -X POST http://localhost:5000/artifact/promote \
  -H "Content-Type: application/json" \
  -d '{"from": "entity-E1", "name": "draft.md"}'

# 3. Baixar artefato
curl http://localhost:5000/artifact/room-123/draft.md
```

## Configuração MCP Server

**Arquivo:** `server-dotnet/src/RoomServer/appsettings.json`

```json
{
  "McpServers": [
    {
      "id": "my-server@local",
      "url": "ws://localhost:8083",
      "visibility": "room"
    }
  ],
  "McpDefaults": {
    "rateLimit": { "perMinute": 60 },
    "scopes": ["net:*"],
    "allowedEntities": "public"
  }
}
```

## Políticas de Governança

### Entity Policy
```json
{
  "allow_commands_from": "orchestrator",  // "any" | "orchestrator" | "E-ID"
  "env_whitelist": ["API_KEY", "DB_URL"]
}
```

### MCP Tool Policy
```json
{
  "visibility": "room",                   // "public" | "room" | "entity"
  "allowedEntities": "public",            // "public" | "team" | "owner"
  "scopes": ["net:github.com"],           // Whitelist de domínios
  "rateLimit": { "perMinute": 30 }
}
```

## Convenções de Código

### Commits (Conventional Commits)
```bash
feat: add new MCP server for GitHub integration
fix: resolve null reference in artifact store
chore: update dependencies
docs: improve onboarding guide
test: add integration tests for RoomHub
```

### Prefixos de ID
- `E-HUMAN-*` - Entidades humanas
- `E-AGENT-*` - Agentes de IA
- `E-ORC-*` - Orquestradores
- `E-NPC-*` - NPCs/entidades reativas
- `room-*` - Salas
- `01J9...` - ULIDs (IDs de mensagens)

## Troubleshooting

### Porta já em uso
```bash
# Matar processo na porta 5000
kill -9 $(lsof -t -i:5000)

# Ou mudar a porta
ASPNETCORE_URLS=http://localhost:5001 make run-server
```

### MCP server não conecta
1. Verificar se server está rodando (`make mcp-up`)
2. Verificar URL em `appsettings.json`
3. Verificar logs do Room Host
4. Testar conexão manual: `wscat -c ws://localhost:8081`

### Testes falhando
```bash
# Limpar e rebuild
cd server-dotnet
dotnet clean
dotnet build -c Debug
dotnet test -c Debug
```

### Schemas inválidos
```bash
cd schemas
pnpm validate
# Verificar exemplos em schemas/examples/
```

## Recursos Úteis

- **Documentação Completa:** [ONBOARDING.md](./ONBOARDING.md)
- **Diagramas:** [ARCHITECTURE.md](./ARCHITECTURE.md)
- **Especificação:** [CONCEPTDEFINITION.md](./CONCEPTDEFINITION.md)
- **SignalR Docs:** https://docs.microsoft.com/aspnet/core/signalr
- **JSON-RPC 2.0:** https://www.jsonrpc.org/specification
- **MCP Protocol:** https://modelcontextprotocol.io/

## Atalhos de Desenvolvimento

```bash
# Terminal 1: Room Host com auto-reload
cd server-dotnet/src/RoomServer
dotnet watch run

# Terminal 2: MCP servers
make mcp-up

# Terminal 3: Schemas validation (watch mode)
cd schemas
pnpm test --watch

# Terminal 4: UI dev server
cd ui
pnpm dev
```

## Variáveis de Ambiente Comuns

```bash
# Room Host
ASPNETCORE_URLS=http://localhost:5000
ASPNETCORE_ENVIRONMENT=Development

# MCP Servers
PORT=8081                              # web.search
PORT=8082                              # http.request
```

---

**Dica:** Use `make bootstrap` apenas na primeira vez. Para desenvolvimento dia-a-dia, use `make run-server` e `make mcp-up`.
