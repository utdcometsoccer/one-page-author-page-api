param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("1", "2", "3", "frontend", "individual", "production")]
    [string]$Scenario
)

$configPath = "InkStainedWretchFunctions\local.settings.json"

if (!(Test-Path $configPath)) {
    Write-Error "❌ local.settings.json not found at $configPath"
    exit 1
}

Write-Host "🔧 Switching to testing scenario..." -ForegroundColor Cyan

try {
    $config = Get-Content $configPath | ConvertFrom-Json
    
    switch ($Scenario) {
        {$_ -in "1", "frontend"} {
            $config.Values.TESTING_MODE = "true"
            $config.Values.MOCK_AZURE_INFRASTRUCTURE = "true"
            $config.Values.MOCK_GOOGLE_DOMAINS = "true"
            $config.Values.MOCK_STRIPE_PAYMENTS = "true"
            $config.Values.MOCK_EXTERNAL_APIS = "true"
            $config.Values.SKIP_DOMAIN_PURCHASE = "true"
            $config.Values.STRIPE_TEST_MODE = "true"
            $config.Values.MAX_TEST_COST_LIMIT = "0.00"
            $config.Values.TEST_SCENARIO = "frontend-safe"
            $config.Values.TEST_DOMAIN_SUFFIX = "test-frontend.local"
            Write-Host "✅ Configured for Scenario 1: Frontend-Safe Testing (Cost: $0.00)" -ForegroundColor Green
            Write-Host "   - All external services mocked" -ForegroundColor Gray
            Write-Host "   - No infrastructure changes" -ForegroundColor Gray
            Write-Host "   - Safe for UI/UX testing" -ForegroundColor Gray
        }
        {$_ -in "2", "individual"} {
            $config.Values.TESTING_MODE = "true"
            $config.Values.MOCK_AZURE_INFRASTRUCTURE = "false"
            $config.Values.MOCK_GOOGLE_DOMAINS = "true"
            $config.Values.MOCK_STRIPE_PAYMENTS = "true"
            $config.Values.MOCK_EXTERNAL_APIS = "false"
            $config.Values.SKIP_DOMAIN_PURCHASE = "true"
            $config.Values.STRIPE_TEST_MODE = "true"
            $config.Values.MAX_TEST_COST_LIMIT = "5.00"
            $config.Values.TEST_SCENARIO = "individual-testing"
            $config.Values.TEST_DOMAIN_SUFFIX = "test-individual.local"
            Write-Host "✅ Configured for Scenario 2: Individual Function Testing (Cost: $0.50-$5.00)" -ForegroundColor Yellow
            Write-Host "   - Real Azure infrastructure operations" -ForegroundColor Gray
            Write-Host "   - External API calls enabled" -ForegroundColor Gray
            Write-Host "   - No domain purchases" -ForegroundColor Gray
        }
        {$_ -in "3", "production"} {
            $config.Values.TESTING_MODE = "true"
            $config.Values.MOCK_AZURE_INFRASTRUCTURE = "false"
            $config.Values.MOCK_GOOGLE_DOMAINS = "false"
            $config.Values.MOCK_STRIPE_PAYMENTS = "false"
            $config.Values.MOCK_EXTERNAL_APIS = "false"
            $config.Values.SKIP_DOMAIN_PURCHASE = "false"
            $config.Values.STRIPE_TEST_MODE = "false"
            $config.Values.MAX_TEST_COST_LIMIT = "50.00"
            $config.Values.TEST_SCENARIO = "production-test"
            $config.Values.TEST_DOMAIN_SUFFIX = "test-production.com"
            Write-Host "⚠️  Configured for Scenario 3: Production Testing (Cost: $12-$50+)" -ForegroundColor Red
            Write-Host "   - REAL MONEY transactions" -ForegroundColor Red
            Write-Host "   - REAL domain purchases" -ForegroundColor Red
            Write-Host "   - REAL infrastructure changes" -ForegroundColor Red
            Write-Host "   - Complete end-to-end testing" -ForegroundColor Yellow
        }
    }
    
    $config | ConvertTo-Json -Depth 10 | Out-File $configPath -Encoding UTF8
    Write-Host ""
    Write-Host "📄 Configuration updated in $configPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🚀 Next steps:" -ForegroundColor Magenta
    Write-Host "1. cd InkStainedWretchFunctions" -ForegroundColor White
    Write-Host "2. func start (or dotnet run)" -ForegroundColor White
    Write-Host "3. Test your endpoints" -ForegroundColor White
    Write-Host ""
    Write-Host "📊 Current configuration:" -ForegroundColor Blue
    Write-Host "   TEST_SCENARIO: $($config.Values.TEST_SCENARIO)" -ForegroundColor Gray
    Write-Host "   MAX_COST_LIMIT: $($config.Values.MAX_TEST_COST_LIMIT)" -ForegroundColor Gray
    Write-Host "   MOCK_AZURE: $($config.Values.MOCK_AZURE_INFRASTRUCTURE)" -ForegroundColor Gray
    Write-Host "   MOCK_DOMAINS: $($config.Values.MOCK_GOOGLE_DOMAINS)" -ForegroundColor Gray
    Write-Host "   MOCK_STRIPE: $($config.Values.MOCK_STRIPE_PAYMENTS)" -ForegroundColor Gray
    
} catch {
    Write-Error "❌ Failed to update configuration: $($_.Exception.Message)"
    exit 1
}

Write-Host ""
Write-Host "💡 Tip: See TESTING_SCENARIOS_GUIDE.md for detailed information about each scenario" -ForegroundColor Cyan