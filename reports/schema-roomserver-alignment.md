# RoomServer vs. Schemas Canônicos — Relatório de Alinhamento

## ✅ room.schema.json

#### Schema Atual:
- Estados definidos: `init`, `active`, `paused`, `ended` via `RoomState` em `common.defs.json` e requisito obrigatório de `state`. 【F:schemas/common.defs.json†L10-L55】【F:schemas/room.schema.json†L8-L74】
- Propriedades obrigatórias: `id`, `state`, `config`, `created_at`. 【F:schemas/room.schema.json†L8-L74】
- Propriedades opcionais destacadas: `updated_at`, `contracts`, `x-extensions`, `config.mounts`, `config.secrets`. 【F:schemas/room.schema.json†L8-L74】
- Referências externas: `RoomId`, `RoomState`, `IsoDateTime`, `KeyValue`, `Policy`, `MimeOrLogicalType`, `Ext` de `common.defs.json`. 【F:schemas/room.schema.json†L8-L74】【F:schemas/common.defs.json†L7-L65】

#### Implementação em .NET:
- Classe principal: **Não há agregador `Room`**; o modelo exposto é apenas o enum `RoomState`. 【F:server-dotnet/src/RoomServer/Models/RoomState.cs†L1-L9】【c05bb2†L1-L2】
- Estados implementados: enum `RoomState` declara `Init`, `Active`, `Paused`, `Ended`, mas não há armazenamento de estado por sala. 【F:server-dotnet/src/RoomServer/Models/RoomState.cs†L1-L9】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L293-L297】
- Métodos de transição: inexistentes; `RoomHub` apenas publica instantâneo de entidades conectadas quando eventos ocorrem. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L40-L151】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L293-L297】
- Propriedades mapeadas: apenas presença/entidades via `SessionStore`; não há suporte para `config`, `contracts`, `created_at/updated_at`. 【F:server-dotnet/src/RoomServer/Services/SessionStore.cs†L8-L60】【F:server-dotnet/src/RoomServer/Program.cs†L7-L27】

#### Alinhamento:
- ❌ Campo `config`: não há equivalente em memória nem persistência. 【F:schemas/room.schema.json†L16-L71】【F:server-dotnet/src/RoomServer/Program.cs†L7-L27】
- ❌ Campo `contracts`: ausente no modelo/serviços. 【F:schemas/room.schema.json†L40-L70】【c05bb2†L1-L2】
- ❌ Campos `created_at` / `updated_at`: não registrados nem emitidos. 【F:schemas/room.schema.json†L8-L74】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L40-L151】
- ⚠️ Campo `state`: enum disponível, porém não há transições explícitas nem armazenamento; apenas eventos `ROOM.STATE`. 【F:server-dotnet/src/RoomServer/Models/RoomState.cs†L1-L9】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L293-L297】
- ⚠️ Responsabilidades globais: sala propaga mensagens e eventos via `RoomHub`, mas não controla ciclo de vida, recursos ou políticas declaradas. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】【F:CONCEPTDEFINITION.md†L20-L64】

#### Gaps Identificados:
- **Gap 1 — Ciclo de vida de sala inexistente (HIGH)**: impossibilita cumprir requisito `init → active → paused → ended`, comprometendo governança e automação. 【F:CONCEPTDEFINITION.md†L20-L47】【F:server-dotnet/src/RoomServer/Models/RoomState.cs†L1-L9】
- **Gap 2 — Configuração e políticas de sala (HIGH)**: sem `config`/`contracts`, não há forma de montar recursos, definir entradas/saídas nem aplicar políticas compartilhadas. 【F:schemas/room.schema.json†L16-L71】【F:CONCEPTDEFINITION.md†L20-L64】
- **Gap 3 — Telemetria resumida ausente (MEDIUM)**: nenhum `events.jsonl`/`room-run.json` é produzido; apenas eventos em tempo real. 【F:CONCEPTDEFINITION.md†L370-L399】【F:server-dotnet/src/RoomServer/Program.cs†L7-L27】

---

## ✅ entity.schema.json

