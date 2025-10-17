# Schemas – Metaplataforma (Sala Viva)

Este diretório contém os 4 contratos canônicos do runtime:
- `room.schema.json` — ciclo de vida e configuração da Sala
- `entity.schema.json` — modelo de Entidade (humano/IA/NPC/orquestrador)
- `message.schema.json` — protocolo de mensagens (chat/command/event/artifact)
- `artifact-manifest.schema.json` — manifestos de artefatos (mesa da Sala/Entidade)

## Convenções
- Draft 2020-12
- `$id` estável e `$metadata.semver` para versionamento
- Reúso via `common.defs.json` (RoomId, EntityId, PortId, Origin etc.)

## Validação
```bash
pnpm i
pnpm validate
```

## Evolução (por sprint)

Os schemas são imutáveis durante um sprint.

Extensões devem ir em x-extensions até a próxima versão.
