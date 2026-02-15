#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies that core modules meet per-module coverage gates.

.DESCRIPTION
    Runs unit tests with coverage collection and verifies that each high-risk module
    meets its own minimum coverage thresholds:
    - Completion/Context: line >= 85%, branch >= 85% (pure logic, high user impact)
    - Metadata/Logging:   line >= 75%, branch >= 70% (infrastructure boundaries)

.PARAMETER TestResultsDir
    Directory where test results and coverage reports are stored. Default: ./TestResults

.PARAMETER CoverageReportPath
    Path to a specific Cobertura XML report. If omitted, the latest report in TestResultsDir is used.

.PARAMETER FailOnViolation
    If true, exits with non-zero code when coverage gates are violated. Default: true

.EXAMPLE
    ./Verify-CoreCoverage.ps1
    
.EXAMPLE
    ./Verify-CoreCoverage.ps1 -CoverageReportPath ./TestResults/abc/coverage.cobertura.xml

.EXAMPLE
    ./Verify-CoreCoverage.ps1 -TestResultsDir ./coverage -FailOnViolation $false
#>

param(
    [string]$TestResultsDir = "./TestResults",
    [string]$CoverageReportPath = "",
    [bool]$FailOnViolation = $true
)

$ErrorActionPreference = "Stop"

Write-Host "=== Core Module Coverage Verification ===" -ForegroundColor Cyan
Write-Host ""

# Define per-module coverage thresholds (differentiated by risk profile)
# Completion/Context: pure logic, high user impact => strict gates
# Metadata/Logging: infrastructure boundaries, some paths integration-only => relaxed gates
$moduleThresholds = @{
    "Completion" = @{ Line = 85.0; Branch = 85.0 }
    "Context"    = @{ Line = 85.0; Branch = 85.0 }
    "Metadata"   = @{ Line = 75.0; Branch = 70.0 }
    "Logging"    = @{ Line = 75.0; Branch = 70.0 }
}
$requiredModules = @("Completion", "Context", "Metadata", "Logging")

# Run tests with coverage
Write-Host "Running tests with coverage collection..." -ForegroundColor Yellow
$testProject = "tests/SqlEssentials.Core.Tests/SqlEssentials.Core.Tests.csproj"
$runSettings = "tests/SqlEssentials.Core.Tests/coverage.runsettings"

dotnet test $testProject `
    --no-build `
    --settings $runSettings `
    --results-directory $TestResultsDir `
    --collect:"XPlat Code Coverage"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Coverage collection complete." -ForegroundColor Green
Write-Host ""

