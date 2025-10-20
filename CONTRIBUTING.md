# Contributing

- Follow **Conventional Commits** (`feat:`, `fix:`, `chore:` ...).
- Every PR must pass: build, tests, lint, schema validation.
- Avoid breaking changes in `schemas/` without versioning.

## Useful Scripts

- `make bootstrap` — install global and local dependencies
- `make run-server` — run the Room Host
- `make mcp-up` — start example MCP servers
- `make schemas-validate` — validate JSON Schemas + examples
