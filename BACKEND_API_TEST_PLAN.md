# Plano de Testes Detalhado - Backend/APIs do Metacore Stack

## Sumário Executivo

Este documento fornece um plano de testes completo e detalhado para as funcionalidades principais de Backend/APIs do RoomServer, baseado na lista de verificação de implementação do Room Host. O plano está organizado em 10 seções principais, cobrindo desde os fundamentos de execução até observabilidade e tratamento de erros.

**Legenda:**
- ✅ Funcionalidade completamente implementada
- ⚠️ Área de atenção especial ou maior complexidade
- 🔴 Item crítico que requer validação cuidadosa

---

## 0) Fundamentos de Projeto/Execução (Pré-flight) ✅

### 0.1 Verificação do Endpoint do Hub SignalR

**Objetivo do Teste:**
Validar que o Hub SignalR está corretamente mapeado no endpoint `/room` e aceita conexões WebSocket.

**Passos de Execução:**
1. Iniciar o RoomServer:
   ```bash
   cd server-dotnet
   dotnet run --project src/RoomServer/RoomServer.csproj
   ```
2. Usar uma ferramenta de teste SignalR (ex: Postman, ou cliente SignalR):
   ```bash
   # Verificar resposta de negociação SignalR (endpoint recomendado para validação)
   curl -X POST "http://localhost:5000/room/negotiate?negotiateVersion=1" -H "Accept: application/json"
   ```

**Resultados Esperados:**
- Servidor iniciado sem erros na porta 5000 (HTTP) ou 5001 (HTTPS)
- Logs do console mostram: "Now listening on: http://localhost:5000"
- Endpoint `/room/negotiate` retorna código HTTP 200
- Resposta JSON contém campos: `connectionId`, `availableTransports` (incluindo WebSocket)
- Exemplo de resposta:
  ```json
  {
    "connectionId": "abc123...",
    "availableTransports": [
      {
        "transport": "WebSockets",
        "transferFormats": ["Text", "Binary"]
      }
    ],
    "negotiateVersion": 1
  }
  ```

**Considerações Adicionais:**
- Verificar que o CORS está configurado para permitir conexões de origem local
- Confirmar que não há erros de binding de porta no console

---

### 0.2 Verificação do Healthcheck

**Objetivo do Teste:**
Confirmar que o endpoint de healthcheck `/health` está exposto e retorna status correto.

**Passos de Execução:**
1. Com o servidor em execução:
   ```bash
   curl -v http://localhost:5000/health
   ```
2. Verificar o corpo da resposta e cabeçalhos HTTP
3. Testar em diferentes estados do servidor (logo após iniciar, com carga, etc.)

**Resultados Esperados:**
- Código HTTP: 200 OK
- Corpo da resposta: "Healthy" (texto simples)
- Tempo de resposta: < 100ms
- Disponível mesmo sob carga moderada

**Considerações Adicionais:**
- Este endpoint é crítico para orquestração de containers e load balancers
- Deve responder rapidamente sem executar lógica de negócio complexa

---

### 0.3 Verificação de Logs Mínimos

**Objetivo do Teste:**
Validar que operações principais (Join, Leave, Send) geram logs adequados com informações essenciais.

**Passos de Execução:**
1. Iniciar o servidor e monitorar console de logs
2. Executar sequência de operações:
   - Conectar cliente e fazer Join
   - Enviar mensagem
   - Fazer Leave
3. Capturar e analisar logs gerados

**Resultados Esperados:**
- Logs de Join contêm: roomId, entityId, timestamp
- Logs de SendToRoom contêm: roomId, entityId, messageType, channel (se DM)
- Logs de Leave contêm: roomId, entityId
- Formato estruturado e legível
- Logs aparecem em tempo real no console

**Considerações Adicionais:**
- ⚠️ Verificar que logs não expõem informações sensíveis (tokens, dados privados)
- Confirmar nível de log apropriado (Info para operações normais, Error para falhas)

---

### 0.4 Configuração CORS

**Objetivo do Teste:**
Verificar que o CORS está configurado corretamente para permitir conexões de desenvolvimento local.

**Passos de Execução:**
1. Fazer requisição OPTIONS preflight:
   ```bash
   curl -X OPTIONS http://localhost:5000/rooms/123/artifacts \
     -H "Origin: http://localhost:3000" \
     -H "Access-Control-Request-Method: POST" \
     -v
   ```
2. Verificar cabeçalhos CORS na resposta

**Resultados Esperados:**
- Header presente: `Access-Control-Allow-Origin: *` (ou origem específica)
- Header presente: `Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS`
- Header presente: `Access-Control-Allow-Headers: *` (ou lista de headers permitidos)
- Código HTTP: 200 ou 204

**Considerações Adicionais:**
- Em produção, substituir `*` por origens específicas
- Verificar que credenciais são permitidas se necessário (`Access-Control-Allow-Credentials: true`)

---

### 0.5 Validação de Configuração por Ambiente

**Objetivo do Teste:**
Confirmar que arquivos appsettings.json e appsettings.Development.json não possuem chaves faltantes ou valores inválidos.

**Passos de Execução:**
1. Revisar arquivos de configuração:
   ```bash
   cat server-dotnet/src/RoomServer/appsettings.json
   cat server-dotnet/src/RoomServer/appsettings.Development.json
   ```
