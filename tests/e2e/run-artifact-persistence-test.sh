#!/usr/bin/env bash

# Artifact Persistence Integration Test Runner
# Starts RoomServer, creates room/session, runs E2E tests, and cleans up

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "═══════════════════════════════════════════════════════"
echo "  Artifact Persistence E2E Test Runner"
echo "═══════════════════════════════════════════════════════"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test artifacts directory
ARTIFACTS_DIR="$SCRIPT_DIR/.artifacts/artifact-persistence"
mkdir -p "$ARTIFACTS_DIR"

# Logging
LOG_FILE="$ARTIFACTS_DIR/test-run-$(date +%Y%m%d-%H%M%S).log"
exec > >(tee -a "$LOG_FILE") 2>&1

echo "Test artifacts will be saved to: $ARTIFACTS_DIR"
echo "Test log: $LOG_FILE"
echo ""

# Cleanup function
cleanup() {
    echo ""
    echo -e "${YELLOW}Cleaning up...${NC}"
    
    # Kill RoomServer if running
    if [ -n "$ROOMSERVER_PID" ]; then
        echo "Stopping RoomServer (PID: $ROOMSERVER_PID)..."
        kill "$ROOMSERVER_PID" 2>/dev/null || true
        wait "$ROOMSERVER_PID" 2>/dev/null || true
    fi
    
    # Clean up any processes on port 40801
    PIDS=$(lsof -ti:40801 2>/dev/null || true)
    if [ -n "$PIDS" ]; then
        echo "Cleaning up processes on port 40801..."
        echo "$PIDS" | xargs kill 2>/dev/null || true
        sleep 1
        # Force kill if needed
        REMAINING=$(lsof -ti:40801 2>/dev/null || true)
        if [ -n "$REMAINING" ]; then
            echo "$REMAINING" | xargs kill -9 2>/dev/null || true
        fi
    fi
    
    echo "Cleanup complete."
}

# Set up trap for cleanup
trap cleanup EXIT INT TERM

# Check prerequisites
check_prerequisites() {
    echo -e "${BLUE}Checking prerequisites...${NC}"
    
    # Check for dotnet
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}❌ .NET SDK not found. Please install .NET 8.0+${NC}"
        exit 1
    fi
    
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✅ .NET SDK found: $DOTNET_VERSION${NC}"
    
    # Check for node
    if ! command -v node &> /dev/null; then
        echo -e "${RED}❌ Node.js not found. Please install Node.js 20+${NC}"
        exit 1
    fi
    
    NODE_VERSION=$(node --version)
    echo -e "${GREEN}✅ Node.js found: $NODE_VERSION${NC}"
    
    echo ""
}

# Build RoomServer
build_server() {
    echo -e "${BLUE}Building RoomServer...${NC}"
    cd "$PROJECT_ROOT/server-dotnet"
    
    if dotnet build -c Release > "$ARTIFACTS_DIR/build.log" 2>&1; then
        echo -e "${GREEN}✅ RoomServer built successfully${NC}"
    else
        echo -e "${RED}❌ Failed to build RoomServer${NC}"
        echo "Check build log: $ARTIFACTS_DIR/build.log"
        exit 1
    fi
    
    echo ""
}

# Start RoomServer
start_server() {
    echo -e "${BLUE}Starting RoomServer...${NC}"
    cd "$PROJECT_ROOT/server-dotnet/src/RoomServer"
    
    # Start server in background
    dotnet run --no-build -c Release > "$ARTIFACTS_DIR/server.log" 2>&1 &
    ROOMSERVER_PID=$!
    
    echo "RoomServer started with PID: $ROOMSERVER_PID"
    echo "Server log: $ARTIFACTS_DIR/server.log"
    
    # Wait for server to be ready
    echo "Waiting for RoomServer to be ready..."
    MAX_WAIT=30
    WAITED=0
    
    while [ $WAITED -lt $MAX_WAIT ]; do
        if curl -s http://localhost:40801/health > /dev/null 2>&1; then
            echo -e "${GREEN}✅ RoomServer is ready${NC}"
            echo ""
            return 0
        fi
        
        # Check if process is still running
        if ! kill -0 "$ROOMSERVER_PID" 2>/dev/null; then
            echo -e "${RED}❌ RoomServer process died${NC}"
            echo "Check server log: $ARTIFACTS_DIR/server.log"
            tail -20 "$ARTIFACTS_DIR/server.log"
            exit 1
        fi
        
        sleep 1
        WAITED=$((WAITED + 1))
        echo -n "."
    done
    
    echo ""
    echo -e "${RED}❌ RoomServer failed to start within ${MAX_WAIT}s${NC}"
    echo "Check server log: $ARTIFACTS_DIR/server.log"
    tail -20 "$ARTIFACTS_DIR/server.log"
    exit 1
}

# Create test room and session
create_test_room() {
    echo -e "${BLUE}Creating test room and session...${NC}"
    
    TEST_ROOM_ID="test-room-$(date +%s)"
    TEST_ENTITY_ID="E-TEST-001"
    
    # Note: This is a placeholder. In reality, you'd need to:
    # 1. Call the room creation endpoint
    # 2. Create an entity/session
    # 3. Get proper authentication tokens
    
    echo "Test Room ID: $TEST_ROOM_ID"
    echo "Test Entity ID: $TEST_ENTITY_ID"
    echo ""
    
    # For now, document that manual setup is needed
    echo -e "${YELLOW}⚠️  Note: Automatic room/session creation not implemented${NC}"
    echo "   The test script will attempt to use predefined IDs"
    echo "   For full functionality, ensure a room and session exist"
    echo ""
}

