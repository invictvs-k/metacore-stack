#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

$pnpm = Start-Process pnpm -ArgumentList '-C', 'mcp-ts', 'dev' -PassThru

try {
  dotnet run --project 'server-dotnet/src/RoomServer/RoomServer.csproj'
}
finally {
  if (-not $pnpm.HasExited) {
    $pnpm.Kill()
  }
}
