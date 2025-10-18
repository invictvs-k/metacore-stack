# 📘 Guia de Onboarding - Metacore Stack

**Bem-vindo ao projeto Metacore Stack!** Este documento foi criado para ajudar desenvolvedores juniores e novos membros da equipe a entenderem rapidamente o projeto e começarem a contribuir.

---

## 1. Visão Geral do Projeto e Propósito

### Nome do Projeto
**Metacore Stack** — também conhecido como **Metaplataforma** ou **Sala Viva**

### Propósito Principal
A Metacore Stack é uma metaplataforma revolucionária que permite a colaboração em tempo real entre **humanos e agentes de IA** em um espaço compartilhado chamado "Sala". O objetivo é criar um ambiente de execução colaborativo onde:

- **Entidades** (humanos, agentes de IA, NPCs, orquestradores) podem coexistir e interagir
- **Artefatos** (arquivos, dados, resultados) são criados, versionados e compartilhados
- **Recursos externos** (APIs, bancos de dados, ferramentas) são conectados via MCP (Model Context Protocol)
- **Fluxos de trabalho** são orquestrados de forma programável e auditável

**Problema que resolve:**
- Transformar interações com IA de algo episódico (prompts e respostas isoladas) em algo **contínuo, governado e evolutivo**
- Permitir que diferentes tecnologias (Python, .NET, TypeScript) e diferentes agentes de IA trabalhem juntos de forma padronizada
- Fornecer rastreabilidade completa, versionamento e governança em ambientes híbridos humano+IA

### Público-alvo/Usuários
- **Desenvolvedores** que constroem aplicações com agentes de IA
- **Times híbridos** humano+IA trabalhando em projetos colaborativos
- **Empresas** que precisam de governança e auditoria em fluxos de trabalho com IA
- **Pesquisadores** explorando sistemas multiagentes e colaboração cognitiva

---

## 2. Inventário de Componentes e Estrutura do Repositório

### Estrutura de Diretórios Principais

```
metacore-stack/
├── server-dotnet/          # Backend principal - Room Host em .NET 8
│   ├── src/RoomServer/     # Código fonte do servidor
│   └── tests/              # Testes unitários e de integração
├── mcp-ts/                 # Servidores MCP em TypeScript
│   └── servers/            # Implementações de MCP servers
│       ├── web.search/     # Servidor de busca na web (exemplo)
│       └── http.request/   # Servidor de requisições HTTP (exemplo)
├── ui/                     # Interface web (Next.js) - MVP mínimo
│   ├── app/                # Páginas Next.js (App Router)
│   └── src/                # Componentes e lógica da UI
├── schemas/                # JSON Schemas para validação de contratos
│   ├── room.schema.json    # Schema da Sala
│   ├── entity.schema.json  # Schema de Entidades
│   ├── message.schema.json # Schema de Mensagens
│   ├── artifact-manifest.schema.json # Schema de Artefatos
│   └── examples/           # Exemplos e casos de teste
├── infra/                  # Infraestrutura Docker
│   └── docker-compose.yml  # Configuração para ambiente local
├── tools/                  # Scripts auxiliares
│   └── scripts/            # Scripts de bootstrap e desenvolvimento
└── .github/                # Configurações GitHub
    └── workflows/          # CI/CD pipelines
```

### Arquivos Importantes na Raiz

- **`README.md`** - Guia rápido de início (quickstart)
- **`CONCEPTDEFINITION.md`** - Especificação funcional completa do sistema (leitura essencial!)
- **`ONBOARDING.md`** - Este documento de onboarding
- **`Makefile`** - Comandos principais para desenvolvimento
- **`CONTRIBUTING.md`** - Guia de contribuição
- **`LICENSE`** - Licença MIT
- **`.editorconfig`** - Configurações de editor
- **`.gitignore`** - Arquivos ignorados pelo Git

### Tecnologias e Ferramentas Utilizadas

