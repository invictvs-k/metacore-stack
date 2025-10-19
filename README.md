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

### Enhanced Integration Tests (Recommended)

The enhanced integration test system provides comprehensive validation with artifact collection, performance metrics, and detailed tracing:

```bash
cd server-dotnet/operator/scripts
./run-integration-enhanced.sh
```

Features:
- Automated service orchestration with readiness checks
- Artifact collection in `.artifacts/integration/{timestamp}/`
- Performance metrics (P50/P95 latency, success rates)
- Structured trace logging (NDJSON format)
- Comprehensive JSON and text reports

See [Enhanced Integration Testing Guide](server-dotnet/operator/docs/ENHANCED_INTEGRATION_TESTING.md) for details.

### Quick Automated Test

```bash
# Quick automated test (builds, starts services, runs tests)
cd server-dotnet/operator/scripts
./run-integration-test.sh
```

### Manual Component Testing

```bash
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
- `npm run test:basic-enhanced` - Enhanced basic flow with trace logging
- `npm run test:error` - Error handling and validation
- `npm run test:stress` - Performance and load testing

**Documentation:**
- [Enhanced Integration Testing](server-dotnet/operator/docs/ENHANCED_INTEGRATION_TESTING.md) - Comprehensive test system with artifacts and metrics
- [Integration Guide](docs/ROOMOPERATOR_ROOMSERVER_INTEGRATION.md) - Complete API reference, communication flows, and error handling
- [Testing Guide](docs/TESTING.md) - Detailed testing instructions and troubleshooting
- [RoomOperator Docs](docs/room-operator.md) - Operator architecture and usage
- [Test Client README](server-dotnet/operator/test-client/README.md) - Test client usage and customization

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
