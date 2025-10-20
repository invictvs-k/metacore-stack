# Agent Execution Runbook

This guide explains how AI coding agents interact with this repository, execute tasks, and maintain automation.

## Overview

The repository is designed to be agent-friendly with:

- **Structured briefs** for focused tasks
- **Playbooks** for multi-step workflows
- **Automated triggers** for agent runs
- **Standardized outputs** and reports

## Agent Resources

### Directory Structure

```
docs/agent/
├── briefs/              # Task-specific instructions
│   └── README.md        # Brief writing guide
├── playbooks/           # Multi-step workflows
│   └── README.md        # Playbook writing guide
└── templates/           # Document templates
    ├── brief-template.md
    ├── playbook-template.md
    └── context-card-template.md

agent/                   # (Optional, created as needed)
├── backlog.md          # Pending and completed agent runs
└── reports/            # Agent execution reports
    └── YYYY-MM-DD-HH-MM-brief-name.md
```

## Working with Briefs

### What is a Brief?

A **brief** is a focused, single-task instruction for an agent. It defines:

- **Input**: What the agent needs to know
- **Output**: What the agent should produce
- **Acceptance Criteria**: How to verify success

### Brief Structure

```markdown
# Brief: [Task Name]

## Context

Background information and motivation

## Input

- What files/data to read
- What to understand before starting

## Task

Clear, actionable steps

## Output

Expected deliverables

## Acceptance Criteria

- [ ] Criterion 1
- [ ] Criterion 2
```

### Creating a Brief

See [docs/agent/briefs/README.md](../agent/briefs/README.md) for detailed guidelines.

### Executing a Brief

**Manual Execution:**

1. Agent reads the brief
2. Agent executes the task
3. Agent generates report in `agent/reports/`
4. Agent updates `agent/backlog.md` with status

**Automated Execution:**

- Briefs in `docs/agent/briefs/` trigger workflows
- GitHub Actions picks up new/updated briefs
- Agent runs and reports results

## Working with Playbooks

### What is a Playbook?

A **playbook** is a multi-step workflow that may involve multiple briefs or complex procedures. Use playbooks for:

- Multi-component updates
- Release processes
- Migration workflows

### Playbook Structure

```markdown
# Playbook: [Workflow Name]

## Overview

What this playbook accomplishes

## Prerequisites

- Required tools
- Required permissions
- Required state

## Steps

### Step 1: [Name]

Instructions...

### Step 2: [Name]

Instructions...

## Verification

How to confirm success

## Rollback

How to undo changes if needed
```

### Executing a Playbook

See [docs/agent/playbooks/README.md](../agent/playbooks/README.md) for details.

## Automated Workflows

### Agent Run Trigger

The `.github/workflows/agent-run.yml` workflow triggers when:

- A new brief is added to `docs/agent/briefs/`
- An existing brief is modified
- Manually dispatched from GitHub Actions UI

### Workflow Behavior

```yaml
on:
  workflow_dispatch:
  push:
    paths:
      - 'docs/agent/briefs/**'
```

The workflow:

1. Detects the changed brief
2. Spawns an agent execution
3. Collects outputs
4. Generates a report
5. Updates backlog

### Agent Update Script

Run to update the agent backlog:

```bash
npm run agent:update
```

This script:

- Scans `docs/agent/briefs/` for tasks
- Checks `agent/reports/` for completed runs
- Updates `agent/backlog.md` with current status

## Automated Versioning

### Conventional Commits → Releases

This repository uses **Conventional Commits** to automate versioning:

1. **Commits** follow the format: `type(scope): description`
   - `feat:` → Minor version bump (0.X.0)
   - `fix:` → Patch version bump (0.0.X)
   - `BREAKING CHANGE:` → Major version bump (X.0.0)

2. **Changelog** is generated automatically from commit messages

3. **Releases** are created based on semantic versioning

### Creating a Release

**Dry Run (CI validates this):**

```bash
npm run release:dry
```

**Actual Release:**

```bash
npm run release
git push --follow-tags origin main
```

This will:

- Bump version in `package.json`
- Generate `CHANGELOG.md`
- Create a git tag
- Commit the changes

### CI Validation

The CI pipeline validates releases with:

```bash
npm run release:dry
```

This ensures the release process will work without actually creating the release.

## Agent Reports

### Report Structure

Agent execution reports are stored in `agent/reports/` with format:

```
agent/reports/YYYY-MM-DD-HH-MM-brief-name.md
```

Each report contains:

- **Timestamp**: When the run occurred
- **Brief**: Which brief was executed
- **Status**: Success/Failure/Partial
- **Changes**: Files modified
- **Notes**: Additional context
- **Artifacts**: Links to generated files

### Example Report