#### Backend (.NET)
- **Linguagem:** C# 11 com .NET 8
- **Framework Web:** ASP.NET Core 8
- **Comunicação em Tempo Real:** SignalR (WebSocket)
- **IDs Únicos:** NUlid (IDs ordenados lexicograficamente)
- **Serialização:** System.Text.Json
- **Testes:** xUnit
- **Total:** ~1.217 linhas de código C#

**Principais dependências:**
- `Microsoft.Extensions.Logging.Console` v8.0.8 → v9.0.0
- `NUlid` v1.7.3

#### MCP Servers (TypeScript)
- **Linguagem:** TypeScript 5.6+
- **Runtime:** Node.js 20
- **Gerenciador de Pacotes:** pnpm 9
- **Comunicação:** WebSocket (biblioteca `ws`)
- **Validação:** Zod
- **Transpilação:** tsx, tsc

**Principais dependências:**
- `ws` v8.17.1 (WebSocket)
- `zod` v3.23.8 (validação de schemas)
- `tsx` v4.19.1 (execução TypeScript)

#### UI (Next.js)
- **Framework:** Next.js 14.2.6 (App Router)
- **Linguagem:** TypeScript
- **Biblioteca UI:** React 18.3.1
- **Linting:** ESLint

**Status:** MVP mínimo - apenas página de placeholder

#### Schemas e Validação
- **JSON Schema:** Draft 2020-12
- **Validador:** AJV 8.17.1
- **Formato:** ajv-formats 3.0.1
- **Utilitários:** globby 14.1.0

#### Infraestrutura
- **Containerização:** Docker e Docker Compose
- **CI/CD:** GitHub Actions
- **Versionamento:** Conventional Commits

### Configurações Importantes

#### Arquivo: `server-dotnet/src/RoomServer/appsettings.json`
```json
{
  "McpServers": [
    {
      "id": "web.search@local",
      "url": "ws://localhost:8081",
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

**Configurações de Ambiente:**
- Variáveis de ambiente são gerenciadas via ASP.NET Core (appsettings.json, appsettings.Development.json)
- Secrets não devem ser commitados (usar variáveis de ambiente ou secrets management)
- MCP servers têm configuração via `PORT` env var (padrão: 8081, 8082)

---

## 3. Arquitetura do Sistema e Fluxo de Dados

> 💡 **Dica:** Para diagramas visuais detalhados, consulte [ARCHITECTURE.md](./ARCHITECTURE.md)

### Tipo de Arquitetura

**Arquitetura Híbrida:**
- **Backend:** Servidor monolítico em .NET (Room Host)
- **Microsserviços:** MCP Servers em TypeScript (serviços externos plugáveis)
- **Frontend:** SPA em Next.js (opcional/em desenvolvimento)
- **Comunicação:** WebSocket via SignalR e JSON-RPC 2.0

### Principais Camadas e Serviços

```
┌─────────────────────────────────────────────────────────────┐
│                    Clients (Navegador/CLI)                  │
│              SignalR WebSocket + HTTP REST                  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Room Host (.NET 8 + SignalR)                   │
├─────────────────────────────────────────────────────────────┤
│  • RoomHub (SignalR Hub) - Comunicação em tempo real       │
│  • SessionStore - Gerenciamento de entidades conectadas     │
│  • ArtifactStore - Armazenamento de artefatos               │
│  • McpRegistry - Registro e conexão com MCP servers         │
│  • PolicyEngine - Governança e controle de acesso           │
│  • PermissionService - Validação de permissões              │
│  • RoomEventPublisher - Publicação de eventos               │
└────────────────────────┬────────────────────────────────────┘
                         │ WebSocket (JSON-RPC 2.0)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              MCP Servers (TypeScript)                       │
