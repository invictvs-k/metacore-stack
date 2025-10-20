# Context Card: Operator Dashboard

> **Location:** `apps/operator-dashboard`  
> **Type:** Application  
> **Status:** Active

## Overview

**Purpose:**
Web-based control and observability dashboard for monitoring and managing RoomServer, RoomOperator, and Test Client components. Provides real-time event streaming, test execution, command orchestration, and configuration management through a modern React-based interface.

**Key Responsibilities:**
- Real-time monitoring of system health and event streams
- Test execution with live log streaming and artifact collection
- Command orchestration with JSON Schema validation
- Configuration management with hot-reload support
- SSE (Server-Sent Events) connection management with resilient reconnection

## Quick Start

### Prerequisites
- Node.js 18+
- Integration API running on port 40901
- RoomServer running on port 40801
- RoomOperator running on port 40802

### How to Run

```bash
# Development
npm install
npm run dev
# Access at http://localhost:5173

# Production
npm run build
npm run preview

# Tests
npm test
```

### Configuration

**Environment Variables:**
Configuration is managed through the Integration API (`/api/config`).

**Configuration Files:**
- `vite.config.ts`: Build and dev server configuration
- Development proxy configured for `/api` → `http://localhost:40901`

## Architecture

### Inputs

**API Endpoints:**
- `GET /api/config`: Dashboard configuration
- `GET /api/config/version`: Configuration version for hot-reload
- `GET /api/health/*`: Health check endpoints
- `POST /api/tests/run`: Test execution
- `POST /api/commands/execute`: Command execution
- `GET /api/mcp/status`: MCP status

**Events/Messages Consumed:**
- SSE from `/api/events/roomserver`: RoomServer event stream
- SSE from `/api/events/roomoperator`: RoomOperator event stream
- SSE from `/api/tests/stream/:runId`: Test execution logs

**Dependencies (External):**
- Integration API (port 40901): Backend service for all API operations
- RoomServer (port 40801): Event stream source
- RoomOperator (port 40802): Event stream source

### Outputs

**User Interface:**
- Overview page: System health and quick actions
- Events page: Real-time event streaming with filtering
- Tests page: Test scenario execution and monitoring
- Commands page: Command catalog and execution
- Settings page: Configuration editor with validation
- About page: Documentation and version info

**Side Effects:**
- HTTP requests to Integration API for configuration, tests, commands
- SSE connections to event streams
- Local storage for theme preference

### Data Flow

```
User Interaction → React Components → Custom Hooks → API/SSE
                                         ↓
                                     Zustand Store
                                         ↓
                                   Component Re-render
```

## Internal Structure

### Key Directories
- `src/`: Main application source
  - `pages/`: Page components (Overview, Events, Tests, Commands, Settings, About)
  - `components/`: Reusable UI components (Layout, EventCard, TestRunner, etc.)
  - `hooks/`: Custom React hooks (useSSE, useConfig, useTestRunner)
  - `store/`: Zustand state management
  - `types/`: TypeScript type definitions
  - `utils/`: Utility functions

### Key Files
- `src/App.tsx`: Main application component with routing
- `src/hooks/useSSE.ts`: SSE connection management with reconnection logic
- `src/hooks/useConfig.ts`: Configuration management with auto-refresh
- `src/hooks/useTestRunner.ts`: Test execution and result streaming
- `src/store/index.ts`: Global state (theme, runId, event history)
- `vite.config.ts`: Vite configuration with API proxy

### Technology Stack
- **Language/Runtime:** TypeScript 5.0, Node 18+
- **Framework:** React 18
- **Build Tool:** Vite
- **Styling:** TailwindCSS
- **State Management:** Zustand
- **Data Fetching:** SWR
- **Routing:** React Router
- **Icons:** Lucide React

## Dependencies

### Internal Dependencies
- `tools/integration-api`: Backend API for all operations
- `schemas/`: JSON schemas for validation

### External Dependencies
- `react` & `react-dom`: UI framework
- `react-router-dom`: Client-side routing
- `zustand`: Lightweight state management
- `swr`: Data fetching and caching
- `lucide-react`: Icon library
- `tailwindcss`: CSS framework

