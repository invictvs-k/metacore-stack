#!/usr/bin/env bash
set -euo pipefail

# Script to run MCP integration test client
# Usage: ./run-test-client.sh <artifacts_dir>

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS_DIR="$1"

if [ -z "$ARTIFACTS_DIR" ]; then
  echo "Error: artifacts_dir argument is required"
  exit 1
fi

mkdir -p "$ARTIFACTS_DIR/logs" "$ARTIFACTS_DIR/results"

echo "Running MCP integration test client..."
echo "Artifacts directory: $ARTIFACTS_DIR"

cd "$ROOT_DIR/server-dotnet/operator/test-client"

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
  echo "Installing test client dependencies..."
  npm install > "$ARTIFACTS_DIR/logs/test-client-install.log" 2>&1
fi

# Run the test scenarios
echo "Executing test scenarios..."
export ARTIFACTS_DIR="$ARTIFACTS_DIR"
export OPERATOR_URL="http://localhost:40802"
export ROOMSERVER_URL="http://localhost:40801"
export VERBOSE="true"

node scenarios/mcp/run-mcp-tests.js > "$ARTIFACTS_DIR/logs/test-client.log" 2>&1
TEST_EXIT=$?

echo "Test client finished with exit code: $TEST_EXIT"

if [ $TEST_EXIT -eq 0 ]; then
  echo "✓ All tests passed"
else
  echo "✗ Some tests failed"
fi

exit $TEST_EXIT
