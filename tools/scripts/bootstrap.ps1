#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) {
  Write-Host 'Installing pnpm@9 globally'
  npm install -g pnpm@9 | Out-Null
}

pnpm --version
node --version

Write-Host 'Installing schema workspace dependencies'
pushd schemas
pnpm install
popd

Write-Host 'Installing MCP workspace dependencies'
pushd mcp-ts
pnpm install
popd

Write-Host 'Restoring .NET tools'
pushd server-dotnet
dotnet --info
dotnet restore
popd

Write-Host 'Bootstrap complete.'