#### Schema Atual:
- Tipos de entidade: enum `human`, `agent`, `npc`, `orchestrator`. 【F:schemas/common.defs.json†L27-L55】【F:schemas/entity.schema.json†L8-L26】
- Estrutura de capacidades: array de `PortId` com zero ou mais itens. 【F:schemas/entity.schema.json†L17-L23】
- Estrutura de políticas: referência `Policy` com `allow_commands_from`, `sandbox_mode`, `env_whitelist`, `scopes`, `rateLimit`. 【F:schemas/entity.schema.json†L17-L23】【F:schemas/common.defs.json†L30-L44】
- Workspace: implícito na especificação funcional (cada entidade possui espaço privado). 【F:CONCEPTDEFINITION.md†L109-L154】
- ID Generation: `EntityId` deve seguir padrão `E-*`. 【F:schemas/common.defs.json†L10-L55】

#### Implementação em .NET:
- Classe principal: `RoomServer.Models.EntitySpec`. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L1-L14】
- Tipos implementados: `Kind` é string livre; validação ocorre apenas nas políticas (nenhum enum nativo). 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L270-L291】
- Tipos faltando: não há enum/constante que restrinja `Kind`, logo valores fora do schema podem passar. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】
- Gerenciamento de capacidades: apenas array simples, sem helpers para adicionar/remover; validação garante `Array.Empty<string>()`. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L12-L13】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L256-L266】
- Suporte a workspace: controles feitos no `PermissionService`, que limita acesso por entidade. 【F:server-dotnet/src/RoomServer/Services/PermissionService.cs†L25-L50】【F:CONCEPTDEFINITION.md†L109-L154】

#### Alinhamento:
- ⚠️ Tipo `human`: aceito, porém sem validação formal. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】
- ⚠️ Tipo `agent`: aceito, sem enum. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】
- ⚠️ Tipo `npc`: aceito, sem enum. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】
- ⚠️ Tipo `orchestrator`: aceito, sem enum; políticas especiais tratadas em `PermissionService`. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】【F:server-dotnet/src/RoomServer/Services/PermissionService.cs†L52-L63】
- ⚠️ Capacidades/Ports: estrutura disponível, mas sem validação de padrão `port.id` ou manipulação dedicada. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L12-L13】
- ⚠️ Workspace: controle de acesso existe, mas não há manifesto por entidade nem diretório lógico fora do store de artefatos. 【F:server-dotnet/src/RoomServer/Services/PermissionService.cs†L25-L35】【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L274-L303】
- ❌ Políticas completas: `PolicySpec` ignora `scopes` e `rateLimit`; `sandbox_mode` é `SandboxMode` (camelCase divergente). 【F:server-dotnet/src/RoomServer/Models/PolicySpec.cs†L5-L10】【F:schemas/common.defs.json†L30-L44】
- ❌ `x-extensions`: não suportado. 【F:schemas/entity.schema.json†L17-L24】【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】

#### Gaps Identificados:
- **Gap 1 — Validação de tipos (MEDIUM)**: ausência de enum causa risco de entidades inválidas. 【F:schemas/common.defs.json†L27-L33】【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】
- **Gap 2 — Política incompleta (HIGH)**: sem `scopes`/`rateLimit`, governança definida no schema não é aplicada. 【F:schemas/common.defs.json†L30-L44】【F:server-dotnet/src/RoomServer/Models/PolicySpec.cs†L5-L10】
- **Gap 3 — Workspace próprio (MEDIUM)**: não há representação explícita do workspace em `EntitySpec`; apenas validação de acesso. 【F:CONCEPTDEFINITION.md†L109-L154】【F:server-dotnet/src/RoomServer/Services/PermissionService.cs†L25-L35】

---

## ✅ message.schema.json

#### Schema Atual:
- Tipos de mensagem: `chat`, `command`, `event`, `artifact`. 【F:schemas/message.schema.json†L7-L118】
- Estrutura de payload base: `id`, `roomId`, `channel`, `from`, `to`, `type`, `ts`, `correlationId`, `payload`. 【F:schemas/message.schema.json†L14-L29】
- Estrutura de payload por tipo:
  - `chat.payload.text` obrigatório.
  - `command.payload` com `target`, `port`, `inputs`, `policies`.
  - `event.payload.kind` em `SCREAMING.CASE`.
  - `artifact.payload.manifest` referencia `artifact-manifest.schema.json`. 【F:schemas/message.schema.json†L31-L118】
