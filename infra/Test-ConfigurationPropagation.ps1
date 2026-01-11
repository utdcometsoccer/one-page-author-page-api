#!/usr/bin/env pwsh
# =========================================
# Test Configuration Propagation
# =========================================
# This script tests that GitHub Secrets properly propagate to Function Apps
# by validating the Bicep template and checking for consistent patterns.
#
# Usage: ./Test-ConfigurationPropagation.ps1

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "Configuration Propagation Test" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Track test results
$testResults = @()
$passedTests = 0
$failedTests = 0

function Add-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = ""
    )
    
    $script:testResults += [PSCustomObject]@{
        TestName = $TestName
        Passed = $Passed
        Message = $Message
    }
    
    if ($Passed) {
        $script:passedTests++
        Write-Host "✓ PASS: $TestName" -ForegroundColor Green
        if ($Message -and $Verbose) {
            Write-Host "  $Message" -ForegroundColor Gray
        }
    } else {
        $script:failedTests++
        Write-Host "✗ FAIL: $TestName" -ForegroundColor Red
        if ($Message) {
            Write-Host "  $Message" -ForegroundColor Yellow
        }
    }
}

# =========================================
# Test 1: Validate Bicep Syntax
# =========================================

Write-Host "Test 1: Validating Bicep template syntax..." -ForegroundColor Yellow

try {
    $bicepFile = "infra/inkstainedwretches.bicep"
    if (-not (Test-Path $bicepFile)) {
        throw "Bicep file not found: $bicepFile"
    }
    
    $buildOutput = az bicep build --file $bicepFile 2>&1
    $buildExitCode = $LASTEXITCODE
    
    if ($buildExitCode -eq 0) {
        Add-TestResult -TestName "Bicep Syntax Validation" -Passed $true -Message "Template compiles successfully"
    } else {
        Add-TestResult -TestName "Bicep Syntax Validation" -Passed $false -Message "Compilation failed: $buildOutput"
    }
} catch {
    Add-TestResult -TestName "Bicep Syntax Validation" -Passed $false -Message $_.Exception.Message
}

# =========================================
# Test 2: Validate Function App Settings Module
# =========================================

Write-Host "`nTest 2: Validating Function App Settings module..." -ForegroundColor Yellow

try {
    $moduleFile = "infra/modules/functionAppSettings.bicep"
    if (-not (Test-Path $moduleFile)) {
        throw "Module file not found: $moduleFile"
    }
    
    $buildOutput = az bicep build --file $moduleFile 2>&1
    $buildExitCode = $LASTEXITCODE
    
    if ($buildExitCode -eq 0) {
        Add-TestResult -TestName "Function App Settings Module Validation" -Passed $true -Message "Module compiles successfully"
    } else {
        Add-TestResult -TestName "Function App Settings Module Validation" -Passed $false -Message "Compilation failed: $buildOutput"
    }
} catch {
    Add-TestResult -TestName "Function App Settings Module Validation" -Passed $false -Message $_.Exception.Message
}

# =========================================
# Test 3: Check AAD Variables in All Function Apps
# =========================================

Write-Host "`nTest 3: Checking AAD variables in all Function Apps..." -ForegroundColor Yellow

$requiredAadVars = @('AAD_AUDIENCE', 'AAD_CLIENT_ID', 'AAD_VALID_ISSUERS')
$bicepContent = Get-Content "infra/inkstainedwretches.bicep" -Raw

foreach ($varName in $requiredAadVars) {
    $pattern = "name:\s*'$varName'"
    $matches = [regex]::Matches($bicepContent, $pattern)
    $count = $matches.Count
    
    # Should have 4 instances (one per function app)
    if ($count -eq 4) {
        Add-TestResult -TestName "AAD Variable '$varName' in all Function Apps" -Passed $true -Message "Found in all 4 function apps"
    } else {
        Add-TestResult -TestName "AAD Variable '$varName' in all Function Apps" -Passed $false -Message "Found in $count function app(s), expected 4"
    }
}

# =========================================
# Test 4: Verify Conditional Pattern Usage
# =========================================

Write-Host "`nTest 4: Verifying conditional pattern usage..." -ForegroundColor Yellow

$functionApps = @('imageApiFunctionApp', 'inkStainedWretchFunctionsApp', 'inkStainedWretchStripeApp', 'inkStainedWretchesConfigApp')

