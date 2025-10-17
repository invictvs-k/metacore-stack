#!/usr/bin/env bash
set -euo pipefail

pnpm -C mcp-ts dev &
MCP_PID=$!

trap 'kill $MCP_PID 2>/dev/null || true' EXIT

dotnet run --project server-dotnet/src/RoomServer/RoomServer.csproj