2. Iniciar servidor em modo Development:
   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project server-dotnet/src/RoomServer/RoomServer.csproj
   ```
3. Verificar que não há warnings de configuração faltante nos logs

**Resultados Esperados:**
- Arquivos JSON válidos (sem erros de sintaxe)
- Todas as chaves necessárias presentes em ambos arquivos
- Servidor inicia sem erros de configuração
- Logs não mostram warnings sobre configurações faltantes

**Considerações Adicionais:**
- Verificar diferenças entre Development e Production (ex: logging verbosity)
- Confirmar que secrets não estão hardcoded nos arquivos

---

## 1) Modelo de Mensagens e Contratos ✅

### 1.1 Validação do MessageModel

**Objetivo do Teste:**
Verificar que o modelo MessageModel contém todos os campos obrigatórios conforme o schema.

**Passos de Execução:**
1. Executar testes unitários existentes:
   ```bash
   cd server-dotnet
   dotnet test --filter "FullyQualifiedName~MessageModel"
   ```
2. Criar mensagem via SignalR e inspecionar payload:
   ```javascript
   // Exemplo em JavaScript
   const message = {
     roomId: "room-test123",
     channel: "main",
     type: "chat",
     payload: { text: "Hello" }
   };
   await connection.invoke("SendToRoom", "room-test123", message);
   ```
3. Capturar mensagem recebida e validar campos

**Resultados Esperados:**
- Campos presentes: `id`, `roomId`, `channel`, `from`, `type`, `payload`, `ts`, `correlationId`
- Campo `id` é ULID válido
- Campo `ts` é timestamp ISO 8601
- Campo `type` é um dos valores: chat, command, event, artifact
- Payload é objeto JSON (não string)

**Considerações Adicionais:**
- ⚠️ Campo `correlationId` é opcional mas deve ser preservado quando fornecido
- Campo `to` (para mensagens diretas) deve ser respeitado quando presente
- ⚠️ **Nota sobre mensagens diretas (DM):** O mecanismo canônico para endereçamento de DMs é o campo `to` na mensagem. O uso de canais do tipo `@E-*` também é suportado para compatibilidade, mas quando ambos estão presentes, o campo `to` tem precedência. Clientes devem priorizar o campo `to` para DMs e tratar o canal `@E-*` como mecanismo alternativo/legado. Documentação adicional sobre compatibilidade pode ser encontrada na seção 5.4.

---

### 1.2 Validação de EntitySpec/EntityInfo

**Objetivo do Teste:**
Confirmar que entidades contêm todos os campos obrigatórios e tipos válidos.

**Passos de Execução:**
1. Executar testes de validação:
   ```bash
   dotnet test --filter "(FullyQualifiedName~EntitySpec)|(FullyQualifiedName~ValidationTests)"
   ```
2. Fazer Join com diferentes tipos de entidade:
   ```bash
   # Via HTTP client ou Postman, conectar SignalR e enviar:
   {
     "roomId": "room-test123",
     "entitySpec": {
       "id": "E-human01",
       "kind": "human",
       "displayName": "Test User",
       "visibility": "public",
       "policy": {
         "allow_commands_from": "any"
       },
       "capabilities": ["port.chat", "port.tools"]
     }
   }
   ```
3. Verificar entidade registrada corretamente

**Resultados Esperados:**
- Campos obrigatórios: `id`, `kind`, `displayName`
- Campo `kind` aceita: human, agent, npc, orchestrator (case-insensitive)
- Campo `id` segue padrão: `^E-[A-Za-z0-9_-]{2,64}$`
- Campo `visibility` aceita: public, team, owner
- Campo `policy.allow_commands_from` aceita: any, orchestrator, owner
- Campo `capabilities` é array de strings (PortId)
- Validação retorna erro 400 para dados inválidos

**Considerações Adicionais:**
- ⚠️ Testar casos extremos: EntityId com 2 caracteres, 64 caracteres
- Verificar que `owner_user_id` é opcional mas validado quando presente

---

### 1.3 Validação de RoomState e Transições

**Objetivo do Teste:**
Verificar que estados de sala são gerenciados corretamente e eventos ROOM.STATE são emitidos.

**Passos de Execução:**
1. Executar testes de contexto de sala:
   ```bash
   dotnet test --filter "FullyQualifiedName~RoomContext"
   ```
2. Testar transições de estado:
   - Criar sala vazia (estado: Init)
   - Primeira entidade entra → Active
   - Última entidade sai → Ended
3. Monitorar eventos ROOM.STATE emitidos
4. Verificar arquivo `room-run.json` após encerramento

**Resultados Esperados:**
- Estado inicial: `Init` (sala sem entidades)
- Estado após primeiro Join: `Active`
- Estado após último Leave/Disconnect: `Ended`
- Evento `ROOM.STATE` emitido em cada transição
- Evento contém campo `state` com valor atual
- Arquivo `.ai-flow/runs/{roomId}/room-run.json` criado ao fim

**Considerações Adicionais:**
- ⚠️ Estado `Paused` pode ser adicionado no futuro (via comando admin)
- Verificar que transições são atômicas e thread-safe
- Confirmar timestamps `created_at`, `updated_at`, `ended_at` no room-run.json

---

## 2) Ciclo de Vida da Sala ✅

### 2.1 Criação e Ativação de Sala

**Objetivo do Teste:**
Validar que uma sala é criada implicitamente no primeiro Join e transiciona para Active.

**Passos de Execução:**
1. Conectar cliente SignalR à sala inexistente
2. Executar Join:
   ```javascript
   const roomId = "room-newtest1";
   const entity = {
     id: "E-test01",
     kind: "human",
     displayName: "First User"
   };
   await connection.invoke("Join", roomId, entity);
   ```
3. Verificar evento ROOM.STATE recebido
4. Verificar logs do servidor

**Resultados Esperados:**
- Join bem-sucedido (sem erro)
- Evento `ROOM.STATE` recebido com `state: "Active"`
- Evento `ENTITY.JOIN` recebido
- Logs mostram criação de contexto de sala
- RoomId validado (formato `room-[A-Za-z0-9_-]{6,}`)

**Considerações Adicionais:**
- Primeira entidade sempre pode entrar (sem autenticação em dev)
- Contexto de sala armazenado em memória (RoomContextStore)

---

### 2.2 Pausa e Retomada (Extensão Futura)

**Objetivo do Teste:**
Verificar suporte básico para estado Paused (implementação futura).

**Passos de Execução:**
1. Verificar que enum RoomState contém valor `Paused`
2. Confirmar que transições para Paused podem ser adicionadas

**Resultados Esperados:**
- Enum RoomState.Paused existe no código
- Documentação indica que pausa pode ser implementada via comando admin

**Considerações Adicionais:**
- ⚠️ Funcionalidade não completamente implementada no MVP
- Planejar para próxima iteração

---

### 2.3 Encerramento de Sala

**Objetivo do Teste:**
Validar que sala é encerrada corretamente quando última entidade sai.

**Passos de Execução:**
1. Entrar com múltiplas entidades
2. Fazer Leave para todas exceto uma
3. Fazer Leave da última entidade ou desconectar abruptamente
4. Verificar evento ROOM.STATE e arquivo room-run.json

**Resultados Esperados:**
- Evento `ROOM.STATE` com `state: "Ended"` emitido
- Evento `ENTITY.LEAVE` emitido para última entidade
- Arquivo `.ai-flow/runs/{roomId}/room-run.json` criado
- Arquivo contém: lista de entidades, contagem de mensagens, duração, timestamps
- Contexto de sala removido da memória

**Considerações Adicionais:**
- ⚠️ Testar desconexão inesperada (matar conexão WebSocket)
- Verificar que cleanup é executado mesmo em caso de erro
- Confirmar que arquivo events.jsonl está completo

---

### 2.4 Evento ROOM.STATE

**Objetivo do Teste:**
Verificar que eventos de mudança de estado são publicados corretamente.

**Passos de Execução:**
1. Conectar listener em SignalR para receber eventos
2. Executar sequência: Join (primeira entidade) → Join (segunda) → Leave (todas)
3. Capturar todos eventos ROOM.STATE

**Resultados Esperados:**
- Evento emitido na transição Init → Active
- Evento emitido na transição Active → Ended
- Mensagem completa inclui envelope com campos obrigatórios (id, roomId, from, ts, etc.)
- Exemplo completo de mensagem de evento Active:
  ```json
  {
    "id": "01JEH7P2MRG9ZTKV3D6V8P5W3Q",
    "roomId": "room-test123",
    "channel": "main",
    "from": "system",
    "type": "event",
    "ts": "2025-10-19T02:00:00Z",
    "correlationId": null,
    "payload": {
      "kind": "ROOM.STATE",
      "state": "Active",
      "entities": [
        {
          "id": "E-user01",
          "kind": "human",
          "displayName": "User 1"
        }
      ]
    }
  }
  ```
- Exemplo completo de mensagem de evento Ended:
  ```json
  {
    "id": "01JEH7P2MRG9ZTKV3D6V8P5W3Q",
    "roomId": "room-test123",
    "channel": "main",
    "from": "system",
    "type": "event",
    "ts": "2025-10-19T02:15:00Z",
    "correlationId": null,
    "payload": {
      "kind": "ROOM.STATE",
      "state": "Ended",
      "entities": []
    }
  }
  ```
- Todos clientes conectados recebem o evento

**Considerações Adicionais:**
- Evento broadcast para grupo SignalR da sala
- Incluir snapshot de entidades no evento

---

## 3) Presença / Sessões (SessionStore) ✅

### 3.1 Operação Join

**Objetivo do Teste:**
Validar que Join cria sessão corretamente e adiciona entidade ao grupo SignalR.

**Passos de Execução:**
1. Executar testes de SessionStore:
   ```bash
   dotnet test --filter "(FullyQualifiedName~SessionStore)|(FullyQualifiedName~RoomHub)"
   ```
2. Conectar via SignalR e fazer Join
3. Verificar que entidade aparece em ListEntities

**Resultados Esperados:**
- Sessão criada com connectionId único
- Sessão indexada por connectionId e (roomId, entityId)
- Cliente adicionado ao grupo SignalR "{roomId}"
- Evento `ENTITY.JOIN` broadcast para sala
- Método `ListEntities(roomId)` retorna entidade

**Considerações Adicionais:**
- ConnectionId é gerado automaticamente pelo SignalR
- Entidade pode ter múltiplas sessões (reconexão em novo dispositivo)

---

### 3.2 Operação Leave

**Objetivo do Teste:**
Confirmar que Leave remove sessão e emite evento apropriado.

**Passos de Execução:**
1. Fazer Join seguido de Leave:
   ```javascript
   await connection.invoke("Join", roomId, entity);
   await connection.invoke("Leave", roomId, entity.id);
   ```
2. Verificar evento ENTITY.LEAVE
3. Confirmar que entidade não aparece mais em ListEntities

**Resultados Esperados:**
- Sessão removida do SessionStore
- Cliente removido do grupo SignalR
- Evento `ENTITY.LEAVE` broadcast
- ListEntities não inclui mais a entidade

**Considerações Adicionais:**
- Leave por uma conexão não afeta outras conexões da mesma entidade (multi-dispositivo)

---

### 3.3 Desconexão Inesperada (OnDisconnectedAsync)

**Objetivo do Teste:**
Validar que desconexões abruptas são tratadas e limpas corretamente.

**Passos de Execução:**
1. Conectar e fazer Join
2. Matar conexão WebSocket abruptamente (fechar navegador, kill -9)
3. Monitorar servidor para evento OnDisconnectedAsync

**Resultados Esperados:**
- Método `OnDisconnectedAsync` chamado automaticamente
- Sessão removida
- Evento `ENTITY.LEAVE` emitido
- Se última entidade, sala transiciona para Ended
- Logs mostram cleanup executado

**Considerações Adicionais:**
- ⚠️ Testar com múltiplas desconexões simultâneas
- Verificar que não há sessões "fantasma" na memória
- Confirmar que grupo SignalR é limpo

---

### 3.4 Listagem de Presença

**Objetivo do Teste:**
Verificar que método ListEntities retorna lista correta de entidades na sala.

**Passos de Execução:**
1. Adicionar múltiplas entidades à sala
2. Chamar ListEntities:
   ```javascript
   const entities = await connection.invoke("ListEntities", roomId);
   ```
3. Verificar resposta

**Resultados Esperados:**
- Lista contém todas entidades conectadas
- Cada item inclui: id, kind, displayName, visibility, capabilities
- Lista atualizada em tempo real (após Join/Leave)
- Resposta em < 50ms

**Considerações Adicionais:**
- Lista retorna apenas entidades visíveis (conforme política de visibilidade)

---

## 4) Permissões Mínimas (PermissionService) ✅

### 4.1 Mensagens Diretas (@E-*) com Visibilidade

**Objetivo do Teste:**
Validar que DMs respeitam regras de visibilidade (public|team|owner).

**Passos de Execução:**
1. Criar entidades com diferentes níveis de visibilidade:
   - E-public (visibility: public)
   - E-team (visibility: team)
   - E-owner (visibility: owner)
2. Tentar enviar DM de uma para outra
3. Verificar permissões aplicadas

**Resultados Esperados:**
- DM para entidade `public`: Sempre permitido
- DM para entidade `team`: Permitido se mesmo owner_user_id
- DM para entidade `owner`: Bloqueado (exceto se mesmo owner_user_id)
- Erro retornado: HubException com código `PERM_DENIED`
- Mensagem de erro clara: "Access denied: cannot direct message this entity"

**Considerações Adicionais:**
- ⚠️ Em ambiente dev (sem JWT), owner_user_id pode ser null → tratamento especial
- Verificar que mensagem não vaza informação sensível no erro

---

### 4.2 Comandos com Policy allow_commands_from

**Objetivo do Teste:**
Confirmar que comandos respeitam política allow_commands_from.

**Passos de Execução:**
1. Executar teste específico:
   ```bash
   dotnet test --filter "FullyQualifiedName~CommandDeniedWhenPolicyRequiresOrchestrator"
   ```
2. Criar entidade com policy.allow_commands_from: "orchestrator"
3. Tentar enviar comando de entidade não-orchestrator
4. Tentar enviar comando de entidade orchestrator

**Resultados Esperados:**
- Comando de não-orchestrator: HubException, código `PERM_DENIED`
- Comando de orchestrator: Aceito e roteado
- Política `any`: Aceita de qualquer entidade
- Política `owner`: Aceita apenas de entidade com mesmo owner_user_id

**Considerações Adicionais:**
- ⚠️ Testar com e sem JWT (dev vs prod)
- Verificar que port de destino é validado

---

### 4.3 Erros Padronizados de Permissão

**Objetivo do Teste:**
Validar formato consistente de erros de permissão.

**Passos de Execução:**
1. Provocar diferentes tipos de erro de permissão
2. Capturar exceções e verificar estrutura

**Resultados Esperados:**
- Tipo: HubException
- Formato do erro:
  ```json
  {
    "error": "PERM_DENIED",
    "code": "PERM_DENIED",
    "message": "<descrição legível>"
  }
  ```
- Código HTTP equivalente (em REST): 403 Forbidden

**Considerações Adicionais:**
- Mensagens devem ser claras mas não expor detalhes de segurança internos
- Logs devem conter mais detalhes que a mensagem ao cliente

---

### 4.4 Integração com JWT (Dev vs Prod)

**Objetivo do Teste:**
Verificar comportamento com e sem autenticação JWT.

**Passos de Execução:**
1. Modo Development (sem JWT):
   - Fazer Join sem header Authorization
   - Verificar que owner_user_id é null
2. Modo Production (com JWT):
   - Fazer Join com token válido
   - Verificar que owner_user_id é extraído do token

**Resultados Esperados:**
- Dev: Funciona sem JWT, permissões relaxadas
- Prod: Requer JWT válido, owner_user_id extraído
- Erro 401 se JWT ausente ou inválido em prod

**Considerações Adicionais:**
- ⚠️ Documentar claramente diferença entre dev e prod
- Configurar variável de ambiente para alternar modo

---

## 5) Hub (SignalR) — Métodos e Canais ✅

### 5.1 Método Join(roomId, entitySpec)

**Objetivo do Teste:**
Testar operação completa de Join com todas validações.

**Passos de Execução:**
1. Executar testes existentes:
   ```bash
   dotnet test --filter "FullyQualifiedName~Join"
   ```
2. Testar com dados válidos e inválidos:
   - RoomId inválido (não começa com "room-")
   - EntityId inválido (não segue padrão E-*)
   - Kind inválido
3. Verificar logs e eventos

**Resultados Esperados:**
- Validação de RoomId: `^room-[A-Za-z0-9_-]{6,}$`
- Validação de EntityId: `^E-[A-Za-z0-9_-]{2,64}$`
- Validação de Kind: human|agent|npc|orchestrator
- Cliente adicionado ao grupo SignalR
- Sessão criada
- Evento `ENTITY.JOIN` broadcast
- Erros claros para dados inválidos (400 Bad Request equivalente)

**Considerações Adicionais:**
- ⚠️ Join idempotente (múltiplas chamadas com mesmo entityId não causam erro)
- Logs devem incluir roomId, entityId, timestamp

---

### 5.2 Método Leave(roomId, entityId)

**Objetivo do Teste:**
Validar operação de saída e cleanup.

**Passos de Execução:**
1. Join seguido de Leave
2. Verificar evento e estado da sala
3. Testar Leave de entidade não conectada (deve retornar erro)

**Resultados Esperados:**
- Sessão removida
- Evento `ENTITY.LEAVE` broadcast
- Cliente removido do grupo
- Leave de entidade inexistente: Erro apropriado
- Se última entidade, sala transiciona para Ended

**Considerações Adicionais:**
- Leave pode ser chamado por qualquer conexão da mesma entidade

---

### 5.3 Método SendToRoom - Validações Gerais

**Objetivo do Teste:**
Testar validações básicas de mensagens.

**Passos de Execução:**
1. Enviar mensagem com roomId inválido
2. Enviar mensagem sem campo type
3. Enviar mensagem com payload inválido

**Resultados Esperados:**
- RoomId validado
- Timestamp (ts) normalizado (gerado se ausente)
- Campo roomId injetado na mensagem
- Erros 400 para dados inválidos

**Considerações Adicionais:**
- ⚠️ Mensagem deve ser clonada/normalizada antes de broadcast

---

### 5.4 SendToRoom - Mensagens Diretas (DM)

**Objetivo do Teste:**
Validar roteamento de DMs via channel @E-*.

**Passos de Execução:**
1. Enviar mensagem com channel: "@E-target01"
2. Verificar que apenas destinatário recebe
3. Testar DM para entidade inexistente

**Resultados Esperados:**
- Channel iniciado com @ indica DM
- EntityId do destinatário resolvido
- Permissão CanDirectMessage aplicada
- Mensagem enviada apenas para conexões do destinatário
- DM para entidade inexistente: Erro `TARGET_NOT_FOUND`

**Considerações Adicionais:**
- ⚠️ DM pode ter múltiplos destinatários (múltiplas conexões)
- Verificar que permissões de visibilidade são aplicadas

---

### 5.5 SendToRoom - Comandos

**Objetivo do Teste:**
Validar roteamento e permissões de comandos.

**Passos de Execução:**
1. Enviar mensagem tipo command:
   ```json
   {
     "type": "command",
     "payload": {
       "target": "E-agent01",
       "port": "port.execute",
       "inputs": { "code": "console.log('hi')" }
     }
   }
   ```
2. Verificar validação de payload.target
3. Verificar aplicação de permissões

**Resultados Esperados:**
- Campo payload.target obrigatório
- Campo payload.port opcional (recomendado)
- Sessão de destino resolvida
- Permissão CanSendCommand aplicada
- Comando roteado apenas para target
- Erro se target não encontrado ou permissão negada

**Considerações Adicionais:**
- ⚠️ Port não é estritamente obrigatório (backward compatibility)
- Validar formato de PortId se fornecido: `^[a-z][a-z0-9]*(\.[a-z0-9]+)*$`

---

### 5.6 SendToRoom - Broadcast para Sala

**Objetivo do Teste:**
Verificar broadcast de mensagens para todos na sala.

**Passos de Execução:**
1. Conectar múltiplas entidades
2. Enviar mensagem tipo chat sem channel específico
3. Verificar que todos recebem

**Resultados Esperados:**
- Mensagem broadcast para grupo SignalR da sala
- Todos clientes conectados recebem
- Ordenação por conexão é preservada (FIFO por cliente)
- Latência baixa (< 100ms para rede local)

**Considerações Adicionais:**
- Mensagens broadcast não passam por filtro de permissão (exceto DM e command)

---

### 5.7 Canal de Eventos

**Objetivo do Teste:**
Validar publicação de eventos via RoomEventPublisher.

**Passos de Execução:**
1. Monitorar eventos: ENTITY.JOIN, ENTITY.LEAVE, ROOM.STATE
2. Verificar payload de cada tipo
3. Confirmar que eventos são gravados em events.jsonl

**Resultados Esperados:**
- Eventos seguem padrão (incluindo envelope obrigatório conforme Seção 2.4):
  ```json
  {
    "id": "string",
    "roomId": "string",
    "channel": "string",
    "from": "string",
    "ts": "2023-01-01T00:00:00.000Z",
    "correlationId": "string",
    "type": "event",
    "payload": {
      "kind": "ENTITY.JOIN|ENTITY.LEAVE|ROOM.STATE",
      ...
    }
  }
  ```
- Kind em SCREAMING_CASE (validação: `^[A-Z]+(\.[A-Z]+)*$`)
- Eventos broadcast e registrados em disco

**Considerações Adicionais:**
- ⚠️ Eventos são síncronos (broadcast) e assíncronos (escrita em disco)
- Verificar que write failures não bloqueiam broadcast

---

## 6) Integração com Workspaces de Artefatos ✅

### 6.1 REST Endpoint - POST /rooms/{roomId}/artifacts

**Objetivo do Teste:**
Validar upload de artefato para workspace de sala.

**Passos de Execução:**
1. Fazer upload via HTTP POST:
   ```bash
   curl -X POST http://localhost:5000/rooms/room-test123/artifacts \
     -H "X-Entity-Id: E-user01" \
     -F "spec={\"name\":\"document.txt\",\"type\":\"text/plain\"};type=application/json" \
     -F "data=@document.txt"
   ```
2. Verificar resposta e arquivos criados

**Resultados Esperados:**
- Código HTTP: 201 Created
- Resposta contém manifest completo:
  ```json
  {
    "name": "document.txt",
    "type": "text/plain",
    "sha256": "<hash>",
    "size": 1234,
    "version": 1,
    "origin": {
      "room": "room-test123",
      "entity": "E-user01",
      "workspace": "room"
    }
  }
  ```
- Arquivos criados:
  - `.ai-flow/runs/room-test123/artifacts/room/document.txt.v1`
  - `.ai-flow/runs/room-test123/artifacts/room/manifest.json`

**Considerações Adicionais:**
- ⚠️ SHA256 calculado automaticamente
- ⚠️ Version incrementado automaticamente
- Validar nome de arquivo (não permitir path traversal)

---

### 6.2 REST Endpoint - POST /rooms/{roomId}/entities/{entityId}/artifacts

**Objetivo do Teste:**
Validar upload de artefato para workspace privado de entidade.

**Passos de Execução:**
1. Fazer upload para workspace privado:
   ```bash
   curl -X POST http://localhost:5000/rooms/room-test123/entities/E-agent01/artifacts \
     -H "X-Entity-Id: E-agent01" \
     -F "spec={\"name\":\"analysis.json\",\"type\":\"application/json\"};type=application/json" \
     -F "data=@analysis.json"
   ```
2. Verificar permissões aplicadas

**Resultados Esperados:**
- Código 201 Created se owner ou orchestrator
- Código 403 se não autorizado
- Workspace separado: `.ai-flow/runs/room-test123/artifacts/E-agent01/`

**Considerações Adicionais:**
- ⚠️ Apenas owner e orchestrator podem acessar workspace privado
- Verificar que arquivo não é acessível por outras entidades

---

### 6.3 Broadcast após Write/Promote

**Objetivo do Teste:**
Verificar que eventos são emitidos após upload.

**Passos de Execução:**
1. Conectar listener SignalR
2. Fazer upload de artefato
3. Capturar eventos emitidos

**Resultados Esperados:**
- Evento `ARTIFACT.ADDED` (primeiro upload) ou `ARTIFACT.UPDATED` (versão > 1)
- Mensagem tipo "artifact" com payload.manifest
- Todos clientes da sala recebem
- Eventos contêm manifest completo

**Considerações Adicionais:**
- ⚠️ Eventos devem ser emitidos APÓS sucesso da escrita em disco
- Verificar ordenação (write → broadcast)

---

### 6.4 Permissões de Workspace Privado

**Objetivo do Teste:**
Validar controle de acesso a workspaces privados.

**Passos de Execução:**
1. Executar teste específico:
   ```bash
   dotnet test --filter "FullyQualifiedName~PromoteDeniedForNonOwner"
   ```
2. Tentar acessar workspace de outra entidade

**Resultados Esperados:**
- Owner: Acesso permitido
- Orchestrator: Acesso permitido
- Outras entidades: 403 Forbidden
- Mensagem: "Access denied: cannot access this workspace"

**Considerações Adicionais:**
- ⚠️ Workspace de sala (room) é acessível a todos na sala

---

### 6.5 Operação Promote

**Objetivo do Teste:**
Validar promoção de artefato de workspace privado para sala.

**Passos de Execução:**
1. Upload para workspace privado
2. Promover para sala:
   ```bash
   curl -X POST http://localhost:5000/rooms/room-test123/artifacts/promote \
     -H "X-Entity-Id: E-agent01" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "analysis.json",
       "fromEntity": "E-agent01"
     }'
   ```

**Resultados Esperados:**
- Bytes copiados de workspace privado para room
- Novo manifest gerado com version incrementada
- origin.workspace muda de entityId para "room"
- Eventos `ARTIFACT.ADDED` e mensagem artifact emitidos
- Arquivo original permanece em workspace privado

**Considerações Adicionais:**
- ⚠️ Apenas owner ou orchestrator podem promover
- Verificar que SHA256 é recalculado

---

## 7) MCP Bridge na Sala ✅

### 7.1 Método ListTools(roomId)

**Objetivo do Teste:**
Verificar listagem de ferramentas MCP disponíveis.

**Passos de Execução:**
1. Iniciar MCP servers:
   ```bash
   make mcp-up
   # ou
   pnpm -C mcp-ts dev
   ```
2. Conectar à sala e listar tools:
   ```javascript
   const tools = await connection.invoke("ListTools", roomId);
   ```

**Resultados Esperados:**
- Lista de tools disponíveis (de MCPs registrados)
- Cada tool contém: name, description, inputSchema
- Apenas tools visíveis para caller (policy aplicada)
- Resposta vazia se nenhum MCP conectado

**Considerações Adicionais:**
- ⚠️ ListTools respeita políticas de visibilidade de cada MCP
- Verificar que catalog é atualizado dinamicamente (MCP pode conectar/desconectar)

---

### 7.2 Método CallTool(roomId, toolIdOrKey, args)

**Objetivo do Teste:**
Validar execução de ferramenta MCP e retorno de resultado.

**Passos de Execução:**
1. Chamar ferramenta:
   ```javascript
   const result = await connection.invoke("CallTool", 
     "room-test123",
     "mcp.example.add",
     { a: 5, b: 3 }
   );
   ```
2. Verificar resultado e eventos emitidos

**Resultados Esperados:**
- Catalog resolvido com base em toolIdOrKey
- Permissão CanCall aplicada
- Evento `RESOURCE.CALLED` emitido antes da execução
- Evento `RESOURCE.RESULT` emitido após execução:
  ```json
  {
    "type": "event",
    "payload": {
      "kind": "RESOURCE.RESULT",
      "ok": true,
      "result": { "sum": 8 }
    }
  }
  ```
- Retorno da função: `{ ok: true, result: {...} }` ou `{ ok: false, error: "..." }`

**Considerações Adicionais:**
- ⚠️ CallTool pode demorar (timeout configurável)
- Verificar que erros do MCP são capturados e retornados como `{ ok: false }`

---

### 7.3 Indisponibilidade de MCP

**Objetivo do Teste:**
Verificar tratamento quando MCP não está disponível.

**Passos de Execução:**
1. Parar todos MCP servers
2. Tentar ListTools e CallTool

**Resultados Esperados:**
- ListTools: Retorna lista vazia (sem erro)
- CallTool: Retorna erro com código `MCP_UNAVAILABLE`
- Mensagem: "MCP client disconnected or unavailable"
- Logs mostram tentativa de conexão falhada

**Considerações Adicionais:**
- ⚠️ Sistema deve degradar gracefully (não crashar)
- Considerar retry automático ou reconexão

---

## 8) Observabilidade Mínima ✅

### 8.1 Arquivo events.jsonl

**Objetivo do Teste:**
Validar que eventos são gravados em arquivo linha a linha.

**Passos de Execução:**
1. Executar sequência de operações em sala
2. Verificar arquivo `.ai-flow/runs/{roomId}/events.jsonl`
3. Analisar conteúdo

**Resultados Esperados:**
- Arquivo criado no primeiro evento
- Uma linha por evento (JSON válido por linha)
- Eventos incluem: ENTITY.JOIN, ENTITY.LEAVE, ROOM.STATE, ARTIFACT.*, RESOURCE.*, COMMAND.*
- Cada linha contém timestamp, tipo, payload
- Exemplo:
  ```json
  {"ts":"2025-10-19T01:00:00Z","type":"event","payload":{"kind":"ENTITY.JOIN","entity":"E-user01"}}
  {"ts":"2025-10-19T01:00:05Z","type":"event","payload":{"kind":"ROOM.STATE","state":"Active"}}
  ```

**Considerações Adicionais:**
- ⚠️ Arquivo pode crescer rapidamente em salas ativas
- Verificar que writes são atômicos e thread-safe (SemaphoreSlim)
- Considerar rotação de logs no futuro

---

### 8.2 Arquivo room-run.json

**Objetivo do Teste:**
Verificar sumário gravado ao final da sala.

**Passos de Execução:**
1. Executar ciclo completo de sala (Join, atividades, Leave final)
2. Verificar que sala transiciona para Ended
3. Verificar arquivo `.ai-flow/runs/{roomId}/room-run.json`

**Resultados Esperados:**
- Arquivo criado quando sala encerra
- Estrutura:
  ```json
  {
    "roomId": "room-test123",
    "state": "Ended",
    "created_at": "2025-10-19T01:00:00Z",
    "ended_at": "2025-10-19T01:15:30Z",
    "duration_seconds": 930,
    "entities": [
      {"id": "E-user01", "kind": "human", "displayName": "User 1"}
    ],
    "message_count": 42,
    "artifact_count": 5,
    "errors": []
  }
  ```
- Duração calculada corretamente
- Contadores acurados

**Considerações Adicionais:**
- ⚠️ Arquivo escrito apenas uma vez (ao fim)
- Se servidor crashar antes, arquivo pode não existir

---

### 8.3 OpenTelemetry (OTEL) - Opcional

**Objetivo do Teste:**
Verificar se infraestrutura para OTEL está presente (não implementada no MVP).

**Passos de Execução:**
1. Revisar código e configuração
2. Verificar se há packages OTEL instalados

**Resultados Esperados:**
- OTEL não implementado no MVP (conforme documentação)
- Planejar para iteração futura
- Considerar métricas: latência de mensagens, taxa de erro, contagem de conexões

**Considerações Adicionais:**
- ⚠️ OTEL é opcional mas recomendado para produção
- Pode ser adicionado sem quebrar funcionalidade existente

---

## 9) Erros & Envelopes ✅

### 9.1 Erros do Hub (HubException)

**Objetivo do Teste:**
Validar formato consistente de erros em métodos SignalR.

**Passos de Execução:**
1. Provocar diferentes tipos de erro:
   - Validação (dados inválidos)
   - Permissão negada
   - Recurso não encontrado
2. Capturar exceções no cliente

**Resultados Esperados:**
- Tipo: HubException
- Estrutura:
  ```javascript
  {
    id: "evt-123456",
    roomId: "room-abc",
    channel: "signalr",
    from: "roomserver",
    ts: 1712345678901,
    correlationId: "corr-7890",
    error: "VALIDATION_ERROR | PERM_DENIED | NOT_FOUND | ...",
    code: "...",
    message: "Human readable description"
  }
  ```
- Códigos padronizados:
  - `VALIDATION_ERROR`: Dados inválidos
  - `PERM_DENIED`: Permissão negada
  - `NOT_FOUND`: Recurso não encontrado
  - `MCP_UNAVAILABLE`: MCP indisponível

**Considerações Adicionais:**
- Mensagens em inglês (internacionalização futura)
- Não expor stack traces ou detalhes internos ao cliente

---

### 9.2 Erros REST (JSON)

**Objetivo do Teste:**
Verificar formato de erros em endpoints REST.

**Passos de Execução:**
1. Fazer requisições inválidas:
   ```bash
   curl -X POST http://localhost:5000/rooms/invalid/artifacts
   curl -X POST http://localhost:5000/rooms/room-test/artifacts/promote \
     -H "X-Entity-Id: E-test01" \
     -H "Content-Type: application/json" \
     -d '{"invalid": "data"}'
   ```
2. Analisar respostas de erro

**Resultados Esperados:**
- Códigos HTTP apropriados:
  - 400: Bad Request (dados inválidos)
  - 401: Unauthorized (autenticação faltante/inválida)
  - 403: Forbidden (permissão negada)
  - 404: Not Found (recurso inexistente)
  - 409: Conflict (conflito de estado)
  - 500: Internal Server Error (erro inesperado)
- Corpo JSON:
  ```json
  {
    "error": "ERROR_CODE",
    "message": "Descriptive message"
  }
  ```

**Considerações Adicionais:**
- ⚠️ Erro 500 deve logar stack trace no servidor mas não enviar ao cliente
- Manter consistência entre REST e SignalR errors

---

### 9.3 Mensagens Inválidas

**Objetivo do Teste:**
Validar tratamento de mensagens malformadas.

**Passos de Execução:**
1. Enviar mensagens com payloads inválidos:
   - Comando sem target
   - Chat sem text
   - Event com kind inválido (não SCREAMING_CASE)
   - Artifact sem manifest

**Resultados Esperados:**
- Erro 400 (InvalidMessage)
- Mensagem específica:
  - "Command payload must include target field"
  - "Chat payload must include text field"
  - "Event kind must be in SCREAMING_CASE format"
  - "Artifact payload must include manifest"

**Considerações Adicionais:**
- ⚠️ Validação deve ser rápida (não bloquear outras mensagens)
- Considerar rate limiting para prevenir spam de mensagens inválidas

---

## 10) Concorrência e Robustez ✅

### 10.1 Estruturas em Memória Thread-Safe

**Objetivo do Teste:**
Verificar que ConcurrentDictionary é usado em toda estrutura de dados compartilhada.

**Passos de Execução:**
1. Revisar código fonte:
   - SessionStore
   - RoomContextStore
   - ArtifactStore (caches se houver)
2. Executar testes de carga com múltiplas conexões simultâneas

**Resultados Esperados:**
- Todas coleções compartilhadas usam ConcurrentDictionary ou equivalente thread-safe
- Não há race conditions em operações de leitura/escrita
- Testes de carga passam sem deadlocks ou erros

**Considerações Adicionais:**
- ⚠️ ConcurrentDictionary garante operações atômicas mas não transações complexas
- Usar locks (SemaphoreSlim) apenas quando necessário

---

### 10.2 Locks Finos (SemaphoreSlim)

**Objetivo do Teste:**
Validar que locks são usados apenas para operações críticas (ex: escrita em arquivo).

**Passos de Execução:**
1. Revisar uso de SemaphoreSlim no código
2. Verificar que locks são por recurso (não global)
3. Testar concorrência em escritas de observabilidade

**Resultados Esperados:**
- SemaphoreSlim usado em RoomObservabilityService para writes
- Um lock por arquivo/sala (não lock global)
- Locks mantidos pelo menor tempo possível
- Não há deadlocks em operações concorrentes

**Considerações Adicionais:**
- ⚠️ Escrita em disco é I/O-bound → usar async/await
- Verificar que lock failures não causam perda de dados

---

### 10.3 Reconexão e Cleanup

**Objetivo do Teste:**
Validar que reconexões são tratadas corretamente sem sessões fantasma.

**Passos de Execução:**
1. Conectar, desconectar, reconectar múltiplas vezes
2. Verificar que apenas uma sessão ativa existe por conexão
3. Monitorar SessionStore para vazamentos de memória

**Resultados Esperados:**
- Desconexão remove sessão antiga
- Reconexão cria nova sessão com novo connectionId
- Não há sessões "órfãs" na memória
- Memória estável após múltiplos ciclos de conexão

**Considerações Adicionais:**
- ⚠️ SignalR gerencia reconexão automaticamente (até certo timeout)
- Verificar que cleanup em OnDisconnectedAsync é sempre executado

---

### 10.4 Envio para Grupos SignalR

**Objetivo do Teste:**
Verificar estabilidade de broadcast para grupos.

**Passos de Execução:**
1. Adicionar 100+ clientes a uma sala
2. Enviar broadcast de mensagens
3. Medir latência e taxa de entrega

**Resultados Esperados:**
- Todos clientes do grupo recebem mensagem
- Latência < 200ms (rede local)
- Não há perda de mensagens
- RoomId estável (não muda durante vida da sala)

**Considerações Adicionais:**
- ⚠️ SignalR escala até ~10k conexões por servidor (com configuração adequada)
- Considerar backpressure e buffering para salas muito ativas

---

## 🔴 Áreas de Atenção Especial (⚠️)

### Fluxos Multi-Etapas

**Operações que requerem monitoramento cuidadoso:**

1. **Upload e Promoção de Artefatos:**
   - Sequência: Upload → Write to disk → Calculate SHA256 → Update manifest → Broadcast event
   - Pontos de falha: I/O disk, concorrência em manifest.json
   - Monitorar: Logs em cada etapa, verificar atomicidade

2. **Ciclo de Vida da Sala:**
   - Sequência: First Join → Active → (operações) → Last Leave → Ended → Write room-run.json
   - Pontos de falha: Desconexões simultâneas, race em contagem de entidades
   - Monitorar: Eventos ROOM.STATE, timestamps, completude do room-run.json

3. **CallTool MCP:**
   - Sequência: Resolve catalog → Check permission → Emit RESOURCE.CALLED → Execute → Emit RESOURCE.RESULT
   - Pontos de falha: MCP timeout, MCP disconnect durante execução
   - Monitorar: Eventos RESOURCE.*, logs de timeout, tratamento de erros

### Diferenças entre Dev e Prod

**Documentar e testar ambos ambientes:**

1. **Autenticação:**
   - Dev: Sem JWT, owner_user_id = null
   - Prod: JWT obrigatório, owner_user_id extraído
   - Testar: Permissões com e sem JWT

2. **CORS:**
   - Dev: Allow-Origin: * (qualquer origem)
   - Prod: Allow-Origin: domínios específicos
   - Testar: Conexões de diferentes origens

3. **Logging:**
   - Dev: Verboso (Debug level)
   - Prod: Conciso (Info/Warning level)
   - Verificar: Configuração em appsettings

### Validações Críticas

**Validações que NÃO devem falhar silenciosamente:**

1. **IDs e Patterns:**
   - RoomId: `^room-[A-Za-z0-9_-]{6,}$`
   - EntityId: `^E-[A-Za-z0-9_-]{2,64}$`
   - PortId: `^[a-z][a-z0-9]*(\.[a-z0-9]+)*$`
   - EventKind: `^[A-Z]+(\.[A-Z]+)*$`
   - **Ação**: Retornar erro 400 com mensagem clara

2. **Payload Type-Specific:**
   - Chat: Requer `text`
   - Command: Requer `target`
   - Event: Requer `kind` em SCREAMING_CASE
   - Artifact: Requer `manifest`
   - **Ação**: ValidationHelper.ValidateMessagePayload

3. **Permissões:**
   - DM: Respeitar visibility
   - Command: Respeitar allow_commands_from
   - Workspace: Apenas owner/orchestrator para privado
   - **Ação**: HubException com código PERM_DENIED

---

## Sumário de Ferramentas de Teste

### Testes Automatizados (.NET)

```bash
# Todos os testes
cd server-dotnet
dotnet test -c Release