├─────────────────────────────────────────────────────────────┤
│  • web.search - Busca na web                                │
│  • http.request - Requisições HTTP                          │
│  • [Extensível para outros recursos]                        │
└─────────────────────────────────────────────────────────────┘
```

### Comunicação entre Componentes

1. **Cliente ↔ Room Host:**
   - Protocolo: **SignalR** (WebSocket com fallback)
   - Formato: JSON
   - Métodos principais:
     - `Join(roomId, entity)` - Entrar em uma sala
     - `SendToRoom(roomId, message)` - Enviar mensagem
     - `ListTools(roomId)` - Listar recursos MCP
     - `CallTool(roomId, toolId, args)` - Chamar recurso MCP

2. **Room Host ↔ MCP Servers:**
   - Protocolo: **JSON-RPC 2.0** sobre WebSocket
   - Formato: JSON
   - Métodos principais:
     - `tools/list` - Listar ferramentas disponíveis
     - `tool/call` - Executar ferramenta

3. **Eventos em Tempo Real:**
   - Todos os eventos são propagados via SignalR Groups
   - Tipos de eventos:
     - `ENTITY.JOIN` - Entidade entrou
     - `ENTITY.LEAVE` - Entidade saiu
     - `ROOM.STATE` - Estado da sala atualizado
     - `RESOURCE.CALLED` - Recurso MCP foi chamado
     - `RESOURCE.RESULT` - Resultado do recurso MCP

### Fluxo de Dados Chave

#### Exemplo 1: Entidade Entra em uma Sala

```
1. Cliente → Room Host
   POST /room (SignalR): Join(roomId: "room-123", entity: {...})

2. Room Host valida:
   - Entity ID é único
   - Visibilidade é adequada
   - Políticas são válidas

3. Room Host:
   - Adiciona sessão ao SessionStore
   - Adiciona conexão ao SignalR Group
   - Publica evento ENTITY.JOIN

4. Room Host → Todos os clientes na sala
   Broadcast via SignalR: ENTITY.JOIN event

5. Room Host → Cliente
   Retorna lista de entidades na sala
```

#### Exemplo 2: Agente Chama Recurso MCP (Web Search)

```
1. Cliente (Agente) → Room Host
   CallTool(roomId: "room-123", toolId: "web.search", args: {q: "AI agents"})

2. Room Host:
   - Valida permissões (PolicyEngine)
   - Resolve toolId → serverId + toolId
   - Publica evento RESOURCE.CALLED

3. Room Host → MCP Server (web.search)
   JSON-RPC: {"method": "tool/call", "params": {"id": "web.search", "args": {...}}}

4. MCP Server executa busca
   Retorna resultados via JSON-RPC response

5. Room Host → Cliente (Agente)
   Retorna resultado da ferramenta

6. Room Host → Todos os clientes na sala
   Broadcast: RESOURCE.RESULT event
```

#### Exemplo 3: Criação e Versionamento de Artefato

```
1. Cliente → Room Host
   POST /artifact/{workspace}/{name} (HTTP)
   Body: conteúdo do arquivo

2. Room Host (FileArtifactStore):
   - Calcula SHA256 do conteúdo
   - Verifica se artefato já existe (por hash)
   - Salva arquivo no disco (data/workspaces/{workspace}/{name})
   - Cria artifact-manifest.json com metadados

3. Room Host → Cliente
   Retorna manifest do artefato criado

4. Room Host → Todos os clientes na sala
   Broadcast: ARTIFACT.ADDED event
