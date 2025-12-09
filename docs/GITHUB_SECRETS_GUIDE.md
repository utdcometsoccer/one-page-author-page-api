# GitHub Secrets Configuration Guide

This document outlines all GitHub Secrets required for the OnePageAuthorAPI CI/CD pipeline.

## Core Infrastructure Secrets

### Azure Credentials
**Secret Name**: `AZURE_CREDENTIALS`  
**Required**: Yes  
**Description**: Service principal credentials for Azure authentication  
**Format**: JSON
```json
{
  "clientId": "<client-id>",
  "clientSecret": "<client-secret>",
  "subscriptionId": "<subscription-id>",
  "tenantId": "<tenant-id>"
}
```

### Resource Group Configuration
**Secret Name**: `AZURE_RESOURCE_GROUP`  
**Required**: For function-app deployment  
**Description**: Resource group name for the legacy function-app  
**Example**: `rg-onepageauthor-prod`

**Secret Name**: `AZURE_LOCATION`  
**Required**: For function-app deployment  
**Description**: Azure region for the legacy function-app  
**Example**: `eastus`

**Secret Name**: `AZURE_FUNCTIONAPP_NAME`  
**Required**: For function-app deployment  
**Description**: Name of the legacy function app  
**Example**: `func-onepageauthor-prod`

## Cosmos DB Secrets

**Secret Name**: `COSMOSDB_RESOURCE_GROUP`  
**Required**: For Cosmos DB deployment  
**Description**: Resource group for Cosmos DB  
**Example**: `rg-cosmosdb-prod`

**Secret Name**: `COSMOSDB_ACCOUNT_NAME`  
**Required**: For Cosmos DB deployment  
**Description**: Cosmos DB account name  
**Example**: `cosmos-onepageauthor-prod`

**Secret Name**: `COSMOSDB_LOCATION`  
**Required**: For Cosmos DB deployment  
**Description**: Azure region for Cosmos DB  
**Example**: `eastus`

**Secret Name**: `COSMOSDB_CONNECTION_STRING`  
**Required**: Yes (for function apps)  
**Description**: Full Cosmos DB connection string including AccountEndpoint and AccountKey  
**Format**: `AccountEndpoint=https://...;AccountKey=...;`

**Secret Name**: `COSMOSDB_ENABLE_FREE_TIER`  
**Required**: No  
**Description**: Enable Cosmos DB free tier (one per subscription)  
**Values**: `true` or `false`  
**Default**: `false`

**Secret Name**: `COSMOSDB_ENABLE_ZONE_REDUNDANCY`  
**Required**: No  
**Description**: Enable zone redundancy for Cosmos DB  
**Values**: `true` or `false`  
**Default**: `false`

## Application Insights Secrets

**Secret Name**: `APPINSIGHTS_NAME`  
**Required**: For Application Insights deployment  
**Description**: Application Insights instance name  
**Example**: `appi-onepageauthor-prod`

## Ink Stained Wretches Infrastructure Secrets

**Secret Name**: `ISW_RESOURCE_GROUP`  
**Required**: For ISW infrastructure deployment  
**Description**: Resource group for Ink Stained Wretches infrastructure  
**Example**: `rg-inkstainedwretch-prod`

**Secret Name**: `ISW_LOCATION`  
**Required**: For ISW infrastructure deployment  
**Description**: Azure region for ISW resources  
**Example**: `eastus`

**Secret Name**: `ISW_BASE_NAME`  
**Required**: For ISW infrastructure deployment  
**Description**: Base name for generating resource names  
**Example**: `isw-prod`  
**Note**: Used to generate names like `isw-prod-imageapi`, `isw-prod-functions`, etc.

**Secret Name**: `ISW_DNS_ZONE_NAME`  
**Required**: No (for DNS zone deployment)  
**Description**: DNS zone name for custom domains  
**Example**: `inkstainedwretch.com`

**Secret Name**: `ISW_FRONTEND_URL`  
**Required**: Recommended (for CORS configuration)  
**Description**: URL of the InkStainedWretch frontend application  
**Example**: `https://app.inkstainedwretch.com`  
**Purpose**: Used to configure CORS origins on function apps

## Authentication Secrets

**Secret Name**: `AAD_TENANT_ID`  
**Required**: Yes  
**Description**: Azure AD (Entra ID) tenant ID  
**Format**: GUID  
**Example**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

**Secret Name**: `AAD_AUDIENCE`  
**Required**: Yes  
**Description**: Azure AD audience (application/client ID)  
**Format**: GUID  
**Example**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

## Payment Processing Secrets

**Secret Name**: `STRIPE_API_KEY`  
**Required**: For InkStainedWretchStripe deployment  
**Description**: Stripe API secret key  
**Format**: `sk_test_...` (test) or `sk_live_...` (production)  
**Security**: **HIGHLY SENSITIVE** - Never commit to code

## Deployment Control Secrets

