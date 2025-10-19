#!/usr/bin/env bash

# Enhanced Integration Test Orchestration
# Executes RoomServer, RoomOperator, and Test Client with comprehensive validation,
# metrics collection, and artifact generation.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

# Default configuration with overrides from environment
ROOMSERVER_HOST="${ROOMSERVER_HOST:-127.0.0.1}"
ROOMSERVER_PORT="${ROOMSERVER_PORT:-40901}"
ROOMOPERATOR_PORT="${ROOMOPERATOR_PORT:-8080}"
TEST_SCENARIOS="${TEST_SCENARIOS:-basic-flow,error-cases}"
LOG_LEVEL="${LOG_LEVEL:-debug}"
READINESS_TIMEOUT="${READINESS_TIMEOUT:-60}"
SCENARIO_TIMEOUT="${SCENARIO_TIMEOUT:-120}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Timestamps
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
ARTIFACTS_DIR="${REPO_ROOT}/.artifacts/integration/${TIMESTAMP}"
LOGS_DIR="${ARTIFACTS_DIR}/logs"
RESULTS_DIR="${ARTIFACTS_DIR}/results"

# Process IDs
ROOMSERVER_PID=""
OPERATOR_PID=""

# Exit codes
EXIT_SUCCESS=0
EXIT_FAILURE=1

# Metrics
START_TIME=$(date +%s)
METRICS_FILE="${RESULTS_DIR}/metrics.json"

print_banner() {
    echo -e "${BOLD}${CYAN}"
    echo "═══════════════════════════════════════════════════════════════"
    echo "  Enhanced RoomOperator-RoomServer Integration Test Suite"
    echo "═══════════════════════════════════════════════════════════════"
    echo -e "${NC}"
}

print_section() {
    echo -e "\n${BOLD}${BLUE}═══ $1 ═══${NC}\n"
}

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

cleanup() {
    print_section "Cleanup"
    
    # Kill background processes
    if [ ! -z "$ROOMSERVER_PID" ]; then
        log_info "Stopping RoomServer (PID: $ROOMSERVER_PID)..."
        kill $ROOMSERVER_PID 2>/dev/null || true
        wait $ROOMSERVER_PID 2>/dev/null || true
    fi
    
    if [ ! -z "$OPERATOR_PID" ]; then
        log_info "Stopping RoomOperator (PID: $OPERATOR_PID)..."
        kill $OPERATOR_PID 2>/dev/null || true
        wait $OPERATOR_PID 2>/dev/null || true
    fi
    
    # Clean up any lingering processes on test ports
    for PORT in $ROOMSERVER_PORT $ROOMOPERATOR_PORT; do
        if command -v lsof &> /dev/null; then
            PIDS=$(lsof -ti:$PORT 2>/dev/null || echo "")
            if [ ! -z "$PIDS" ]; then
                log_info "Killing processes on port $PORT..."
                echo "$PIDS" | xargs kill 2>/dev/null || true
                sleep 1
                REMAINING_PIDS=$(lsof -ti:$PORT 2>/dev/null || echo "")
                if [ ! -z "$REMAINING_PIDS" ]; then
                    echo "$REMAINING_PIDS" | xargs kill -9 2>/dev/null || true
                fi
            fi
        fi
    done
    
    log_success "Cleanup complete"
}

trap cleanup EXIT INT TERM

check_port_available() {
    local port=$1
    if command -v lsof &> /dev/null; then
        if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
            return 1
        fi
    elif command -v netstat &> /dev/null; then
        if netstat -an | grep -E -q "[:.]$port[[:space:]]+.*LISTEN"; then
            return 1
        fi
    fi
    return 0
}

find_available_port() {
    local start_port=$1
    local port=$start_port
    
    while [ $port -lt $(($start_port + 100)) ]; do
        if check_port_available $port; then
            echo $port
            return 0
        fi
        port=$(($port + 1))
    done
    
    return 1
}

