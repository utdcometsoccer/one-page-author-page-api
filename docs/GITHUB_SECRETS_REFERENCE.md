# GitHub Secrets Quick Reference

This document provides a quick reference for all GitHub Secrets required by the deployment workflow.

## 📋 Secrets Checklist

Use this checklist when setting up the deployment workflow:

### ✅ Required Secrets (Must be configured)

- [ ] `AZURE_CREDENTIALS` - Azure Service Principal credentials (JSON format)
- [ ] `ISW_RESOURCE_GROUP` - Resource group name for Ink Stained Wretches
- [ ] `ISW_LOCATION` - Azure region (e.g., "West US 2")
- [ ] `ISW_BASE_NAME` - Base name for resources (must be globally unique)
- [ ] `COSMOSDB_CONNECTION_STRING` - Cosmos DB connection string
- [ ] `AAD_TENANT_ID` - Microsoft Entra ID tenant GUID
- [ ] `AAD_AUDIENCE` - API application/client ID

### 🔄 Optional Secrets (For existing function-app deployment)

- [ ] `AZURE_FUNCTIONAPP_NAME` - Existing function app name
- [ ] `AZURE_RESOURCE_GROUP` - Resource group for existing function app
- [ ] `AZURE_LOCATION` - Azure region for existing function app

### ➕ Optional Infrastructure Secrets

- [ ] `ISW_DNS_ZONE_NAME` - Custom domain name (e.g., "yourdomain.com")
- [ ] `ISW_STATIC_WEB_APP_REPO_URL` - GitHub repository URL for Static Web App
- [ ] `ISW_STATIC_WEB_APP_BRANCH` - GitHub branch (default: "main")

### 🎛️ Optional Deployment Control Secrets

- [ ] `DEPLOY_IMAGE_API` - Set to "true" to deploy ImageAPI Function App
- [ ] `DEPLOY_ISW_FUNCTIONS` - Set to "true" to deploy InkStainedWretchFunctions
- [ ] `DEPLOY_ISW_STRIPE` - Set to "true" to deploy InkStainedWretchStripe

### 💳 Payment Processing Secret (Required for Stripe functionality)

- [ ] `STRIPE_API_KEY` - Stripe secret API key

## 📖 Detailed Secret Descriptions

### AZURE_CREDENTIALS

**Format**: JSON object
**Required**: Yes
**Example**:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "your-client-secret",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

**How to obtain**:
```bash
az ad sp create-for-rbac --name "github-actions-sp" \
  --role Contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

### ISW_RESOURCE_GROUP

**Format**: String
**Required**: Yes
**Example**: `InkStainedWretches-RG`
**Description**: The Azure resource group where all Ink Stained Wretches resources will be deployed.
**Notes**:
- Will be created if it doesn't exist
- Should use a descriptive name
- Recommended format: `{ProjectName}-RG`

### ISW_LOCATION

**Format**: String (Azure region name)
**Required**: Yes
**Example**: `West US 2`, `East US`, `Central US`
**Description**: The Azure region where resources will be deployed.
**Common values**:
- `East US`
- `West US 2`
- `Central US`
- `West Europe`
- `Southeast Asia`

**How to list available locations**:
```bash
az account list-locations -o table
```

### ISW_BASE_NAME

**Format**: String (lowercase, alphanumeric, no special characters)
**Required**: Yes
**Example**: `inkstainedwretches`, `myapp`, `authorapi`
**Description**: Base name used to generate all resource names.
**Constraints**:
- Must be globally unique
- Use only lowercase letters and numbers
- Keep it short (recommended: 10-20 characters)
- Storage account name generated from this must be 3-24 characters

**Generated resource names**:
- Storage Account: `{baseName}storage` (e.g., `inkstainedwretchesstorage`)
- Key Vault: `{baseName}-kv` (e.g., `inkstainedwretches-kv`)
- App Insights: `{baseName}-insights`
- Function Apps:
  - ImageAPI: `{baseName}-imageapi`
  - Functions: `{baseName}-functions`
  - Stripe: `{baseName}-stripe`

### COSMOSDB_CONNECTION_STRING

**Format**: String (connection string)
**Required**: Yes
**Example**: `AccountEndpoint=https://myaccount.documents.azure.com:443/;AccountKey=...`
**Description**: Azure Cosmos DB connection string for database access.

