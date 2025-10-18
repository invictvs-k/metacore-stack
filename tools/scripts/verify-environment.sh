#!/usr/bin/env bash
set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Expected versions
EXPECTED_NODE_MAJOR=20
EXPECTED_PNPM_MAJOR=9
EXPECTED_DOTNET_MAJOR=8

# Report file
REPORT_FILE="environment-validation-report-$(date +%Y%m%d-%H%M%S).md"
LOG_DIR="/tmp/metacore-verification"
mkdir -p "$LOG_DIR"

# Initialize report
echo "# Metacore Stack - Environment and Dependencies Verification Report" > "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "**Date:** $(date)" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

log_success() {
    echo -e "${GREEN}✓${NC} $1"
    echo "✓ $1" >> "$REPORT_FILE"
}

log_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
    echo "⚠ $1" >> "$REPORT_FILE"
}

log_error() {
    echo -e "${RED}✗${NC} $1"
    echo "✗ $1" >> "$REPORT_FILE"
}

log_info() {
    echo -e "${BLUE}ℹ${NC} $1"
    echo "ℹ $1" >> "$REPORT_FILE"
}

log_section() {
    echo ""
    echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
    echo "" >> "$REPORT_FILE"
    echo "## $1" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
}

# Track overall status
ERRORS=0
WARNINGS=0

# ========================================================================
# Section 1: Tool Version Validation
# ========================================================================
log_section "1. Tool Version Validation"

# Node.js
if command -v node >/dev/null 2>&1; then
    NODE_VERSION=$(node -v)
    NODE_MAJOR=$(echo "$NODE_VERSION" | cut -d'v' -f2 | cut -d'.' -f1)
    log_info "Node.js version: $NODE_VERSION"
    
    if [ "$NODE_MAJOR" -eq "$EXPECTED_NODE_MAJOR" ]; then
        log_success "Node.js version matches expected (v${EXPECTED_NODE_MAJOR}.x)"
    else
        log_warning "Node.js version mismatch: found v${NODE_MAJOR}.x, expected v${EXPECTED_NODE_MAJOR}.x"
        WARNINGS=$((WARNINGS + 1))
    fi
else
    log_error "Node.js is not installed"
    ERRORS=$((ERRORS + 1))
fi

# pnpm
if command -v pnpm >/dev/null 2>&1; then
    PNPM_VERSION=$(pnpm -v)
    PNPM_MAJOR=$(echo "$PNPM_VERSION" | cut -d'.' -f1)
    log_info "pnpm version: $PNPM_VERSION"
    
    if [ "$PNPM_MAJOR" -eq "$EXPECTED_PNPM_MAJOR" ]; then
        log_success "pnpm version matches expected (v${EXPECTED_PNPM_MAJOR}.x)"
    else
        log_warning "pnpm version mismatch: found v${PNPM_MAJOR}.x, expected v${EXPECTED_PNPM_MAJOR}.x"
        WARNINGS=$((WARNINGS + 1))
    fi
else
    log_warning "pnpm is not installed (will be installed during bootstrap)"
    WARNINGS=$((WARNINGS + 1))
fi

# .NET
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version)
    DOTNET_MAJOR=$(echo "$DOTNET_VERSION" | cut -d'.' -f1)
    log_info ".NET SDK version: $DOTNET_VERSION"
    
    # Get full dotnet info
    echo "" >> "$REPORT_FILE"
    echo "### .NET SDK and Runtime Information" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    dotnet --info >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    
    if [ "$DOTNET_MAJOR" -ge "$EXPECTED_DOTNET_MAJOR" ]; then
        log_success ".NET SDK version is compatible (v${DOTNET_MAJOR}.x >= v${EXPECTED_DOTNET_MAJOR}.x)"
    else
        log_error ".NET SDK version mismatch: found v${DOTNET_MAJOR}.x, expected v${EXPECTED_DOTNET_MAJOR}.x or higher"
        ERRORS=$((ERRORS + 1))
    fi
else
    log_error ".NET SDK is not installed"
    ERRORS=$((ERRORS + 1))
fi

# Docker (optional)
if command -v docker >/dev/null 2>&1; then
    DOCKER_VERSION=$(docker --version)
    log_info "Docker version: $DOCKER_VERSION"
    log_success "Docker is available"
else
    log_warning "Docker is not available (optional for local development)"
    WARNINGS=$((WARNINGS + 1))
fi

