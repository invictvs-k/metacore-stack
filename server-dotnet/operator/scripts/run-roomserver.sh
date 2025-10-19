#!/usr/bin/env bash

# Script to run RoomServer for integration testing

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOMSERVER_DIR="${SCRIPT_DIR}/../../src/RoomServer"

echo "═══════════════════════════════════════════════════════"
echo "  Starting RoomServer (Test Mode)"
echo "═══════════════════════════════════════════════════════"
echo ""

# Check if RoomServer directory exists
if [ ! -d "$ROOMSERVER_DIR" ]; then
    echo "ERROR: RoomServer directory not found at $ROOMSERVER_DIR"
    exit 1
fi

cd "$ROOMSERVER_DIR"

# Set environment for testing
export ASPNETCORE_ENVIRONMENT=Test
export DOTNET_ENVIRONMENT=Test

echo "Configuration:"
echo "  - Environment: $ASPNETCORE_ENVIRONMENT"
echo "  - Directory: $(pwd)"
echo "  - Port: 5000"
echo ""

# Check if already running
if lsof -Pi :5000 -sTCP:LISTEN -t >/dev/null 2>&1; then
    echo "WARNING: Port 5000 is already in use"
    echo "To stop existing process: kill \$(lsof -t -i:5000)"
    echo ""
    read -p "Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo "Starting RoomServer..."
echo "Press Ctrl+C to stop"
echo ""

dotnet run --no-build --configuration Debug
