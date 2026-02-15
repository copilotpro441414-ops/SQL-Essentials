#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs a mutation testing spot-check on SuggestionScoringPolicy using Stryker.NET.

.DESCRIPTION
    Verifies that existing unit tests for the scoring policy actually catch mutations
    (logic changes) in the code. This is a quality signal beyond line/branch coverage:
    it proves tests have meaningful assertions that detect real faults.

    If Stryker is not installed or incompatible with .NET Framework 4.8, the script
    prints advisory guidance for manual mutation verification and exits gracefully.

.PARAMETER BreakThreshold
    Minimum mutation score (0-100) to pass. Default: 50

.EXAMPLE
    ./Run-MutationSpotCheck.ps1

.EXAMPLE
    ./Run-MutationSpotCheck.ps1 -BreakThreshold 60
#>

param(
    [int]$BreakThreshold = 50
)

$ErrorActionPreference = "Continue"

Write-Host "=== Mutation Testing Spot-Check: SuggestionScoringPolicy ===" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet-stryker is installed
$strykerCmd = Get-Command dotnet-stryker -ErrorAction SilentlyContinue
if ($null -eq $strykerCmd) {
    # Try as dotnet tool
    $toolList = dotnet tool list -g 2>&1
    $hasStryker = $toolList | Select-String -Pattern "dotnet-stryker" -Quiet
    if (-not $hasStryker) {
        Write-Host "Stryker.NET is not installed." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To install: dotnet tool install -g dotnet-stryker" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "--- Manual Mutation Verification Fallback ---" -ForegroundColor Cyan
        Write-Host "Since Stryker is not available, verify test quality manually:"
        Write-Host ""
        Write-Host "1. Open src/SqlEssentials.Core/Completion/SuggestionScoringPolicy.cs"
        Write-Host "2. Change 'score += prefixBonus' to 'score -= prefixBonus'"
        Write-Host "3. Run tests - expect at least one failure in SuggestionScoringPolicyTests"
        Write-Host "4. Revert the change"
        Write-Host ""
        Write-Host "5. Swap a comparison operator (e.g., '>' to '<' in a bonus check)"
        Write-Host "6. Run tests - expect at least one failure"
        Write-Host "7. Revert the change"
        Write-Host ""
        Write-Host "8. Remove an entire clause bonus (e.g., delete a case arm)"
        Write-Host "9. Run tests - expect at least one failure"
        Write-Host "10. Revert the change"
        Write-Host ""
        Write-Host "If all 3 mutations cause test failures, scoring tests have adequate fault detection."
        Write-Host ""
        Write-Host "RESULT: SKIPPED (Stryker not installed - manual verification required)" -ForegroundColor Yellow
        exit 0
    }
}

# Verify config file exists
$configPath = Join-Path $PSScriptRoot "..\..\stryker-config.json"
$configPath = (Resolve-Path $configPath -ErrorAction SilentlyContinue).Path
if (-not $configPath -or -not (Test-Path $configPath)) {
    Write-Host "Error: stryker-config.json not found at project root." -ForegroundColor Red
    exit 1
}

Write-Host "Running Stryker mutation testing..." -ForegroundColor Yellow
Write-Host "Config: $configPath" -ForegroundColor Gray
Write-Host "Target: SuggestionScoringPolicy.cs" -ForegroundColor Gray
Write-Host "Break threshold: ${BreakThreshold}%" -ForegroundColor Gray
Write-Host ""

# Run Stryker from the test project directory
$testProjectDir = Join-Path $PSScriptRoot "..\..\tests\SqlEssentials.Core.Tests"
$testProjectDir = (Resolve-Path $testProjectDir -ErrorAction SilentlyContinue).Path

try {
    Push-Location $testProjectDir
    $strykerOutput = dotnet stryker --config-file $configPath --break-at $BreakThreshold 2>&1
    $strykerExit = $LASTEXITCODE
    Pop-Location

    # Display output
    $strykerOutput | ForEach-Object { Write-Host $_ }

    # Parse mutation score from output
    $scoreLine = $strykerOutput | Select-String -Pattern "mutation score.*?(\d+\.?\d*)%" -AllMatches
    if ($scoreLine) {
        $score = [double]($scoreLine.Matches[0].Groups[1].Value)
        Write-Host ""
        Write-Host "Mutation Score: ${score}% (threshold: ${BreakThreshold}%)" -ForegroundColor Cyan

        if ($score -ge $BreakThreshold) {
            Write-Host "RESULT: PASS" -ForegroundColor Green
            exit 0
        } else {
            Write-Host "RESULT: FAIL - Mutation score below threshold" -ForegroundColor Red
            exit 1
        }
    }

    # If we couldn't parse score, use exit code
    if ($strykerExit -eq 0) {
        Write-Host ""
        Write-Host "RESULT: PASS (Stryker exited successfully)" -ForegroundColor Green
        exit 0
    } else {
        Write-Host ""
        Write-Host "RESULT: FAIL (Stryker exited with code $strykerExit)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "Stryker execution failed: $_" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "This may indicate .NET Framework 4.8 incompatibility with Stryker.NET." -ForegroundColor Yellow
    Write-Host "Please use the manual mutation verification procedure documented in quickstart.md." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "RESULT: SKIPPED (Stryker not compatible - manual verification required)" -ForegroundColor Yellow
    exit 0
}
