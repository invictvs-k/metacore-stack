#!/usr/bin/env bash

# Script to run test client against RoomOperator and RoomServer

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLIENT_DIR="${SCRIPT_DIR}/../test-client"

echo "═══════════════════════════════════════════════════════"
echo "  RoomOperator Test Client Runner"
echo "═══════════════════════════════════════════════════════"
echo ""

# Check if test-client directory exists
if [ ! -d "$CLIENT_DIR" ]; then
    echo "ERROR: Test client directory not found at $CLIENT_DIR"
    exit 1
fi

cd "$CLIENT_DIR"

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
    echo ""
fi

# Set default environment variables if not set
export OPERATOR_URL="${OPERATOR_URL:-http://localhost:40802}"
export ROOMSERVER_URL="${ROOMSERVER_URL:-http://localhost:40801}"
export TEST_ROOM_ID="${TEST_ROOM_ID:-room-test-integration}"

echo "Configuration:"
echo "  - Operator URL: $OPERATOR_URL"
echo "  - RoomServer URL: $ROOMSERVER_URL"
echo "  - Test Room ID: $TEST_ROOM_ID"
echo "  - Auth Token: ${ROOM_AUTH_TOKEN:+***SET***}${ROOM_AUTH_TOKEN:-NOT SET}"
echo "  - Verbose: ${VERBOSE:-false}"
echo ""

# Parse command line arguments
SCENARIO="${1:-all}"

case "$SCENARIO" in
    basic)
        echo "Running basic flow scenario..."
        npm run test:basic
        ;;
    error)
        echo "Running error cases scenario..."
        npm run test:error
        ;;
    stress)
        echo "Running stress test scenario..."
        npm run test:stress
        ;;
    all)
        echo "Running all test scenarios..."
        npm run test:all
        ;;
    *)
        echo "ERROR: Unknown scenario: $SCENARIO"
        echo ""
        echo "Usage: $0 [scenario]"
        echo ""
        echo "Available scenarios:"
        echo "  basic  - Run basic flow (happy path)"
        echo "  error  - Run error cases"
        echo "  stress - Run stress tests"
        echo "  all    - Run all scenarios (default)"
        exit 1
        ;;
esac