wait_for_http_endpoint() {
    local url=$1
    local timeout=$2
    local name=$3
    
    log_info "Waiting for $name to be ready at $url (timeout: ${timeout}s)..."
    
    local elapsed=0
    local backoff=1
    
    while [ $elapsed -lt $timeout ]; do
        if curl -sf "$url" > /dev/null 2>&1; then
            log_success "$name is ready"
            return 0
        fi
        
        sleep $backoff
        elapsed=$(($elapsed + $backoff))
        
        # Exponential backoff, capped at 5 seconds
        backoff=$(($backoff * 2))
        if [ $backoff -gt 5 ]; then
            backoff=5
        fi
    done
    
    log_error "$name failed to become ready within ${timeout}s"
    return 1
}

create_artifacts_structure() {
    print_section "Artifact Structure Setup"
    
    mkdir -p "$LOGS_DIR"
    mkdir -p "$RESULTS_DIR"
    
    log_success "Created artifact directories:"
    log_info "  Root: $ARTIFACTS_DIR"
    log_info "  Logs: $LOGS_DIR"
    log_info "  Results: $RESULTS_DIR"
}

initialize_metrics() {
    cat > "$METRICS_FILE" << EOF
{
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "configuration": {
    "roomserver_host": "$ROOMSERVER_HOST",
    "roomserver_port": $ROOMSERVER_PORT,
    "roomoperator_port": $ROOMOPERATOR_PORT,
    "test_scenarios": "$TEST_SCENARIOS",
    "readiness_timeout": $READINESS_TIMEOUT,
    "scenario_timeout": $SCENARIO_TIMEOUT
  },
  "services": {},
  "scenarios": {},
  "summary": {}
}
EOF
}

update_metrics_json() {
    local jq_expression=$1
    local tmp_file="${METRICS_FILE}.tmp"
    
    if [ -f "$METRICS_FILE" ]; then
        jq "$jq_expression" "$METRICS_FILE" > "$tmp_file" && mv "$tmp_file" "$METRICS_FILE"
    fi
}

parse_test_results() {
    local log_file=$1
    local default_failed=${2:-0}
    
    local passed=$(grep -oP '✓ Passed: \K\d+' "$log_file" 2>/dev/null | tail -1)
    local failed=$(grep -oP '✗ Failed: \K\d+' "$log_file" 2>/dev/null | tail -1)
    
    # Use defaults if grep found nothing
    passed=${passed:-0}
    failed=${failed:-$default_failed}
    
    echo "$passed $failed"
}

record_service_metric() {
    local service=$1
    local metric=$2
    local value=$3
    
    update_metrics_json ".services.\"$service\".\"$metric\" = $value"
}

record_scenario_result() {
    local scenario=$1
    local passed=$2
    local failed=$3
    local duration=$4
    
    update_metrics_json ".scenarios.\"$scenario\" = {\"passed\": $passed, \"failed\": $failed, \"duration_ms\": $duration}"
}

build_projects() {
    print_section "Build Projects"
    
    cd "$REPO_ROOT"
    
    log_info "Building .NET solution..."
    if dotnet build -c Debug server-dotnet/RoomServer.sln > "${LOGS_DIR}/build.log" 2>&1; then
        log_success "Build completed successfully"
    else
        log_error "Build failed. Check ${LOGS_DIR}/build.log"
        tail -20 "${LOGS_DIR}/build.log"
        exit $EXIT_FAILURE
    fi
}

