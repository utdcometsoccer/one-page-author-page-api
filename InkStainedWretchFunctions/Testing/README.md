# Testing Configuration Files

This directory contains configuration files for different testing scenarios.

## Scenario 1: Frontend UI Testing (No Infrastructure Changes)
- **Purpose**: Test your frontend UI without creating actual Azure resources or domains
- **Configuration**: `scenario1.local.settings.json`
- **Endpoints**: 
  - `POST /api/test/scenario1` - Full end-to-end frontend test
  - All individual test harness endpoints work in mocked mode

## Scenario 2: Individual Function Testing (Minimal Cost)
- **Purpose**: Test each function individually with real Azure APIs but mock expensive operations
- **Configuration**: `scenario2.local.settings.json`
- **Endpoints**:
  - `POST /api/test/frontdoor` - Test Front Door operations
  - `POST /api/test/dns` - Test DNS Zone operations  
  - `POST /api/test/googledomains` - Test Google Domains operations

## Scenario 3: Full End-to-End with Real Money (Production Test)
- **Purpose**: Complete production test with real domain registration and Azure infrastructure
- **Configuration**: `scenario3.local.settings.json`
- **Endpoints**:
  - `POST /api/test/scenario3` - Full production test (USE WITH CAUTION!)

## How to Use

### For Scenario 1 (Safe Testing):
1. Copy `scenario1.local.settings.json` to `local.settings.json`
2. Run your Function App
3. Test your UI - all backend operations will be mocked
4. No actual costs or infrastructure changes

### For Scenario 2 (Individual Testing):
1. Copy `scenario2.local.settings.json` to `local.settings.json`
2. Run your Function App
3. Use the individual test endpoints to verify each component
4. Minimal costs (DNS zones ~$0.50, Front Door ~$0.10)

### For Scenario 3 (Production Testing):
1. Copy `scenario3.local.settings.json` to `local.settings.json`
2. **IMPORTANT**: Review the `MAX_TEST_COST_LIMIT` setting
3. Set `ConfirmRealMoney: true` in your test requests
4. Monitor costs carefully - this creates real domains (~$12-35 each)

## Cost Management

- **MAX_TEST_COST_LIMIT**: Maximum allowed cost per test run
- **SKIP_DOMAIN_PURCHASE**: Prevents actual domain purchases when true
- Monitor your Azure and Google Cloud billing during tests

## Test Endpoints

All test endpoints require Function-level authentication (function key in query string or header).

### Test Request Format:
```json
{
  "domainName": "test-example.com",
  "operation": "add|remove|create|register|etc",
  "userEmail": "test@example.com",
  "confirmRealMoney": false
}
```

### Response Format:
```json
{
  "testName": "FrontDoor",
  "success": true,
  "message": "Operation completed successfully",
  "estimatedCost": 0.10,
  "isMocked": true,
  "timestamp": "2025-10-14T10:30:00Z"
}
```
