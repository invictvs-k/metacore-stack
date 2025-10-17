#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

Write-Host 'Starting RoomServer via dotnet watch'
pushd server-dotnet/src/RoomServer
try {
  dotnet watch run
} finally {
  popd
}
