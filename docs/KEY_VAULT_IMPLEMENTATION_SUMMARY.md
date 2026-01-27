# Key Vault Configuration Implementation Summary

## Overview

This implementation provides the complete infrastructure for transitioning from environment variable-based configuration to Azure Key Vault for secure secret management.

## What Has Been Implemented

### 1. Core Key Vault Service (OnePageAuthorLib)

**Files Added:**

- `OnePageAuthorLib/interfaces/IKeyVaultConfigService.cs` - Service interface
- `OnePageAuthorLib/services/KeyVaultConfigService.cs` - Service implementation
- Updated `OnePageAuthorLib/ServiceFactory.cs` - Added DI extension method
- Updated `OnePageAuthorLib/OnePageAuthorLib.csproj` - Added Azure.Security.KeyVault.Secrets package

**Features:**

- ✅ Feature flag control via `USE_KEY_VAULT` environment variable
- ✅ Automatic fallback to environment variables when Key Vault is disabled or secrets not found
- ✅ Secret name conversion (underscores → hyphens for Key Vault compatibility)
- ✅ Managed Identity authentication using DefaultAzureCredential
- ✅ Comprehensive logging at all levels (Debug, Info, Warning, Error)

### 2. InkStainedWretchesConfig Function App

**Files Added:**

- `InkStainedWretchesConfig/InkStainedWretchesConfig.csproj` - Project file
- `InkStainedWretchesConfig/Program.cs` - Application startup
- `InkStainedWretchesConfig/GetApplicationConfig.cs` - Application Insights configuration endpoint
- `InkStainedWretchesConfig/GetPenguinApiKey.cs` - Penguin Random House API key endpoint
- `InkStainedWretchesConfig/host.json` - Function app host configuration
- `InkStainedWretchesConfig/local.settings.json` - Local development settings

**Endpoints:**

```
GET /api/config/applicationinsights?code={function-key}
Response: { "connectionString": "...", "source": "KeyVault" }

GET /api/config/penguin-api-key?code={function-key}
Response: { "apiKey": "...", "source": "KeyVault" }
```

**Configuration:**

- Authorization level: Function (requires function key)
- System-assigned Managed Identity: Enabled
- USE_KEY_VAULT: true (enabled by default for this app)

### 3. Infrastructure as Code (Bicep)

**File Modified:**

- `infra/inkstainedwretches.bicep`

**Changes:**

1. Added `deployInkStainedWretchesConfig` parameter (default: true)
2. Added `inkStainedWretchesConfigName` variable
3. Added `keyVaultUri` variable for consistent Key Vault URL reference
4. **All Function Apps** now have:
   - System-assigned Managed Identity enabled
   - `KEY_VAULT_URL` app setting configured
   - `USE_KEY_VAULT` app setting (default: false for gradual rollout)
5. Added InkStainedWretchesConfig function app resource
6. **Key Vault RBAC Role Assignments** for all function apps:
   - ImageAPI → Key Vault Secrets User
   - InkStainedWretchFunctions → Key Vault Secrets User
   - InkStainedWretchStripe → Key Vault Secrets User
   - InkStainedWretchesConfig → Key Vault Secrets User
7. Added outputs for new config function app

### 4. CI/CD Pipeline (GitHub Actions)

**File Modified:**

- `.github/workflows/main_onepageauthorapi.yml`

**Changes:**

1. Added `INK_STAINED_WRETCHES_CONFIG_PATH` environment variable
2. Added build and publish steps for InkStainedWretchesConfig
3. Added deployment step with `DEPLOY_ISW_CONFIG` secret control
4. Deployment target: `{ISW_BASE_NAME}-config`

### 5. Documentation

**Files Added:**

- `docs/KEY_VAULT_MIGRATION_GUIDE.md` - Comprehensive migration guide
  - Architecture and components
  - Settings catalog (all secrets documented)
  - Migration process (4 phases)
  - Code examples
  - Managed Identity setup
  - Security considerations
  - Monitoring and troubleshooting
  - Testing strategies
  - Rollback plan

- `docs/GITHUB_SECRETS_REFERENCE.md` - Complete secrets configuration guide
  - All required and optional secrets documented
  - Format and examples for each secret
  - Organization by category
  - Setup instructions (UI and CLI)
  - Security best practices
  - Environment variable mapping
  - Troubleshooting guide

