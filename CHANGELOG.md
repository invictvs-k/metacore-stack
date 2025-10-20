# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Automation & Quality

- **Conventional Commits**: Added commitlint configuration with `@commitlint/config-conventional`
- **Commitizen**: Interactive commit message helper with `cz-conventional-changelog`
- **Standard Version**: Automated versioning and changelog generation
- **Pre-commit Hooks**: Husky hooks for linting and formatting before commits
- **Lint-staged**: Runs prettier, eslint, and dotnet format on staged files
- **ESLint 9**: Migrated to flat config format with TypeScript support

#### Security & Dependencies

- **Dependabot**: Weekly automated dependency updates for npm, nuget, and GitHub Actions
- **Security Audits**: Automated npm and dotnet vulnerability scanning
- **Security Reports**: Generated reports in `.artifacts/security/`
- **Secrets Baseline**: detect-secrets baseline configuration

#### Testing Infrastructure

- **Smoke Tests**: Build validation and endpoint health checks
- **Contract Tests**: Schema and contract validation suite
- **E2E Tests**: Basic end-to-end test framework with SSE support
- **Test Reports**: Automated test report generation in markdown format
- **Test Scripts**: Added `test:smoke`, `test:contract`, `test:e2e`, `test:all`

#### Documentation

- **Development Runbook**: Complete guide for local setup, workflows, and conventions
- **Agent Execution Runbook**: Guide for AI agents with brief/playbook system
- **Roadmap**: Product roadmap with now/next/later priorities
- **Runbooks Index**: Updated runbooks README with available guides

#### Agent Integration

- **Agent Briefs**: System for focused, single-task agent instructions
- **Agent Playbooks**: Multi-step workflow system
- **Agent Backlog**: Automated tracking of agent tasks and completion
- **Agent Workflows**: GitHub Actions workflow for agent run triggers
- **Agent Reports**: Structured reporting system in `agent/reports/`

#### CI/CD Enhancement

- **Pre-commit Job**: Validates formatting and smoke tests
- **Security Job**: Runs security audits and uploads reports
- **Release Dry-run Job**: Validates release process on PRs
- **E2E Job**: Runs end-to-end tests with services
- **Docs Job**: Validates documentation and manifest
- **Quality Reports**: Aggregated quality metrics in `ci/quality-report.md`
- **Test Reports**: Detailed test results in `ci/test-report.md`

#### Scripts & Automation

- `npm run commit` - Interactive commit with commitizen
- `npm run release` - Create version, changelog, and tag
- `npm run release:dry` - Dry-run release process
- `npm run security:audit` - Run security scans
- `npm run agent:update` - Update agent backlog
- `npm run report:test` - Generate test report
- `npm run report:quality` - Generate quality report

#### Infrastructure

- `.artifacts/` directory for build artifacts (gitignored)
- `agent/` directory for agent execution tracking
- Enhanced CI pipeline with quality gates
- Badge generation for quality metrics

### Changed

- **ESLint Configuration**: Migrated from `.eslintrc.json` to `eslint.config.mjs` for ESLint 9
- **Husky**: Updated to use `husky` command instead of `husky install`
- **Package Scripts**: Added numerous automation scripts
- **CI Workflows**: Enhanced with additional quality checks
- **Documentation Structure**: Added comprehensive runbooks

### Fixed

- ESLint 9 compatibility with flat config format
- Pre-commit hooks now properly validate staged files
- Lint-staged configuration for multi-language support

## [1.0.0] - 2024-12-XX

### Initial Release

- .NET 8 RoomServer with SignalR
- TypeScript Integration API
- React Operator Dashboard
- JSON Schema definitions
- Basic CI/CD pipeline
- Documentation structure
- MCP servers in TypeScript

[Unreleased]: https://github.com/invictvs-k/metacore-stack/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/invictvs-k/metacore-stack/releases/tag/v1.0.0