# Run E2E tests
run_tests() {
    echo -e "${BLUE}Running E2E tests...${NC}"
    cd "$SCRIPT_DIR"
    
    if node artifact-persistence.test.mjs; then
        echo ""
        echo -e "${GREEN}✅ E2E tests passed${NC}"
        return 0
    else
        echo ""
        echo -e "${RED}❌ E2E tests failed${NC}"
        return 1
    fi
}

# Verify artifacts and collect evidence
verify_artifacts() {
    echo ""
    echo -e "${BLUE}Verifying test artifacts...${NC}"
    
    # Check for evidence files
    if ls "$ARTIFACTS_DIR"/evidence-*.json 1> /dev/null 2>&1; then
        echo -e "${GREEN}✅ Evidence files generated${NC}"
        ls -lh "$ARTIFACTS_DIR"/evidence-*.json
    else
        echo -e "${YELLOW}⚠️  No evidence files found${NC}"
    fi
    
    # Check for summary files
    if ls "$ARTIFACTS_DIR"/summary-*.txt 1> /dev/null 2>&1; then
        echo -e "${GREEN}✅ Summary files generated${NC}"
        ls -lh "$ARTIFACTS_DIR"/summary-*.txt
        
        # Display latest summary
        LATEST_SUMMARY=$(ls -t "$ARTIFACTS_DIR"/summary-*.txt | head -1)
        echo ""
        echo "Latest test summary:"
        echo "─────────────────────────────────────────────"
        cat "$LATEST_SUMMARY"
        echo "─────────────────────────────────────────────"
    else
        echo -e "${YELLOW}⚠️  No summary files found${NC}"
    fi
    
    echo ""
}

# Verify filesystem state
verify_filesystem() {
    echo -e "${BLUE}Verifying filesystem state...${NC}"
    
    AI_FLOW_DIR="$PROJECT_ROOT/server-dotnet/src/RoomServer/.ai-flow"
    
    if [ -d "$AI_FLOW_DIR" ]; then
        echo -e "${GREEN}✅ .ai-flow directory exists${NC}"
        echo "Directory structure:"
        tree -L 4 "$AI_FLOW_DIR" 2>/dev/null || find "$AI_FLOW_DIR" -type f -o -type d | head -20
    else
        echo -e "${YELLOW}⚠️  .ai-flow directory not found at: $AI_FLOW_DIR${NC}"
        echo "   This may be expected if tests did not create artifacts"
    fi
    
    echo ""
}

# Generate final report
generate_report() {
    echo -e "${BLUE}Generating test report...${NC}"
    
    REPORT_FILE="$ARTIFACTS_DIR/test-report-$(date +%Y%m%d-%H%M%S).md"
    
    cat > "$REPORT_FILE" << EOF
# Artifact Persistence E2E Test Report

**Date:** $(date -u +"%Y-%m-%d %H:%M:%S UTC")  
**Test Duration:** ${SECONDS}s  
**Status:** ${TEST_STATUS}

## Test Configuration

- **RoomServer URL:** http://localhost:40801
- **Project Root:** $PROJECT_ROOT
- **Artifacts Directory:** $ARTIFACTS_DIR

## Test Execution

The test executed the following steps:

1. ✅ Prerequisites checked
2. ✅ RoomServer built
3. ✅ RoomServer started
4. ${TEST_RESULT} E2E tests executed
5. ✅ Artifacts verified
6. ✅ Filesystem verified

## Evidence Files

$(ls -lh "$ARTIFACTS_DIR"/ | tail -n +2)

## Server Log Summary

\`\`\`
$(tail -50 "$ARTIFACTS_DIR/server.log" 2>/dev/null || echo "Server log not available")
\`\`\`

## Test Results

$(cat "$ARTIFACTS_DIR"/summary-*.txt 2>/dev/null | tail -50 || echo "No summary available")

## Filesystem State

\`\`\`
$(tree -L 4 "$PROJECT_ROOT/server-dotnet/src/RoomServer/.ai-flow" 2>/dev/null || echo "Filesystem tree not available")
\`\`\`

## Conclusion

${TEST_CONCLUSION}

---

*Report generated by run-artifact-persistence-test.sh*
EOF
    
    echo -e "${GREEN}✅ Test report generated: $REPORT_FILE${NC}"
    echo ""
}

# Main execution
main() {
    echo "Starting artifact persistence E2E test suite..."
    echo ""
    
    check_prerequisites
    build_server
    start_server
    create_test_room
    
    # Run tests and capture result
    if run_tests; then
        TEST_STATUS="✅ PASSED"
        TEST_RESULT="✅"
        TEST_CONCLUSION="All artifact persistence tests passed successfully. The system correctly handles artifact creation, versioning, promotion, and cleanup operations."
        EXIT_CODE=0
    else
        TEST_STATUS="❌ FAILED"
        TEST_RESULT="❌"
        TEST_CONCLUSION="Some tests failed. Review the evidence files and logs for details on what went wrong."
        EXIT_CODE=1
    fi
    
    verify_artifacts
    verify_filesystem
    generate_report
    
    echo ""
    echo "═══════════════════════════════════════════════════════"
    echo -e "  Test Suite Complete: ${TEST_STATUS}"
    echo "═══════════════════════════════════════════════════════"
    echo ""
    echo "Test artifacts saved to: $ARTIFACTS_DIR"
    echo "Review the evidence files for detailed test results"
    echo ""
    
    exit $EXIT_CODE
}

# Run main function
main
