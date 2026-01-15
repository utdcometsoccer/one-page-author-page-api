# Common Configuration Patterns Reference

## Overview

This document provides a comprehensive reference for common configuration patterns used in the OnePageAuthor API platform, specifically focusing on GitHub Secrets propagation to Azure Function Apps.

## Table of Contents

1. [Configuration Flow](#configuration-flow)
2. [Bicep Module Pattern](#bicep-module-pattern)
3. [Conditional Configuration](#conditional-configuration)
4. [AAD Authentication Configuration](#aad-authentication-configuration)
5. [Cosmos DB Configuration](#cosmos-db-configuration)
6. [Testing Configuration](#testing-configuration)
7. [Troubleshooting](#troubleshooting)

---

## Configuration Flow

GitHub Secrets flow through multiple layers before reaching Azure Function Apps:

```
GitHub Secrets (Repository Settings)
    ↓
GitHub Actions Workflow (.github/workflows/main_onepageauthorapi.yml)
    ↓ (Environment Variables)
Deployment Step (Bash Script in Workflow)
    ↓ (Bicep Parameters)
Bicep Template (infra/inkstainedwretches.bicep)
    ↓ (Function App Settings)
Azure Function Apps (4 apps)
    ↓ (Environment Variables at Runtime)
.NET Application Code
```

### Key Points

- **GitHub Secrets** use UPPER_CASE_WITH_UNDERSCORES (e.g., `AAD_AUDIENCE`)
- **Bicep Parameters** use camelCase (e.g., `aadAudience`)
- **Function App Settings** use UPPER_CASE_WITH_UNDERSCORES (e.g., `AAD_AUDIENCE`)

---

## Bicep Module Pattern

### Using the Function App Settings Module

The `infra/modules/functionAppSettings.bicep` module provides a reusable way to configure Function App settings.

#### Basic Usage

```bicep
module imageApiSettings 'modules/functionAppSettings.bicep' = {
  name: 'imageApiSettings'
  params: {
    functionAppName: imageApiFunctionName
    storageConnectionString: storageConnectionString
    appInsightsConnectionString: deployAppInsights ? appInsights.properties.ConnectionString : ''
    
    // Cosmos DB (optional)
    cosmosDbConnectionString: cosmosDbConnectionString
    cosmosDbEndpointUri: cosmosDbEndpointUri
    cosmosDbPrimaryKey: cosmosDbPrimaryKey
    cosmosDbDatabaseId: cosmosDbDatabaseId
    
    // AAD Authentication (optional)
    aadTenantId: aadTenantId
    aadAudience: aadAudience
    aadClientId: aadClientId
    aadAuthority: aadAuthority
    aadValidIssuers: aadValidIssuers
    
    // Additional optional parameters...
  }
}

// Use the module output in Function App resource
resource imageApiFunctionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: imageApiFunctionName
  // ... other properties
  properties: {
    siteConfig: {
      appSettings: imageApiSettings.outputs.appSettings
    }
  }
}
```

#### Module Benefits

1. **Consistency**: Same configuration pattern across all function apps
2. **Maintainability**: Update logic in one place
3. **Testability**: Easier to test configuration generation
4. **Reusability**: Use for new function apps without code duplication

---

## Conditional Configuration

### Pattern: Empty Check with Ternary Operator

All optional configuration uses the conditional pattern:

```bicep
!empty(parameterName) ? [
  {
    name: 'SETTING_NAME'
    value: parameterName
  }
] : []
```

### Why This Pattern?

1. **No Empty Values**: Prevents setting environment variables to empty strings
2. **Clean Configuration**: Only adds settings that have actual values
3. **Bicep Compliance**: Works with Bicep's type system
4. **Azure Best Practice**: Follows Azure Function App configuration patterns

### Common Mistake

❌ **Don't do this:**
```bicep
{
  name: 'AAD_AUDIENCE'
  value: aadAudience  // Will be empty string if not provided
}
```

✅ **Do this instead:**
```bicep
!empty(aadAudience) ? [
  {
    name: 'AAD_AUDIENCE'
    value: aadAudience
  }
] : []
```

---

## AAD Authentication Configuration

### Required GitHub Secrets

| Secret Name | Description | Example | Required |
|-------------|-------------|---------|----------|
| `AAD_TENANT_ID` | Microsoft Entra ID tenant GUID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` | Yes |
| `AAD_AUDIENCE` | API application/client ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` | Yes |
| `AAD_CLIENT_ID` | Client ID (alternative to audience) | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` | Optional |
| `AAD_AUTHORITY` | Authority URL | `https://{your-ciam-domain}.ciamlogin.com/{your-tenant}/` | Optional |
| `AAD_VALID_ISSUERS` | Comma-separated issuer URLs | `https://{your-ciam-domain}.ciamlogin.com/{your-tenant}/B2C_1_signup_signin/v2.0/, https://{your-ciam-domain}.ciamlogin.com/{your-tenant}/B2C_1_SignUpSignIn/v2.0` | Optional |

### Configuration in GitHub Workflow

```yaml
env:
  AAD_TENANT_ID: ${{ secrets.AAD_TENANT_ID }}
  AAD_AUDIENCE: ${{ secrets.AAD_AUDIENCE }}
  AAD_CLIENT_ID: ${{ secrets.AAD_CLIENT_ID }}
  AAD_AUTHORITY: ${{ secrets.AAD_AUTHORITY }}
  AAD_VALID_ISSUERS: ${{ secrets.AAD_VALID_ISSUERS }}
```

### Passing to Bicep Deployment

```bash
[ -n "$AAD_TENANT_ID" ] && PARAMS_ARRAY+=("aadTenantId=$AAD_TENANT_ID")
[ -n "$AAD_AUDIENCE" ] && PARAMS_ARRAY+=("aadAudience=$AAD_AUDIENCE")
[ -n "$AAD_CLIENT_ID" ] && PARAMS_ARRAY+=("aadClientId=$AAD_CLIENT_ID")
[ -n "$AAD_AUTHORITY" ] && PARAMS_ARRAY+=("aadAuthority=$AAD_AUTHORITY")
[ -n "$AAD_VALID_ISSUERS" ] && PARAMS_ARRAY+=("aadValidIssuers=$AAD_VALID_ISSUERS")
```

### Bicep Template Parameters

```bicep
@description('Azure AD Tenant ID (optional)')
param aadTenantId string = ''

@description('Azure AD Audience/Client ID (optional)')
param aadAudience string = ''

@description('Azure AD Client ID (optional)')
param aadClientId string = ''

@description('Azure AD Authority URL (optional)')
param aadAuthority string = ''

@description('Azure AD Valid Issuers (optional, comma-separated)')
param aadValidIssuers string = ''
```

### Function App Settings

```bicep
!empty(aadTenantId) ? [
  { name: 'AAD_TENANT_ID', value: aadTenantId }
] : [],
!empty(aadAudience) ? [
  { name: 'AAD_AUDIENCE', value: aadAudience }
] : [],
!empty(aadClientId) ? [
  { name: 'AAD_CLIENT_ID', value: aadClientId }
] : [],
!empty(aadAuthority) ? [
  { name: 'AAD_AUTHORITY', value: aadAuthority }
] : [],
!empty(aadValidIssuers) ? [
  { name: 'AAD_VALID_ISSUERS', value: aadValidIssuers }
] : []
```

### Usage in Application Code

```csharp
var audience = config["AAD_AUDIENCE"] ?? config["AAD_CLIENT_ID"];
var tenantId = config["AAD_TENANT_ID"];
var validIssuersRaw = config["AAD_VALID_ISSUERS"];
```

---

## Cosmos DB Configuration

### Required GitHub Secrets

| Secret Name | Description | Required |
|-------------|-------------|----------|
| `COSMOSDB_CONNECTION_STRING` | Full connection string | Yes |
| `COSMOSDB_ENDPOINT_URI` | Account endpoint | Optional |
| `COSMOSDB_PRIMARY_KEY` | Primary key | Optional |
| `COSMOSDB_DATABASE_ID` | Database name | Yes |

### Configuration Pattern

```bicep
// Cosmos DB Configuration
!empty(cosmosDbConnectionString) ? [
  { name: 'COSMOSDB_CONNECTION_STRING', value: cosmosDbConnectionString }
] : [],
!empty(cosmosDbEndpointUri) ? [
  { name: 'COSMOSDB_ENDPOINT_URI', value: cosmosDbEndpointUri }
] : [],
!empty(cosmosDbPrimaryKey) ? [
  { name: 'COSMOSDB_PRIMARY_KEY', value: cosmosDbPrimaryKey }
] : [],
!empty(cosmosDbDatabaseId) ? [
  { name: 'COSMOSDB_DATABASE_ID', value: cosmosDbDatabaseId }
] : []
```

---

## Testing Configuration

### Automated Test Script

Use the provided test script to verify configuration:

```bash
# Run from repository root
pwsh infra/Test-ConfigurationPropagation.ps1
```

### Test Coverage

The test script validates:

1. ✅ Bicep template syntax
2. ✅ Function App Settings module syntax
3. ✅ AAD variables in all 4 function apps
4. ✅ Conditional pattern usage
5. ✅ Cosmos DB settings propagation
6. ✅ GitHub workflow integration
7. ✅ Module parameter consistency

### Expected Output

```
=======================================
Configuration Propagation Test
=======================================

Test 1: Validating Bicep template syntax...
✓ PASS: Bicep Syntax Validation

[... more tests ...]

=======================================
Test Summary
=======================================
Total Tests: 25
Passed: 25
Failed: 0

All tests passed! ✓
```

---

## Troubleshooting

### Issue: Secrets Not Appearing in Function App

**Symptom**: Environment variable is empty in Function App runtime

**Checklist**:

1. ✅ Secret is configured in GitHub repository settings
2. ✅ Workflow reads secret and sets environment variable
3. ✅ Deployment step passes environment variable to Bicep
4. ✅ Bicep parameter is defined
5. ✅ Bicep uses conditional pattern to add setting
6. ✅ Function App resource includes the setting

**Debug Steps**:

```bash
# 1. Check GitHub Secrets are set
gh secret list

# 2. Validate Bicep template
az bicep build --file infra/inkstainedwretches.bicep

# 3. Run configuration tests
pwsh infra/Test-ConfigurationPropagation.ps1

# 4. Check Function App settings in Azure
az functionapp config appsettings list \
  --name <function-app-name> \
  --resource-group <resource-group>
```

### Issue: Bicep Compilation Errors

**Symptom**: Deployment fails with Bicep errors

**Common Causes**:

1. **Missing closing bracket**: Check `concat()` has matching parentheses
2. **Type mismatch**: Ensure parameter types match (string, bool, etc.)
3. **Syntax error**: Validate with `az bicep build`

**Fix**:

```bash
# Validate template
az bicep build --file infra/inkstainedwretches.bicep

# Check for syntax errors
az bicep lint --file infra/inkstainedwretches.bicep
```

### Issue: Static Array Instead of Conditional

**Symptom**: Empty values appear in Function App settings

**Problem**:
```bicep
appSettings: [
  { name: 'AAD_AUDIENCE', value: aadAudience }  // ❌ Always added
]
```

**Solution**:
```bicep
appSettings: concat([
  // base settings
],
!empty(aadAudience) ? [
  { name: 'AAD_AUDIENCE', value: aadAudience }  // ✅ Only if not empty
] : [])
```

---

## Best Practices

### 1. Always Use Conditional Pattern

```bicep
// ✅ Good
!empty(parameter) ? [settings] : []

// ❌ Bad
[settings]  // Always includes, even if empty
```

### 2. Consistent Naming

- **GitHub Secrets**: `UPPER_CASE_WITH_UNDERSCORES`
- **Bicep Parameters**: `camelCase`
- **Function Settings**: `UPPER_CASE_WITH_UNDERSCORES`

### 3. Secure Parameters

```bicep
@secure()
param cosmosDbPrimaryKey string = ''
```

### 4. Document Optional Parameters

```bicep
@description('Azure AD Audience (optional)')
param aadAudience string = ''
```

### 5. Test Configuration Changes

Always run tests after changes:
```bash
pwsh infra/Test-ConfigurationPropagation.ps1
```

---

## Related Documentation

- [GITHUB_SECRETS_CONFIGURATION.md](GITHUB_SECRETS_CONFIGURATION.md) - Complete GitHub Secrets guide
- [GITHUB_SECRETS_REFERENCE.md](GITHUB_SECRETS_REFERENCE.md) - Secret definitions
- [IMPLEMENTATION_SUMMARY_CONFIG_PROPAGATION_FIX.md](IMPLEMENTATION_SUMMARY_CONFIG_PROPAGATION_FIX.md) - Implementation details

---

## Quick Reference

### Function Apps

1. **ImageAPI** (`${baseName}-imageapi`) - Image upload and management
2. **InkStainedWretchFunctions** (`${baseName}-functions`) - Domain and localization
3. **InkStainedWretchStripe** (`${baseName}-stripe`) - Payment processing
4. **InkStainedWretchesConfig** (`${baseName}-config`) - Configuration management

### Key Files

- `infra/inkstainedwretches.bicep` - Main infrastructure template
- `infra/modules/functionAppSettings.bicep` - Reusable settings module
- `infra/Test-ConfigurationPropagation.ps1` - Automated test script
- `.github/workflows/main_onepageauthorapi.yml` - Deployment workflow

---

*Last Updated: January 11, 2026*