# ========================================================================
# Section 2: Bootstrap Execution
# ========================================================================
log_section "2. Bootstrap Execution"

log_info "Running 'make bootstrap'..."
BOOTSTRAP_LOG="$LOG_DIR/bootstrap.log"
if make bootstrap > "$BOOTSTRAP_LOG" 2>&1; then
    log_success "Bootstrap completed successfully"
    
    # Check for warnings in bootstrap
    if grep -i "warning\|warn" "$BOOTSTRAP_LOG" >/dev/null; then
        log_warning "Bootstrap completed with warnings:"
        echo "" >> "$REPORT_FILE"
        echo "### Bootstrap Warnings" >> "$REPORT_FILE"
        echo '```' >> "$REPORT_FILE"
        grep -i "warning\|warn" "$BOOTSTRAP_LOG" >> "$REPORT_FILE"
        echo '```' >> "$REPORT_FILE"
        WARNINGS=$((WARNINGS + 1))
    fi
else
    log_error "Bootstrap failed"
    echo "" >> "$REPORT_FILE"
    echo "### Bootstrap Errors" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    tail -50 "$BOOTSTRAP_LOG" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    ERRORS=$((ERRORS + 1))
fi

# ========================================================================
# Section 3: Specific Package Installations
# ========================================================================
log_section "3. Specific Package Installations"

# mcp-ts
log_info "Installing mcp-ts dependencies..."
MCP_LOG="$LOG_DIR/mcp-install.log"
if pnpm -C mcp-ts install > "$MCP_LOG" 2>&1; then
    log_success "mcp-ts dependencies installed successfully"
else
    log_error "mcp-ts installation failed"
    echo "" >> "$REPORT_FILE"
    echo "### mcp-ts Installation Errors" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    tail -30 "$MCP_LOG" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    ERRORS=$((ERRORS + 1))
fi

# ui
log_info "Installing ui dependencies..."
UI_LOG="$LOG_DIR/ui-install.log"
if pnpm -C ui install > "$UI_LOG" 2>&1; then
    log_success "ui dependencies installed successfully"
    
    # Check for peer dependency warnings
    if grep -i "peer" "$UI_LOG" >/dev/null; then
        log_warning "ui installation completed with peer dependency warnings"
        echo "" >> "$REPORT_FILE"
        echo "### UI Peer Dependency Warnings" >> "$REPORT_FILE"
        echo '```' >> "$REPORT_FILE"
        grep -A 5 "peer" "$UI_LOG" >> "$REPORT_FILE"
        echo '```' >> "$REPORT_FILE"
        WARNINGS=$((WARNINGS + 1))
    fi
else
    log_error "ui installation failed"
    echo "" >> "$REPORT_FILE"
    echo "### UI Installation Errors" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    tail -30 "$UI_LOG" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    ERRORS=$((ERRORS + 1))
fi

# server-dotnet
log_info "Restoring server-dotnet dependencies..."
DOTNET_LOG="$LOG_DIR/dotnet-restore.log"
if dotnet restore server-dotnet > "$DOTNET_LOG" 2>&1; then
    log_success "server-dotnet dependencies restored successfully"
    
    # Check for warnings
    if grep -i "warning" "$DOTNET_LOG" >/dev/null; then
        log_warning "server-dotnet restore completed with warnings"
        echo "" >> "$REPORT_FILE"
        echo "### server-dotnet Restore Warnings" >> "$REPORT_FILE"
        echo '```' >> "$REPORT_FILE"
        grep -i "warning" "$DOTNET_LOG" >> "$REPORT_FILE"
        echo '```' >> "$REPORT_FILE"
        WARNINGS=$((WARNINGS + 1))
    fi
else
    log_error "server-dotnet restore failed"
    echo "" >> "$REPORT_FILE"
    echo "### server-dotnet Restore Errors" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    tail -30 "$DOTNET_LOG" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    ERRORS=$((ERRORS + 1))
fi

# ========================================================================
# Section 4: Service Initialization Tests
# ========================================================================
log_section "4. Service Initialization Tests"

