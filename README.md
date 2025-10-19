# Metacore Stack — Metaplataforma (MVP)

Mono-repo com:
- `server-dotnet/` — Room Host (.NET 8 + SignalR)
- `mcp-ts/` — MCP servers em TypeScript
- `ui/` — UI mínima (Next.js) [opcional neste ciclo]
- `schemas/` — JSON Schemas base + exemplos + validação AJV
- `infra/` — docker-compose para ambiente local

## Quickstart
```bash
# 0) verify environment (optional but recommended)
make verify-environment

# 1) preparar ferramentas
make bootstrap

# 2) subir MCP servers de exemplo
make mcp-up

# 3) rodar Room Host
make run-server

# 4) validar schemas
make schemas-validate
```

## Estrutura e Convenções

* .NET 8, Node 20, pnpm 9
* Conventional Commits
* CI: build + lint + teste + validação de schemas

## Documentação

* [**BACKEND_API_TEST_PLAN.md**](./BACKEND_API_TEST_PLAN.md) — Plano de testes detalhado para Backend/APIs
* [ROOM_HOST_IMPLEMENTATION.md](./ROOM_HOST_IMPLEMENTATION.md) — Status de implementação do Room Host
* [ENVIRONMENT_VERIFICATION.md](./ENVIRONMENT_VERIFICATION.md) — Verificação do ambiente de desenvolvimento
* [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md) — Resumo de mudanças de implementação

## Licença

MIT (ajuste conforme necessidade)
