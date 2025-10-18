# Metacore Stack - Environment and Dependencies Verification Script
# PowerShell version

# Expected versions
$ExpectedNodeMajor = 20
$ExpectedPnpmMajor = 9
$ExpectedDotnetMajor = 8

# Report file
$ReportFile = "environment-validation-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').md"
$LogDir = "$env:TEMP\metacore-verification"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

# Initialize report
@"
# Metacore Stack - Environment and Dependencies Verification Report

**Date:** $(Get-Date)

"@ | Out-File -FilePath $ReportFile -Encoding UTF8

# Track overall status
$Script:Errors = 0
$Script:Warnings = 0

function Log-Success {
    param($Message)
    Write-Host "✓ $Message" -ForegroundColor Green
    "✓ $Message" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
}

function Log-Warning {
    param($Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
    "⚠ $Message" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
    $Script:Warnings++
}

function Log-Error {
    param($Message)
    Write-Host "✗ $Message" -ForegroundColor Red
    "✗ $Message" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
    $Script:Errors++
}

function Log-Info {
    param($Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
    "ℹ $Message" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
}

function Log-Section {
    param($Title)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
    Write-Host $Title -ForegroundColor Blue
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
    @"

## $Title

"@ | Out-File -FilePath $ReportFile -Append -Encoding UTF8
}

# ========================================================================
# Section 1: Tool Version Validation
# ========================================================================
Log-Section "1. Tool Version Validation"

# Node.js
try {
    $NodeVersion = & node -v 2>&1
    if ($LASTEXITCODE -eq 0) {
        $NodeMajor = [int]($NodeVersion.TrimStart('v').Split('.')[0])
        Log-Info "Node.js version: $NodeVersion"
        
        if ($NodeMajor -eq $ExpectedNodeMajor) {
            Log-Success "Node.js version matches expected (v$ExpectedNodeMajor.x)"
        } else {
            Log-Warning "Node.js version mismatch: found v$NodeMajor.x, expected v$ExpectedNodeMajor.x"
        }
    }
} catch {
    Log-Error "Node.js is not installed"
}

# pnpm
try {
    $PnpmVersion = & pnpm -v 2>&1
    if ($LASTEXITCODE -eq 0) {
        $PnpmMajor = [int]($PnpmVersion.Split('.')[0])
        Log-Info "pnpm version: $PnpmVersion"
        
        if ($PnpmMajor -eq $ExpectedPnpmMajor) {
            Log-Success "pnpm version matches expected (v$ExpectedPnpmMajor.x)"
        } else {
            Log-Warning "pnpm version mismatch: found v$PnpmMajor.x, expected v$ExpectedPnpmMajor.x"
        }
    }
} catch {
    Log-Warning "pnpm is not installed (will be installed during bootstrap)"
}

# .NET
try {
    $DotnetVersion = & dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        $DotnetMajor = [int]($DotnetVersion.Split('.')[0])
        Log-Info ".NET SDK version: $DotnetVersion"
        
        # Get full dotnet info
        @"

### .NET SDK and Runtime Information
``````
"@ | Out-File -FilePath $ReportFile -Append -Encoding UTF8
        & dotnet --info | Out-File -FilePath $ReportFile -Append -Encoding UTF8
        "``````" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
        
        if ($DotnetMajor -ge $ExpectedDotnetMajor) {
            Log-Success ".NET SDK version is compatible (v$DotnetMajor.x >= v$ExpectedDotnetMajor.x)"
        } else {
            Log-Error ".NET SDK version mismatch: found v$DotnetMajor.x, expected v$ExpectedDotnetMajor.x or higher"
        }
    }
} catch {
    Log-Error ".NET SDK is not installed"
}

# Docker (optional)
try {
    $DockerVersion = & docker --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Log-Info "Docker version: $DockerVersion"
        Log-Success "Docker is available"
    }
} catch {
    Log-Warning "Docker is not available (optional for local development)"
}

# ========================================================================
# Section 2: Bootstrap Execution
# ========================================================================
Log-Section "2. Bootstrap Execution"

Log-Info "Running bootstrap script..."
$BootstrapLog = "$LogDir\bootstrap.log"
try {
    if (Test-Path "tools\scripts\bootstrap.ps1") {
        & "tools\scripts\bootstrap.ps1" > $BootstrapLog 2>&1
    } else {
        & make bootstrap > $BootstrapLog 2>&1
    }
    
    if ($LASTEXITCODE -eq 0) {
        Log-Success "Bootstrap completed successfully"
        
        # Check for warnings in bootstrap
        $BootstrapContent = Get-Content $BootstrapLog -Raw
        if ($BootstrapContent -match "warning|warn") {
            Log-Warning "Bootstrap completed with warnings"
            @"

### Bootstrap Warnings
``````
"@ | Out-File -FilePath $ReportFile -Append -Encoding UTF8
            Select-String -Path $BootstrapLog -Pattern "warning|warn" -CaseSensitive:$false | 
                ForEach-Object { $_.Line } | Out-File -FilePath $ReportFile -Append -Encoding UTF8
            "``````" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
        }
    } else {
        Log-Error "Bootstrap failed"
        @"

### Bootstrap Errors
``````
"@ | Out-File -FilePath $ReportFile -Append -Encoding UTF8
        Get-Content $BootstrapLog -Tail 50 | Out-File -FilePath $ReportFile -Append -Encoding UTF8
        "``````" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
    }
} catch {
    Log-Error "Bootstrap failed: $_"
}