**How to obtain**:
```bash
az cosmosdb keys list \
  --name {cosmos-account-name} \
  --resource-group {resource-group} \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv
```

**Alternative** (via Azure Portal):
1. Navigate to Azure Cosmos DB account
2. Go to "Keys" blade
3. Copy "PRIMARY CONNECTION STRING"

### STRIPE_API_KEY

**Format**: String (starts with `sk_test_` or `sk_live_`)
**Required**: For Stripe functionality
**Example**: `sk_test_51H...` (test mode) or `sk_live_51H...` (live mode)
**Description**: Stripe secret API key for payment processing.

**How to obtain**:
1. Login to [Stripe Dashboard](https://dashboard.stripe.com)
2. Go to Developers → API Keys
3. Copy "Secret key"
4. **Important**: Use test keys for development, live keys for production

### AAD_TENANT_ID

**Format**: GUID
**Required**: Yes
**Example**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
**Description**: Microsoft Entra ID (Azure AD) tenant identifier.

**How to obtain**:
1. Navigate to Azure Portal
2. Go to "Microsoft Entra ID" (or "Azure Active Directory")
3. Go to "Overview"
4. Copy "Tenant ID"

**Alternative** (CLI):
```bash
az account show --query tenantId -o tsv
```

### AAD_AUDIENCE

**Format**: GUID (Application/Client ID)
**Required**: Yes
**Example**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
**Description**: The Application (Client) ID of your API app registration.

**How to obtain**:
1. Navigate to Azure Portal
2. Go to "Microsoft Entra ID" → "App registrations"
3. Select your API application
4. Copy "Application (client) ID"

### AZURE_FUNCTIONAPP_NAME

**Format**: String
**Required**: Only for existing function-app deployment
**Example**: `onepageauthorapi`
**Description**: Name of the existing function app to deploy.

### AZURE_RESOURCE_GROUP

**Format**: String
**Required**: Only for existing function-app deployment
**Example**: `OnePageAuthorAPI-RG`
**Description**: Resource group containing the existing function app.

### AZURE_LOCATION

**Format**: String (Azure region name)
**Required**: Only for existing function-app deployment
**Example**: `East US`
**Description**: Azure region for the existing function app.

### ISW_DNS_ZONE_NAME

**Format**: String (domain name)
**Required**: No (optional)
**Example**: `yourdomain.com`, `example.org`
**Description**: Custom domain name for DNS zone creation.
**Notes**:
- Do not include "https://" or "www"
- Must be a valid domain name you own
- Used for custom domain mapping

### ISW_STATIC_WEB_APP_REPO_URL

**Format**: String (GitHub repository URL)
**Required**: No (optional)
**Example**: `https://github.com/username/repository`
**Description**: GitHub repository URL for Static Web App deployment.
**Notes**:
- Must be a valid GitHub repository URL
- Repository should contain a static web application
- GitHub token will be used for authentication

### ISW_STATIC_WEB_APP_BRANCH

**Format**: String (git branch name)
**Required**: No (optional, defaults to "main")
**Example**: `main`, `master`, `production`
**Description**: Git branch to deploy for Static Web App.

### DEPLOY_IMAGE_API

**Format**: String (`"true"` or `"false"`)
**Required**: No (optional)
**Example**: `true`
**Description**: Flag to enable/disable ImageAPI Function App deployment.
**Notes**:
- Set to `"true"` to deploy
- Leave empty or set to `"false"` to skip
- Infrastructure must be deployed first

### DEPLOY_ISW_FUNCTIONS

**Format**: String (`"true"` or `"false"`)
**Required**: No (optional)
**Example**: `true`
**Description**: Flag to enable/disable InkStainedWretchFunctions deployment.
**Notes**:
- Set to `"true"` to deploy
- Leave empty or set to `"false"` to skip
- Infrastructure must be deployed first

### DEPLOY_ISW_STRIPE

**Format**: String (`"true"` or `"false"`)
**Required**: No (optional)
**Example**: `true`
**Description**: Flag to enable/disable InkStainedWretchStripe deployment.
**Notes**:
- Set to `"true"` to deploy
- Leave empty or set to `"false"` to skip
- Requires `STRIPE_API_KEY` to be configured
- Infrastructure must be deployed first

## 🚀 Deployment Scenarios

### Scenario 1: Full Deployment (All Resources)

Configure all required secrets plus:
- `DEPLOY_IMAGE_API=true`
- `DEPLOY_ISW_FUNCTIONS=true`
- `DEPLOY_ISW_STRIPE=true`
- `STRIPE_API_KEY` (required for Stripe)

**Result**: All infrastructure and all three Function Apps deployed.

### Scenario 2: Infrastructure Only

Configure only required secrets, skip deployment control flags.

**Result**: Infrastructure deployed (Storage, Key Vault, App Insights), no Function Apps deployed.

### Scenario 3: Partial Deployment

Configure required secrets plus specific deployment flags:
- `DEPLOY_IMAGE_API=true`
- Leave others empty

**Result**: Infrastructure + ImageAPI only.

### Scenario 4: Existing function-app Only

Configure only:
- `AZURE_CREDENTIALS`
- `AZURE_FUNCTIONAPP_NAME`
- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION`

**Result**: Only the existing function-app is deployed, Ink Stained Wretches infrastructure is skipped.

## 🛡️ Security Best Practices

1. **Never commit secrets to code** ✅
   - Always use GitHub Secrets
   - Never hardcode in workflows or code

2. **Use test credentials for development** ✅
   - Stripe: Use `sk_test_` keys
   - Cosmos DB: Use separate development account
   - Azure: Use separate development subscription

3. **Rotate secrets regularly** ✅
   - Service Principal credentials
   - Stripe API keys
   - Cosmos DB keys

4. **Use minimal permissions** ✅
   - Service Principal: Use custom roles instead of Contributor when possible
   - Limit scope to specific resource groups

5. **Audit secret access** ✅
   - Review GitHub Actions logs regularly
   - Monitor Azure Activity logs
   - Enable Azure Key Vault logging

## ❓ Common Questions

**Q: What if I don't have some of the optional secrets?**
A: The deployment will skip those components. For example, if `ISW_DNS_ZONE_NAME` is not set, the DNS Zone will not be created.

**Q: Can I use the same Service Principal for multiple environments?**
A: Yes, but it's recommended to use separate Service Principals for development, staging, and production environments for better security and isolation.

**Q: How do I know which secrets are missing?**
A: The workflow logs will show warnings like "⚠️ Skipping deployment: Required secrets not configured" with the specific secret names.

**Q: Can I change secrets after initial deployment?**
A: Yes, update the secrets in GitHub repository settings. The next workflow run will use the new values.

**Q: What happens if deployment fails?**
A: The workflow uses `continue-on-error: true`, so a failure in one component won't stop the entire workflow. Check the logs for the specific error.

## 📞 Support

If you need help setting up secrets:
1. Review this document and the [Deployment Guide](DEPLOYMENT_GUIDE.md)
2. Check the GitHub Actions logs for specific error messages
3. Verify secret names match exactly (case-sensitive)
4. Open an issue in the repository if problems persist

---

**Last Updated**: 2024
**Related Documentation**: 
- [Deployment Guide](DEPLOYMENT_GUIDE.md)
- [README.md](../README.md)
