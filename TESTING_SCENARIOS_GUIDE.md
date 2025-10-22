# 🧪 Testing Scenarios Quick Reference

## Your 3 Testing Scenarios

### Scenario 1: Frontend-Safe Testing (Cost: $0.00)
**Purpose**: End-to-end UI testing without creating domains or modifying infrastructure

**To activate this scenario, set these values in `local.settings.json`:**
```json
{
  "TESTING_MODE": "true",
  "MOCK_AZURE_INFRASTRUCTURE": "true",
  "MOCK_GOOGLE_DOMAINS": "true", 
  "MOCK_STRIPE_PAYMENTS": "true",
  "MOCK_EXTERNAL_APIS": "true",
  "SKIP_DOMAIN_PURCHASE": "true",
  "STRIPE_TEST_MODE": "true",
  "MAX_TEST_COST_LIMIT": "0.00",
  "TEST_SCENARIO": "frontend-safe",
  "TEST_DOMAIN_SUFFIX": "test-frontend.local"
}
```

### Scenario 2: Individual Function Testing (Cost: $0.50-$5.00)
**Purpose**: Test individual Azure Functions that modify Azure infrastructure

**To activate this scenario, set these values in `local.settings.json`:**
```json
{
  "TESTING_MODE": "true",
  "MOCK_AZURE_INFRASTRUCTURE": "false",
  "MOCK_GOOGLE_DOMAINS": "true",
  "MOCK_STRIPE_PAYMENTS": "true", 
  "MOCK_EXTERNAL_APIS": "false",
  "SKIP_DOMAIN_PURCHASE": "true",
  "STRIPE_TEST_MODE": "true",
  "MAX_TEST_COST_LIMIT": "5.00",
  "TEST_SCENARIO": "individual-testing",
  "TEST_DOMAIN_SUFFIX": "test-individual.local"
}
```

### Scenario 3: Production Testing (Cost: $12-$50+)
**Purpose**: Full end-to-end test with real money, real domains, real infrastructure

**To activate this scenario, set these values in `local.settings.json`:**
```json
{
  "TESTING_MODE": "true",
  "MOCK_AZURE_INFRASTRUCTURE": "false",
  "MOCK_GOOGLE_DOMAINS": "false",
  "MOCK_STRIPE_PAYMENTS": "false",
  "MOCK_EXTERNAL_APIS": "false", 
  "SKIP_DOMAIN_PURCHASE": "false",
  "STRIPE_TEST_MODE": "false",
  "MAX_TEST_COST_LIMIT": "50.00",
  "TEST_SCENARIO": "production-test",
  "TEST_DOMAIN_SUFFIX": "test-production.com"
}
```

## 🚀 How to Run Each Scenario

1. **Update local.settings.json** with the scenario configuration above
2. **Set up your secrets** using dotnet user-secrets (for sensitive values)
3. **Start the Functions app**:
   ```bash
   cd InkStainedWretchFunctions
   func start
   ```
4. **Test your endpoints** according to the scenario

## 📊 What Each Scenario Tests

### Scenario 1 (Frontend-Safe)
- ✅ UI/UX flows
- ✅ API response structures  
- ✅ Error handling
- ❌ No real external API calls
- ❌ No infrastructure changes
- ❌ No costs incurred

### Scenario 2 (Individual Functions)
- ✅ Real Azure operations (DNS zones, Front Door)
- ✅ External API integrations (Amazon, Penguin Random House)
- ✅ Infrastructure modifications
- ❌ No domain purchases
- ❌ No real Stripe charges
- 💰 Minimal Azure costs ($0.50-$5.00)

### Scenario 3 (Production)
- ✅ Everything real
- ✅ Domain purchases
- ✅ Real Stripe transactions
- ✅ Complete end-to-end flow
- 💰 Significant costs ($12-$50+)

## ⚠️ Important Notes

- **Always verify** `MAX_TEST_COST_LIMIT` before running
- **Scenario 3** requires live Stripe keys and real payment methods
- **Monitor costs** during testing
- **Use descriptive** `TEST_DOMAIN_SUFFIX` values to identify test domains

## 🔧 Quick Configuration Script

Save this as `switch-scenario.ps1` to quickly switch between scenarios:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("1", "2", "3", "frontend", "individual", "production")]
    [string]$Scenario
)

$configPath = "local.settings.json"
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
        Write-Host "✅ Configured for Scenario 1: Frontend-Safe Testing" -ForegroundColor Green
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
        Write-Host "✅ Configured for Scenario 2: Individual Function Testing" -ForegroundColor Yellow
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
        Write-Host "⚠️  Configured for Scenario 3: Production Testing (REAL MONEY)" -ForegroundColor Red
    }
}

$config | ConvertTo-Json -Depth 10 | Out-File $configPath -Encoding UTF8
Write-Host "Configuration updated in $configPath" -ForegroundColor Cyan
```

## Usage:
```powershell
# Switch to Scenario 1
.\switch-scenario.ps1 -Scenario 1

# Switch to Scenario 2  
.\switch-scenario.ps1 -Scenario individual

# Switch to Scenario 3
.\switch-scenario.ps1 -Scenario production
```