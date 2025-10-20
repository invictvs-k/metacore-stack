# Development Runbook

## Prerequisites

- **Node.js**: 20.x or later
- **.NET SDK**: 8.0.x
- **pnpm**: 9.x (for MCP servers)
- **Git**: 2.x or later

## Initial Setup

### 1. Clone the Repository

```bash
git clone https://github.com/invictvs-k/metacore-stack.git
cd metacore-stack
```

### 2. Install Dependencies

```bash
# Install root dependencies
npm install

# Install integration-api dependencies
cd tools/integration-api && npm install && cd ../..

# Install dashboard dependencies
cd apps/operator-dashboard && npm install && cd ../..

# Install schema dependencies
cd schemas && npm install && cd ..

# Install MCP server dependencies
cd mcp-ts && pnpm install && cd ..

# Restore .NET dependencies
cd server-dotnet && dotnet restore && cd ..
```

Or use the convenience script:

```bash
npm run install:all
```

### 3. Setup Git Hooks

Git hooks are automatically installed via husky when you run `npm install`. If needed, you can manually trigger:

```bash
npm run prepare
```

## Common Development Commands

### Building

```bash
# Build all Node/TypeScript projects
npm run build

# Build integration API only
npm run build:api

# Build dashboard only
npm run build:dashboard

# Build .NET projects
cd server-dotnet && dotnet build -c Release

# Build MCP servers
cd mcp-ts && pnpm -r -F "*" build
```

### Running Services

```bash
# Start integration API (development mode with hot reload)
npm run api:dev

# Start operator dashboard (development mode)
npm run dashboard:dev

# Start both in parallel
npm run dev:parallel

# Start RoomServer (.NET)
cd server-dotnet/src/RoomServer
dotnet run

# Start RoomOperator (.NET)
cd server-dotnet/operator
dotnet run
```

### Testing

```bash
# Run all tests
npm test

# Run specific test suites
npm run test:schemas      # Schema validation
npm run test:contracts    # Contract validation
npm run test:smoke        # Smoke tests (build validation)
npm run test:contract     # Full contract test suite
npm run test:e2e          # E2E tests (requires services running)

# Run .NET tests
cd server-dotnet
dotnet test
```

### Code Quality

```bash
# Lint JavaScript/TypeScript
npm run lint

# Format check
npm run format

# Format fix (auto-fix)
npm run format:fix

# Format .NET code
cd server-dotnet
dotnet format
```

### Type Checking

```bash
# Check all TypeScript projects
npm run typecheck

# Check specific projects
npm run typecheck:api
npm run typecheck:dashboard
```

### Security

```bash
# Run security audit
npm run security:audit

# View audit results
cat .artifacts/security/security-report.md
```

## Development Workflow

### 1. Create a Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/your-fix-name
```

### 2. Make Changes

Edit files as needed. The pre-commit hook will automatically:

- Run prettier on staged files
- Run eslint on TypeScript files
- Verify dotnet format on C# files

### 3. Commit Changes

We use **Conventional Commits** format:

```bash
# Using commitizen (interactive)
npm run commit

# Or manually with the format:
git commit -m "feat: add new feature"
git commit -m "fix: resolve bug in component"
git commit -m "docs: update documentation"
git commit -m "test: add test cases"
git commit -m "chore: update dependencies"
```

**Commit Types:**

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `perf`: Performance improvements
- `ci`: CI/CD changes

### 4. Push Changes

```bash
git push origin your-branch-name
```

### 5. Create Pull Request

- Go to GitHub and create a pull request
- Fill in the PR template
- Wait for CI checks to pass
- Request review from team members

## Repository Structure

```
metacore-stack/
├── apps/
│   └── operator-dashboard/     # React dashboard (Vite)
├── tools/
│   └── integration-api/        # Express API (TypeScript)
├── server-dotnet/
│   ├── src/RoomServer/         # .NET 8 SignalR server
│   └── operator/               # RoomOperator service
├── mcp-ts/
│   └── servers/                # MCP servers (TypeScript)
├── schemas/                    # JSON schemas
├── tests/
│   ├── smoke/                  # Smoke tests
│   ├── contract/               # Contract tests
│   └── e2e/                    # End-to-end tests
├── docs/                       # Documentation
│   ├── runbooks/               # Operational guides
│   ├── agent/                  # Agent briefs and playbooks
│   └── interfaces/             # API contracts
├── scripts/                    # Utility scripts
├── infra/                      # Infrastructure configs
└── ci/                         # CI reports
```

## Port Configuration

All services use standardized ports:

| Service            | Port  | URL                    |
| ------------------ | ----- | ---------------------- |
| RoomServer         | 40801 | http://localhost:40801 |
| RoomOperator       | 40802 | http://localhost:40802 |
| Integration API    | 40901 | http://localhost:40901 |
| Operator Dashboard | 5173  | http://localhost:5173  |

See [PORT_CONFIGURATION.md](../../PORT_CONFIGURATION.md) for details.

## Troubleshooting

### Build Failures

```bash
# Clean node_modules and reinstall
rm -rf node_modules package-lock.json
npm install

# Clean .NET build
cd server-dotnet
dotnet clean
dotnet restore
dotnet build
```

### Pre-commit Hook Issues

```bash
# Reinstall husky
npm run prepare

# Manually format files
npm run format:fix
```

### Test Failures

```bash
# Check if services are running
curl http://localhost:40901/health

# View detailed test output
npm run test:smoke -- --verbose
```

### Port Conflicts

```bash
# Find process using a port
lsof -i :40901

# Kill process
kill -9 <PID>
```

## Environment Variables

Create `.env` files for local configuration:

### Integration API (.env in tools/integration-api/)

```env
PORT=40901
NODE_ENV=development
LOG_LEVEL=debug
```

### RoomServer (.env in server-dotnet/src/RoomServer/)

```env
ASPNETCORE_URLS=http://localhost:40801
ASPNETCORE_ENVIRONMENT=Development
```

## IDE Setup

### VS Code

Recommended extensions are defined in `.vscode/extensions.json`:

- ESLint
- Prettier
- C# Dev Kit
- EditorConfig

Settings are in `.vscode/settings.json`.

### JetBrains Rider

- Enable EditorConfig support
- Configure code style to use project settings
- Enable .NET format on save

## Further Reading

- [Testing Guide](../TESTING.md)
- [Integration Guide](../ROOMOPERATOR_ROOMSERVER_INTEGRATION.md)
- [Contributing Guide](../../CONTRIBUTING.md)
- [Agent Execution Guide](./agent-execution.md)