- Metadados de origem: `from`, `to`, `channel`, `roomId`. 【F:schemas/message.schema.json†L14-L29】
- ID Generation: ULID. 【F:schemas/message.schema.json†L14-L24】【F:schemas/common.defs.json†L7-L15】

#### Implementação em .NET:
- Classe principal: `RoomServer.Models.MessageModel`. 【F:server-dotnet/src/RoomServer/Models/MessageModel.cs†L1-L15】
- Tipos de mensagem implementados: livre (`Type` string). `RoomHub` trata `command`, `chat` (default) e `artifact` via publisher; eventos são enviados separadamente. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】【F:server-dotnet/src/RoomServer/Services/RoomEventPublisher.cs†L20-L52】
- Tipos faltando: nenhum tipo bloqueado, porém ausência de validação específica (qualquer `Type` é aceito). 【F:server-dotnet/src/RoomServer/Models/MessageModel.cs†L11-L14】
- Hub SignalR methods: `Join`, `Leave`, `SendToRoom`, `ListEntities`. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L54-L158】
- Armazenamento de histórico: inexistente; mensagens são apenas broadcast. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】
- Eventos disparados: `ENTITY.JOIN`, `ENTITY.LEAVE`, `ROOM.STATE` e `ARTIFACT.*`. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L93-L118】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L293-L297】【F:server-dotnet/src/RoomServer/Services/RoomEventPublisher.cs†L20-L52】【F:server-dotnet/src/RoomServer/Controllers/ArtifactEndpoints.cs†L212-L275】

#### Alinhamento:
- ⚠️ Tipo `chat`: payload tratado implicitamente (qualquer objeto). Falta validação de `text`. 【F:server-dotnet/src/RoomServer/Models/MessageModel.cs†L11-L14】【F:schemas/message.schema.json†L31-L50】
- ⚠️ Tipo `command`: Hub valida `target` e permissões, mas payload permanece dinamicamente tipado. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L181-L204】【F:schemas/message.schema.json†L52-L74】
- ⚠️ Tipo `event`: mensagens de evento são publicadas separadamente (`RoomEventPublisher`), porém payload não segue `Base` (usa objeto anônimo). 【F:server-dotnet/src/RoomServer/Services/RoomEventPublisher.cs†L20-L33】【F:schemas/message.schema.json†L14-L96】
- ⚠️ Tipo `artifact`: publicado conforme schema, mas sem validação do manifesto recebido. 【F:server-dotnet/src/RoomServer/Services/RoomEventPublisher.cs†L35-L52】【F:schemas/message.schema.json†L98-L118】
- ❌ Histórico de mensagens: nenhuma persistência ou replay. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】【F:CONCEPTDEFINITION.md†L20-L64】
- ❌ Campos `to` / `correlationId`: ignorados no modelo. 【F:server-dotnet/src/RoomServer/Models/MessageModel.cs†L6-L15】【F:schemas/message.schema.json†L14-L29】

#### Gaps Identificados:
- **Gap 1 — Validação de payload (MEDIUM)**: ausência de contratos fortes pode quebrar interoperabilidade entre clientes. 【F:schemas/message.schema.json†L31-L118】【F:server-dotnet/src/RoomServer/Models/MessageModel.cs†L6-L15】
- **Gap 2 — Telemetria/Historico (HIGH)**: sem armazenamento, requisitos de rastreabilidade não são cumpridos. 【F:CONCEPTDEFINITION.md†L370-L399】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】
- **Gap 3 — Metadados de roteamento (MEDIUM)**: campos `to`/`correlationId` do schema não são expostos, dificultando tracing e mensagens direcionadas. 【F:schemas/message.schema.json†L14-L29】【F:server-dotnet/src/RoomServer/Models/MessageModel.cs†L6-L15】

---

## ✅ artifact-manifest.schema.json

#### Schema Atual:
- Estrutura de versionamento: campo `version` inteiro (default 1) e lista `parents`. 【F:schemas/artifact-manifest.schema.json†L7-L24】
- Rastreamento de mudanças: `parents` aceita hashes ancestrais; `metadata` flexível. 【F:schemas/artifact-manifest.schema.json†L16-L24】
- Metadados de origem: `origin` com `room`, `entity`, `port`, `workspace`, `channel`. 【F:schemas/artifact-manifest.schema.json†L8-L20】【F:schemas/common.defs.json†L45-L55】
- Suporte a ACL: não explícito no schema; políticas ficam em `origin.workspace` + policies externas. 【F:schemas/artifact-manifest.schema.json†L7-L24】
- Hash/Checksum: `sha256`. 【F:schemas/artifact-manifest.schema.json†L12-L20】

