#!/usr/bin/env bash
set -euo pipefail

if ! command -v pnpm >/dev/null 2>&1; then
  echo "Installing pnpm@9 globally"
  npm install -g pnpm@9
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK is required. Please install .NET 8 before continuing." >&2
  exit 1
fi

echo "dotnet version:" $(dotnet --version)

pushd schemas >/dev/null
pnpm install
popd >/dev/null

pushd mcp-ts >/dev/null
pnpm install
popd >/dev/null

pushd server-dotnet >/dev/null
dotnet restore
popd >/dev/null

echo "Bootstrap complete"
