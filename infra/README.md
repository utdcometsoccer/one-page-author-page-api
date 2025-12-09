# Infrastructure Templates

This directory contains Bicep templates for deploying Azure infrastructure components.

## Available Templates

### keyvault.bicep
Deploys a standalone Azure Key Vault for secure storage of secrets, keys, and certificates.

**Parameters:**
- `keyVaultName` (required) - Name of the Key Vault (3-24 chars, globally unique)
- `location` (optional) - Azure region (defaults to resource group location)
- `enableRbacAuthorization` (optional) - Enable RBAC authorization (default: true, recommended)
- `enableSoftDelete` (optional) - Enable soft delete (default: true, recommended)
- `softDeleteRetentionInDays` (optional) - Retention period 7-90 days (default: 90)
- `enablePurgeProtection` (optional) - Prevent permanent deletion (default: false)
- `skuName` (optional) - SKU: standard or premium (default: standard)
- `enabledForDeployment` (optional) - Enable for ARM deployment (default: true)
- `enabledForTemplateDeployment` (optional) - Enable for template deployment (default: true)
- `publicNetworkAccess` (optional) - Enabled or Disabled (default: Enabled)

**Outputs:**
- `keyVaultName` - The name of the deployed Key Vault
- `keyVaultId` - The resource ID
- `keyVaultUri` - The vault URI for accessing secrets

**Example deployment:**
```bash
az deployment group create \
  --resource-group MyResourceGroup \
  --template-file keyvault.bicep \
  --parameters keyVaultName=myapp-secrets-kv \
              location="West US 2" \
              enableRbacAuthorization=true \
              enablePurgeProtection=true
```

### cosmosdb.bicep
Deploys an Azure Cosmos DB account with serverless or provisioned capacity.

**Key parameters:**
- `cosmosDbAccountName` (required)
- `location` (optional)
- `enableFreeTier` (optional) - Enable free tier (one per subscription)
- `capacityMode` (optional) - Serverless or Provisioned
- `enableZoneRedundancy` (optional)

### applicationinsights.bicep
Deploys Application Insights for monitoring and diagnostics.

**Key parameters:**
- `appInsightsName` (required)
- `location` (optional)
- `retentionInDays` (optional) - 30-730 days

### functionapp.bicep
Deploys a Function App with storage and app service plan.

**Key parameters:**
- `functionAppName` (required)
- `location` (optional)

### inkstainedwretches.bicep
Comprehensive deployment template for the Ink Stained Wretches platform including storage, Key Vault, App Insights, DNS, and multiple Function Apps.

**Key parameters:**
- `baseName` (required) - Base name for all resources
- `location` (optional)
- `deployKeyVault` (optional) - Deploy Key Vault component
- `deployStorageAccount` (optional)
- `deployAppInsights` (optional)
- Various function app deployment flags

## Usage in GitHub Actions

All templates are integrated into the `.github/workflows/main_onepageauthorapi.yml` workflow with conditional deployment based on configured GitHub Secrets.

See [DEPLOYMENT_GUIDE.md](../docs/DEPLOYMENT_GUIDE.md) for detailed deployment instructions.