#### Implementação em .NET:
- Classe principal: `RoomServer.Services.ArtifactStore.ArtifactManifest`. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L49-L61】
- Versionamento: `FileArtifactStore.WriteAsync` incrementa versão e mantém histórico em `manifest.json`. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L30-L125】
- Histórico: `manifest.json` agrega todas as versões; `ListAsync` filtra e ordena. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L144-L209】
- Eventos: `ARTIFACT.ADDED` / `ARTIFACT.UPDATED` e mensagens `artifact` publicados após gravação/promoção. 【F:server-dotnet/src/RoomServer/Controllers/ArtifactEndpoints.cs†L270-L275】
- Armazenamento: diretórios separados por workspace (sala vs entidade) sob `.ai-flow/runs/{room}`. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L274-L303】

#### Alinhamento:
- ✅ Versionamento: implementado com incremento automático e promoção preservando histórico. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L30-L272】
- ⚠️ Histórico de mudanças: versões antigas persistem, porém `ListAsync` retorna apenas último por entidade; não há diff/delta. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L144-L209】
- ⚠️ Provenance/Owner: `Origin` inclui `room`, `entity`, `workspace`, `port`, mas não `channel`. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L63-L69】【F:schemas/common.defs.json†L45-L55】
- ⚠️ ACL/Permissões: controle feito via `PermissionService` antes da chamada; manifesto não registra políticas. 【F:server-dotnet/src/RoomServer/Services/PermissionService.cs†L25-L50】【F:schemas/artifact-manifest.schema.json†L7-L24】
- ✅ Eventos ARTIFACT.*: enviados após upload/promoção. 【F:server-dotnet/src/RoomServer/Controllers/ArtifactEndpoints.cs†L270-L275】
- ❌ `metadata` flexível: implementação restringe a `Dictionary<string,string>`, perdendo suporte a objetos arbitrários. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L49-L61】【F:schemas/artifact-manifest.schema.json†L16-L24】
- ❌ `x-extensions`: ausente. 【F:schemas/artifact-manifest.schema.json†L20-L24】【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L49-L61】

#### Gaps Identificados:
- **Gap 1 — `Origin.channel` ausente (MEDIUM)**: dificulta rastrear canal/membro que originou o artefato. 【F:schemas/common.defs.json†L45-L55】【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L63-L69】
- **Gap 2 — Metadata limitada (MEDIUM)**: dicionário string→string não preserva objetos complexos exigidos pelo schema. 【F:schemas/artifact-manifest.schema.json†L16-L24】【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L49-L61】
- **Gap 3 — Auditoria integrada (MEDIUM)**: falta associação a políticas/ACL explícitas no manifesto. 【F:CONCEPTDEFINITION.md†L109-L154】【F:server-dotnet/src/RoomServer/Services/PermissionService.cs†L25-L35】

---

## ✅ common.defs.json

#### Tipos Compartilhados:
- RoomId: `room-[A-Za-z0-9_-]{6,}`. 【F:schemas/common.defs.json†L10-L15】
- EntityId: `E-[A-Za-z0-9_-]{2,64}`. 【F:schemas/common.defs.json†L10-L15】
- PortId: `^[a-z][a-z0-9]*(\.[a-z0-9]+)*$`. 【F:schemas/common.defs.json†L16-L21】
- Origin: exige `room`, `entity`, opcional `port`, `workspace`, `channel`. 【F:schemas/common.defs.json†L45-L55】

#### Versionamento:
- Estratégia: metadata semântica `semver` dentro de cada schema (`1.0.0`). 【F:schemas/room.schema.json†L4-L6】【F:schemas/common.defs.json†L1-L6】
- Versão atual: `1.0.0`. 【F:schemas/common.defs.json†L1-L6】
- $id pattern: arquivos locais (`*.schema.json`, `common.defs.json`). 【F:schemas/room.schema.json†L2-L6】【F:schemas/common.defs.json†L1-L6】

