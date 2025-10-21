# Agent Backlog

_Last updated: 2025-10-20T21:02:04.839Z_

This file tracks agent tasks and their execution status.

## How to Use

### Adding a Task

1. Create a brief in `docs/agent/briefs/your-task.md`
2. Run `npm run agent:update` to add it to the backlog
3. Agent will pick it up automatically or via workflow dispatch

### Completing a Task

1. Agent executes the brief
2. Agent creates report in `agent/reports/`
3. Run `npm run agent:update` to mark as completed

### Viewing Reports

```bash
# List all reports
ls -lt agent/reports/

# View a specific report
cat agent/reports/YYYY-MM-DD-HH-MM-task-name.md
```