start_roomserver() {
    print_section "Start RoomServer"
    
    # Check if port is available, find alternative if needed
    if ! check_port_available $ROOMSERVER_PORT; then
        log_warn "Port $ROOMSERVER_PORT is occupied, finding alternative..."
        ROOMSERVER_PORT=$(find_available_port $ROOMSERVER_PORT)
        if [ $? -ne 0 ]; then
            log_error "Could not find available port for RoomServer"
            exit $EXIT_FAILURE
        fi
        log_info "Using port $ROOMSERVER_PORT for RoomServer"
    fi
    
    cd "${REPO_ROOT}/server-dotnet/src/RoomServer"
    
    export ASPNETCORE_ENVIRONMENT=Test
    export ASPNETCORE_URLS="http://${ROOMSERVER_HOST}:${ROOMSERVER_PORT}"
    
    log_info "Starting RoomServer on http://${ROOMSERVER_HOST}:${ROOMSERVER_PORT}..."
    
    local start_time=$(date +%s%3N)
    dotnet run --no-build -c Debug > "${LOGS_DIR}/roomserver.log" 2>&1 &
    ROOMSERVER_PID=$!
    
    log_info "RoomServer started (PID: $ROOMSERVER_PID)"
    
    # Wait for readiness
    local health_url="http://${ROOMSERVER_HOST}:${ROOMSERVER_PORT}/health"
    if ! wait_for_http_endpoint "$health_url" $READINESS_TIMEOUT "RoomServer"; then
        log_error "RoomServer failed to start. Last 50 lines of log:"
        tail -50 "${LOGS_DIR}/roomserver.log"
        exit $EXIT_FAILURE
    fi
    
    local end_time=$(date +%s%3N)
    local startup_time=$(($end_time - $start_time))
    
    record_service_metric "roomserver" "startup_time_ms" $startup_time
    record_service_metric "roomserver" "port" $ROOMSERVER_PORT
    record_service_metric "roomserver" "pid" $ROOMSERVER_PID
    
    log_success "RoomServer ready (startup: ${startup_time}ms)"
}

start_roomoperator() {
    print_section "Start RoomOperator"
    
    # Check if port is available, find alternative if needed
    if ! check_port_available $ROOMOPERATOR_PORT; then
        log_warn "Port $ROOMOPERATOR_PORT is occupied, finding alternative..."
        ROOMOPERATOR_PORT=$(find_available_port $ROOMOPERATOR_PORT)
        if [ $? -ne 0 ]; then
            log_error "Could not find available port for RoomOperator"
            exit $EXIT_FAILURE
        fi
        log_info "Using port $ROOMOPERATOR_PORT for RoomOperator"
    fi
    
    cd "${REPO_ROOT}/server-dotnet/operator"
    
    export ROOM_AUTH_TOKEN="${ROOM_AUTH_TOKEN:-test-token}"
    export HttpApi__Port=$ROOMOPERATOR_PORT
    export RoomServer__BaseUrl="http://${ROOMSERVER_HOST}:${ROOMSERVER_PORT}"
    
    log_info "Starting RoomOperator on http://localhost:${ROOMOPERATOR_PORT}..."
    log_info "  Connected to RoomServer: http://${ROOMSERVER_HOST}:${ROOMSERVER_PORT}"
    
    local start_time=$(date +%s%3N)
    dotnet run --no-build -c Debug > "${LOGS_DIR}/roomoperator.log" 2>&1 &
    OPERATOR_PID=$!
    
    log_info "RoomOperator started (PID: $OPERATOR_PID)"
    
    # Wait for readiness
    local health_url="http://localhost:${ROOMOPERATOR_PORT}/health"
    if ! wait_for_http_endpoint "$health_url" $READINESS_TIMEOUT "RoomOperator"; then
        log_error "RoomOperator failed to start. Last 50 lines of log:"
        tail -50 "${LOGS_DIR}/roomoperator.log"
        exit $EXIT_FAILURE
    fi
    
    local end_time=$(date +%s%3N)
    local startup_time=$(($end_time - $start_time))
    
    record_service_metric "roomoperator" "startup_time_ms" $startup_time
    record_service_metric "roomoperator" "port" $ROOMOPERATOR_PORT
    record_service_metric "roomoperator" "pid" $OPERATOR_PID
    
    log_success "RoomOperator ready (startup: ${startup_time}ms)"
}

install_test_client_deps() {
    cd "${REPO_ROOT}/server-dotnet/operator/test-client"
    
    if [ ! -d "node_modules" ]; then
        log_info "Installing test client dependencies..."
        npm install > "${LOGS_DIR}/npm-install.log" 2>&1
        log_success "Dependencies installed"
    else
        log_info "Dependencies already installed"
    fi
}

