#!/usr/bin/env bash
set -euo pipefail

# Script to run RoomServer in test mode
# Usage: ./run-roomserver-test.sh [artifacts_dir]

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS_DIR="${1:-$(pwd)/.artifacts/integration/$(date +%Y%m%d-%H%M%S)}"

mkdir -p "$ARTIFACTS_DIR/logs"

echo "Starting RoomServer in test mode..."
echo "Artifacts directory: $ARTIFACTS_DIR"

cd "$ROOT_DIR/server-dotnet/src/RoomServer"

# Build the project
echo "Building RoomServer..."
dotnet build --configuration Debug > "$ARTIFACTS_DIR/logs/roomserver-build.log" 2>&1

# Start RoomServer with test configuration
echo "Starting RoomServer with test configuration..."
ASPNETCORE_ENVIRONMENT=Test \
  dotnet run --no-build --configuration Debug \
  > "$ARTIFACTS_DIR/logs/roomserver.log" 2>&1 &

ROOMSERVER_PID=$!
echo $ROOMSERVER_PID > "$ARTIFACTS_DIR/roomserver.pid"

echo "RoomServer started (PID: $ROOMSERVER_PID)"
echo "Logs: $ARTIFACTS_DIR/logs/roomserver.log"

# Wait for RoomServer to be ready
echo "Waiting for RoomServer to be ready..."
MAX_WAIT=60
START_TIME=$(date +%s)

while true; do
  if curl -fsS http://localhost:40801/health > /dev/null 2>&1; then
    echo "✓ RoomServer is ready"
    break
  fi
  
  CURRENT_TIME=$(date +%s)
  ELAPSED=$((CURRENT_TIME - START_TIME))
  
  if [ $ELAPSED -ge $MAX_WAIT ]; then
    echo "✗ Timeout waiting for RoomServer to be ready"
    exit 1
  fi
  
  sleep 1
done

echo "$ARTIFACTS_DIR"
