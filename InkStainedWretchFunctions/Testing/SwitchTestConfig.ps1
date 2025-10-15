# PowerShell script to switch between testing configurations
# Usage: .\SwitchTestConfig.ps1 -Scenario 1

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet(1, 2, 3, "production")]
    [string]$Scenario
)

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath

# Map scenarios to configuration files
$configFiles = @{
    "1" = "Testing\scenario1.local.settings.json"
    "2" = "Testing\scenario2.local.settings.json" 
    "3" = "Testing\scenario3.local.settings.json"
    "production" = "local.settings.json.backup"
}

$scenarioNames = @{
    "1" = "Frontend UI Testing (Safe)"
    "2" = "Individual Function Testing (Minimal Cost)"
    "3" = "Full End-to-End with Real Money (PRODUCTION)"
    "production" = "Production Configuration"
}

$sourceFile = Join-Path $rootPath $configFiles[$Scenario]
$targetFile = Join-Path $rootPath "local.settings.json"

# Backup current configuration if it's not already backed up
$backupFile = Join-Path $rootPath "local.settings.json.backup"
if ((Test-Path $targetFile) -and -not (Test-Path $backupFile)) {
    Copy-Item $targetFile $backupFile
    Write-Host "📄 Backed up current configuration to local.settings.json.backup" -ForegroundColor Green
}

# Check if source configuration exists
if (-not (Test-Path $sourceFile)) {
    Write-Error "❌ Configuration file not found: $sourceFile"
    return
}

# Copy the configuration
try {
    Copy-Item $sourceFile $targetFile -Force
    Write-Host "✅ Switched to $($scenarioNames[$Scenario])" -ForegroundColor Green
    Write-Host "📄 Configuration loaded from: $($configFiles[$Scenario])" -ForegroundColor Gray
    
    # Display key settings for the scenario
    $config = Get-Content $targetFile | ConvertFrom-Json
    $values = $config.Values
    
    Write-Host ""
    Write-Host "🔧 Key Settings:" -ForegroundColor Cyan
    Write-Host "   Testing Mode: $($values.TESTING_MODE)" -ForegroundColor $(if($values.TESTING_MODE -eq "true") { "Yellow" } else { "White" })
    Write-Host "   Mock Azure Infrastructure: $($values.MOCK_AZURE_INFRASTRUCTURE)" -ForegroundColor $(if($values.MOCK_AZURE_INFRASTRUCTURE -eq "true") { "Green" } else { "Red" })
    Write-Host "   Mock Google Domains: $($values.MOCK_GOOGLE_DOMAINS)" -ForegroundColor $(if($values.MOCK_GOOGLE_DOMAINS -eq "true") { "Green" } else { "Red" })
    Write-Host "   Skip Domain Purchase: $($values.SKIP_DOMAIN_PURCHASE)" -ForegroundColor $(if($values.SKIP_DOMAIN_PURCHASE -eq "true") { "Green" } else { "Red" })
    Write-Host "   Max Test Cost Limit: `$$($values.MAX_TEST_COST_LIMIT)" -ForegroundColor $(if([decimal]$values.MAX_TEST_COST_LIMIT -eq 0) { "Green" } else { "Yellow" })
    Write-Host "   Test Scenario: $($values.TEST_SCENARIO)" -ForegroundColor White
    
    # Show warnings for dangerous scenarios
    if ($Scenario -eq "3") {
        Write-Host ""
        Write-Host "⚠️  WARNING: Scenario 3 uses REAL MONEY!" -ForegroundColor Red
        Write-Host "   - Domain purchases will be charged to your account" -ForegroundColor Red
        Write-Host "   - Azure resources will incur actual costs" -ForegroundColor Red
        Write-Host "   - Always check the MAX_TEST_COST_LIMIT setting" -ForegroundColor Yellow
    }
    elseif ($Scenario -eq "2") {
        Write-Host ""
        Write-Host "💡 Scenario 2 creates real Azure resources but mocks expensive operations" -ForegroundColor Yellow
        Write-Host "   - DNS zones cost ~`$0.50/month" -ForegroundColor Gray
        Write-Host "   - Front Door operations cost ~`$0.10 per test" -ForegroundColor Gray
    }
    else {
        Write-Host ""
        Write-Host "✅ Scenario 1 is completely safe - all operations are mocked" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "🚀 You can now run your Function App with this configuration!" -ForegroundColor Cyan
    Write-Host "   Use: func start" -ForegroundColor Gray
    Write-Host "   Or run tests with: .\Testing\RunTests.ps1 -Scenario $Scenario -DomainName 'test.example.com'" -ForegroundColor Gray
}
catch {
    Write-Error "❌ Failed to switch configuration: $($_.Exception.Message)"
}