run_test_scenarios() {
    print_section "Execute Test Scenarios"
    
    cd "${REPO_ROOT}/server-dotnet/operator/test-client"
    
    install_test_client_deps
    
    # Set environment variables for test client
    export OPERATOR_URL="http://localhost:${ROOMOPERATOR_PORT}"
    export ROOMSERVER_URL="http://${ROOMSERVER_HOST}:${ROOMSERVER_PORT}"
    export TEST_ROOM_ID="room-test-integration-${TIMESTAMP}"
    export VERBOSE=true
    export ARTIFACTS_DIR="${ARTIFACTS_DIR}"
    
    # Parse scenarios
    IFS=',' read -ra SCENARIO_ARRAY <<< "$TEST_SCENARIOS"
    
    local all_passed=0
    local all_failed=0
    local scenario_results=()
    
    for scenario in "${SCENARIO_ARRAY[@]}"; do
        scenario=$(echo "$scenario" | xargs) # trim whitespace
        
        log_info "Running scenario: $scenario"
        
        local start_time=$(date +%s%3N)
        local scenario_log="${LOGS_DIR}/test-client-${scenario}.log"
        
        local exit_code=0

        case "$scenario" in
            basic-flow|basic)
                errexit_was_set=false
                [[ $- == *e* ]] && errexit_was_set=true
                set +e
                timeout $SCENARIO_TIMEOUT npm run test:basic-enhanced > "$scenario_log" 2>&1
                exit_code=$?
                $errexit_was_set && set -e
                ;;
            error-cases|error)
                errexit_was_set=false
                [[ $- == *e* ]] && errexit_was_set=true
                set +e
                timeout $SCENARIO_TIMEOUT npm run test:error > "$scenario_log" 2>&1
                exit_code=$?
                $errexit_was_set && set -e
                ;;
            stress-test|stress)
                errexit_was_set=false
                [[ $- == *e* ]] && errexit_was_set=true
                set +e
                timeout $SCENARIO_TIMEOUT npm run test:stress > "$scenario_log" 2>&1
                exit_code=$?
                $errexit_was_set && set -e
                ;;
            *)
                log_warn "Unknown scenario: $scenario, skipping"
                continue
                ;;
        esac

        local end_time=$(date +%s%3N)
        local duration=$(($end_time - $start_time))
        
        if [ $exit_code -eq 0 ]; then
            log_success "Scenario '$scenario' passed (${duration}ms)"
            
            # Parse test results from log
            read passed failed <<< $(parse_test_results "$scenario_log" 0)
            
            all_passed=$(($all_passed + $passed))
            all_failed=$(($all_failed + $failed))
            
            record_scenario_result "$scenario" $passed $failed $duration
            scenario_results+=("$scenario:PASS:$duration")
        else
            log_error "Scenario '$scenario' failed (exit code: $exit_code)"
            
            # Parse test results from log (default to 1 failure if no results found)
            read passed failed <<< $(parse_test_results "$scenario_log" 1)
            
            all_passed=$(($all_passed + $passed))
            all_failed=$(($all_failed + $failed))
            
            record_scenario_result "$scenario" $passed $failed $duration
            scenario_results+=("$scenario:FAIL:$duration")
            
            log_error "Last 30 lines of scenario log:"
            tail -30 "$scenario_log"
        fi
    done
    
    # Store overall results
    echo "$all_passed" > "${RESULTS_DIR}/tests_passed.txt"
    echo "$all_failed" > "${RESULTS_DIR}/tests_failed.txt"
    
    return $([ $all_failed -eq 0 ] && echo 0 || echo 1)
}

generate_report() {
    print_section "Generate Report"
    
    local end_time=$(date +%s)
    local total_duration=$(($end_time - $START_TIME))
    
    local tests_passed=$(cat "${RESULTS_DIR}/tests_passed.txt" 2>/dev/null || echo "0")
    local tests_failed=$(cat "${RESULTS_DIR}/tests_failed.txt" 2>/dev/null || echo "0")
    local tests_total=$(($tests_passed + $tests_failed))
    local success_rate=0
    
    if [ $tests_total -gt 0 ]; then
        success_rate=$(awk "BEGIN {printf \"%.2f\", ($tests_passed / $tests_total) * 100}")
    fi
    
    # Update metrics with summary
    update_metrics_json ".summary = {
        \"total_duration_s\": $total_duration,
        \"tests_total\": $tests_total,
        \"tests_passed\": $tests_passed,
        \"tests_failed\": $tests_failed,
        \"success_rate\": $success_rate,
        \"status\": \"$([ $tests_failed -eq 0 ] && echo 'SUCCESS' || echo 'FAILED')\"
    }"
    
    # Generate human-readable report
    cat > "${RESULTS_DIR}/report.txt" << EOF