log_warning "Service initialization tests require manual verification"
log_info "To test service initialization:"
echo "" >> "$REPORT_FILE"
echo "### Service Initialization (Manual Steps Required)" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "To test service initialization, run the following commands in separate terminals:" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "1. **MCP TypeScript Services:**" >> "$REPORT_FILE"
echo '   ```bash' >> "$REPORT_FILE"
echo "   pnpm -C mcp-ts dev" >> "$REPORT_FILE"
echo '   ```' >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "2. **Room Server (.NET):**" >> "$REPORT_FILE"
echo '   ```bash' >> "$REPORT_FILE"
echo "   make run-server" >> "$REPORT_FILE"
echo '   ```' >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "Monitor the console output for:" >> "$REPORT_FILE"
echo "- Any error messages" >> "$REPORT_FILE"
echo "- Warning messages" >> "$REPORT_FILE"
echo "- Indications of missing external dependencies" >> "$REPORT_FILE"
echo "- Successful service startup confirmations" >> "$REPORT_FILE"

# Quick build test instead of full service run
log_info "Testing server-dotnet build..."
BUILD_LOG="$LOG_DIR/dotnet-build.log"
if dotnet build server-dotnet/src/RoomServer/RoomServer.csproj -c Debug > "$BUILD_LOG" 2>&1; then
    log_success "server-dotnet builds successfully"
else
    log_error "server-dotnet build failed"
    echo "" >> "$REPORT_FILE"
    echo "### server-dotnet Build Errors" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    tail -30 "$BUILD_LOG" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    ERRORS=$((ERRORS + 1))
fi

log_info "Testing mcp-ts build..."
MCP_BUILD_LOG="$LOG_DIR/mcp-build.log"
if pnpm -C mcp-ts build > "$MCP_BUILD_LOG" 2>&1; then
    log_success "mcp-ts builds successfully"
else
    # Build failure might be expected for some packages
    log_warning "mcp-ts build completed with issues (may be expected)"
    echo "" >> "$REPORT_FILE"
    echo "### mcp-ts Build Output" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    tail -30 "$MCP_BUILD_LOG" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    WARNINGS=$((WARNINGS + 1))
fi

# ========================================================================
# Section 5: External Connectivity Checks
# ========================================================================
log_section "5. External Connectivity Checks"

# Check if docker-compose is available and services are configured
if [ -f "infra/docker-compose.yml" ] || [ -f "infra/docker-compose.yaml" ]; then
    log_info "Docker compose configuration found"
    
    if command -v docker >/dev/null 2>&1; then
        log_info "Checking docker compose configuration..."
        COMPOSE_LOG="$LOG_DIR/docker-compose.log"
        if docker compose -f infra/docker-compose.yml config > "$COMPOSE_LOG" 2>&1 || docker compose -f infra/docker-compose.yaml config > "$COMPOSE_LOG" 2>&1; then
            log_success "Docker compose configuration is valid"
        else
            log_warning "Docker compose configuration validation failed or no compose file found"
            WARNINGS=$((WARNINGS + 1))
        fi
    fi
    
    echo "" >> "$REPORT_FILE"
    echo "### External Services Configuration" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    echo "To start external services (if needed):" >> "$REPORT_FILE"
    echo '```bash' >> "$REPORT_FILE"
    echo "cd infra && docker compose up -d" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    echo "To check service health:" >> "$REPORT_FILE"
    echo '```bash' >> "$REPORT_FILE"
    echo "docker compose ps" >> "$REPORT_FILE"
    echo '```' >> "$REPORT_FILE"
else
    log_info "No docker-compose configuration found (external services may not be required)"
fi

# ========================================================================
# Summary
# ========================================================================
log_section "Summary"

echo "" >> "$REPORT_FILE"
echo "### Results" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

if [ $ERRORS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
    log_success "All checks passed! Environment is ready."
    echo "**Status:** ✓ All checks passed" >> "$REPORT_FILE"
elif [ $ERRORS -eq 0 ]; then
    log_warning "Environment verification completed with $WARNINGS warning(s)"
    echo "**Status:** ⚠ Completed with warnings" >> "$REPORT_FILE"
else
    log_error "Environment verification failed with $ERRORS error(s) and $WARNINGS warning(s)"
    echo "**Status:** ✗ Failed" >> "$REPORT_FILE"
fi

echo "" >> "$REPORT_FILE"
echo "**Errors:** $ERRORS" >> "$REPORT_FILE"
echo "**Warnings:** $WARNINGS" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "**Detailed logs:** $LOG_DIR" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

log_info "Full report saved to: $REPORT_FILE"
log_info "Detailed logs saved to: $LOG_DIR"

# Exit with appropriate code
if [ $ERRORS -gt 0 ]; then
    exit 1
else
    exit 0
fi
