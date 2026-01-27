# Implementation Summary: Conditional Environment Variable Configuration

## Overview

This implementation adds conditional configuration of environment variables for all Function Apps (ImageAPI, InkStainedWretchFunctions, and InkStainedWretchStripe) based on the availability of GitHub Secrets.

## Problem Statement

Previously, all environment variables were hardcoded in the Bicep template with fixed values passed from the GitHub workflow. This approach had several limitations:

- Optional features required secrets to be set even if not used
- No graceful degradation for optional integrations
- Deployment would fail or create misconfigured apps if optional secrets were missing

## Solution Architecture

### 1. Bicep Template Updates

**File**: `infra/inkstainedwretches.bicep`

#### Added Parameters

- **Core Parameters** (existing, now more flexible):
  - `cosmosDbConnectionString` - Cosmos DB connection string
  - `cosmosDbEndpointUri` - Cosmos DB endpoint (newly optional)
  - `cosmosDbPrimaryKey` - Cosmos DB key (newly optional)
  - `cosmosDbDatabaseId` - Database name

- **ImageAPI Specific**:
  - `azureStorageConnectionString` - Blob storage for images

- **Stripe Parameters**:
  - `stripeApiKey` - Stripe secret key
  - `stripeWebhookSecret` - Webhook signing secret (newly added)

- **Azure AD Authentication** (all optional):
  - `aadTenantId`, `aadAudience`, `aadClientId`, `aadAuthority`

- **Azure Infrastructure** (optional, for domain management):
  - `azureSubscriptionId`, `azureDnsResourceGroup`

- **Google Domains Integration** (optional):
  - `googleCloudProjectId`, `googleDomainsLocation`

- **Amazon Product API** (optional):
  - `amazonProductAccessKey`, `amazonProductSecretKey`
  - `amazonProductPartnerTag`, `amazonProductRegion`, `amazonProductMarketplace`

- **Penguin Random House API** (optional):
  - `penguinRandomHouseApiKey`, `penguinRandomHouseApiDomain`

#### Conditional Configuration Pattern

Used `concat()` with conditional arrays to build environment variable lists:

```bicep
appSettings: concat([
  // Always-present settings
  { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
],
// Conditional settings
!empty(stripeApiKey) ? [
  { name: 'STRIPE_API_KEY', value: stripeApiKey }
] : [],
!empty(stripeWebhookSecret) ? [
  { name: 'STRIPE_WEBHOOK_SECRET', value: stripeWebhookSecret }
] : []
)
```

#### Smart Defaults

For parameters with common defaults, implemented two-level conditionals:

```bicep
// If explicit value provided, use it
!empty(amazonProductRegion) ? [
  { name: 'AMAZON_PRODUCT_REGION', value: amazonProductRegion }
]
// Otherwise, if primary secret exists, use default
: !empty(amazonProductAccessKey) ? [
  { name: 'AMAZON_PRODUCT_REGION', value: 'us-east-1' }
]
// Otherwise, omit the variable
: []
```

This ensures defaults are only applied when the feature is actually configured.

### 2. GitHub Workflow Updates

**File**: `.github/workflows/main_onepageauthorapi.yml`

#### Environment Variables

Added all optional secrets to the workflow environment:

- Core secrets (Cosmos DB, Stripe, Azure AD)
- Optional integration secrets (Amazon, Penguin Random House, Google Domains)
- Azure infrastructure secrets

#### Conditional Parameter Building

```bash
# Build parameter strings conditionally
SECURE_PARAMS=""
[ -n "$STRIPE_API_KEY" ] && SECURE_PARAMS="$SECURE_PARAMS stripeApiKey='$STRIPE_API_KEY'"
[ -n "$STRIPE_WEBHOOK_SECRET" ] && SECURE_PARAMS="$SECURE_PARAMS stripeWebhookSecret='$STRIPE_WEBHOOK_SECRET'"

# Pass to deployment (unquoted for word splitting)
az deployment group create \
  --parameters $PARAMS $SECURE_PARAMS
```

### 3. Documentation

**File**: `docs/GITHUB_SECRETS_REFERENCE.md`

Created comprehensive guide including:

- Mapping of GitHub Secrets to environment variables
- Required vs optional secrets for each Function App
- Instructions for setting up secrets via GitHub UI and CLI
- Deployment scenarios and troubleshooting
- Default value behavior documentation
- Security best practices
- Troubleshooting guide

## Function App Configuration

### ImageAPI

**Required Environment Variables**:

