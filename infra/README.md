# Infrastructure Templates

This directory contains Bicep templates and management scripts for deploying and configuring Azure infrastructure components.

## Management Scripts

### Assign-KeyVaultRole.ps1 / Assign-KeyVaultRole.sh
Assigns the Key Vault Secrets Officer role (or any other specified role) to a service principal for a specific Key Vault. The script checks if the role assignment already exists before creating it, making it idempotent.

**PowerShell Usage:**
```powershell
# Basic usage with required parameter
./Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault"

# With resource group specified
./Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault" -ResourceGroupName "MyResourceGroup"

# With custom service principal and role
./Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault" -ServicePrincipalName "my-sp" -RoleName "Key Vault Administrator"
```

**Bash Usage:**
```bash
# Basic usage with required parameter
./Assign-KeyVaultRole.sh -k mykeyvault

# With resource group specified
./Assign-KeyVaultRole.sh -k mykeyvault -r MyResourceGroup

# With custom service principal and role
./Assign-KeyVaultRole.sh -k mykeyvault -s my-sp -R "Key Vault Administrator"

# Show help
./Assign-KeyVaultRole.sh -h
```

**Default Values:**
- Service Principal Name: `github-actions-inkstainedwretches`
- Role Name: `Key Vault Secrets Officer`

**Features:**
- Idempotent - Safe to run multiple times
- Validates Azure CLI installation and authentication
- Checks if role assignment already exists
- Clear, colorful output with progress indicators
- Comprehensive error handling
- Uses variables to avoid hardcoding values

**Requirements:**
- Azure CLI installed
- User authenticated with `az login`
- Sufficient permissions to assign roles (typically Owner or User Access Administrator)

## Available Templates

### keyvault.bicep
Deploys a standalone Azure Key Vault for secure storage of secrets, keys, and certificates.

**Note**: This template is available for manual deployment but is not used by the GitHub Actions workflow. The Key Vault is automatically deployed as part of the `inkstainedwretches.bicep` infrastructure template.

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
- `deployCommunicationServices` (optional) - Deploy Azure Communication Services for email notifications
- Various function app deployment flags

**Note**: When deploying with `deployCommunicationServices=true`, the `Microsoft.Communication` resource provider must be registered in your Azure subscription. The GitHub Actions workflow handles this automatically.

### communication-services.bicep
Deploys Azure Communication Services for email notifications used by the Author Invitation Tool.

**Key parameters:**
- `baseName` (required) - Base name for the Communication Services resource
- `dataLocation` (optional) - Data location (default: "United States")
- `tags` (optional) - Resource tags

**Prerequisites:**
- The `Microsoft.Communication` resource provider must be registered: `az provider register --namespace Microsoft.Communication --wait`
- In GitHub Actions, this is handled automatically by the "Register Microsoft.Communication Resource Provider" workflow step

**Resources created:**
- Communication Services resource (`${baseName}-acs`)
- Email Service (`${baseName}-email`)
- Azure Managed Domain for quick setup (e.g., `<uniqueid>.azurecomm.net`)

**Outputs:**
- `communicationServiceName` - Name of the Communication Services resource
- `communicationServiceId` - Resource ID
- `communicationServiceEndpoint` - Endpoint hostname
- `emailServiceName` - Email Service name
- `senderDomain` - Azure Managed Domain for sending emails
- Connection string must be retrieved via Azure Portal or CLI after deployment

**Example deployment:**
```bash
# Register provider first
az provider register --namespace Microsoft.Communication --wait

# Deploy Communication Services
az deployment group create \
  --resource-group MyResourceGroup \
  --template-file communication-services.bicep \
  --parameters baseName=myapp \
               dataLocation="United States"
```

See [AZURE_COMMUNICATION_SERVICES_SETUP.md](../docs/AZURE_COMMUNICATION_SERVICES_SETUP.md) for detailed setup and configuration instructions.

## Usage in GitHub Actions

All templates are integrated into the `.github/workflows/main_onepageauthorapi.yml` workflow with conditional deployment based on configured GitHub Secrets.

See [DEPLOYMENT_GUIDE.md](../docs/DEPLOYMENT_GUIDE.md) for detailed deployment instructions.