```

---

## 4. Funcionalidades Implementadas

### 4.1 Gerenciamento de Salas (Rooms)

**Descrição:** Sistema de "salas" onde entidades podem se conectar, colaborar e compartilhar recursos.

**Funcionalidades:**
- ✅ Múltiplas salas simultâneas isoladas
- ✅ Ciclo de vida da sala (Init, Active, Paused, Ended) - definido em schemas
- ✅ Broadcast de eventos em tempo real para membros da sala

**Arquivos chave:**
- `server-dotnet/src/RoomServer/Hubs/RoomHub.cs` - Hub principal SignalR
- `server-dotnet/src/RoomServer/Models/RoomState.cs` - Enum de estados
- `schemas/room.schema.json` - Contrato da sala

---

### 4.2 Entidades (Entities)

**Descrição:** Representação de humanos, agentes de IA, NPCs e orquestradores que participam de uma sala.

**Funcionalidades:**
- ✅ Tipos de entidade: `human`, `agent`, `npc`, `orchestrator`
- ✅ Capabilities (portas/funções que a entidade sabe executar)
- ✅ Visibilidade: `public`, `team`, `owner`
- ✅ Políticas de governança por entidade
- ✅ Join/Leave de entidades em salas

**Arquivos chave:**
- `server-dotnet/src/RoomServer/Models/EntitySpec.cs` - Modelo de entidade
- `server-dotnet/src/RoomServer/Services/SessionStore.cs` - Gerenciamento de sessões
- `schemas/entity.schema.json` - Contrato de entidade

**Métodos SignalR:**
- `Join(roomId, entity)` - Entrar em uma sala
- `Leave(roomId, entityId)` - Sair de uma sala
- `ListEntities(roomId)` - Listar entidades na sala

---

### 4.3 Sistema de Mensageria

**Descrição:** Protocolo de mensagens para comunicação entre entidades em tempo real.

**Funcionalidades:**
- ✅ Mensagens de sala (broadcast)
- ✅ Mensagens diretas (DM) entre entidades
- ✅ Tipos de mensagem: `chat`, `command`, `event`, `artifact`
- ✅ Validação de remetente e destinatário
- ✅ IDs únicos (ULID) para rastreamento

**Arquivos chave:**
- `server-dotnet/src/RoomServer/Models/MessageModel.cs` - Modelo de mensagem
- `server-dotnet/src/RoomServer/Hubs/RoomHub.cs` (métodos HandleDirectMessage, HandleRoomMessage)
- `schemas/message.schema.json` - Contrato de mensagens

**Métodos SignalR:**
- `SendToRoom(roomId, message)` - Enviar mensagem

**Tipos de canal:**
- `"room"` - Broadcast para toda a sala
- `"@entityId"` - Mensagem direta para uma entidade específica

---

### 4.4 Artefatos e Workspaces

**Descrição:** Sistema de armazenamento versionado de arquivos/dados produzidos pelas entidades.

**Funcionalidades:**
- ✅ Workspace da sala (compartilhado)
- ✅ Workspace por entidade (privado)
- ✅ Versionamento via SHA256
- ✅ Manifests com metadados (origem, timestamp, hash)
- ✅ Upload e download de artefatos
- ✅ Promoção de artefatos (de entidade para sala)

**Arquivos chave:**
- `server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs` - Armazenamento em disco
- `server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs` - Modelos de artefato
- `server-dotnet/src/RoomServer/Controllers/ArtifactEndpoints.cs` - Endpoints HTTP
- `schemas/artifact-manifest.schema.json` - Contrato de manifest

**Endpoints HTTP:**
- `POST /artifact/{workspace}/{name}` - Criar/atualizar artefato
- `GET /artifact/{workspace}/{name}` - Baixar artefato
- `POST /artifact/promote` - Promover artefato de entidade para sala
- `GET /artifact/{workspace}` - Listar artefatos do workspace

**Estrutura em disco:**
```
data/
└── workspaces/
    ├── room-{roomId}/           # Workspace da sala
    │   ├── file1.txt
    │   └── artifact-manifest.json
    └── entity-{entityId}/        # Workspace da entidade
        ├── file2.txt
        └── artifact-manifest.json
