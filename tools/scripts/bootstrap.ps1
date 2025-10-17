#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) {
  Write-Host 'Installing pnpm@9 globally'
  npm install -g pnpm@9 | Out-Null
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Write-Error 'dotnet SDK is required. Please install .NET 8 before continuing.'
}

Write-Host "dotnet version: $(dotnet --version)"

Push-Location schemas
pnpm install
Pop-Location

Push-Location mcp-ts
pnpm install
Pop-Location

Push-Location server-dotnet
dotnet restore
Pop-Location

Write-Host 'Bootstrap complete'
