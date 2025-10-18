SHELL := /usr/bin/env bash

.PHONY: bootstrap build test lint format run-server mcp-up mcp-dev schemas-validate compose-up verify-environment

bootstrap:
	@echo ">> Installing pnpm (if missing) and dotnet tools"
	npm -g ls pnpm >/dev/null 2>&1 || npm i -g pnpm@9
	cd schemas && pnpm i
	cd mcp-ts && pnpm i
	cd server-dotnet && dotnet restore

build:
	cd server-dotnet && dotnet build -c Debug
	cd mcp-ts && pnpm -r -F "*" build || true

test:
	cd server-dotnet/tests/RoomServer.Tests && dotnet test -c Debug
	cd schemas && pnpm test || true

lint:
	cd mcp-ts && pnpm -r -F "*" lint || true

format:
	cd server-dotnet && dotnet format
	cd mcp-ts && pnpm -r -F "*" format || true

run-server:
	cd server-dotnet/src/RoomServer && dotnet run

mcp-up:
	cd mcp-ts/servers/web.search && pnpm dev &
	cd mcp-ts/servers/http.request && pnpm dev &

mcp-dev:
	pnpm -C mcp-ts dev

schemas-validate:
	cd schemas && pnpm validate

compose-up:
	cd infra && docker compose up -d

verify-environment:
	@./tools/scripts/verify-environment.sh