```

---

### 4.5 Integração com MCP (Model Context Protocol)

**Descrição:** Ponte para conectar recursos externos (APIs, bancos de dados, ferramentas) via MCP servers.

**Funcionalidades:**
- ✅ Registro de múltiplos MCP servers
- ✅ Descoberta automática de ferramentas (tools/list)
- ✅ Execução de ferramentas (tool/call)
- ✅ Rate limiting (MVP - estrutura implementada)
- ✅ Políticas de visibilidade e acesso
- ✅ Reconexão automática com backoff exponencial
- ✅ Eventos de rastreamento (RESOURCE.CALLED, RESOURCE.RESULT)

**Arquivos chave:**
- `server-dotnet/src/RoomServer/Services/Mcp/McpClient.cs` - Cliente WebSocket JSON-RPC
- `server-dotnet/src/RoomServer/Services/Mcp/McpRegistry.cs` - Registro de servers
- `server-dotnet/src/RoomServer/Services/Mcp/ResourceCatalog.cs` - Catálogo de ferramentas
- `server-dotnet/src/RoomServer/Services/Mcp/PolicyEngine.cs` - Governança
- `server-dotnet/src/RoomServer/Hubs/RoomHub.Resources.cs` - Métodos SignalR
- `server-dotnet/src/RoomServer/Services/Mcp/README.md` - Documentação detalhada

**Métodos SignalR:**
- `ListTools(roomId)` - Listar ferramentas disponíveis
- `CallTool(roomId, toolIdOrKey, args)` - Executar ferramenta

**Configuração (appsettings.json):**
```json
{
  "McpServers": [
    {"id": "web.search@local", "url": "ws://localhost:8081", "visibility": "room"}
  ]
}
```

---

### 4.6 Governança e Políticas

**Descrição:** Sistema de controle de acesso e segurança.

**Funcionalidades:**
- ✅ Políticas por entidade (`allow_commands_from`: "any" | "orchestrator" | "specific-id")
- ✅ Validação de permissões para comandos
- ✅ Validação de permissões para mensagens diretas
- ✅ Visibilidade de ferramentas MCP
- ✅ Scopes e whitelists de recursos
- ✅ Estrutura para rate limiting (não totalmente implementado)

**Arquivos chave:**
- `server-dotnet/src/RoomServer/Models/PolicySpec.cs` - Modelo de política
- `server-dotnet/src/RoomServer/Services/PermissionService.cs` - Validação de permissões
- `server-dotnet/src/RoomServer/Services/Mcp/PolicyEngine.cs` - Engine de políticas MCP

**Exemplo de política:**
```json
{
  "allow_commands_from": "orchestrator",
  "scopes": ["net:github.com", "net:*.openai.com"],
  "rateLimit": {"perMinute": 30}
}
```

---

### 4.7 Telemetria e Eventos

**Descrição:** Sistema de logging e rastreamento de eventos.

**Funcionalidades:**
- ✅ Publisher/subscriber de eventos em tempo real
- ✅ Eventos tipados (ENTITY.JOIN, ENTITY.LEAVE, ROOM.STATE, RESOURCE.CALLED, RESOURCE.RESULT)
- ✅ Logging estruturado via ASP.NET Core Logging
- 🚧 Persistência em `events.jsonl` (planejado)
- 🚧 Integração com OpenTelemetry (planejado)

**Arquivos chave:**
- `server-dotnet/src/RoomServer/Services/RoomEventPublisher.cs` - Publisher de eventos

---

## 5. Como Iniciar/Rodar o Projeto

### Pré-requisitos

**Ferramentas necessárias:**
- **.NET SDK 8.0+** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 20+** - [Download](https://nodejs.org/)
- **pnpm 9** - Instalado automaticamente via `make bootstrap`
- **Git** - Para clonar o repositório
- **(Opcional) Docker** - Para rodar via containers

### Pontos de Entrada

**Backend (.NET):**
- Arquivo: `server-dotnet/src/RoomServer/Program.cs`
- Classe: `Program` (top-level statements)
- Porta padrão: `http://localhost:5000` ou configurado via `ASPNETCORE_URLS`

**MCP Servers (TypeScript):**
- `mcp-ts/servers/web.search/src/index.ts` - Porta 8081
- `mcp-ts/servers/http.request/src/index.ts` - Porta 8082

**UI (Next.js):**
- Arquivo: `ui/app/page.tsx`
- Porta padrão: `http://localhost:3000`

### Passos de Configuração e Execução Local

#### Opção 1: Desenvolvimento Local (Recomendado)

```bash
# 1. Clonar o repositório
git clone https://github.com/invictvs-k/metacore-stack.git
cd metacore-stack

# 2. Instalar todas as dependências
make bootstrap
# Isso instala: pnpm, dependências Node.js, restaura pacotes .NET

# 3. (Opcional) Validar schemas
make schemas-validate

# 4. Subir MCP servers em background
make mcp-up
# Isso inicia:
# - web.search em ws://localhost:8081
# - http.request em ws://localhost:8082

# 5. Rodar o Room Host (.NET)
make run-server
# Servidor estará em http://localhost:5000
# SignalR Hub em ws://localhost:5000/room

# 6. (Opcional) Rodar UI em outro terminal
cd ui
pnpm dev
# UI estará em http://localhost:3000
```