**Secret Name**: `DEPLOY_IMAGE_API`  
**Required**: No  
**Description**: Enable/disable ImageAPI deployment  
**Values**: `true` or `false`  
**Default**: Deploy if not set

**Secret Name**: `DEPLOY_ISW_FUNCTIONS`  
**Required**: No  
**Description**: Enable/disable InkStainedWretchFunctions deployment  
**Values**: `true` or `false`  
**Default**: Deploy if not set

**Secret Name**: `DEPLOY_ISW_STRIPE`  
**Required**: No  
**Description**: Enable/disable InkStainedWretchStripe deployment  
**Values**: `true` or `false`  
**Default**: Deploy if not set

**Secret Name**: `DEPLOY_ISW_CONFIG`  
**Required**: No  
**Description**: Enable/disable InkStainedWretchesConfig deployment  
**Values**: `true` or `false`  
**Default**: Deploy if not set  
**Note**: New function app for Key Vault configuration retrieval

**Secret Name**: `DEPLOY_COMMUNICATION_SERVICES`  
**Required**: No  
**Description**: Enable/disable Azure Communication Services deployment for email notifications  
**Values**: `true` or `false`  
**Default**: `false`  
**Note**: When set to `true`, the workflow will automatically register the `Microsoft.Communication` resource provider if needed and deploy Azure Communication Services

## Key Vault Configuration (Future)

**Secret Name**: `KEY_VAULT_SECRETS_JSON`  
**Required**: No (planned feature)  
**Description**: JSON string containing all secrets to populate Key Vault  
**Format**: JSON object with secret name/value pairs
```json
{
  "COSMOSDB-PRIMARY-KEY": "...",
  "STRIPE-API-KEY": "...",
  "APPLICATIONINSIGHTS-CONNECTION-STRING": "..."
}
```
**Note**: This will be used by a future GitHub Actions step to automatically populate Key Vault

## How to Set GitHub Secrets

### Via GitHub Web UI

1. Navigate to your repository on GitHub
2. Go to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Enter the secret name exactly as shown above
5. Paste the secret value
6. Click "Add secret"

### Via GitHub CLI

```bash
# Set a single secret
gh secret set SECRET_NAME --body "secret-value"

# Set a secret from a file
gh secret set SECRET_NAME < secret-file.txt

# Set a JSON secret
gh secret set AZURE_CREDENTIALS --body '{"clientId":"...","clientSecret":"...","subscriptionId":"...","tenantId":"..."}'
```

## Secret Management Best Practices

### Security
- **Never commit secrets to code** - Use User Secrets for local development
- **Rotate secrets regularly** - Especially for production environments
- **Use separate secrets for environments** - Different secrets for dev/staging/production
- **Limit secret access** - Only grant access to users who need it
- **Monitor secret usage** - Review GitHub Actions logs for unauthorized access

### Organization
- **Use consistent naming** - Follow the naming conventions in this document
- **Document all secrets** - Maintain this document as source of truth
- **Version control** - Track when secrets were created/updated
- **Backup strategy** - Keep secure backups of critical secrets

### Minimal Deployment Configuration

To deploy with minimal configuration, you need:

1. `AZURE_CREDENTIALS` - For authentication
2. `ISW_RESOURCE_GROUP` - Where to deploy
3. `ISW_BASE_NAME` - Resource naming
4. `ISW_LOCATION` - Azure region
5. `COSMOSDB_CONNECTION_STRING` - Database connection
6. `STRIPE_API_KEY` - Payment processing
7. `AAD_TENANT_ID` - Authentication
8. `AAD_AUDIENCE` - Authentication

All other secrets are optional and control specific features.

## Troubleshooting

### Deployment Fails with "Secret not found"
- Verify the secret name matches exactly (case-sensitive)
- Check the secret is set at the repository level, not environment level
- Ensure you have appropriate permissions to view secrets

### Workflow Skips Deployment Steps
- Check that deployment control secrets are set to `true`
- Verify all required secrets for that deployment are configured
- Review workflow logs for specific skip reasons

### Invalid Secret Format
- JSON secrets must be valid JSON (use a validator)
- Connection strings should not have trailing spaces
- GUIDs should be in standard format (with hyphens)

## Migration from Environment Variables to Key Vault

As part of the Key Vault migration:

1. **Phase 1** (Current): Secrets stored in GitHub Secrets, passed to function apps as environment variables
2. **Phase 2** (Future): Secrets stored in Azure Key Vault, GitHub Actions populates Key Vault
3. **Phase 3** (Future): Function apps retrieve secrets directly from Key Vault using Managed Identity

See [KEY_VAULT_MIGRATION_GUIDE.md](./KEY_VAULT_MIGRATION_GUIDE.md) for detailed migration plan.

## Support

For questions about secret configuration:
- Review the GitHub Actions workflow: `.github/workflows/main_onepageauthorapi.yml`
- Check Bicep templates in `infra/` directory
- Consult [GitHub Secrets documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- Open an issue in the repository
