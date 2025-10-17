#!/usr/bin/env bash
set -euo pipefail

command -v pnpm >/dev/null 2>&1 || npm install -g pnpm@9

pnpm --version
node --version

echo "Installing schema workspace dependencies"
pushd schemas >/dev/null
pnpm install
popd >/dev/null

echo "Installing MCP workspace dependencies"
pushd mcp-ts >/dev/null
pnpm install
popd >/dev/null

echo "Restoring .NET tools"
pushd server-dotnet >/dev/null
dotnet --info
dotnet restore
popd >/dev/null

echo "Bootstrap complete."