#### Opção 2: Docker Compose

```bash
# 1. Clonar e navegar
git clone https://github.com/invictvs-k/metacore-stack.git
cd metacore-stack

# 2. Subir todos os serviços
make compose-up
# OU
cd infra && docker compose up -d

# Serviços disponíveis:
# - Room Host: http://localhost:8080
# - MCP web.search: ws://localhost:8081
```

### Verificação da Instalação

```bash
# Testar Room Host
curl http://localhost:5000
# Resposta: "RoomServer alive"

# Testar health check
curl http://localhost:5000/health
# Resposta: "Healthy"

# Listar entidades (vazio se nenhuma sala ativa)
# Conectar via SignalR client e chamar ListEntities
```

### Comandos Make Úteis

```bash
make bootstrap         # Instalar dependências
make build            # Compilar tudo (.NET + TypeScript)
make test             # Rodar testes
make lint             # Rodar linters
make format           # Formatar código
make run-server       # Rodar Room Host
make mcp-up           # Subir MCP servers
make schemas-validate # Validar JSON Schemas
make compose-up       # Docker Compose up
```

---

## 6. Testes e Qualidade de Código

### Estratégia de Testes

#### Testes .NET (xUnit)

**Localização:** `server-dotnet/tests/RoomServer.Tests/`

**Tipos de teste:**
- ✅ **Testes de Integração:** Usando `WebApplicationFactory` para testar SignalR Hub end-to-end
- ✅ **Testes de Unidade:** Para serviços isolados (SessionStore, PermissionService, etc.)
- ✅ **Smoke Tests:** Verificações básicas de funcionalidade

**Arquivos de teste:**
- `RoomHub_SmokeTests.cs` - Testes básicos de Join/Leave/SendToRoom
- `McpBridge_SmokeTests.cs` - Testes de integração MCP
- `SecurityTests.cs` - Testes de permissões e políticas
- `FileArtifactStoreTests.cs` - Testes de armazenamento de artefatos
- `CommandTargetResolutionTests.cs` - Testes de parsing de comandos

**Como rodar:**
```bash
# Rodar todos os testes
make test
# OU
cd server-dotnet/tests/RoomServer.Tests
dotnet test -c Debug

# Rodar testes específicos
dotnet test --filter "FullyQualifiedName~McpBridge"
```

**Cobertura atual:** ~28 testes (alguns falhando - issues conhecidos)

#### Validação de Schemas (AJV)

**Localização:** `schemas/`

**Estratégia:**
- Validação de JSON Schemas usando AJV
- Exemplos positivos e negativos
- Validação automática no CI

**Como rodar:**
```bash
cd schemas
pnpm validate
```

**Saída esperada:**
```
✅ examples/room-min.json ok
✅ examples/entity-human.json ok
✅ examples/message-command.json ok
✅ examples/artifact-sample.json ok
✅ invalid example rejected: examples/invalid/artifact-bad-hash.json
✅ invalid example rejected: examples/invalid/message-missing-fields.json
```

#### Testes MCP TypeScript

**Status:** 🚧 Não implementado ainda (build atual com erros de TypeScript)

### Ferramentas de Qualidade

#### .NET

**Linter/Formatter:**
- `dotnet format` - Formatador de código .NET
- Configuração via `.editorconfig`

**Como rodar:**
```bash
make format
# OU
cd server-dotnet && dotnet format
```

**Análise Estática:**
- Microsoft.CodeAnalysis.NetAnalyzers (incluído no .NET SDK)
- Avisos de nullable reference types habilitados

#### TypeScript

**Linter:**
- ESLint com configuração `.eslintrc.json`

**Formatter:**
- Prettier

**Como rodar:**
```bash
make lint
# OU
cd mcp-ts && pnpm -r -F "*" lint

make format
# OU
cd mcp-ts && pnpm -r -F "*" format
```

