#!/usr/bin/env bash

# Complete integration test script
# Starts RoomServer, RoomOperator, and runs test client

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "═══════════════════════════════════════════════════════"
echo "  RoomOperator-RoomServer Integration Test Suite"
echo "═══════════════════════════════════════════════════════"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

cleanup() {
    echo ""
    echo "Cleaning up..."
    
    # Kill background processes
    if [ ! -z "$ROOMSERVER_PID" ]; then
        echo "Stopping RoomServer (PID: $ROOMSERVER_PID)..."
        kill $ROOMSERVER_PID 2>/dev/null || true
    fi
    
    if [ ! -z "$OPERATOR_PID" ]; then
        echo "Stopping RoomOperator (PID: $OPERATOR_PID)..."
        kill $OPERATOR_PID 2>/dev/null || true
    fi
    
    # Clean up any lingering processes on ports
    lsof -ti:5000 | xargs kill -9 2>/dev/null || true
    lsof -ti:8080 | xargs kill -9 2>/dev/null || true
    
    echo "Cleanup complete"
}

# Setup trap for cleanup
trap cleanup EXIT INT TERM

# Step 1: Build projects
echo "Step 1: Building projects..."
cd "${SCRIPT_DIR}/../../.."
dotnet build -c Debug server-dotnet/RoomServer.sln
echo -e "${GREEN}✓ Build complete${NC}"
echo ""

# Step 2: Start RoomServer
echo "Step 2: Starting RoomServer..."
cd "${SCRIPT_DIR}/../../src/RoomServer"
export ASPNETCORE_ENVIRONMENT=Test
dotnet run --no-build -c Debug > /tmp/roomserver.log 2>&1 &
ROOMSERVER_PID=$!
echo "RoomServer started (PID: $ROOMSERVER_PID)"

# Wait for RoomServer to be ready
echo "Waiting for RoomServer to be ready..."
for i in {1..30}; do
    if curl -s http://localhost:5000/health > /dev/null 2>&1 || \
       curl -s http://localhost:5000/ > /dev/null 2>&1; then
        echo -e "${GREEN}✓ RoomServer is ready${NC}"
        break
    fi
    if [ $i -eq 30 ]; then
        echo -e "${RED}✗ RoomServer failed to start${NC}"
        echo "Last 20 lines of log:"
        tail -20 /tmp/roomserver.log
        exit 1
    fi
    sleep 1
done
echo ""

# Step 3: Start RoomOperator
echo "Step 3: Starting RoomOperator..."
cd "${SCRIPT_DIR}/.."
export ROOM_AUTH_TOKEN="${ROOM_AUTH_TOKEN:-test-token}"
dotnet run --no-build -c Debug > /tmp/operator.log 2>&1 &
OPERATOR_PID=$!
echo "RoomOperator started (PID: $OPERATOR_PID)"

# Wait for RoomOperator to be ready
echo "Waiting for RoomOperator to be ready..."
for i in {1..30}; do
    if curl -s http://localhost:8080/health > /dev/null 2>&1; then
        echo -e "${GREEN}✓ RoomOperator is ready${NC}"
        break
    fi
    if [ $i -eq 30 ]; then
        echo -e "${RED}✗ RoomOperator failed to start${NC}"
        echo "Last 20 lines of log:"
        tail -20 /tmp/operator.log
        exit 1
    fi
    sleep 1
done
echo ""

# Step 4: Run tests
echo "Step 4: Running integration tests..."
cd "${SCRIPT_DIR}/../test-client"

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "Installing test client dependencies..."
    npm install
fi

# Set environment variables
export OPERATOR_URL=http://localhost:8080
export ROOMSERVER_URL=http://localhost:5000
export TEST_ROOM_ID=room-test-integration
export VERBOSE=true

# Run test scenario (default to basic)
SCENARIO="${1:-basic}"

case "$SCENARIO" in
    basic)
        npm run test:basic
        ;;
    error)
        npm run test:error
        ;;
    stress)
        npm run test:stress
        ;;
    all)
        npm run test:all
        ;;
    *)
        echo -e "${YELLOW}Unknown scenario: $SCENARIO, running basic${NC}"
        npm run test:basic
        ;;
esac

TEST_EXIT_CODE=$?

# Step 5: Show logs if tests failed
if [ $TEST_EXIT_CODE -ne 0 ]; then
    echo ""
    echo -e "${RED}Tests failed!${NC}"
    echo ""
    echo "═══ RoomServer Logs (last 50 lines) ═══"
    tail -50 /tmp/roomserver.log
    echo ""
    echo "═══ RoomOperator Logs (last 50 lines) ═══"
    tail -50 /tmp/operator.log
    exit $TEST_EXIT_CODE
else
    echo ""
    echo -e "${GREEN}═══════════════════════════════════════════════════════"
    echo "  All tests passed! ✓"
    echo "═══════════════════════════════════════════════════════${NC}"
fi

exit $TEST_EXIT_CODE