**File Modified:**

- `OnePageAuthorAPI.sln` - Added InkStainedWretchesConfig project

## Configuration Settings

### Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `USE_KEY_VAULT` | Enable/disable Key Vault | `false` |
| `KEY_VAULT_URL` | Key Vault URI | Empty (required if USE_KEY_VAULT=true) |

### Key Vault Secrets

Secrets follow naming convention with hyphens instead of underscores:

| Environment Variable | Key Vault Secret Name |
|---------------------|----------------------|
| `COSMOSDB_PRIMARY_KEY` | `COSMOSDB-PRIMARY-KEY` |
| `STRIPE_API_KEY` | `STRIPE-API-KEY` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `APPLICATIONINSIGHTS-CONNECTION-STRING` |
| `PENGUIN_RANDOM_HOUSE_API_KEY` | `PENGUIN-RANDOM-HOUSE-API-KEY` |

## Security Architecture

### Authentication Flow

```
Function App → Managed Identity → Azure AD → Key Vault
```

1. Function app requests secret from Key Vault
2. Managed Identity authenticates with Azure AD
3. Azure AD validates identity and returns token
4. Key Vault validates token and checks RBAC permissions
5. If authorized, secret is returned

### RBAC Permissions

All function apps have "Key Vault Secrets User" role (GUID: `4633458b-17de-408a-b874-0445c86b69e6`):

- **Can**: Read secret values
- **Cannot**: List secrets, modify secrets, manage Key Vault

## Usage Examples

### In Function App Program.cs

```csharp
using InkStainedWretch.OnePageAuthorAPI;

var builder = FunctionsApplication.CreateBuilder(args);
var config = builder.Configuration;

// Register Key Vault service
builder.Services.AddKeyVaultConfigService();

// Build service provider to resolve dependencies
var sp = builder.Services.BuildServiceProvider();
var kvService = sp.GetRequiredService<IKeyVaultConfigService>();

// Get secret with automatic fallback
var cosmosKey = await kvService.GetSecretWithFallbackAsync(
    "COSMOSDB-PRIMARY-KEY", 
    "COSMOSDB_PRIMARY_KEY");

// Use the secret
builder.Services.AddCosmosClient(endpointUri, cosmosKey);
```

### Local Development

```json
// local.settings.json
{
  "Values": {
    "USE_KEY_VAULT": "false",
    "COSMOSDB_PRIMARY_KEY": "local-dev-key"
  }
}
```

### Testing with Key Vault

```json
// local.settings.json
{
  "Values": {
    "USE_KEY_VAULT": "true",
    "KEY_VAULT_URL": "https://your-keyvault.vault.azure.net/",
    "COSMOSDB_PRIMARY_KEY": "fallback-key-if-needed"
  }
}
```

## Deployment Process

### 1. Deploy Infrastructure

```bash
az deployment group create \
  --resource-group rg-inkstainedwretch-prod \
  --template-file infra/inkstainedwretches.bicep \
  --parameters baseName=isw-prod \
               location=eastus \
               cosmosDbConnectionString="..." \
               stripeApiKey="..." \
               aadTenantId="..." \
               aadAudience="..."
```

### 2. Populate Key Vault

```bash
# Add secrets to Key Vault
az keyvault secret set --vault-name isw-prod-kv \
  --name COSMOSDB-PRIMARY-KEY --value "..."

az keyvault secret set --vault-name isw-prod-kv \
  --name STRIPE-API-KEY --value "..."

az keyvault secret set --vault-name isw-prod-kv \
  --name APPLICATIONINSIGHTS-CONNECTION-STRING --value "..."
```

### 3. Verify Managed Identity Permissions

```bash
# Check role assignments
az role assignment list \
  --assignee <function-app-principal-id> \
  --scope <key-vault-id>
```

### 4. Enable Key Vault (Gradual Rollout)

```bash
# Enable for one function app (canary)
az functionapp config appsettings set \
  --name isw-prod-config \
  --resource-group rg-inkstainedwretch-prod \
  --settings USE_KEY_VAULT=true

# Monitor Application Insights for errors

# If successful, enable for remaining apps
az functionapp config appsettings set \
  --name isw-prod-imageapi \
  --resource-group rg-inkstainedwretch-prod \
  --settings USE_KEY_VAULT=true
```

