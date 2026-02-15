#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies that the core unit test suite completes within the runtime budget.

.DESCRIPTION
    Runs the full SqlEssentials.Core.Tests suite and measures wall-clock time.
    Fails if the runtime exceeds the specified budget (default: 90 seconds).

    This prevents silent test suite degradation as new tests are added over time.

.PARAMETER MaxSeconds
    Maximum allowed wall-clock seconds for the test run. Default: 90

.PARAMETER ProjectPath
    Path to the test project. Default: .\tests\SqlEssentials.Core.Tests\SqlEssentials.Core.Tests.csproj

.PARAMETER NoBuild
    If set, passes --no-build to dotnet test to skip rebuild.

.EXAMPLE
    ./Verify-TestRuntime.ps1

.EXAMPLE
    ./Verify-TestRuntime.ps1 -MaxSeconds 120

.EXAMPLE
    ./Verify-TestRuntime.ps1 -NoBuild
#>

param(
    [int]$MaxSeconds = 90,
    [string]$ProjectPath = ".\tests\SqlEssentials.Core.Tests\SqlEssentials.Core.Tests.csproj",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

Write-Host "=== Test Runtime Budget Verification ===" -ForegroundColor Cyan
Write-Host ""

# Verify dotnet CLI is available
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnetCmd) {
    Write-Host "ERROR: dotnet CLI not found. Ensure .NET SDK is installed." -ForegroundColor Red
    exit 1
}

# Verify project file exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "ERROR: Project not found at: $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Project:  $ProjectPath" -ForegroundColor Gray
Write-Host "Budget:   ${MaxSeconds}s" -ForegroundColor Gray
Write-Host "No-build: $($NoBuild.IsPresent)" -ForegroundColor Gray
Write-Host ""

# Build arguments
$testArgs = @($ProjectPath, "--verbosity", "quiet")
if ($NoBuild.IsPresent) {
    $testArgs += "--no-build"
}

# Run tests with timing
Write-Host "Running tests..." -ForegroundColor Yellow
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

& dotnet test @testArgs
$testExitCode = $LASTEXITCODE

$stopwatch.Stop()
$elapsed = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)

Write-Host ""

# Check if tests themselves failed
if ($testExitCode -ne 0) {
    Write-Host "Test Runtime: ${elapsed}s" -ForegroundColor Red
    Write-Host "Status:       FAIL - Tests failed (exit code: $testExitCode). Runtime check skipped." -ForegroundColor Red
    exit 1
}

# Compare runtime against budget
$budgetStr = "${MaxSeconds}.0"
if ($elapsed -le $MaxSeconds) {
    $headroom = [Math]::Round($MaxSeconds - $elapsed, 1)
    $headroomPct = [Math]::Round(($headroom / $MaxSeconds) * 100, 1)

    Write-Host "Test Runtime: ${elapsed}s" -ForegroundColor Green
    Write-Host "Budget:       ${budgetStr}s" -ForegroundColor Gray
    Write-Host "Headroom:     ${headroom}s (${headroomPct}%)" -ForegroundColor Green
    Write-Host "Status:       PASS" -ForegroundColor Green
    exit 0
} else {
    $over = [Math]::Round($elapsed - $MaxSeconds, 1)
    $overPct = [Math]::Round(($over / $MaxSeconds) * 100, 1)

    Write-Host "Test Runtime: ${elapsed}s" -ForegroundColor Red
    Write-Host "Budget:       ${budgetStr}s" -ForegroundColor Gray
    Write-Host "Over Budget:  ${over}s (${overPct}%)" -ForegroundColor Red
    Write-Host "Status:       FAIL - Test suite exceeds runtime budget" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Profile slow tests: dotnet test --logger 'console;verbosity=detailed'" -ForegroundColor Gray
    Write-Host "  2. Look for tests creating expensive objects repeatedly (move to shared fixtures)" -ForegroundColor Gray
    Write-Host "  3. Look for tests with unnecessary Task.Delay or real I/O (replace with fakes)" -ForegroundColor Gray
    Write-Host "  4. Consider splitting into fast/slow test categories" -ForegroundColor Gray
    exit 1
}
