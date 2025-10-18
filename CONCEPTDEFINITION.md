# 🧠 Metacore Stack — Especificação Funcional

### (Versão 1.0)

---

## 1. Visão Geral

A Metacore Stack é um ambiente de execução colaborativo que permite que **humanos e agentes de IA** coexistam, interajam e trabalhem sobre **artefatos compartilhados**, de forma coordenada e persistente.

A ideia central é simples:

> “Uma Sala é um espaço vivo onde entidades (humanas ou artificiais) entram, interagem, produzem e transformam artefatos, usando recursos conectados, com governança e rastreabilidade total.”

O sistema é **agnóstico de linguagem e tecnologia de IA**.  
Um agente Python, um humano no navegador, e um orquestrador em .NET podem coexistir na mesma Sala — todos agindo por meio de interfaces e protocolos padronizados.

---

## 2. Conceito de Sala (Room)

### O que é

Uma **Sala** é o ambiente lógico e de execução onde o trabalho acontece.  
Pense nela como um **“servidor de jogo colaborativo”**:

- tem um **ciclo de vida** (`init → active → paused → ended`),
    
- mantém **recursos, entidades, artefatos e políticas**,
    
- e permanece viva até ser encerrada.
    

### Função

A Sala:

- gerencia o **estado global** (quem está presente, que recursos estão ativos, que artefatos existem);
    
- propaga **mensagens e eventos em tempo real** entre os membros;
    
- armazena e versiona **artefatos produzidos**;
    
- aplica **políticas de segurança e governança**;
    
- registra **telemetria e histórico** de tudo o que ocorreu.
    

### Exemplo

Imagine uma Sala chamada `room-ai-workflow`.  
Nela estão:

- Marcelo (humano),
    
- o Agente `TextRefiner`,
    
- e o Orquestrador `StageManager`.
    

Marcelo envia um arquivo Markdown.  
O `TextRefiner` o lê, melhora a clareza, e grava uma nova versão.  
O `StageManager` detecta o evento `ARTIFACT.ADDED` e dispara a próxima tarefa.  
Tudo isso ocorre **dentro da Sala**, com logs e versionamento automático.

---

## 3. Entidades (Entities)

### O que são

**Entidades** são os membros da Sala.  
Elas representam tanto **pessoas humanas** quanto **agentes de IA**, **processos automatizados** ou **NPCs (entidades reativas)**.

Cada entidade:

- tem um **ID**, um **tipo** (`human`, `agent`, `npc`, `orchestrator`),
    
- possui **capacidades** (ports/funções que sabe executar),
    
- obedece a **políticas** (quem pode comandá-la, o que pode acessar),
    
- e pode ter um **workspace próprio** (sua “mesa de trabalho”).
    

### Função

As Entidades são **os atores**.  
Tudo o que acontece na Sala parte de uma Entidade —  
toda mensagem, artefato ou ação tem um `from` e, opcionalmente, um `to`.

### Exemplo

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

Este agente aceita comandos para gerar e revisar textos, e só o orquestrador pode dar instruções diretas a ele.

---

## 4. Workspaces e Artefatos

### O que são

Os **Workspaces** são as “mesas” de trabalho.  
Há dois níveis:

- **Workspace da Sala**: espaço compartilhado, visível a todos.
    
- **Workspace da Entidade**: espaço privado, visível só a quem o possui (salvo se promovido).
    

**Artefatos** são os arquivos, textos, dados ou outputs criados pelas entidades.  
Cada artefato possui um **manifesto** (`artifact-manifest.json`) com:

- nome, tipo (ex: `doc/markdown`, `app/json`);
    
- origem (sala, entidade, port);
    
- hash SHA256 e versionamento;
    
- metadados e timestamp.
    

### Função

Os Workspaces permitem:

- isolamento controlado;
    
- versionamento transparente;
    
- reconstrução e auditoria de resultados.
    

### Exemplo de fluxo

1. Marcelo (E-H1) faz upload de `input.txt`.
    
2. O Agente `TextRefiner` gera `output_refined.txt`.
    
3. O Orquestrador lê o evento e envia o resultado para revisão.
    
4. Todos os arquivos ficam na “mesa” da Sala, versionados e rastreáveis.
    

