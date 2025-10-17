#!/usr/bin/env bash
set -euo pipefail

echo "Starting RoomServer via dotnet watch"
(cd server-dotnet/src/RoomServer && dotnet watch run)