# ========================================================================
# Section 3: Specific Package Installations
# ========================================================================
Log-Section "3. Specific Package Installations"

# mcp-ts
Log-Info "Installing mcp-ts dependencies..."
$McpLog = "$LogDir\mcp-install.log"
& pnpm -C mcp-ts install > $McpLog 2>&1
if ($LASTEXITCODE -eq 0) {
    Log-Success "mcp-ts dependencies installed successfully"
} else {
    Log-Error "mcp-ts installation failed"
}

# ui
Log-Info "Installing ui dependencies..."
$UiLog = "$LogDir\ui-install.log"
& pnpm -C ui install > $UiLog 2>&1
if ($LASTEXITCODE -eq 0) {
    Log-Success "ui dependencies installed successfully"
    
    # Check for peer dependency warnings
    $UiContent = Get-Content $UiLog -Raw
    if ($UiContent -match "peer") {
        Log-Warning "ui installation completed with peer dependency warnings"
    }
} else {
    Log-Error "ui installation failed"
}

# server-dotnet
Log-Info "Restoring server-dotnet dependencies..."
$DotnetLog = "$LogDir\dotnet-restore.log"
& dotnet restore server-dotnet > $DotnetLog 2>&1
if ($LASTEXITCODE -eq 0) {
    Log-Success "server-dotnet dependencies restored successfully"
    
    # Check for warnings
    $DotnetContent = Get-Content $DotnetLog -Raw
    if ($DotnetContent -match "warning") {
        Log-Warning "server-dotnet restore completed with warnings"
    }
} else {
    Log-Error "server-dotnet restore failed"
}

# ========================================================================
# Section 4: Service Initialization Tests
# ========================================================================
Log-Section "4. Service Initialization Tests"

Log-Warning "Service initialization tests require manual verification"
Log-Info "To test service initialization:"

@"

### Service Initialization (Manual Steps Required)

To test service initialization, run the following commands in separate terminals:

1. **MCP TypeScript Services:**
   ``````powershell
   pnpm -C mcp-ts dev
   ``````

2. **Room Server (.NET):**
   ``````powershell
   make run-server
   # or
   dotnet run --project server-dotnet/src/RoomServer/RoomServer.csproj
   ``````

Monitor the console output for:
- Any error messages
- Warning messages
- Indications of missing external dependencies
- Successful service startup confirmations
"@ | Out-File -FilePath $ReportFile -Append -Encoding UTF8

# Quick build test
Log-Info "Testing server-dotnet build..."
$BuildLog = "$LogDir\dotnet-build.log"
& dotnet build server-dotnet/src/RoomServer/RoomServer.csproj -c Debug > $BuildLog 2>&1
if ($LASTEXITCODE -eq 0) {
    Log-Success "server-dotnet builds successfully"
} else {
    Log-Error "server-dotnet build failed"
}

Log-Info "Testing mcp-ts build..."
$McpBuildLog = "$LogDir\mcp-build.log"
& pnpm -C mcp-ts build > $McpBuildLog 2>&1
if ($LASTEXITCODE -eq 0) {
    Log-Success "mcp-ts builds successfully"
} else {
    Log-Warning "mcp-ts build completed with issues (may be expected)"
}

# ========================================================================
# Section 5: External Connectivity Checks
# ========================================================================
Log-Section "5. External Connectivity Checks"

# Check if docker-compose is available
if ((Test-Path "infra\docker-compose.yml") -or (Test-Path "infra\docker-compose.yaml")) {
    Log-Info "Docker compose configuration found"
    
    try {
        $ComposeLog = "$LogDir\docker-compose.log"
        & docker compose -f infra\docker-compose.yml config > $ComposeLog 2>&1
        if ($LASTEXITCODE -eq 0) {
            Log-Success "Docker compose configuration is valid"
        } else {
            Log-Warning "Docker compose configuration validation failed"
        }
    } catch {
        Log-Warning "Could not validate docker compose configuration"
    }
    
    @"

### External Services Configuration

To start external services (if needed):
``````powershell
cd infra
docker compose up -d
``````

To check service health:
``````powershell
docker compose ps
``````
"@ | Out-File -FilePath $ReportFile -Append -Encoding UTF8
} else {
    Log-Info "No docker-compose configuration found (external services may not be required)"
}

# ========================================================================
# Summary
# ========================================================================
Log-Section "Summary"

@"

### Results

"@ | Out-File -FilePath $ReportFile -Append -Encoding UTF8

if ($Script:Errors -eq 0 -and $Script:Warnings -eq 0) {
    Log-Success "All checks passed! Environment is ready."
    "**Status:** ✓ All checks passed" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
} elseif ($Script:Errors -eq 0) {
    Log-Warning "Environment verification completed with $($Script:Warnings) warning(s)"
    "**Status:** ⚠ Completed with warnings" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
} else {
    Log-Error "Environment verification failed with $($Script:Errors) error(s) and $($Script:Warnings) warning(s)"
    "**Status:** ✗ Failed" | Out-File -FilePath $ReportFile -Append -Encoding UTF8
}

@"

**Errors:** $($Script:Errors)
**Warnings:** $($Script:Warnings)

**Detailed logs:** $LogDir

"@ | Out-File -FilePath $ReportFile -Append -Encoding UTF8

Log-Info "Full report saved to: $ReportFile"
Log-Info "Detailed logs saved to: $LogDir"

# Exit with appropriate code
if ($Script:Errors -gt 0) {
    exit 1
} else {
    exit 0
}
