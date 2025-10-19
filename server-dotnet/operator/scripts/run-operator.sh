#!/usr/bin/env bash

# Script to run RoomOperator for integration testing

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OPERATOR_DIR="${SCRIPT_DIR}/.."

echo "═══════════════════════════════════════════════════════"
echo "  Starting RoomOperator"
echo "═══════════════════════════════════════════════════════"
echo ""

# Check if operator directory exists
if [ ! -d "$OPERATOR_DIR" ]; then
    echo "ERROR: Operator directory not found at $OPERATOR_DIR"
    exit 1
fi

cd "$OPERATOR_DIR"

echo "Configuration:"
echo "  - Directory: $(pwd)"
echo "  - Port: 8080"
echo "  - RoomServer: http://localhost:5000"
echo ""

# Check if ROOM_AUTH_TOKEN is set
if [ -z "$ROOM_AUTH_TOKEN" ]; then
    echo "WARNING: ROOM_AUTH_TOKEN not set (authentication might fail)"
    echo "Set it with: export ROOM_AUTH_TOKEN=your-token"
    echo ""
fi

# Check if already running
if lsof -Pi :8080 -sTCP:LISTEN -t >/dev/null 2>&1; then
    echo "WARNING: Port 8080 is already in use"
    echo "To stop existing process: kill \$(lsof -t -i:8080)"
    echo ""
    read -p "Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo "Starting RoomOperator..."
echo "Press Ctrl+C to stop"
echo ""

dotnet run --no-build --configuration Debug