---

## 5. Mensageria e Comunicação

### O que é

O **Bus da Sala** é o sistema de mensagens em tempo real.  
Baseado em **SignalR (WebSocket)**, ele conecta todas as entidades e propaga mensagens do tipo:

- `chat` — comunicação livre/humana;
    
- `command` — instrução formal de execução;
    
- `event` — evento do sistema ou da entidade;
    
- `artifact` — notificação sobre novo ou alterado artefato.
    

### Função

É o **coração da Sala**.  
Tudo o que acontece é comunicado via mensagens —  
isso permite que humanos, agentes e orquestradores compartilhem o mesmo canal.

### Exemplo (mensagem `command`)

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
    "inputs": { "text": "Otimize este texto." }
  }
}
```

O agente `E-AGENT-1` recebe a mensagem e executa o port `text.generate`.

---

## 6. Ports e Capabilities

### O que são

**Ports** são contratos de função padronizados — definem o que uma entidade _sabe fazer_.

Exemplo:

- `text.generate` — recebe texto e parâmetros, retorna nova versão.
    
- `review` — analisa e dá feedback.
    
- `plan` — elabora plano de tarefas.
    
- `search.web` — executa pesquisa via recurso MCP.
    

### Função

Os Ports transformam agentes e humanos em **módulos intercambiáveis**.  
Qualquer entidade pode anunciar seus ports e ser chamada por outro componente.

### Exemplo (adapter)

Um `text.generate` pode ser implementado por:

- um agente local via API OpenAI,
    
- um humano revisando texto manualmente,
    
- um serviço externo plugado via MCP.
    

Todos seguem o mesmo contrato de entrada/saída.

---

## 7. Recursos (Resources) e MCP

### O que são

**Recursos** são as ferramentas externas disponíveis na Sala.  
Eles podem ser:

- repositórios Git,
    
- APIs HTTP,
    
- mecanismos de busca,
    
- bancos de dados,
    
- ferramentas de conversão, etc.
    

São expostos via **MCP (Model Context Protocol)** —  
um padrão aberto que permite conectar ferramentas por WebSocket/JSON-RPC.

### Função

Os Recursos expandem o “alcance” da Sala —  
as Entidades podem consultar dados, enviar requisições e buscar conhecimento fora do ambiente, com segurança e controle.

### Exemplo

Um MCP Server `web.search` (em TypeScript) expõe:

```json
{
  "tools": [{
    "id": "web.search",
    "title": "Search the Web",
    "inputSchema": { "q": "string", "limit": "number" },
    "outputSchema": { "items": "array" }
  }]
}
```

A Entidade na Sala chama:

```json
{ "toolId": "web.search", "args": { "q": "agentes cognitivos", "limit": 3 } }
```

E recebe uma lista de resultados.  
Tudo registrado, versionado e auditado.

---

## 8. Orquestradores e Tasks

### O que são

Os **Orquestradores** são Entidades especiais que possuem “scripts” de coordenação — chamados **Tasks**.  
Esses scripts definem:

- **comandos sequenciais ou condicionais**;
    
- **dependências entre tarefas**;
    
- **checkpoints de validação humana**;
    
- **resultados esperados**.
    

### Função

Eles transformam a Sala em um **ambiente de execução programável**.  
Ao invés de escrever um fluxo rígido de código, você escreve um JSON que descreve o trabalho — e o Orquestrador executa.

### Exemplo (Task Script simplificado)

```json
{
  "name": "Refinar Documento",
  "steps": [
    {
      "task": "gerar_texto",
      "target": "E-AGENT-1",
      "port": "text.generate",
      "inputs": { "text": "draft.md", "guidance": "clareza e fluidez" },
      "output": "refined.md"
    },
    {
      "task": "revisar",
      "target": "E-HUMAN-1",
      "port": "review",
      "inputs": { "artifact": "refined.md" },
      "checkpoint": "aguardar_aprovação"
    }
  ]
}
```

O Orquestrador executa passo a passo, aguardando confirmações e publicando eventos (`TASK.START`, `TASK.END`, `CHECKPOINT.REACHED`).

---

## 9. Policies e Governança

### O que são

**Policies** são regras de segurança e governança aplicadas em tempo real:

- quem pode enviar comandos a quem,
    
- quais recursos cada entidade pode acessar,
    
- quantas vezes pode usar um tool (rate-limit),
    
- o que pode ser logado ou mascarado (PII).
    

### Função

Garantem **controle e conformidade**, sem bloquear a fluidez do trabalho.  
São aplicadas pelo Host da Sala, e registradas nos manifests e logs de telemetria.

### Exemplo

```json
"policy": {
  "allow_commands_from": "orchestrator",
  "scopes": ["net:github.com", "net:*.openai.com"],
  "rateLimit": { "perMinute": 30 }
}
```

---

## 10. Telemetria e Histórico

### O que é

Todo evento gerado na Sala é gravado em:

- `events.jsonl` — log contínuo de eventos;
    
- `room-run.json` — resumo consolidado (entidades, artefatos, duração);
    
- e opcionalmente enviado via **OpenTelemetry** para observabilidade em tempo real.
    

### Função

Permite:

- rastreabilidade completa (quem fez o quê, quando e com o quê);
    
- auditoria e replay de execuções passadas;
    
- aprendizado e ajuste de fluxos.
    

### Exemplo (linha de log)

```json
{"ts":"2025-10-17T12:10:03Z","event":"RESOURCE.CALLED","room":"room-ai-workflow","entity":"E-AGENT-1","tool":"web.search","args":{"q":"Azure AI"}}
```

---

## 11. Potenciais e Extensões

### a) Metaplataforma universal

Por ser baseada em **protocolos**, a Sala pode integrar:

- Agentes Python (LangGraph, Agno, AutoGen);
    
- Orquestradores .NET (Orleans);
    
- UI/Apps web (Next.js);
    
- Recursos MCP escritos em qualquer linguagem.
    

### b) Ambientes híbridos

Uma Sala pode ser aberta para múltiplos humanos e agentes simultaneamente, tornando-se um **espaço de trabalho cognitivo colaborativo** — híbrido humano+IA.

### c) Reaproveitamento

Cada **Stage** de um projeto maior é apenas uma **Sala encapsulada**, com entrada e saída definidas, permitindo reuso como módulos em pipelines mais amplos.

### d) Aplicações futuras

- **Reuniões cognitivas persistentes**: times humanos + IAs trabalhando com contexto contínuo.
    
- **Ambientes de desenvolvimento orientados a IA**: agentes refatorando, testando e versionando código em tempo real.
    
- **Assistentes multiagente em domínios específicos**: jurídico, médico, educacional, criativo.
    
- **Governança autônoma**: políticas dinâmicas adaptando-se ao contexto e performance das entidades.
    

---

## 12. Exemplo funcional completo

1. **Sala criada**  
    `POST /rooms → {"id":"room-abc123","state":"active"}`
    
2. **Entidades entram**
    
    - Humano (E-H1) via UI.
        
    - Agente de IA (E-A1) via WS.
        
    - Orquestrador (E-ORC) via script.
        
3. **Artefato enviado**  
    Marcelo envia `texto_original.md`.
    
4. **Orquestrador envia comando**  
    `E-ORC` → `E-A1` (`port=text.generate`, input: `texto_original.md`).
    
5. **Agente produz saída**  
    `E-A1` grava `texto_refinado.md`.
    
6. **Orquestrador pede revisão**  
    `E-ORC` → `E-H1` (`port=review`, input: `texto_refinado.md`).
    
7. **Marcelo aprova**  
    Artefato é marcado como final.
    
8. **Sala finaliza**  
    Todos os artefatos, eventos e manifests são salvos.  
    `room-run.json` resume o histórico da sessão.
    

---

## 13. Potencial Estratégico

A Metaplataforma é um **framework universal de colaboração cognitiva**.

Ela não é “mais um sistema de chat com IA”.  
É uma **infraestrutura de trabalho vivo**, onde:

- cada projeto pode se tornar uma rede de salas e stages;
    
- cada sala pode ser reaberta, auditada e reutilizada;
    
- e cada entidade (humana ou IA) é interoperável via contratos.
    

Em outras palavras:

> A Sala Viva é o que transforma o trabalho com IA de algo episódico (prompts e respostas) em algo contínuo, governado e evolutivo.

---
