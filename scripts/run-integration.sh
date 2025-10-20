#!/usr/bin/env bash
set -euo pipefail

# Main integration test orchestrator
# Starts RoomServer and RoomOperator, runs test client, and collects artifacts

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
ARTIFACTS_DIR="$ROOT_DIR/.artifacts/integration/$TIMESTAMP"

echo "=========================================="
echo "MCP Integration Test Suite"
echo "=========================================="
echo ""
echo "Artifacts directory: $ARTIFACTS_DIR"
echo "Timestamp: $TIMESTAMP"
echo ""

mkdir -p "$ARTIFACTS_DIR/logs" "$ARTIFACTS_DIR/results"

# Cleanup function
cleanup() {
  echo ""
  echo "Cleaning up processes..."
  
  if [ -f "$ARTIFACTS_DIR/roomserver.pid" ]; then
    ROOMSERVER_PID=$(cat "$ARTIFACTS_DIR/roomserver.pid")
    if kill -0 $ROOMSERVER_PID 2>/dev/null; then
      echo "Stopping RoomServer (PID: $ROOMSERVER_PID)"
      kill $ROOMSERVER_PID || true
    fi
  fi
  
  if [ -f "$ARTIFACTS_DIR/roomoperator.pid" ]; then
    ROOMOPERATOR_PID=$(cat "$ARTIFACTS_DIR/roomoperator.pid")
    if kill -0 $ROOMOPERATOR_PID 2>/dev/null; then
      echo "Stopping RoomOperator (PID: $ROOMOPERATOR_PID)"
      kill $ROOMOPERATOR_PID || true
    fi
  fi
  
  # Give processes time to shut down
  sleep 2
  
  echo "Cleanup complete"
}

# Register cleanup on exit
trap cleanup EXIT INT TERM

# Step 1: Start RoomServer
echo "Step 1: Starting RoomServer..."
echo "=========================================="
bash "$ROOT_DIR/scripts/run-roomserver-test.sh" "$ARTIFACTS_DIR"
echo ""

# Step 2: Start RoomOperator
echo "Step 2: Starting RoomOperator..."
echo "=========================================="
bash "$ROOT_DIR/scripts/run-roomoperator-test.sh" "$ARTIFACTS_DIR"
echo ""

# Step 3: Run test client
echo "Step 3: Running test client..."
echo "=========================================="
set +e
bash "$ROOT_DIR/scripts/run-test-client.sh" "$ARTIFACTS_DIR"
TEST_EXIT=$?
set -e
echo ""

# Step 4: Collect final status
echo "Step 4: Collecting final status..."
echo "=========================================="

# Get final MCP status from RoomServer
echo "Fetching final MCP status..."
curl -fsS "http://localhost:40801/status/mcp" -o "$ARTIFACTS_DIR/results/mcp-status-final.json" 2>/dev/null || echo "Failed to fetch MCP status"

# Generate final report
echo "Generating final report..."
cat > "$ARTIFACTS_DIR/results/summary.txt" <<EOF
MCP Integration Test Summary
=============================
Timestamp: $TIMESTAMP
Artifacts Directory: $ARTIFACTS_DIR

Test Exit Code: $TEST_EXIT

Logs:
  - RoomServer: $ARTIFACTS_DIR/logs/roomserver.log
  - RoomOperator: $ARTIFACTS_DIR/logs/roomoperator.log
  - Test Client: $ARTIFACTS_DIR/logs/test-client.log

Results:
  - Report: $ARTIFACTS_DIR/results/report.json
  - JUnit: $ARTIFACTS_DIR/results/junit.xml
  - MCP Status: $ARTIFACTS_DIR/results/mcp-status-final.json
EOF

# Print summary
echo ""
echo "=========================================="
echo "Integration Test Suite: $(if [ $TEST_EXIT -eq 0 ]; then echo "SUCCESS"; else echo "FAILED"; fi)"
echo "=========================================="
echo ""
cat "$ARTIFACTS_DIR/results/summary.txt"
echo ""
echo "Full artifacts available at: $ARTIFACTS_DIR"
echo ""

# Return test exit code
exit $TEST_EXIT
