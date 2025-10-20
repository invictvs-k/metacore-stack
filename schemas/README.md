# Schemas – Meta-Platform (Living Room)

This directory contains the 4 canonical runtime contracts:

- `room.schema.json` — Room lifecycle and configuration
- `entity.schema.json` — Entity model (human/AI/NPC/orchestrator)
- `message.schema.json` — Message protocol (chat/command/event/artifact)
- `artifact-manifest.schema.json` — Artifact manifests (Room/Entity desk)

## Conventions

- Draft 2020-12
- Stable `$id` and `$metadata.semver` for versioning
- Reuse via `common.defs.json` (RoomId, EntityId, PortId, Origin, etc.)

## Validation

```bash
pnpm i
pnpm validate
```

## Evolution (per sprint)

Schemas are immutable during a sprint.

Extensions must go in x-extensions until the next version.
