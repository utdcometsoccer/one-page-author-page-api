# Key Vault Configuration Migration Guide

## Overview

This guide documents the transition plan for moving all environment configuration settings from environment variables to Azure Key Vault. This approach provides:

- **Enhanced Security**: Secrets stored in Azure Key Vault with access controls
- **Centralized Management**: Single source of truth for configuration
- **Audit Trail**: Track who accessed which secrets and when
- **Feature Flag Control**: Gradual rollout with `USE_KEY_VAULT` flag

## Architecture

### Key Components

1. **IKeyVaultConfigService**: Interface for retrieving secrets from Key Vault
2. **KeyVaultConfigService**: Implementation using Azure.Security.KeyVault.Secrets
3. **InkStainedWretchesConfig Function App**: Dedicated API for configuration retrieval
4. **Feature Flag**: `USE_KEY_VAULT` environment variable controls Key Vault usage

### Feature Flag Behavior

- `USE_KEY_VAULT=false` (default): Uses environment variables (current behavior)
- `USE_KEY_VAULT=true`: Reads from Key Vault, falls back to environment variables if secret not found

## Configuration Settings Catalog

### Common Settings (All Function Apps)

| Setting Name | Key Vault Name | Sensitivity | Used By |
|-------------|----------------|-------------|---------|
| `COSMOSDB_ENDPOINT_URI` | `COSMOSDB-ENDPOINT-URI` | Low | All apps |
| `COSMOSDB_PRIMARY_KEY` | `COSMOSDB-PRIMARY-KEY` | **HIGH** | All apps |
| `COSMOSDB_DATABASE_ID` | `COSMOSDB-DATABASE-ID` | Low | All apps |
| `COSMOSDB_CONNECTION_STRING` | `COSMOSDB-CONNECTION-STRING` | **HIGH** | All apps |
| `AAD_TENANT_ID` | `AAD-TENANT-ID` | Medium | All apps |
| `AAD_AUDIENCE` | `AAD-AUDIENCE` | Medium | All apps |
| `AAD_CLIENT_ID` | `AAD-CLIENT-ID` | Medium | All apps |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `APPLICATIONINSIGHTS-CONNECTION-STRING` | **HIGH** | All apps |
| `AZURE_STORAGE_CONNECTION_STRING` | `AZURE-STORAGE-CONNECTION-STRING` | **HIGH** | Storage-dependent apps |
| `AzureWebJobsStorage` | `AzureWebJobsStorage` | **HIGH** | Function apps |

### Stripe Configuration

| Setting Name | Key Vault Name | Sensitivity | Used By |
|-------------|----------------|-------------|---------|
| `STRIPE_API_KEY` | `STRIPE-API-KEY` | **HIGH** | InkStainedWretchStripe |
| `STRIPE_WEBHOOK_SECRET` | `STRIPE-WEBHOOK-SECRET` | **HIGH** | InkStainedWretchStripe |

### External API Configuration

| Setting Name | Key Vault Name | Sensitivity | Used By |
|-------------|----------------|-------------|---------|
| `PENGUIN_RANDOM_HOUSE_API_KEY` | `PENGUIN-RANDOM-HOUSE-API-KEY` | **HIGH** | InkStainedWretchFunctions |
| `PENGUIN_RANDOM_HOUSE_API_URL` | `PENGUIN-RANDOM-HOUSE-API-URL` | Low | InkStainedWretchFunctions |
| `AMAZON_PRODUCT_ACCESS_KEY` | `AMAZON-PRODUCT-ACCESS-KEY` | **HIGH** | InkStainedWretchFunctions |
| `AMAZON_PRODUCT_SECRET_KEY` | `AMAZON-PRODUCT-SECRET-KEY` | **HIGH** | InkStainedWretchFunctions |
| `AMAZON_PRODUCT_PARTNER_TAG` | `AMAZON-PRODUCT-PARTNER-TAG` | Medium | InkStainedWretchFunctions |

## Migration Process

### Phase 1: Infrastructure Setup (Current Phase)

1. ✅ Add Azure Key Vault NuGet package to OnePageAuthorLib
2. ✅ Create `IKeyVaultConfigService` interface
3. ✅ Implement `KeyVaultConfigService` with feature flag support
4. ✅ Create `InkStainedWretchesConfig` function app
5. ✅ Update Bicep templates with `KEY_VAULT_URL` settings
6. ✅ Update GitHub Actions workflow

### Phase 2: Function App Integration

For each function app:

1. Update `Program.cs` to register `IKeyVaultConfigService`
2. Modify configuration loading to use `GetSecretWithFallbackAsync`
3. Test with `USE_KEY_VAULT=false` (should work as before)
4. Test with `USE_KEY_VAULT=true` and Key Vault populated

### Phase 3: Console Application Integration

For each console app:

1. Add Key Vault service registration
2. Update configuration loading logic
3. Test both feature flag states

### Phase 4: Production Rollout

1. Deploy Key Vault infrastructure
2. Populate Key Vault with production secrets
3. Grant Managed Identity access to Key Vault
4. Enable `USE_KEY_VAULT=true` for one function app (canary)
5. Monitor for issues
6. Gradually roll out to remaining function apps
7. Enable for console applications

## Code Examples

### Using KeyVaultConfigService in Program.cs