**Compilação TypeScript:**
```bash
make build
# OU
cd mcp-ts && pnpm -r -F "*" build
```

### CI/CD (GitHub Actions)

**Workflows:** `.github/workflows/ci.yml`

**Jobs:**
1. **schemas** - Valida JSON Schemas
2. **dotnet** - Build e test do .NET
3. **mcp-ts** - Build dos MCP servers TypeScript

**Quando roda:**
- Push para `main`
- Pull Requests

**Comandos executados:**
```bash
# Job: schemas
pnpm i && pnpm validate

# Job: dotnet
dotnet restore
dotnet build -c Release
dotnet test -c Release

# Job: mcp-ts
pnpm i && pnpm -r -F "*" build
```

---

## 7. Conceitos Avançados

### 7.1 Ports e Capabilities

**O que são:** Contratos padronizados de funcionalidades que uma entidade oferece.

**Exemplos:**
- `text.generate` - Geração de texto
- `review` - Revisão de conteúdo
- `plan` - Planejamento de tarefas
- `search.web` - Busca na web (via MCP)

**Como usar:** Entidades declaram suas capabilities no campo `capabilities: string[]`

### 7.2 Orquestradores e Tasks

**Status:** 🚧 Conceito definido, implementação em desenvolvimento

**Ideia:** Entidades especiais que executam scripts JSON para coordenar fluxos de trabalho.

**Exemplo conceitual:**
```json
{
  "name": "Refinar Documento",
  "steps": [
    {"task": "gerar_texto", "target": "E-AGENT-1", "port": "text.generate"},
    {"task": "revisar", "target": "E-HUMAN-1", "port": "review", "checkpoint": true}
  ]
}
```

### 7.3 Versionamento de Artefatos

- Artefatos são identificados por **SHA256 hash**
- Se o conteúdo muda, um novo hash é gerado
- Manifests mantêm histórico de versões
- **Deduplica automaticamente:** Mesmo conteúdo = mesmo arquivo

### 7.4 Extensibilidade via MCP

**Como adicionar um novo MCP server:**

1. Criar novo diretório em `mcp-ts/servers/meu-server/`
2. Implementar protocolo JSON-RPC 2.0:
   - Método `tools/list` - Retorna ferramentas disponíveis
   - Método `tool/call` - Executa ferramenta
3. Adicionar configuração em `appsettings.json`:
   ```json
   {"id": "meu-server@local", "url": "ws://localhost:8083", "visibility": "room"}
   ```
4. Reiniciar Room Host

---

## 8. Próximos Passos Sugeridos

### Para Começar a Contribuir:

1. **Leia os documentos:**
   - ✅ `CONCEPTDEFINITION.md` - Entender a visão completa
   - ✅ `ONBOARDING.md` - Este documento
   - ✅ `CONTRIBUTING.md` - Convenções de código
   - ✅ `server-dotnet/src/RoomServer/Services/Mcp/README.md` - Integração MCP

2. **Configure o ambiente:**
   ```bash
   make bootstrap
   make build
   make test
   ```

3. **Explore o código:**
   - Comece por `Program.cs` - Entry point
   - Leia `RoomHub.cs` - Lógica principal SignalR
   - Analise `EntitySpec.cs` e `MessageModel.cs` - Modelos core

4. **Rode um exemplo:**
   ```bash
   make mcp-up        # Terminal 1
   make run-server    # Terminal 2
   # Conecte um cliente SignalR (curl, Postman, ou UI)
   ```

5. **Contribua:**
   - Pegue uma issue no GitHub
   - Siga Conventional Commits (`feat:`, `fix:`, `chore:`)
   - Garanta que CI passa (build, lint, test, schemas)

### Áreas para Contribuição:

- 🚧 **UI:** Implementar interface web completa em Next.js
- 🚧 **MCP Servers:** Criar novos servers (GitHub, Database, etc.)
- 🚧 **Orquestradores:** Implementar execução de task scripts
- 🚧 **Persistência:** Adicionar logging em `events.jsonl`
- 🚧 **Observabilidade:** Integrar OpenTelemetry
- 🚧 **Testes:** Aumentar cobertura de testes
- 🚧 **Documentação:** Exemplos de uso, tutoriais