# Resolve coverage file
if ($CoverageReportPath -ne "") {
    if (-not (Test-Path $CoverageReportPath)) {
        Write-Host "Error: Coverage report not found at $CoverageReportPath" -ForegroundColor Red
        exit 1
    }
    $coverageFile = $CoverageReportPath
} else {
    $coverageFiles = Get-ChildItem -Path $TestResultsDir -Filter "coverage.cobertura.xml" -Recurse
    if ($coverageFiles.Count -eq 0) {
        Write-Host "Error: No coverage file found in $TestResultsDir" -ForegroundColor Red
        exit 1
    }
    $coverageFile = ($coverageFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
}
Write-Host "Analyzing coverage from: $coverageFile" -ForegroundColor Yellow
Write-Host ""

[xml]$coverage = Get-Content -Path $coverageFile

function Get-ModuleFromFilename {
    param([string]$Filename)

    if ([string]::IsNullOrWhiteSpace($Filename)) {
        return $null
    }

    $normalized = $Filename -replace '\\', '/'
    if ($normalized -match '(^|/)Completion/') { return 'Completion' }
    if ($normalized -match '(^|/)Context/') { return 'Context' }
    if ($normalized -match '(^|/)Metadata/') { return 'Metadata' }
    if ($normalized -match '(^|/)Logging/') { return 'Logging' }

    return $null
}

$moduleStats = @{}
foreach ($module in $requiredModules) {
    $moduleStats[$module] = [pscustomobject]@{
        Module = $module
        LinesCovered = 0.0
        LinesValid = 0.0
        BranchesCovered = 0.0
        BranchesValid = 0.0
        LineCoverage = 0.0
        BranchCoverage = 0.0
    }
}

$classNodes = @($coverage.coverage.packages.package.classes.class)
foreach ($classNode in $classNodes) {
    $module = Get-ModuleFromFilename -Filename $classNode.filename
    if ($null -eq $module -or -not $moduleStats.ContainsKey($module)) {
        continue
    }

    $lineCovered = 0.0
    $lineValid = 0.0
    $branchCovered = 0.0
    $branchValid = 0.0

    $lineNodes = @($classNode.lines.line)
    foreach ($lineNode in $lineNodes) {
        $lineValid += 1.0
        if ([int]$lineNode.hits -gt 0) {
            $lineCovered += 1.0
        }

        if (($lineNode.branch -eq 'True') -and $lineNode.'condition-coverage') {
            if ($lineNode.'condition-coverage' -match '\((\d+)/(\d+)\)') {
                $branchCovered += [double]$Matches[1]
                $branchValid += [double]$Matches[2]
            }
        }
    }

    $stats = $moduleStats[$module]
    $stats.LinesCovered += $lineCovered
    $stats.LinesValid += $lineValid
    $stats.BranchesCovered += $branchCovered
    $stats.BranchesValid += $branchValid
}

foreach ($module in $requiredModules) {
    $stats = $moduleStats[$module]
    $stats.LineCoverage = if ($stats.LinesValid -gt 0) { ($stats.LinesCovered / $stats.LinesValid) * 100.0 } else { 0.0 }
    $stats.BranchCoverage = if ($stats.BranchesValid -gt 0) { ($stats.BranchesCovered / $stats.BranchesValid) * 100.0 } else { 0.0 }
}

Write-Host "Coverage Results:" -ForegroundColor Cyan
Write-Host ""
Write-Host ("{0,-12} | {1,7} | {2,6} | {3,8} | {4,6} | {5}" -f "Module", "Line%", "Gate", "Branch%", "Gate", "Status")
Write-Host ("{0,-12} | {1,7} | {2,6} | {3,8} | {4,6} | {5}" -f "------------", "-------", "------", "--------", "------", "------")
foreach ($module in $requiredModules) {
    $stats = $moduleStats[$module]
    $thresh = $moduleThresholds[$module]
    $line = [Math]::Round($stats.LineCoverage, 2)
    $branch = [Math]::Round($stats.BranchCoverage, 2)
    $lineGate = $thresh.Line
    $branchGate = $thresh.Branch
    $lineOk = $line -ge $lineGate
    $branchOk = $branch -ge $branchGate
    $status = if ($lineOk -and $branchOk) { 'PASS' } else { 'FAIL' }
    $color = if ($status -eq 'PASS') { 'Green' } else { 'Red' }
    Write-Host ("{0,-12} | {1,6}% | {2,5}% | {3,7}% | {4,5}% | {5}" -f $module, $line, $lineGate, $branch, $branchGate, $status) -ForegroundColor $color
}

$violations = @()
foreach ($module in $requiredModules) {
    $stats = $moduleStats[$module]
    $thresh = $moduleThresholds[$module]
    if ($stats.LinesValid -eq 0) {
        $violations += "${module}: WARNING - no lines found in coverage report (module missing?)"
    }
    if ($stats.LineCoverage -lt $thresh.Line) {
        $violations += "${module}: line coverage $([Math]::Round($stats.LineCoverage, 2))% < $($thresh.Line)%"
    }
    if ($stats.BranchCoverage -lt $thresh.Branch) {
        $violations += "${module}: branch coverage $([Math]::Round($stats.BranchCoverage, 2))% < $($thresh.Branch)%"
    }
}

Write-Host ""
if ($violations.Count -eq 0) {
    Write-Host "✓ All core module coverage gates passed." -ForegroundColor Green
    exit 0
}

Write-Host "✗ Coverage gate violations:" -ForegroundColor Red
foreach ($violation in $violations) {
    Write-Host ("  - {0}" -f $violation) -ForegroundColor Red
}

if ($FailOnViolation) {
    exit 1
}

exit 0