═══════════════════════════════════════════════════════════════
  Integration Test Report
═══════════════════════════════════════════════════════════════

Timestamp: $(date -u +%Y-%m-%dT%H:%M:%SZ)
Duration: ${total_duration}s
Artifacts: $ARTIFACTS_DIR

Configuration:
  RoomServer: http://${ROOMSERVER_HOST}:${ROOMSERVER_PORT}
  RoomOperator: http://localhost:${ROOMOPERATOR_PORT}
  Scenarios: $TEST_SCENARIOS

Test Results:
  Total Tests: $tests_total
  Passed: $tests_passed
  Failed: $tests_failed
  Success Rate: ${success_rate}%

Status: $([ $tests_failed -eq 0 ] && echo 'SUCCESS ✓' || echo 'FAILED ✗')

Logs:
  RoomServer: ${LOGS_DIR}/roomserver.log
  RoomOperator: ${LOGS_DIR}/roomoperator.log
  Test Client: ${LOGS_DIR}/test-client-*.log

Metrics: ${RESULTS_DIR}/metrics.json

═══════════════════════════════════════════════════════════════
EOF
    
    # Display report
    cat "${RESULTS_DIR}/report.txt"
    
    log_success "Report generated: ${RESULTS_DIR}/report.txt"
    log_success "Metrics generated: ${RESULTS_DIR}/metrics.json"
}

print_final_summary() {
    print_section "Final Summary"
    
    local tests_passed=$(cat "${RESULTS_DIR}/tests_passed.txt" 2>/dev/null || echo "0")
    local tests_failed=$(cat "${RESULTS_DIR}/tests_failed.txt" 2>/dev/null || echo "0")
    
    echo -e "${BOLD}Artifacts Directory:${NC}"
    echo -e "  ${CYAN}$ARTIFACTS_DIR${NC}"
    echo ""
    
    if [ $tests_failed -eq 0 ]; then
        echo -e "${BOLD}${GREEN}"
        echo "═══════════════════════════════════════════════════════════════"
        echo "  Integration Test: SUCCESS ✓"
        echo "  All $tests_passed tests passed"
        echo "═══════════════════════════════════════════════════════════════"
        echo -e "${NC}"
        return 0
    else
        echo -e "${BOLD}${RED}"
        echo "═══════════════════════════════════════════════════════════════"
        echo "  Integration Test: FAILED ✗"
        echo "  $tests_failed of $(($tests_passed + $tests_failed)) tests failed"
        echo "═══════════════════════════════════════════════════════════════"
        echo -e "${NC}"
        
        echo -e "\n${YELLOW}Diagnostics - Last 100 lines of each service log:${NC}\n"
        
        echo -e "${BOLD}RoomServer Log:${NC}"
        tail -100 "${LOGS_DIR}/roomserver.log" || echo "  (log not available)"
        echo ""
        
        echo -e "${BOLD}RoomOperator Log:${NC}"
        tail -100 "${LOGS_DIR}/roomoperator.log" || echo "  (log not available)"
        echo ""
        
        return 1
    fi
}

# Main execution flow
main() {
    print_banner
    
    # Ensure we have required tools
    command -v dotnet >/dev/null 2>&1 || { log_error "dotnet is required but not installed"; exit 1; }
    command -v node >/dev/null 2>&1 || { log_error "node is required but not installed"; exit 1; }
    command -v curl >/dev/null 2>&1 || { log_error "curl is required but not installed"; exit 1; }
    command -v jq >/dev/null 2>&1 || { log_error "jq is required but not installed"; exit 1; }
    
    # Setup
    create_artifacts_structure
    initialize_metrics
    
    # Build
    build_projects
    
    # Start services
    start_roomserver
    start_roomoperator
    
    # Run tests
    if run_test_scenarios; then
        generate_report
        print_final_summary
        exit $EXIT_SUCCESS
    else
        generate_report
        print_final_summary
        exit $EXIT_FAILURE
    fi
}

# Run main
main "$@"