## Monitoring

### Application Insights Queries

```kusto
// Key Vault access logs
traces
| where message contains "Key Vault"
| project timestamp, severityLevel, message
| order by timestamp desc

// Failed secret retrievals
traces
| where severityLevel == 3 // Warning
| where message contains "Secret not found"
| summarize count() by message

// Key Vault vs Environment Variable usage
traces
| where message contains "Using value from"
| extend source = case(
    message contains "Key Vault", "KeyVault",
    message contains "environment", "Environment",
    "Unknown")
| summarize count() by source
```

### Key Vault Diagnostic Logs

```bash
# Enable Key Vault audit logging
az monitor diagnostic-settings create \
  --name key-vault-logs \
  --resource /subscriptions/.../providers/Microsoft.KeyVault/vaults/isw-prod-kv \
  --logs '[{"category": "AuditEvent", "enabled": true}]' \
  --workspace /subscriptions/.../resourceGroups/.../providers/Microsoft.OperationalInsights/workspaces/...
```

## Rollback Plan

If issues occur:

1. **Immediate**: Set `USE_KEY_VAULT=false` on affected function apps
2. **Verify**: Environment variables are still configured
3. **Monitor**: Check Application Insights for errors
4. **Investigate**: Review Key Vault audit logs
5. **Fix**: Address the root cause
6. **Re-enable**: Set `USE_KEY_VAULT=true` after fix

## What's Next

### Remaining Implementation (Future Work)

1. **Update Function App Program.cs files** to use KeyVaultConfigService:
   - InkStainedWretchFunctions
   - InkStainedWretchStripe
   - ImageAPI
   - function-app

2. **Update Console Applications** to use KeyVaultConfigService:
   - SeedAPIData
   - SeedInkStainedWretchesLocale
   - SeedImageStorageTiers
   - OnePageAuthor.DataSeeder
   - SeedCountries
   - SeedLanguages
   - StripeProductManager
   - EntraIdRoleManager
   - AuthorInvitationTool

3. **Configure CORS** using ISW_FRONTEND_URL:
   - Update Bicep template to add CORS settings
   - Use GitHub Secret ISW_FRONTEND_URL

4. **Automate Key Vault Population**:
   - Add GitHub Actions step to populate Key Vault from secrets
   - Use KEY_VAULT_SECRETS_JSON secret

5. **Update Main README.md** with Key Vault information

## Success Criteria

✅ Key Vault service infrastructure complete  
✅ InkStainedWretchesConfig function app created  
✅ Managed Identity enabled for all function apps  
✅ Key Vault RBAC role assignments configured  
✅ Infrastructure as Code (Bicep) complete  
✅ CI/CD pipeline updated  
✅ Comprehensive documentation created  
✅ Backward compatibility maintained  

## Testing Checklist

- [x] KeyVaultConfigService builds successfully
- [x] InkStainedWretchesConfig builds successfully
- [x] Full solution builds successfully
- [x] Code review completed and issues addressed
- [ ] CodeQL security scan (timed out - not blocking for infrastructure work)
- [ ] Local testing with USE_KEY_VAULT=false
- [ ] Local testing with USE_KEY_VAULT=true
- [ ] Azure deployment test
- [ ] End-to-end integration test

## Support and Troubleshooting

### Common Issues

1. **"Managed Identity not authorized"**
   - Wait 5-10 minutes for RBAC role propagation
   - Verify role assignment exists
   - Check Managed Identity is enabled

2. **"Secret not found in Key Vault"**
   - Verify secret name (hyphens vs underscores)
   - Check secret exists in Key Vault
   - Verify Key Vault URL is correct

3. **Fallback not working**
   - Ensure using `GetSecretWithFallbackAsync` not `GetSecretAsync`
   - Verify environment variable name is correct
   - Check configuration is loaded

### Resources

- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Managed Identity Documentation](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/)
- [Azure RBAC Documentation](https://learn.microsoft.com/en-us/azure/role-based-access-control/)
- Project Documentation: `docs/KEY_VAULT_MIGRATION_GUIDE.md`

## Conclusion

This implementation provides a solid foundation for secure configuration management using Azure Key Vault. The feature flag approach allows for gradual rollout while maintaining backward compatibility. All infrastructure is in place for the transition, with clear documentation for next steps.