# Testes específicos
dotnet test --filter "FullyQualifiedName~ValidationTests"
dotnet test --filter "FullyQualifiedName~RoomContext"
dotnet test --filter "FullyQualifiedName~SecurityTests"
dotnet test --filter "FullyQualifiedName~SessionStore"

# Com verbosidade
dotnet test -c Release -v normal

# Com coverage (requer ferramentas adicionais)
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover
```

### Testes Manuais (Postman/cURL)

**Healthcheck:**
```bash
curl -v http://localhost:5000/health
```

**Upload de Artefato:**
```bash
curl -X POST http://localhost:5000/rooms/room-test123/artifacts \
  -H "X-Entity-Id: E-test01" \
  -F "spec={\"name\":\"test.txt\",\"type\":\"text/plain\"};type=application/json" \
  -F "data=@test.txt"
```

**Promoção de Artefato:**
```bash
curl -X POST http://localhost:5000/rooms/room-test123/artifacts/promote \
  -H "X-Entity-Id: E-test01" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "test.txt",
    "fromEntity": "E-test01"
  }'
```

### Testes SignalR (JavaScript/TypeScript)

```javascript
// Exemplo de cliente SignalR
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/room")
  .build();

await connection.start();

// Join
await connection.invoke("Join", "room-test123", {
  id: "E-test01",
  kind: "human",
  displayName: "Test User"
});

