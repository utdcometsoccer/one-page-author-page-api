# Complete Testing Strategy for InkStainedWretchFunctions

I've created a comprehensive testing framework for your Azure Function App with three distinct scenarios and feature flags to minimize costs while ensuring thorough testing.

## 🎯 Testing Scenarios Overview

### Scenario 1: Frontend UI Testing (Completely Safe)
- **Purpose**: Test your client UI frontend without creating domains or modifying Azure infrastructure
- **Cost**: $0.00 (all operations mocked)
- **Safety**: 100% safe - no real resources created
- **Use Case**: Daily development, UI testing, integration testing

### Scenario 2: Individual Function Testing (Minimal Cost)
- **Purpose**: Test each major function individually with real Azure APIs but mock expensive operations
- **Cost**: ~$0.50-2.00 per test run (DNS zones, minimal Front Door usage)
- **Safety**: Low cost - creates some Azure resources but no domain purchases
- **Use Case**: Function validation, Azure integration testing

### Scenario 3: Full End-to-End with Real Money (Production Test)
- **Purpose**: Complete production test that creates real domains, DNS zones, and Front Door configurations
- **Cost**: $12-50+ per test (domain registration costs vary by TLD)
- **Safety**: ⚠️ HIGH COST - uses real money and creates production resources
- **Use Case**: Final production validation before launch

## 🛠️ What I've Created

### 1. Testing Infrastructure Files
- **`TestingConfiguration.cs`** - Feature flag management and configuration
- **`Testing/Mocks/`** - Mock implementations for all major services:
  - `MockFrontDoorService.cs` - Azure Front Door operations
  - `MockDnsZoneService.cs` - Azure DNS zone operations  
  - `MockGoogleDomainsService.cs` - Google Domains API operations
- **`TestingServiceExtensions.cs`** - Dependency injection for testing services

### 2. Test Harness Functions
- **`TestHarnessFunction.cs`** - Individual function testing endpoints:
  - `POST /api/test/frontdoor` - Test Front Door operations
  - `POST /api/test/dns` - Test DNS zone operations
  - `POST /api/test/googledomains` - Test Google Domains operations
- **`EndToEndTestFunction.cs`** - Complete workflow testing:
  - `POST /api/test/scenario1` - Safe frontend testing
  - `POST /api/test/scenario3` - Full production testing

### 3. Configuration Files
- **`scenario1.local.settings.json`** - All mocking enabled, $0 cost limit
- **`scenario2.local.settings.json`** - Azure real, domains mocked, $5 cost limit
- **`scenario3.local.settings.json`** - Everything real, $50 cost limit

### 4. PowerShell Helper Scripts
- **`RunTests.ps1`** - Execute test scenarios with proper error handling
- **`SwitchTestConfig.ps1`** - Switch between testing configurations easily

## 🚀 How to Use

### Quick Start - Scenario 1 (Safe Testing)

1. **Switch to safe testing configuration:**
   ```powershell
   cd InkStainedWretchFunctions
   .\Testing\SwitchTestConfig.ps1 -Scenario 1
   ```

2. **Start your Function App:**
   ```powershell
   func start
   ```

3. **Run the test:**
   ```powershell
   .\Testing\RunTests.ps1 -Scenario 1 -DomainName "test.example.com" -UserEmail "test@example.com"
   ```

4. **Test your UI frontend** - all backend operations will be mocked and return success responses

### Individual Function Testing - Scenario 2

1. **Switch configuration:**
   ```powershell
   .\Testing\SwitchTestConfig.ps1 -Scenario 2
   ```

2. **Test individual components:**
   ```powershell
   .\Testing\RunTests.ps1 -Scenario 2 -DomainName "test.example.com"
   ```

3. **Review costs** - should be under $5 total for all tests

### Production Testing - Scenario 3 (Use with Extreme Caution!)

1. **Switch to production configuration:**
   ```powershell
   .\Testing\SwitchTestConfig.ps1 -Scenario 3
   ```

2. **Update Stripe key** in `local.settings.json` (replace placeholder)

3. **Run production test with confirmation:**
   ```powershell
   .\Testing\RunTests.ps1 -Scenario 3 -DomainName "real-test-domain.com" -ConfirmRealMoney $true
   ```

## 📊 Test Endpoints

### Individual Test Harnesses

**Front Door Testing:**
```bash
POST http://localhost:7072/api/test/frontdoor
{
  "domainName": "test.example.com",
  "operation": "add|remove|validate|exists"
}
```

**DNS Zone Testing:**
```bash
POST http://localhost:7072/api/test/dns
{
  "domainName": "test.example.com", 
  "operation": "create|delete|exists|nameservers"
}
```

**Google Domains Testing:**
```bash
POST http://localhost:7072/api/test/googledomains
{
  "domainName": "test.example.com",
  "operation": "register|available"
}
```

### End-to-End Testing

**Scenario 1 (Safe):**
```bash
POST http://localhost:7072/api/test/scenario1
{
  "domainName": "test.example.com",
  "userEmail": "test@example.com"
}
```

**Scenario 3 (Production):**
```bash
POST http://localhost:7072/api/test/scenario3
{
  "domainName": "real-domain.com",
  "userEmail": "user@example.com",
  "confirmRealMoney": true
}
```

## 🔧 Configuration Flags

Your `local.settings.json` now includes these testing controls:

```json
{
  "TESTING_MODE": "true|false",
  "MOCK_AZURE_INFRASTRUCTURE": "true|false",
  "MOCK_GOOGLE_DOMAINS": "true|false", 
  "MOCK_STRIPE_PAYMENTS": "true|false",
  "STRIPE_TEST_MODE": "true|false",
  "MOCK_EXTERNAL_APIS": "true|false",
  "ENABLE_TEST_LOGGING": "true|false",
  "TEST_SCENARIO": "frontend-safe|individual-testing|production-test",
  "MAX_TEST_COST_LIMIT": "0.00|5.00|50.00",
  "TEST_DOMAIN_SUFFIX": "test-domain.local",
  "SKIP_DOMAIN_PURCHASE": "true|false"
}
```

## 💰 Cost Management

### Scenario 1 - $0.00
- All operations mocked
- No real resources created
- Perfect for daily development

### Scenario 2 - ~$0.50-2.00 per test
- DNS zones: ~$0.50/month each
- Front Door operations: ~$0.10 per test
- Domain checks: Free (mocked)

### Scenario 3 - $12-50+ per test
- Domain registration: $12 (.com) to $35 (.io)
- DNS zone: $0.50/month
- Front Door: $0.10
- Stripe fees: 2.9% + $0.30

## 🛡️ Safety Features

1. **Cost Limits**: `MAX_TEST_COST_LIMIT` prevents expensive operations
2. **Domain Purchase Protection**: `SKIP_DOMAIN_PURCHASE` prevents accidental purchases
3. **Confirmation Required**: Scenario 3 requires explicit `confirmRealMoney: true`
4. **Detailed Logging**: `ENABLE_TEST_LOGGING` shows all operations
5. **Mock Overrides**: Feature flags override real services with mocks

## 📝 Next Steps

1. **Start with Scenario 1** to test your UI integration safely
2. **Use Scenario 2** to validate individual Azure Functions work correctly
3. **Only use Scenario 3** when you're ready for final production validation
4. **Monitor costs** using the PowerShell scripts and Azure/Google Cloud billing
5. **Add more mock services** as needed for additional external APIs

This testing framework gives you complete control over cost and safety while ensuring your application works correctly at every level. Start with Scenario 1 for daily development and only escalate to higher scenarios when needed!
