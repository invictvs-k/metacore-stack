# Context Card: [Component Name]

> **Location:** `[path/to/component]`  
> **Type:** [Application | Service | Tool | Library]  
> **Status:** [Active | Development | Deprecated]

## Overview

**Purpose:**
[One paragraph describing what this component does and why it exists]

**Key Responsibilities:**
- [Responsibility 1]
- [Responsibility 2]
- [Responsibility 3]

## Quick Start

### Prerequisites
- [Requirement 1]
- [Requirement 2]

### How to Run

```bash
# Development
[commands to run in development mode]

# Production
[commands to run in production mode]

# Tests
[commands to run tests]
```

### Configuration

**Environment Variables:**
- `VAR_NAME`: [Description] (default: `value`)
- `OTHER_VAR`: [Description] (required)

**Configuration Files:**
- `[file.json]`: [Purpose]
- `[other.config]`: [Purpose]

## Architecture

### Inputs

**API Endpoints (if applicable):**
- `GET /endpoint`: [Description]
- `POST /endpoint`: [Description]

**Events/Messages Consumed:**
- `[Event Type]`: [Description and source]

**Files Read:**
- `[file path]`: [Purpose]

**Dependencies (External):**
- [External service 1]: [Purpose]
- [External service 2]: [Purpose]

### Outputs

**API Endpoints (if applicable):**
- `GET /endpoint`: [Description]
- `POST /endpoint`: [Description]

**Events/Messages Published:**
- `[Event Type]`: [Description and destination]

**Files Written:**
- `[file path]`: [Purpose]

**Side Effects:**
- [Database writes]
- [External API calls]

### Data Flow

```
[Simple text or ASCII diagram showing data flow]
Example:
HTTP Request → Controller → Service → Database
                    ↓
              Response ← Transform ← Result
```

## Internal Structure

### Key Directories
- `src/`: [Description]
  - `controllers/`: [Description]
  - `services/`: [Description]
  - `models/`: [Description]
- `tests/`: [Description]
- `config/`: [Description]

### Key Files
- `[important-file.ts]`: [Purpose and when to modify]
- `[another-file.ts]`: [Purpose and when to modify]

### Technology Stack
- **Language/Runtime:** [e.g., TypeScript 5.0, Node 20]
- **Framework:** [e.g., Express, React, .NET 8]
- **Database:** [if applicable]
- **Key Libraries:**
  - `[library-1]`: [Purpose]
  - `[library-2]`: [Purpose]

## Dependencies

### Internal Dependencies
- `[other/component]`: [What it's used for]
- `[another/component]`: [What it's used for]

### External Dependencies
- `[npm/nuget package]`: [Purpose]
- `[another package]`: [Purpose]

**Dependency Graph:**
```
[Simple text representation]
Example:
operator-dashboard → integration-api → room-server
                  ↘                  ↗
                    schemas (shared)
```

## Testing

### Test Structure
- `tests/unit/`: [Unit tests for...]
- `tests/integration/`: [Integration tests for...]
- `tests/e2e/`: [End-to-end tests for...]

### How to Run Tests
```bash
# All tests
[command]

# Unit tests only
[command]

# With coverage
[command]
```

### Test Coverage
- Current: [X%]
- Target: [Y%]

## Known Limits & Issues

### Performance
- [Known performance characteristic or limit]

### Scalability
- [Current scaling limits]
- [Maximum load tested]

### Technical Debt
- [Known technical debt item 1]
- [Known technical debt item 2]

### Compatibility
- [Browser/runtime compatibility notes]
- [Version constraints]

## Development Guidelines

### Code Style
- [Linter config used]
- [Style guide reference]

### Common Patterns
- [Pattern 1]: [When to use]
- [Pattern 2]: [When to use]

### Anti-Patterns
- [Anti-pattern to avoid]: [Why and what to do instead]

## Deployment

### Build Process
```bash
[Build command and process]
```

### Deployment Steps
1. [Step 1]
2. [Step 2]
3. [Step 3]

### Environment-Specific Notes
- **Development:** [Notes]
- **Staging:** [Notes]
- **Production:** [Notes]

## Monitoring & Observability

### Logs
- Location: [where logs are written]
- Format: [log format]
- Key log patterns to watch: [patterns]

### Metrics
- [Metric 1]: [What it means and healthy range]
- [Metric 2]: [What it means and healthy range]

### Health Checks
- Endpoint: `[/health]`
- What it checks: [description]

## Troubleshooting

### Common Issues

#### Issue: [Problem description]
**Symptoms:**
- [Symptom 1]
- [Symptom 2]

**Solution:**
```bash
[Commands or steps to resolve]
```

#### Issue: [Another problem]
**Symptoms:**
- [Symptom 1]

**Solution:**
- [Resolution steps]

## Useful Links

- **Documentation:**
  - [Link to main docs]
  - [Link to API docs]
  
- **Related ADRs:**
  - [ADR-XXX: Decision about this component]
  
- **External Resources:**
  - [Framework documentation]
  - [Tutorial or guide]

## Owner & Contact

- **Primary Owner:** [Name/Team]
- **Backup Contact:** [Name/Team]
- **Slack Channel:** [#channel]
- **Email:** [team email if applicable]

## Change Log

| Date | Change | Author |
|------|--------|--------|
| YYYY-MM-DD | Initial context card | [Name] |
| YYYY-MM-DD | [Significant change] | [Name] |

---

**Last Updated:** YYYY-MM-DD  
**Version:** [Component version if applicable]
