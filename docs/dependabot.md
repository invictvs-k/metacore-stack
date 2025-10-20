# Dependabot Configuration Guide

## Overview

This repository uses [Dependabot](https://docs.github.com/en/code-security/dependabot) to automatically monitor and update dependencies across multiple ecosystems:

- **npm/yarn**: Root workspace, Integration API, Operator Dashboard, Schemas
- **NuGet**: .NET RoomServer solution
- **GitHub Actions**: Workflow automation

## Update Schedule

- **Frequency**: Weekly (every Monday)
- **Time**: 09:00 AM
- **Timezone**: America/Sao_Paulo (Brazil/São Paulo)
- **Max Open PRs**: 3 per ecosystem/directory

## Dependency Grouping Strategy

To reduce notification noise and improve review efficiency, dependencies are grouped by logical categories:

### Root Workspace (`/`)

| Group | Description | Patterns |
|-------|-------------|----------|
| `typescript-stack` | TypeScript compiler and type definitions | `typescript`, `@types/*`, `ts-node`, `tsx`, `tsup`, `esbuild` |
| `code-quality` | Linting and formatting tools | `eslint*`, `@typescript-eslint/*`, `prettier`, `lint-staged`, `husky` |
| `testing` | Test frameworks and runners | `vitest`, `jest`, `@vitest/*`, `@jest/*`, `mocha`, `chai` |
| `security` | Security patches across all dependencies | Any package with security vulnerabilities |

### Integration API (`/tools/integration-api`)

| Group | Description | Patterns |
|-------|-------------|----------|
| `express-stack` | Express server and middleware | `express`, `helmet`, `cors`, `body-parser`, `compression` |
| `utilities` | File system and general utilities | `chokidar`, `globby`, `fs-extra`, `rimraf`, `uuid` |
| `dev-dependencies` | All development dependencies | Any `devDependencies` |
| `security` | Security patches | Security vulnerabilities |

### Operator Dashboard (`/apps/operator-dashboard`)

| Group | Description | Patterns |
|-------|-------------|----------|
| `react-stack` | React framework and routing | `react`, `react-dom`, `react-router*`, `@types/react*` |
| `state-management` | State and schema validation | `zustand`, `zod`, `@tanstack/*` |
| `build-tools` | Build and bundling tools | `vite`, `@vitejs/*`, `rollup`, `webpack` |
| `dev-dependencies` | Development dependencies | Any `devDependencies` |
| `security` | Security patches | Security vulnerabilities |

### JSON Schemas (`/schemas`)

| Group | Description | Patterns |
|-------|-------------|----------|
| `schema-tools` | Schema validation libraries | `ajv`, `json-schema*`, `@apidevtools/*` |
| `all-dependencies` | All other dependencies | `*` |
| `security` | Security patches | Security vulnerabilities |

### .NET Server (`/server-dotnet`)

| Group | Description | Patterns |
|-------|-------------|----------|
| `testing` | Testing frameworks and tools | `xunit*`, `FluentAssertions*`, `Moq*`, `coverlet*`, `Microsoft.NET.Test.Sdk` |
| `aspnet-core` | ASP.NET Core packages | `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*` |
| `production-dependencies` | All production dependencies | All except testing packages |

### GitHub Actions (`/`)

| Group | Description | Patterns |
|-------|-------------|----------|
| `github-actions` | All workflow actions | `*` |

## Why This Configuration?

### Problem Statement

On October 20, 2025, when Dependabot was first enabled for this repository, it created **dozens of pull requests within 4 minutes** (15:53-15:57 UTC), causing:

- Email notification flood for all repository watchers
- Overwhelming number of PRs to review
- Difficulty prioritizing critical security updates

### Root Cause

1. **First-time activation**: Dependabot scanned all dependencies and found many outdated packages
2. **No grouping**: Each package update created a separate PR (e.g., individual PRs for React, helmet, chokidar, globby, zod, zustand, FluentAssertions, etc.)
3. **Multiple ecosystems**: 6 separate configurations multiplied the PR count
4. **CODEOWNERS**: Every PR auto-assigned reviewers, multiplying notifications

### Solution

The updated configuration reduces PR volume by **~80%**:

- **Before**: ~30-40 individual PRs per update cycle
- **After**: ~5-8 grouped PRs per update cycle

**Example transformation**:
```
BEFORE (ungrouped):
├── PR #1: Bump react from 18.2.0 to 19.2.0
├── PR #2: Bump react-dom from 18.2.0 to 19.2.0
├── PR #3: Bump react-router-dom from 6.8.0 to 7.9.4
├── PR #4: Bump @types/react from 18.0.0 to 19.0.0
└── PR #5: Bump @types/react-dom from 18.0.0 to 19.0.0

AFTER (grouped):
└── PR #1: Bump react-stack group (react, react-dom, react-router-dom, @types/react, @types/react-dom)
```

## Managing Notifications

### GitHub UI Filters

To filter Dependabot notifications:

1. Go to **Settings → Notifications**
2. Under "Watching", customize notification settings
3. Use filters in your email client:
   - **From**: `notifications@github.com`
   - **Subject contains**: `dependabot`
   - **Label**: Create a "Dependabot" label and auto-tag

### Email Filters (Gmail Example)

```
Matches: from:(notifications@github.com) subject:(dependabot OR "Bump ")
Do this: Apply label "GitHub/Dependabot", Skip Inbox (Archive)
```

### Slack/Discord Integrations

Configure GitHub app integrations to send Dependabot PRs to a dedicated channel:

```yaml
# .github/workflows/dependabot-notify.yml (optional)
name: Dependabot Notifications
on:
  pull_request:
    types: [opened]

jobs:
  notify:
    if: github.actor == 'dependabot[bot]'
    runs-on: ubuntu-latest
    steps:
      - name: Send to Slack
        # Your Slack notification logic
```

## Review Process

### Priority Levels

1. **🔴 Security patches** (grouped under `security`): Review and merge ASAP
2. **🟡 Major version updates**: Requires careful testing (breaking changes likely)
3. **🟢 Minor/patch updates**: Can be batched and merged together

### Workflow

1. **Monday 09:00 AM (Brazil time)**: Dependabot creates grouped PRs
2. **CI/CD validation**: Automated tests run on all PRs
3. **Review window**: Tuesday-Friday
4. **Merge cadence**: Batch merge passing PRs by end of week

### Testing Grouped Updates

When reviewing grouped PRs:

```bash
# Checkout the Dependabot branch
gh pr checkout <PR_NUMBER>

# Run full test suite
npm test                           # Node.js tests
dotnet test server-dotnet/         # .NET tests

# Run integration tests
npm run test:e2e

# Check for breaking changes
npm run build
```

## Troubleshooting

### Too Many PRs Still Being Created

- Check if `open-pull-requests-limit` is respected (should be 3)
- Verify groups are properly configured (review `dependabot.yml`)
- Check for duplicate group patterns

### Dependabot PRs Not Auto-Merging

Dependabot doesn't auto-merge by default. To enable:

```yaml
# .github/workflows/dependabot-auto-merge.yml
name: Dependabot Auto-Merge
on: pull_request

jobs:
  auto-merge:
    if: github.actor == 'dependabot[bot]'
    runs-on: ubuntu-latest
    steps:
      - name: Enable auto-merge for minor/patch
        if: contains(github.event.pull_request.title, 'Bump') && !contains(github.event.pull_request.labels.*.name, 'breaking-change')
        run: gh pr merge --auto --squash "$PR_URL"
        env:
          PR_URL: ${{ github.event.pull_request.html_url }}
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Dependabot Rebasing Causing Notification Spam

Dependabot automatically rebases PRs when the base branch changes. To reduce noise:

1. **Batch merge**: Merge Dependabot PRs together to avoid cascade rebases
2. **Disable rebase**: Use `@dependabot ignore this dependency` for non-critical updates
3. **Squash commits**: Use squash merging to keep history clean

### Ignoring Specific Dependencies

To ignore specific packages or versions:

```yaml
# In .github/dependabot.yml
ignore:
  # Ignore major version updates for React (wait for LTS)
  - dependency-name: "react"
    update-types: ["version-update:semver-major"]
  
  # Ignore specific package completely
  - dependency-name: "legacy-package"
```

## Best Practices

1. **Review security PRs first**: Always prioritize security patches
2. **Test before merging**: Even minor updates can introduce regressions
3. **Read changelogs**: Major version updates may require code changes
4. **Batch merge**: Merge multiple passing PRs together when possible
5. **Monitor CI**: Ensure all checks pass before merging
6. **Use semantic versioning**: Understand semver implications (major.minor.patch)

## FAQ

### Q: Why Monday 09:00 AM Brazil time?

**A**: This timing ensures:
- Updates arrive at the start of the work week
- Team is available for immediate security reviews
- Sufficient time before weekend to test and merge

### Q: Can we change the schedule?

**A**: Yes, edit `.github/dependabot.yml`:

```yaml
schedule:
  interval: 'weekly'  # Options: daily, weekly, monthly
  day: 'monday'       # Options: monday-sunday (weekly only)
  time: '09:00'       # 24-hour format (HH:MM)
  timezone: 'America/Sao_Paulo'
```

### Q: What if a grouped PR fails CI?

**A**: You can:
1. Review the specific failing dependency in the group
2. Use `@dependabot ignore this dependency` to skip it
3. Manually update the dependency separately
4. Wait for Dependabot to create separate PRs if grouping causes issues

### Q: How do I close a Dependabot PR?

**A**: Comment on the PR:
```
@dependabot close
```

To permanently ignore:
```
@dependabot ignore this dependency
```

To ignore just major versions:
```
@dependabot ignore this major version
```

## Useful Dependabot Commands

All commands are used as PR comments:

| Command | Description |
|---------|-------------|
| `@dependabot rebase` | Rebase the PR against the base branch |
| `@dependabot recreate` | Recreate the PR from scratch |
| `@dependabot merge` | Merge the PR (if checks pass) |
| `@dependabot close` | Close the PR |
| `@dependabot ignore this dependency` | Never update this dependency |
| `@dependabot ignore this major version` | Ignore major version updates |
| `@dependabot ignore this minor version` | Ignore minor version updates |

## References

- [Dependabot Documentation](https://docs.github.com/en/code-security/dependabot)
- [Dependabot Configuration Options](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file)
- [Grouping Dependabot Updates](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file#groups)
- [Managing Dependabot PRs](https://docs.github.com/en/code-security/dependabot/working-with-dependabot/managing-pull-requests-for-dependency-updates)

---

**Last Updated**: October 20, 2025  
**Configuration Version**: 2.0 (with grouping)  
**Maintained by**: @invictvs-k