foreach ($appName in $functionApps) {
    # Find the resource definition
    $pattern = "resource $appName.*?\n.*?appSettings:\s*concat\("
    $match = [regex]::Match($bicepContent, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    
    if ($match.Success) {
        Add-TestResult -TestName "Function App '$appName' uses concat pattern" -Passed $true
    } else {
        Add-TestResult -TestName "Function App '$appName' uses concat pattern" -Passed $false -Message "Static array detected instead of concat()"
    }
}

# =========================================
# Test 5: Verify Cosmos DB Settings
# =========================================

Write-Host "`nTest 5: Checking Cosmos DB settings propagation..." -ForegroundColor Yellow

$cosmosDbVars = @('COSMOSDB_CONNECTION_STRING', 'COSMOSDB_ENDPOINT_URI', 'COSMOSDB_PRIMARY_KEY', 'COSMOSDB_DATABASE_ID')

foreach ($varName in $cosmosDbVars) {
    $pattern = "name:\s*'$varName'"
    $matches = [regex]::Matches($bicepContent, $pattern)
    $count = $matches.Count
    
    # Should have at least 3 instances (not all apps need all variables)
    if ($count -ge 3) {
        Add-TestResult -TestName "Cosmos DB Variable '$varName'" -Passed $true -Message "Found in $count function app(s)"
    } else {
        Add-TestResult -TestName "Cosmos DB Variable '$varName'" -Passed $false -Message "Found in $count function app(s), expected at least 3"
    }
}

# =========================================
# Test 6: Check GitHub Workflow Integration
# =========================================

Write-Host "`nTest 6: Checking GitHub workflow integration..." -ForegroundColor Yellow

$workflowFile = ".github/workflows/main_onepageauthorapi.yml"
if (Test-Path $workflowFile) {
    $workflowContent = Get-Content $workflowFile -Raw
    
    # Check that secrets are defined
    $secretsToCheck = @('AAD_AUDIENCE', 'AAD_CLIENT_ID', 'AAD_VALID_ISSUERS')
    
    foreach ($secret in $secretsToCheck) {
        $pattern = "${secret}:\s*\$\{\{\s*secrets\.${secret}\s*\}\}"
        if ($workflowContent -match $pattern) {
            Add-TestResult -TestName "Workflow defines secret '$secret'" -Passed $true
        } else {
            Add-TestResult -TestName "Workflow defines secret '$secret'" -Passed $false -Message "Secret not found in workflow"
        }
    }
    
    # Check that secrets are passed to deployment
    foreach ($secret in $secretsToCheck) {
        $paramName = $secret.ToLower() -replace '_', ''
        if ($workflowContent -match "PARAMS_ARRAY.*$paramName") {
            Add-TestResult -TestName "Workflow passes '$secret' to deployment" -Passed $true
        } else {
            Add-TestResult -TestName "Workflow passes '$secret' to deployment" -Passed $false -Message "Parameter not passed in deployment step"
        }
    }
} else {
    Add-TestResult -TestName "GitHub Workflow File Exists" -Passed $false -Message "Workflow file not found: $workflowFile"
}

# =========================================
# Test 7: Verify Module Parameter Consistency
# =========================================

Write-Host "`nTest 7: Checking module parameter consistency..." -ForegroundColor Yellow

if (Test-Path "infra/modules/functionAppSettings.bicep") {
    $moduleContent = Get-Content "infra/modules/functionAppSettings.bicep" -Raw
    
    # Check that module has all AAD parameters
    $aadParams = @('aadTenantId', 'aadAudience', 'aadClientId', 'aadAuthority', 'aadValidIssuers')
    
    foreach ($param in $aadParams) {
        if ($moduleContent -match "param\s+$param\s+string") {
            Add-TestResult -TestName "Module has parameter '$param'" -Passed $true
        } else {
            Add-TestResult -TestName "Module has parameter '$param'" -Passed $false -Message "Parameter not found in module"
        }
    }
    
    # Check that module outputs appSettings
    if ($moduleContent -match "output\s+appSettings\s+array") {
        Add-TestResult -TestName "Module outputs appSettings array" -Passed $true
    } else {
        Add-TestResult -TestName "Module outputs appSettings array" -Passed $false -Message "Output not found in module"
    }
} else {
    Add-TestResult -TestName "Function App Settings Module Exists" -Passed $false -Message "Module file not found"
}

# =========================================
# Test Summary
# =========================================

Write-Host "`n=======================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

$totalTests = $passedTests + $failedTests
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host ""

if ($failedTests -eq 0) {
    Write-Host "All tests passed! ✓" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some tests failed. Please review the results above." -ForegroundColor Red
    
    if ($Verbose) {
        Write-Host "`nFailed Tests:" -ForegroundColor Yellow
        $testResults | Where-Object { -not $_.Passed } | ForEach-Object {
            Write-Host "  - $($_.TestName)" -ForegroundColor Red
            if ($_.Message) {
                Write-Host "    $($_.Message)" -ForegroundColor Gray
            }
        }
    }
    
    exit 1
}
