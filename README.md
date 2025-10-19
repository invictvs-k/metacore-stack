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

## Validação de Fluxos

* **Layer 3 Flows:** ✅ Validados e testados ([ver relatório](LAYER3_VALIDATION_SUMMARY.md))
  - Fluxo 3.1: Criação de Sala (5 testes)
  - Fluxo 3.2: Entrada de Entidade (8 testes)
  - 13 testes automatizados, 100% aprovação

## Licença

MIT (ajuste conforme necessidade)
