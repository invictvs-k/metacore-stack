# Metacore Stack — Metaplataforma (MVP)

[![CI](https://github.com/invictvs-k/metacore-stack/workflows/ci/badge.svg)](https://github.com/invictvs-k/metacore-stack/actions/workflows/ci.yml)
[![PR Validation](https://github.com/invictvs-k/metacore-stack/workflows/pr-validation/badge.svg)](https://github.com/invictvs-k/metacore-stack/actions/workflows/pr-validation.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Node.js](https://img.shields.io/badge/Node.js-20.x-339933?logo=node.js)](https://nodejs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.3-3178C6?logo=typescript)](https://www.typescriptlang.org/)

> **Technical Report**: See [ci/tech-report.md](ci/tech-report.md) for detailed build status, test results, and pipeline observability.

Mono-repo com:
- `server-dotnet/` — Room Host (.NET 8 + SignalR) + RoomOperator
- `mcp-ts/` — MCP servers em TypeScript
- `apps/operator-dashboard/` — Operator Dashboard (Vite/React)
- `tools/integration-api/` — Integration API (Express/TypeScript)
- `ui/` — UI mínima (Next.js) [opcional neste ciclo]
- `schemas/` — JSON Schemas base + exemplos + validação AJV
- `infra/` — docker-compose para ambiente local
- `docs/` — Documentation (see [Table of Contents](docs/TOC.md))

## Port Configuration

All components use standardized ports for easy integration:
- **RoomServer**: 40801
- **RoomOperator**: 40802
- **Integration API**: 40901
- **Dashboard UI**: 5173

See [PORT_CONFIGURATION.md](PORT_CONFIGURATION.md) for detailed setup and configuration.

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

## CI/CD Pipeline & Development Workflow

### Local Development Commands

```bash
# Build all projects
npm run build        # Node/TypeScript projects
dotnet build -c Release server-dotnet/RoomServer.sln  # .NET projects

# Run tests
npm test            # Schemas + contract validation
dotnet test server-dotnet/RoomServer.sln  # .NET tests

# Code quality
npm run lint        # ESLint
npm run format      # Prettier check
npm run format:fix  # Prettier write
dotnet format server-dotnet/RoomServer.sln  # .NET format

# Type checking
npm run typecheck   # TypeScript
```

### Observability & Testing

```bash
# Test SSE heartbeat endpoint
npm run smoke:stream

# Validate contracts
npm run test:contracts

# Start Integration API with structured logging
cd tools/integration-api
npm run dev
```

**Key Features:**
- ✅ Structured JSON logging with traceId/runId
- ✅ SSE endpoints with heartbeat monitoring
- ✅ OpenAPI 3.1 specification with validation
- ✅ Contract-based testing for all schemas
- ✅ Reproducible builds with pinned SDK versions

See [ci/tech-report.md](ci/tech-report.md) for detailed technical documentation.

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

## Validação de Fluxos

* **Layer 3 Flows:** ✅ Validados e testados ([ver relatório](docs/_deprecated/LAYER3_VALIDATION_SUMMARY.md))
  - Fluxo 3.1: Criação de Sala (5 testes)
  - Fluxo 3.2: Entrada de Entidade (8 testes)
  - Cenários adicionais: 2 testes
  - 15 testes automatizados, 100% aprovação

## Documentação

### 📚 Quick Navigation

- **[Table of Contents](docs/TOC.md)** - Complete documentation index organized by category
- **[Glossary](docs/glossary.md)** - Domain terms and concepts reference
- **[Getting Started](QUICKSTART.md)** - Quick start guide for the operator dashboard
- **[Development Setup](DEVELOPMENT_SETUP.md)** - Development environment setup
- **[Port Configuration](PORT_CONFIGURATION.md)** - Port assignments and configuration
- **[Testing Guide](docs/TESTING.md)** - Comprehensive testing instructions
- **[Integration Guide](docs/ROOMOPERATOR_ROOMSERVER_INTEGRATION.md)** - RoomOperator and RoomServer integration

### 🏗️ Architecture & Decisions

- **[Architecture Decisions (ADRs)](docs/_adr/)** - Architecture Decision Records
  - [ADR Template](docs/_adr/000-template.md) - Template for new ADRs
- **[Concept Definition](CONCEPTDEFINITION.md)** - Core concepts and architecture
- **[Room Operator](docs/room-operator.md)** - Operator architecture and usage
- **[MCP Connection Behavior](docs/MCP_CONNECTION_BEHAVIOR.md)** - MCP connection patterns

### 🤖 Agent Resources

Resources for AI coding agents and automation:

- **[Agent Briefs](docs/agent/briefs/)** - Guide for writing focused task briefs
- **[Agent Playbooks](docs/agent/playbooks/)** - Guide for multi-step workflows
- **[Templates](docs/agent/templates/)** - Brief, playbook, and context card templates

### 📖 Component Context

Quick-start guides for key components:

- **[Operator Dashboard Context](apps/operator-dashboard/CONTEXT.md)** - Dashboard component guide
- **[Integration API Context](tools/integration-api/CONTEXT.md)** - API component guide

### 📋 Operational Guides

- **[Runbooks](docs/runbooks/)** - Operational procedures and troubleshooting
- **[Interfaces](docs/interfaces/)** - Machine-readable API contracts and specifications

### 📊 Reports & Analysis

- **[Layer 3 Flow Validation](reports/LAYER3_FLOW_VALIDATION.md)** - Flow validation results
- **[Schema-RoomServer Alignment](reports/schema-roomserver-alignment.md)** - Alignment report
- **[Documentation Curation Report](docs/curation-report.md)** - Documentation organization summary

### 🗂️ Repository Structure

```
metacore-stack/
├── apps/
│   └── operator-dashboard/     # Operator Dashboard (Vite/React)
│       └── CONTEXT.md          # Component context card
├── tools/
│   └── integration-api/        # Integration API (Express/TypeScript)
│       └── CONTEXT.md          # Component context card
├── server-dotnet/
│   ├── src/RoomServer/         # Room Server (.NET 8)
│   └── operator/               # Room Operator
├── docs/                       # Active documentation
│   ├── TOC.md                  # Table of Contents
│   ├── glossary.md             # Glossary of terms
│   ├── curation-report.md      # Curation report
│   ├── docs.manifest.json      # Documentation inventory
│   ├── _adr/                   # Architecture Decision Records
│   ├── _deprecated/            # Deprecated docs (historical reference)
│   ├── _archive/               # Archived docs
│   ├── interfaces/             # API contracts and specifications
│   ├── runbooks/               # Operational runbooks
│   └── agent/                  # Agent scaffolding
│       ├── briefs/             # Brief writing guide
│       ├── playbooks/          # Playbook writing guide
│       └── templates/          # Document templates
├── schemas/                    # JSON Schemas (domain objects)
├── configs/                    # Configuration files
│   └── schemas/                # Configuration schemas
├── infra/                      # Infrastructure (docker-compose)
└── scripts/                    # Utility scripts
```

### 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Commit conventions (Conventional Commits)
- Pull request requirements
- Build and test expectations
- Schema versioning guidelines

## Licença

MIT (ajuste conforme necessidade)