#### Referências cruzadas:
- `room.schema.json` referencia `RoomId`, `RoomState`, `Policy`, `KeyValue`, `MimeOrLogicalType`, `Ext`. 【F:schemas/room.schema.json†L8-L74】【F:schemas/common.defs.json†L7-L65】
- `entity.schema.json` referencia `EntityId`, `EntityKind`, `PortId`, `Visibility`, `Policy`, `Ext`. 【F:schemas/entity.schema.json†L8-L26】【F:schemas/common.defs.json†L7-L65】
- `message.schema.json` referencia `Ulid`, `RoomId`, `Channel`, `EntityId`, `PortId`, `Policy`. 【F:schemas/message.schema.json†L14-L112】【F:schemas/common.defs.json†L7-L55】
- `artifact-manifest.schema.json` referencia `MimeOrLogicalType`, `Sha256`, `Origin`, `Ext`. 【F:schemas/artifact-manifest.schema.json†L7-L24】【F:schemas/common.defs.json†L16-L65】

#### Observações de Implementação:
- IDs: servidor não valida padrões `RoomId`/`EntityId`; apenas strings livres. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L270-L291】
- Policies: implementação cobre `allow_commands_from`, `sandbox_mode`, `env_whitelist`, mas ignora `scopes` e `rateLimit`. 【F:server-dotnet/src/RoomServer/Models/PolicySpec.cs†L5-L10】【F:schemas/common.defs.json†L30-L44】
- Origin: classe `Origin` não inclui `channel`. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L63-L69】【F:schemas/common.defs.json†L45-L55】

---

# Relatório Consolidado

## Resumo Executivo

- **Porcentagem de Alinhamento**: ~45% (componentes críticos parcialmente atendidos; faltam lifecycle, políticas completas e telemetria).
- **Schemas Analisados**: 5 (room, entity, message, artifact, common).
- **Data da Análise**: 2025-10-18. 【717f26†L1-L2】

### Críticos Faltando (HIGH IMPACT)
- Ausência de ciclo de vida e agregador `Room` impede estados `init/active/paused/ended` e políticas globais. 【F:CONCEPTDEFINITION.md†L20-L47】【F:server-dotnet/src/RoomServer/Models/RoomState.cs†L1-L9】
- Telemetria/histórico central (events.jsonl, room-run.json) não existe, inviabilizando auditoria exigida. 【F:CONCEPTDEFINITION.md†L370-L399】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】

### Ajustes Necessários (MEDIUM IMPACT)
- Completar `PolicySpec` com `scopes`, `rateLimit` e normalizar nomes (`sandbox_mode`). 【F:schemas/common.defs.json†L30-L44】【F:server-dotnet/src/RoomServer/Models/PolicySpec.cs†L5-L10】
- Incluir `Origin.channel` e metadados ricos (`metadata` objeto) nos manifests. 【F:schemas/common.defs.json†L45-L55】【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L49-L69】

### Melhorias Futuras (LOW IMPACT)
- Expor `x-extensions` nos modelos para extensibilidade. 【F:schemas/room.schema.json†L68-L71】【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】
- Implementar validações de formato (`RoomId`, `PortId`) no hub/serviços para feedback antecipado. 【F:schemas/common.defs.json†L10-L21】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L270-L291】

---

## Matriz de Implementação

