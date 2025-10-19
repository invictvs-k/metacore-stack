#!/usr/bin/env bash
set -euo pipefail

# Script to run RoomOperator in test mode
# Usage: ./run-roomoperator-test.sh <artifacts_dir>

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS_DIR="$1"

if [ -z "$ARTIFACTS_DIR" ]; then
  echo "Error: artifacts_dir argument is required"
  exit 1
fi

mkdir -p "$ARTIFACTS_DIR/logs"

echo "Starting RoomOperator in test mode..."
echo "Artifacts directory: $ARTIFACTS_DIR"

cd "$ROOT_DIR/server-dotnet/operator"

# Build the project
echo "Building RoomOperator..."
dotnet build --configuration Debug > "$ARTIFACTS_DIR/logs/roomoperator-build.log" 2>&1

# Start RoomOperator with test configuration
echo "Starting RoomOperator with test configuration..."
ASPNETCORE_ENVIRONMENT=Test \
  ROOM_AUTH_TOKEN="" \
  dotnet run --no-build --configuration Debug --no-launch-profile \
  > "$ARTIFACTS_DIR/logs/roomoperator.log" 2>&1 &

ROOMOPERATOR_PID=$!
echo $ROOMOPERATOR_PID > "$ARTIFACTS_DIR/roomoperator.pid"

echo "RoomOperator started (PID: $ROOMOPERATOR_PID)"
echo "Logs: $ARTIFACTS_DIR/logs/roomoperator.log"

# Wait for RoomOperator to be ready
echo "Waiting for RoomOperator to be ready..."
MAX_WAIT=60
START_TIME=$(date +%s)

while true; do
  if curl -fsS http://localhost:40802/health > /dev/null 2>&1; then
    echo "✓ RoomOperator is ready"
    break
  fi
  
  CURRENT_TIME=$(date +%s)
  ELAPSED=$((CURRENT_TIME - START_TIME))
  
  if [ $ELAPSED -ge $MAX_WAIT ]; then
    echo "✗ Timeout waiting for RoomOperator to be ready"
    exit 1
  fi
  
  sleep 1
done