**Dependency Graph:**
```
operator-dashboard → integration-api → room-server
                  ↘                  ↗  room-operator
                    schemas (shared)
```

## Testing

### Test Structure
Currently minimal test infrastructure. Tests should be added for:
- `tests/unit/`: Unit tests for hooks and components
- `tests/integration/`: Integration tests for user flows
- `tests/e2e/`: End-to-end tests

### How to Run Tests
```bash
# All tests
npm test

# With coverage
npm run test:coverage
```

### Test Coverage
- Current: Low (infrastructure exists, coverage needs improvement)
- Target: 70%+

## Known Limits & Issues

### Performance
- Event history is windowed to 2000 events to prevent memory issues
- SSE connections can accumulate if not properly cleaned up
- Large log streams may impact browser performance

### Scalability
- Designed for single-user/operator use
- Not tested with multiple simultaneous users
- Event windowing prevents indefinite memory growth

### Technical Debt
- Test coverage needs improvement
- Some components could be better decomposed
- Error handling could be more granular

### Compatibility
- Requires EventSource (SSE) support
- Modern browsers only: Chrome/Edge 90+, Firefox 88+, Safari 14+

## Development Guidelines

### Code Style
- ESLint configuration in `.eslintrc.cjs`
- TypeScript strict mode enabled
- Follow React hooks best practices

### Common Patterns
- **Custom Hooks**: Encapsulate complex logic (SSE, config, test runner)
- **SWR for Fetching**: Use SWR for GET requests with caching
- **Zustand for State**: Use Zustand for global state (theme, runId, events)
- **Component Composition**: Build complex UIs from smaller components

### Anti-Patterns
- Don't fetch data directly in components: Use custom hooks or SWR
- Don't store ephemeral data in Zustand: Use local state or SWR cache
- Don't create SSE connections without cleanup: Always use useEffect cleanup

## Deployment

### Build Process
```bash
npm run build
# Output: dist/
```

### Deployment Steps
1. Build the application: `npm run build`
2. Serve the `dist/` folder with a static file server
3. Ensure API proxy is configured at deployment (e.g., nginx)
4. Verify Integration API is accessible

### Environment-Specific Notes
- **Development:** Uses Vite dev server with HMR and API proxy
- **Staging:** Build and serve with static file server, configure proxy
- **Production:** Same as staging with production API endpoints

## Monitoring & Observability

### Logs
- Location: Browser console
- Format: Console logs from React and custom hooks
- Key log patterns: SSE connection status, API errors, test execution

### Metrics
- Not currently collected (could add analytics/monitoring)

### Health Checks
- Visual health indicators on Overview page
- Connection status shown for each service

## Troubleshooting

### Common Issues

#### Issue: API Connection Failed
**Symptoms:**
- Dashboard shows connection errors
- No data loads

**Solution:**
```bash
# Ensure Integration API is running
cd tools/integration-api
npm run dev
```

#### Issue: SSE Connection Errors
**Symptoms:**
- Events not streaming
- Reconnection loops

**Solution:**
- Check RoomServer and RoomOperator are running at configured URLs
- Verify no CORS issues (only localhost:5173 allowed by default)
- Check browser console for detailed error messages

#### Issue: Build Errors
**Symptoms:**
- Build fails with dependency errors

**Solution:**
```bash
rm -rf node_modules dist
npm install
npm run build
```

## Useful Links

- **Documentation:**
  - [Dashboard README](./README.md)
  - [Integration API](../../tools/integration-api/README.md)
  
- **Related ADRs:**
  - Check `docs/_adr/` for relevant architectural decisions
  
- **External Resources:**
  - [React Documentation](https://react.dev)
  - [Vite Documentation](https://vitejs.dev)
  - [TailwindCSS Documentation](https://tailwindcss.com)

## Owner & Contact

- **Primary Owner:** Development Team
- **Slack Channel:** TBD
- **Repository:** invictvs-k/metacore-stack

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2025-10-20 | Initial context card created | AI Agent |

---

**Last Updated:** 2025-10-20  
**Version:** 1.0