| Conceito | Schema | Implementado? | % Cobertura | Observações |
|----------|--------|---------------|-------------|-------------|
| Room Lifecycle | room.schema.json | ❌ | 10% | Enum existe, mas não há estado persistido nem transições. 【F:server-dotnet/src/RoomServer/Models/RoomState.cs†L1-L9】 |
| Room State Management | room.schema.json | ⚠️ | 30% | `SessionStore`/`ROOM.STATE` listam entidades, porém sem `config`/políticas. 【F:server-dotnet/src/RoomServer/Services/SessionStore.cs†L8-L60】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L293-L297】 |
| Entity Types | entity.schema.json | ⚠️ | 70% | Estrutura presente, falta enum/validação e `x-extensions`. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】 |
| Entity Capabilities | entity.schema.json | ⚠️ | 60% | Array suportado, porém sem contratos/ports normalizados. 【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L12-L13】 |
| Entity Policies | entity.schema.json | ⚠️ | 40% | Só `allow_commands_from`, `SandboxMode`, `EnvWhitelist`. 【F:server-dotnet/src/RoomServer/Models/PolicySpec.cs†L5-L10】 |
| Entity Workspace | entity.schema.json | ⚠️ | 35% | Permissões mínimas, sem workspace manifest. 【F:server-dotnet/src/RoomServer/Services/PermissionService.cs†L25-L35】 |
| Message Types | message.schema.json | ⚠️ | 60% | Broadcast cobre chat/command/event/artifact sem validação específica. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】 |
| Message History | message.schema.json | ❌ | 0% | Nenhuma persistência ou replay. 【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】 |
| Artifact Versioning | artifact-manifest.schema.json | ✅ | 80% | Versionamento + promoção atendidos. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L30-L272】 |
| Artifact History | artifact-manifest.schema.json | ⚠️ | 60% | Histórico guardado, mas API expõe apenas últimos itens. 【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L144-L209】 |
| Artifact Events | artifact-manifest.schema.json | ✅ | 80% | Eventos `ARTIFACT.*` emitidos. 【F:server-dotnet/src/RoomServer/Controllers/ArtifactEndpoints.cs†L270-L275】 |
| Common Definitions | common.defs.json | ⚠️ | 50% | Tipos definidos, porém não validados/consumidos totalmente. 【F:schemas/common.defs.json†L7-L65】【F:server-dotnet/src/RoomServer/Models/PolicySpec.cs†L5-L10】 |

---

## Recomendações Prioritizadas

### P0 (BLOCKER) - Corrigir ANTES de qualquer outra coisa
- [ ] Implementar agregado `Room` com ciclo de vida, timestamps e `config` persistida; expor endpoints/hub para transições. 【F:schemas/room.schema.json†L8-L74】【F:CONCEPTDEFINITION.md†L20-L47】
- [ ] Adicionar pipeline de telemetria (persistir `events.jsonl`/`room-run.json` e emitir OpenTelemetry opcional). 【F:CONCEPTDEFINITION.md†L370-L399】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L121-L204】

### P1 (HIGH) - Incluir no próximo sprint
- [ ] Expandir `PolicySpec` para cobrir `scopes`, `rateLimit` e alinhar naming com schema (`allow_commands_from`, `sandbox_mode`). 【F:schemas/common.defs.json†L30-L44】【F:server-dotnet/src/RoomServer/Models/PolicySpec.cs†L5-L10】
- [ ] Normalizar mensagens (`MessageModel`) para incluir `to`, `correlationId` e validações específicas por tipo. 【F:schemas/message.schema.json†L14-L118】【F:server-dotnet/src/RoomServer/Models/MessageModel.cs†L6-L15】

### P2 (MEDIUM) - Planejar para futuro
- [ ] Enriquecer `ArtifactManifest` (`Origin.channel`, metadata arbitrária, ACL opcional). 【F:schemas/common.defs.json†L45-L55】【F:server-dotnet/src/RoomServer/Services/ArtifactStore/ArtifactModels.cs†L49-L69】
- [ ] Formalizar validação de IDs (`RoomId`, `EntityId`, `PortId`) no Hub/REST para reforçar contratos. 【F:schemas/common.defs.json†L10-L21】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L270-L291】

### P3 (LOW) - Nice to have / Futuro
- [ ] Introduzir suporte a `x-extensions` nos DTOs para extensibilidade sem breaking changes. 【F:schemas/room.schema.json†L68-L71】【F:server-dotnet/src/RoomServer/Models/EntitySpec.cs†L7-L13】
- [ ] Disponibilizar APIs para listar histórico completo de artefatos (todas as versões + diffs). 【F:schemas/artifact-manifest.schema.json†L16-L24】【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L144-L209】

---

## Conclusão
O RoomServer cobre mensageria em tempo real e gestão básica de artefatos conforme os schemas, mas ainda falta um agregador de Sala que implemente o ciclo de vida completo, políticas compartilhadas e telemetria exigidos pela especificação funcional. Priorizar o modelo `Room` com persistência de estado/configuração e completar políticas/telemetria elevará o alinhamento para os 80%+ requeridos, habilitando governança e auditoria de ponta a ponta. 【F:CONCEPTDEFINITION.md†L20-L64】【F:CONCEPTDEFINITION.md†L370-L399】【F:server-dotnet/src/RoomServer/Hubs/RoomHub.cs†L40-L204】【F:server-dotnet/src/RoomServer/Services/ArtifactStore/FileArtifactStore.cs†L30-L303】