```csharp
var builder = FunctionsApplication.CreateBuilder(args);
var config = builder.Configuration;

// Register Key Vault service
builder.Services.AddKeyVaultConfigService();

// Option 1: Get secret with fallback (recommended)
var sp = builder.Services.BuildServiceProvider();
var kvService = sp.GetRequiredService<IKeyVaultConfigService>();
var cosmosKey = await kvService.GetSecretWithFallbackAsync(
    "COSMOSDB-PRIMARY-KEY", 
    "COSMOSDB_PRIMARY_KEY");

// Option 2: Get from Key Vault only
if (kvService.IsKeyVaultEnabled())
{
    var secret = await kvService.GetSecretAsync("SECRET-NAME");
}
```

### InkStainedWretchesConfig Functions

The new function app provides HTTP endpoints for configuration retrieval:

```bash
# Get Application Insights connection string
GET https://{base-name}-config.azurewebsites.net/api/config/applicationinsights?code={function-key}

# Get Penguin API key
GET https://{base-name}-config.azurewebsites.net/api/config/penguin-api-key?code={function-key}
```

Response format:
```json
{
  "connectionString": "InstrumentationKey=...",
  "source": "KeyVault"
}
```

## Managed Identity Setup

### Grant Function Apps Access to Key Vault

```bash
# Enable system-assigned managed identity for function app
az functionapp identity assign \
  --name {function-app-name} \
  --resource-group {resource-group}

# Grant Key Vault Secrets User role
az role assignment create \
  --assignee {principal-id} \
  --role "Key Vault Secrets User" \
  --scope {key-vault-id}
```

### Bicep Template (Future Enhancement)

```bicep
resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  identity: {
    type: 'SystemAssigned'
  }
  // ... other properties
}

resource keyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, functionApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
```

## GitHub Secrets Required

### Existing Secrets
- `AZURE_CREDENTIALS` - Azure service principal credentials
- `ISW_RESOURCE_GROUP` - Resource group name
- `ISW_BASE_NAME` - Base name for resources
- `COSMOSDB_CONNECTION_STRING` - Cosmos DB connection string
- `STRIPE_API_KEY` - Stripe API key
- `AAD_TENANT_ID` - Azure AD tenant ID
- `AAD_AUDIENCE` - Azure AD audience

### New Secrets (Recommended)
- `ISW_FRONTEND_URL` - InkStainedWretch frontend URL for CORS configuration
- `DEPLOY_ISW_CONFIG` - Set to `true` to deploy InkStainedWretchesConfig function app
- `KEY_VAULT_SECRETS_JSON` - (Future) JSON string containing all secrets to populate Key Vault

## Security Considerations

### Secret Naming Convention

Key Vault secret names use hyphens instead of underscores:
- Environment variable: `COSMOSDB_PRIMARY_KEY`
- Key Vault secret: `COSMOSDB-PRIMARY-KEY`

This is handled automatically by `KeyVaultConfigService`.

### Access Control

- **Function Apps**: Use Managed Identity with "Key Vault Secrets User" role
- **Developers**: Use Azure AD authentication for local development
- **CI/CD**: Use service principal with minimal required permissions

### Least Privilege

Each function app should only have access to the secrets it needs. Consider creating separate Key Vaults or using Key Vault access policies for fine-grained control.

## Monitoring and Troubleshooting

### Enable Key Vault Logging

```bash
az monitor diagnostic-settings create \
  --name key-vault-logs \
  --resource {key-vault-id} \
  --logs '[{"category": "AuditEvent", "enabled": true}]' \
  --workspace {log-analytics-workspace-id}
```

### Common Issues

1. **"Secret not found" errors**
   - Verify secret name matches (hyphens vs underscores)
   - Check Key Vault access permissions
   - Ensure `USE_KEY_VAULT=true` is set

2. **"Managed Identity not authorized"**
   - Verify Managed Identity is enabled for function app
   - Check RBAC role assignment exists
   - Wait 5-10 minutes for role assignment propagation

3. **Fallback to environment variables not working**
   - Verify environment variable name is correct
   - Check `GetSecretWithFallbackAsync` is used (not `GetSecretAsync`)

### Logging

KeyVaultConfigService logs all operations:
- Debug: Secret retrieval attempts
- Info: Successful Key Vault initialization
- Warning: Secrets not found, falling back to environment
- Error: Key Vault connection failures

## Testing

### Local Development

```json
// local.settings.json
{
  "Values": {
    "USE_KEY_VAULT": "false",
    "KEY_VAULT_URL": "",
    "COSMOSDB_PRIMARY_KEY": "your-local-key"
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
    // Fallback values
    "COSMOSDB_PRIMARY_KEY": "fallback-key"
  }
}
```

## Rollback Plan

If issues arise:

1. Set `USE_KEY_VAULT=false` on affected function apps
2. Verify environment variables are still configured
3. Monitor Application Insights for errors
4. Function apps will immediately fall back to environment variables

## Future Enhancements

1. **Automated Key Vault Population**: GitHub Actions workflow step to populate Key Vault from secrets
2. **Secret Rotation**: Implement automatic secret rotation with Azure Key Vault
3. **Per-Environment Key Vaults**: Separate Key Vaults for dev/staging/production
4. **Secret Versioning**: Track and manage secret versions
5. **CORS Configuration**: Automate CORS origin configuration from `ISW_FRONTEND_URL`

## Support

For questions or issues:
- Review Application Insights logs
- Check Key Vault audit logs
- Consult [Azure Key Vault documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- Open an issue in the repository
