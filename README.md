# Metacore Stack — Metaplataforma (MVP)

Mono-repo com:
- `server-dotnet/` — Room Host (.NET 8 + SignalR) + RoomOperator
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

## RoomOperator Integration Testing

Test the complete integration between RoomOperator and RoomServer:

```bash
# Quick automated test (builds, starts services, runs tests)
cd server-dotnet/operator/scripts
./run-integration-test.sh

# Or run components manually:
# Terminal 1: Start RoomServer
cd server-dotnet/src/RoomServer
dotnet run

# Terminal 2: Start RoomOperator
cd server-dotnet/operator
dotnet run

# Terminal 3: Run test client
cd server-dotnet/operator/test-client
npm install
npm run test:all
```

**Available test scenarios:**
- `npm run test:basic` - Complete happy path (entities → artifacts → policies)
- `npm run test:error` - Error handling and validation
- `npm run test:stress` - Performance and load testing

**Documentation:**
- [Integration Guide](docs/ROOMOPERATOR_ROOMSERVER_INTEGRATION.md) - Complete API reference, communication flows, and error handling
- [Testing Guide](docs/TESTING.md) - Detailed testing instructions and troubleshooting
- [RoomOperator Docs](docs/room-operator.md) - Operator architecture and usage
- [Test Client README](server-dotnet/operator/test-client/README.md) - Test client usage and customization

## Estrutura e Convenções

* .NET 8, Node 20, pnpm 9
* Conventional Commits
* CI: build + lint + teste + validação de schemas

## Licença

MIT (ajuste conforme necessidade)