- `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
- `COSMOSDB_CONNECTION_STRING`
- `AZURE_STORAGE_CONNECTION_STRING`

**Optional Environment Variables**:

- `AAD_TENANT_ID`, `AAD_AUDIENCE`, `AAD_AUTHORITY`
- `KEY_VAULT_URL`, `USE_KEY_VAULT`

### InkStainedWretchFunctions

**Required Environment Variables**:

- `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
- `CosmosDBConnection` (for Cosmos DB triggers)

**Optional Environment Variables**:

- Azure AD: `AAD_TENANT_ID`, `AAD_AUDIENCE`
- Domain Management: `AZURE_SUBSCRIPTION_ID`, `AZURE_DNS_RESOURCE_GROUP`
- Google Domains: `GOOGLE_CLOUD_PROJECT_ID`, `GOOGLE_DOMAINS_LOCATION`
- Amazon API: `AMAZON_PRODUCT_ACCESS_KEY`, `AMAZON_PRODUCT_SECRET_KEY`, `AMAZON_PRODUCT_PARTNER_TAG`, `AMAZON_PRODUCT_REGION`, `AMAZON_PRODUCT_MARKETPLACE`
- Penguin Random House: `PENGUIN_RANDOM_HOUSE_API_KEY`, `PENGUIN_RANDOM_HOUSE_API_DOMAIN`
- Key Vault: `KEY_VAULT_URL`, `USE_KEY_VAULT`

### InkStainedWretchStripe

**Required Environment Variables**:

- `STRIPE_API_KEY`, `STRIPE_WEBHOOK_SECRET`
- `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
- `COSMOSDB_CONNECTION_STRING`

**Optional Environment Variables**:

- `AAD_TENANT_ID`, `AAD_AUDIENCE`, `AAD_CLIENT_ID`
- `KEY_VAULT_URL`, `USE_KEY_VAULT`

## Benefits

### 1. Flexible Deployment

- Deploy with minimal configuration (only required secrets)
- Add optional features by configuring additional secrets
- No need to reconfigure or redeploy for optional features

### 2. Graceful Degradation

- Optional API integrations (Amazon, Penguin Random House) can be disabled
- Domain management features can be omitted
- Apps function correctly with partial configuration

### 3. Security

- Secrets only set when needed
- Secure parameters for sensitive values
- No hardcoded defaults that might be incorrect

### 4. Maintainability

- Clear separation of required vs optional configuration
- Well-documented secret-to-variable mapping
- Easy to add new optional parameters in the future

## Backward Compatibility

- Existing deployments continue to work
- No breaking changes to required parameters
- Optional parameters default to empty (not set)

## Testing & Validation

- ✅ Bicep template compiles successfully
- ✅ Code review passed (all issues addressed)
- ✅ CodeQL security scan passed (no vulnerabilities)
- ⏳ Runtime testing recommended with actual Azure deployment

## Future Enhancements

1. **Key Vault Integration**: Store secrets in Azure Key Vault instead of environment variables
2. **Managed Identity**: Use managed identity for Azure service authentication
3. **Configuration Validation**: Add startup validation to check required configuration
4. **Environment-Specific Configs**: Support multiple deployment environments (dev, staging, prod)

## Related Issues

- Addresses issue: "Configure the Function Apps"
- Implements conditional configuration based on secret availability
- Provides comprehensive documentation for GitHub Secrets setup

## Files Changed

1. `infra/inkstainedwretches.bicep` - Added conditional environment variable configuration
2. `.github/workflows/main_onepageauthorapi.yml` - Updated to pass optional secrets conditionally
3. `docs/GITHUB_SECRETS_REFERENCE.md` - Created comprehensive configuration guide

## Migration Guide

For users with existing deployments:

1. **No immediate action required** - existing deployments continue to work
2. **To enable optional features**:
   - Add the corresponding GitHub Secret
   - Re-run the deployment workflow
   - The feature will be automatically configured

3. **Example**: To enable Amazon API integration:

   ```bash
   gh secret set AMAZON_PRODUCT_ACCESS_KEY --body "AKIA..."
   gh secret set AMAZON_PRODUCT_SECRET_KEY --body "..."
   gh secret set AMAZON_PRODUCT_PARTNER_TAG --body "yourtag-20"
   # Re-run deployment workflow
   ```

## References

- [InkStainedWretchFunctions README](../InkStainedWretchFunctions/README.md)
- [ImageAPI README](../ImageAPI/README.md)
- [InkStainedWretchStripe README](../InkStainedWretchStripe/README.md)
- [GitHub Secrets Reference Guide](../docs/GITHUB_SECRETS_REFERENCE.md)