---

## 9. Recursos e Referências

### Documentação Interna
- [CONCEPTDEFINITION.md](./CONCEPTDEFINITION.md) - Especificação funcional completa
- [README.md](./README.md) - Quickstart
- [CONTRIBUTING.md](./CONTRIBUTING.md) - Guia de contribuição
- [MCP Bridge README](./server-dotnet/src/RoomServer/Services/Mcp/README.md) - Documentação MCP

### Tecnologias
- [SignalR](https://docs.microsoft.com/aspnet/core/signalr) - Comunicação em tempo real
- [JSON-RPC 2.0](https://www.jsonrpc.org/specification) - Protocolo de comunicação
- [JSON Schema](https://json-schema.org/) - Validação de contratos
- [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) - Protocolo de contexto
- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) - Framework .NET
- [Next.js](https://nextjs.org/) - Framework React
- [pnpm](https://pnpm.io/) - Gerenciador de pacotes Node.js

### Convenções
- [Conventional Commits](https://www.conventionalcommits.org/) - Formato de commits
- [Semantic Versioning](https://semver.org/) - Versionamento de schemas

---

## 10. FAQ - Perguntas Frequentes

### Q: Como faço para criar uma nova sala?
**A:** Atualmente não há API REST para criar salas. Salas são criadas implicitamente quando a primeira entidade faz `Join(roomId, entity)`. O `roomId` é uma string livre (ex: `"room-123"`, `"projeto-ai"`, etc.).

### Q: Preciso de autenticação para usar o sistema?
**A:** Depende da configuração. Para entidades com visibilidade `"owner"`, é necessário fornecer `owner_user_id` e um header `X-User-Id` ou claim JWT `sub`. Para visibilidade `"public"` ou `"team"`, não é obrigatório.

### Q: Como adiciono um novo tipo de mensagem?
**A:** 
1. Adicione o tipo em `schemas/message.schema.json`
2. Atualize a validação se necessário
3. Implemente tratamento em `RoomHub.cs` ou no cliente
4. Documente o novo tipo

### Q: Os MCP servers precisam estar sempre rodando?
**A:** Sim, para usar recursos MCP. O Room Host tenta reconectar automaticamente se a conexão cair. Você pode desabilitar MCP servers removendo-os de `McpServers` em `appsettings.json`.

### Q: Como debugar problemas de conexão SignalR?
**A:** 
1. Habilite logging detalhado em `appsettings.Development.json`:
   ```json
   {"Logging": {"LogLevel": {"Microsoft.AspNetCore.SignalR": "Debug"}}}
   ```
2. Use ferramentas como [SignalR Client Tools](https://github.com/aspnet/SignalR-Client-Cpp)
3. Verifique network tab no navegador (WebSocket connection)

### Q: Posso usar Python para criar um agente?
**A:** Sim! Qualquer linguagem que suporte **SignalR client** pode conectar. Há bibliotecas Python como `signalrcore`. O agente Python apenas precisa:
1. Conectar ao Hub em `ws://localhost:5000/room`
2. Chamar `Join(roomId, entity)` com seu EntitySpec
3. Escutar eventos e enviar mensagens via `SendToRoom`

### Q: Como contribuo com um novo MCP server?
**A:** 
1. Crie diretório em `mcp-ts/servers/meu-server/`
2. Implemente `tools/list` e `tool/call` em JSON-RPC 2.0
3. Adicione `package.json` com scripts `dev`, `build`, `lint`
4. Adicione configuração em `appsettings.json`
5. Documente o servidor em README
6. Abra PR seguindo Conventional Commits

---

## 📞 Contato e Suporte

- **Issues:** [GitHub Issues](https://github.com/invictvs-k/metacore-stack/issues)
- **Discussões:** [GitHub Discussions](https://github.com/invictvs-k/metacore-stack/discussions)
- **Código de Conduta:** [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md)

---

**Bem-vindo à equipe! Boa sorte e divirta-se construindo o futuro da colaboração humano+IA! 🚀🤖**