```markdown
# Agent Run Report

**Date**: 2025-10-20T16:30:00Z
**Brief**: docs/agent/briefs/update-schemas.md
**Status**: ✅ Success

## Summary

Updated all JSON schemas to draft 2020-12

## Changes

- schemas/room.schema.json
- schemas/entity.schema.json
- schemas/message.schema.json

## Artifacts

- .artifacts/schema-validation.json

## Notes

All schema examples validated successfully.
```

## Integration with CI/CD

### Quality Gates

Agents should respect CI quality gates:

- **Pre-commit**: Lint and format checks
- **CI Build**: All builds must pass
- **Tests**: All tests must pass
- **Security**: No critical vulnerabilities

### Agent Permissions

Agents can:

- ✅ Read all repository files (except .github/agents/)
- ✅ Create/modify code and documentation
- ✅ Run tests and builds
- ✅ Generate reports in `agent/reports/`
- ✅ Update `agent/backlog.md`

Agents cannot:

- ❌ Directly push to main branch
- ❌ Bypass CI checks
- ❌ Access GitHub secrets
- ❌ Modify `.github/workflows/` without review

### Creating Pull Requests

Agents should:

1. Create a feature branch
2. Make changes
3. Run tests locally
4. Create PR with detailed description
5. Link to the originating brief
6. Wait for CI validation
7. Request human review if needed

## Backlog Management

### Backlog Format

The `agent/backlog.md` file tracks agent tasks:

```markdown
# Agent Backlog

## Pending

- [ ] Brief: Update integration tests (docs/agent/briefs/update-tests.md)
- [ ] Brief: Add security scanning (docs/agent/briefs/add-security.md)

## In Progress

- [ ] Brief: Refactor API endpoints (docs/agent/briefs/refactor-api.md)
  - Started: 2025-10-20T15:00:00Z
  - Agent: copilot-001

## Completed

- [x] Brief: Update documentation (docs/agent/briefs/update-docs.md)
  - Completed: 2025-10-19T14:30:00Z
  - Report: agent/reports/2025-10-19-14-30-update-docs.md
  - PR: #123
```

### Updating the Backlog

**Automatic (via script):**

```bash
npm run agent:update
```

**Manual:**
Edit `agent/backlog.md` directly to:

- Add new tasks
- Mark tasks as completed
- Add notes/context

## Best Practices

### For Brief Authors

1. **Be specific**: Clearly define input, output, and success criteria
2. **Be atomic**: One brief = one focused task
3. **Provide context**: Link to relevant documentation
4. **Include examples**: Show expected outcomes
5. **Define validation**: How to verify the task is complete

### For Agents

1. **Read thoroughly**: Understand the entire brief before starting
2. **Validate early**: Test changes incrementally
3. **Document changes**: Create clear, detailed reports
4. **Follow conventions**: Use established patterns and styles
5. **Ask for help**: Flag unclear requirements in the report

### For Playbook Authors

1. **Break into steps**: Make each step clear and actionable
2. **Define prerequisites**: List all requirements upfront
3. **Include verification**: Add checkpoints between steps
4. **Plan for failure**: Include rollback procedures
5. **Keep updated**: Maintain playbooks as the system evolves

## Monitoring Agent Activity

### Viewing Agent Runs

```bash
# List recent reports
ls -lt agent/reports/ | head -10

# View a specific report
cat agent/reports/2025-10-20-16-30-update-schemas.md
```

### Checking CI Status

```bash
# View recent workflow runs
gh run list --workflow=agent-run.yml

# View specific run details
gh run view <run-id>
```

### Analyzing Agent Performance

Track metrics like:

- Success rate of agent runs
- Time to completion
- Number of iterations needed
- Quality of generated code

Store metrics in `agent/metrics/` (if implemented).

## Troubleshooting

### Brief Not Triggering

**Problem**: New brief doesn't trigger workflow

**Solution**:

1. Check file is in `docs/agent/briefs/`
2. Ensure workflow is enabled in GitHub
3. Manually trigger with `workflow_dispatch`

### Agent Run Failed

**Problem**: Agent run fails with errors

**Solution**:

1. Check the report in `agent/reports/`
2. Review CI logs
3. Verify prerequisites were met
4. Re-run with more context if needed

### Backlog Out of Sync

**Problem**: Backlog doesn't reflect actual state

**Solution**:

```bash
# Regenerate backlog
npm run agent:update

# Or manually edit
vim agent/backlog.md
```

## Further Reading

- [Brief Writing Guide](../agent/briefs/README.md)
- [Playbook Writing Guide](../agent/playbooks/README.md)
- [Templates](../agent/templates/)
- [Development Runbook](./development.md)
- [Contributing Guide](../../CONTRIBUTING.md)