// Send message
await connection.invoke("SendToRoom", "room-test123", {
  channel: "main",
  type: "chat",
  payload: { text: "Hello!" }
});

// Receive messages
connection.on("ReceiveMessage", (message) => {
  console.log("Received:", message);
});

// Leave
await connection.invoke("Leave", "room-test123", "E-test01");
```

### Validação de Schemas

```bash
cd schemas
pnpm install
pnpm validate
```

---

## Checklist de Execução de Testes

### Antes de Iniciar Testes

- [ ] Verificar que ambiente está configurado (`make verify-environment`)
- [ ] Servidor RoomServer compilado sem erros (`dotnet build`)
- [ ] MCP servers iniciados (se testar integração MCP) (`make mcp-up`)
- [ ] Diretórios de observabilidade limpos (`.ai-flow/runs/`)

### Durante Testes

- [ ] Monitorar console de logs do servidor
- [ ] Verificar arquivos gerados em `.ai-flow/runs/{roomId}/`
- [ ] Capturar screenshots/logs de erros
- [ ] Documentar comportamentos inesperados

### Após Testes

- [ ] Revisar logs de erros (se houver)
- [ ] Validar completude dos arquivos de observabilidade
- [ ] Verificar que não há processos zumbis ou connections abertas
- [ ] Limpar dados de teste (se necessário)

### Relatório de Testes

Para cada teste executado, documentar:

1. **ID do Teste**: Referência da seção (ex: 5.1)
2. **Data/Hora**: Timestamp da execução
3. **Resultado**: Passou / Falhou / Parcial
4. **Evidências**: Logs, screenshots, payloads capturados
5. **Observações**: Comportamentos notáveis, sugestões de melhoria
6. **Issues**: Links para bugs criados (se aplicável)

---

## Conclusão

Este plano de testes cobre todos os aspectos críticos do Backend/APIs do Metacore Stack, desde fundamentos de execução até operações avançadas de artefatos e integração MCP. A execução sistemática deste plano garantirá que:

- ✅ Todos endpoints e métodos funcionam conforme especificação
- ✅ Validações são aplicadas corretamente
- ✅ Permissões protegem recursos apropriadamente
- ✅ Observabilidade fornece visibilidade adequada
- ✅ Sistema é robusto e thread-safe
- ✅ Erros são tratados gracefully com mensagens claras

**Prioridade de Execução:**
1. **P0**: Fundamentos (0), Modelo de Mensagens (1), Presença/Sessões (3)
2. **P1**: Permissões (4), Hub SignalR (5), Ciclo de Vida (2)
3. **P2**: Artefatos (6), MCP (7), Observabilidade (8)
4. **P3**: Erros & Envelopes (9), Concorrência (10)

**Estimativa de Tempo:**
- Setup inicial: 30 minutos
- Testes automatizados: 1 hora
- Testes manuais (completo): 4-6 horas
- Documentação de resultados: 2 horas
- **Total**: ~8 horas para execução completa

---

**Documento gerado em**: 2025-10-19  
**Versão**: 1.0  
**Baseado em**: ROOM_HOST_IMPLEMENTATION.md e documentação do projeto
