# Azure Functions Deployment Guide

This guide provides comprehensive documentation for the automated deployment workflow that deploys the One Page Author API infrastructure and Azure Functions to Azure.

## 📋 Overview

The deployment workflow (`main_onepageauthorapi.yml`) automatically builds and deploys:

1. **Existing function-app** - Core author data and infrastructure functions
2. **Ink Stained Wretches Infrastructure** - A complete resource group with:
   - Storage Account
   - Key Vault
   - DNS Zone (optional)
   - Application Insights
   - Three Function Apps:
     - **ImageAPI** - Image upload and management services
     - **InkStainedWretchFunctions** - Domain registration, localization, and external API integrations
     - **InkStainedWretchStripe** - Stripe payment processing and subscription management

All deployments are **conditional** and only execute when the required GitHub Secrets are configured.

## 🔑 Required GitHub Secrets

### Azure Authentication

| Secret Name | Required | Description | Example Value |
|-------------|----------|-------------|---------------|
| `AZURE_CREDENTIALS` | ✅ Yes | Azure Service Principal credentials in JSON format | `{"clientId": "...", "clientSecret": "...", "subscriptionId": "...", "tenantId": "..."}` |

**How to obtain**: Create a Service Principal with Contributor role on your subscription:
```bash
az ad sp create-for-rbac --name "github-actions-sp" \
  --role Contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

### Existing function-app Deployment

| Secret Name | Required | Description | Example Value |
|-------------|----------|-------------|---------------|
| `AZURE_FUNCTIONAPP_NAME` | ✅ For function-app | Name of the existing function app | `onepageauthorapi` |
| `AZURE_RESOURCE_GROUP` | ✅ For function-app | Resource group for the existing function app | `OnePageAuthorAPI-RG` |
| `AZURE_LOCATION` | ✅ For function-app | Azure region | `East US` |

### Standalone Cosmos DB Deployment

| Secret Name | Required | Description | Example Value |
|-------------|----------|-------------|---------------|
| `COSMOSDB_RESOURCE_GROUP` | Optional | Resource group for standalone Cosmos DB | `CosmosDB-RG` |
| `COSMOSDB_ACCOUNT_NAME` | Optional | Cosmos DB account name (globally unique) | `onepageauthor-db` |
| `COSMOSDB_LOCATION` | Optional | Azure region for Cosmos DB | `Central US` |
| `COSMOSDB_ENABLE_FREE_TIER` | Optional | Enable free tier (one per subscription) | `true` or `false` |
| `COSMOSDB_ENABLE_ZONE_REDUNDANCY` | Optional | Enable zone redundancy | `true` or `false` |

### Standalone Application Insights Deployment

| Secret Name | Required | Description | Example Value |
|-------------|----------|-------------|---------------|
| `APPINSIGHTS_NAME` | Optional | Application Insights resource name | `onepageauthor-insights` |

**Note**: `COSMOSDB_RESOURCE_GROUP` and `COSMOSDB_LOCATION` are reused for Application Insights deployment if configured.

### Ink Stained Wretches Infrastructure

| Secret Name | Required | Description | Example Value |
|-------------|----------|-------------|---------------|
| `ISW_RESOURCE_GROUP` | ✅ Yes | Resource group name for Ink Stained Wretches resources | `InkStainedWretches-RG` |
| `ISW_LOCATION` | ✅ Yes | Azure region for deployment | `West US 2` |
| `ISW_BASE_NAME` | ✅ Yes | Base name for all resources (must be globally unique) | `inkstainedwretches` |
| `ISW_DNS_ZONE_NAME` | Optional | DNS Zone name (e.g., your custom domain) | `yourdomain.com` |

### Function App Configuration Secrets

| Secret Name | Required | Description | Example Value |
|-------------|----------|-------------|---------------|
| `COSMOSDB_CONNECTION_STRING` | ✅ Yes | Azure Cosmos DB connection string | `AccountEndpoint=https://...;AccountKey=...` |
| `STRIPE_API_KEY` | ✅ For Stripe | Stripe secret API key | `sk_test_...` or `sk_live_...` |
| `AAD_TENANT_ID` | ✅ Yes | Microsoft Entra ID tenant GUID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AAD_AUDIENCE` | ✅ Yes | API application/client ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |

### Deployment Control Flags

| Secret Name | Required | Description | Example Value |
|-------------|----------|-------------|---------------|
| `DEPLOY_IMAGE_API` | Optional | Enable ImageAPI deployment | `true` or `false` |
| `DEPLOY_ISW_FUNCTIONS` | Optional | Enable InkStainedWretchFunctions deployment | `true` or `false` |
| `DEPLOY_ISW_STRIPE` | Optional | Enable InkStainedWretchStripe deployment | `true` or `false` |

## 🚀 Deployment Workflow

### Trigger Events

The workflow runs automatically on:
- **Push to `main` branch** - Automatic deployment
- **Manual trigger** - Use GitHub Actions "Run workflow" button

### Workflow Steps

1. **Checkout Code** - Clone the repository
2. **Setup .NET** - Install .NET 10.0 SDK
3. **Build All Function Apps** - Compile and publish:
   - function-app
   - ImageAPI
   - InkStainedWretchFunctions
   - InkStainedWretchStripe
4. **Azure Authentication** - Login using Service Principal
5. **Deploy Cosmos DB Account** (Conditional)
   - Checks if Cosmos DB account exists
   - Creates account if needed using `cosmosdb.bicep`
6. **Deploy Application Insights** (Conditional)
   - Checks if Application Insights exists
   - Creates resource if needed using `applicationinsights.bicep`
7. **Deploy Existing function-app Infrastructure** (Conditional)
   - Checks if function app exists
   - Creates infrastructure if needed using `functionapp.bicep`
8. **Deploy function-app Code** (Conditional)
   - Deploys using `config-zip` method
9. **Deploy Ink Stained Wretches Infrastructure** (Conditional)
    - Creates resource group if it doesn't exist
    - Deploys all infrastructure using `inkstainedwretches.bicep`
    - Includes: Storage Account, Key Vault, App Insights, DNS Zone (optional), Function Apps
10. **Deploy ImageAPI** (Conditional)
    - Only if `DEPLOY_IMAGE_API=true`
11. **Deploy InkStainedWretchFunctions** (Conditional)
    - Only if `DEPLOY_ISW_FUNCTIONS=true`
12. **Deploy InkStainedWretchStripe** (Conditional)
    - Only if `DEPLOY_ISW_STRIPE=true`

## 🏗️ Infrastructure Components

### Storage Account

**Purpose**: Provides blob storage for Azure Functions runtime and application data.

**Configuration**:
- SKU: Standard_LRS
- TLS: Minimum 1.2
- Public access: Disabled
- Name format: `{baseName}storage` (e.g., `inkstainedwretchesstorage`)

### Key Vault

**Purpose**: Secure storage for secrets and certificates.

**Configuration**:
- SKU: Standard
- RBAC authorization: Enabled
- Soft delete: Enabled (90 days retention)
- Name format: `{baseName}-kv` (e.g., `inkstainedwretches-kv`)

### Application Insights

**Purpose**: Monitoring, logging, and diagnostics for all Function Apps.

**Configuration**:
- Type: Web
- Retention: 90 days
- Name format: `{baseName}-insights` (e.g., `inkstainedwretches-insights`)

**Key Features**:
- Request tracking
- Dependency monitoring
- Exception logging
- Performance metrics

### DNS Zone

**Purpose**: Manage custom domain DNS records.

**Configuration**:
- Zone type: Public
- Location: Global
- Deployed only if `ISW_DNS_ZONE_NAME` is provided

### Function Apps

Three Azure Functions are deployed on a shared Consumption Plan (Y1/Dynamic):

#### 1. ImageAPI
**Purpose**: Image upload, management, and retrieval services

**Configuration**:
- Runtime: .NET Isolated
- Name format: `{baseName}-imageapi`
- Environment Variables:
  - `COSMOSDB_CONNECTION_STRING`
  - `AAD_TENANT_ID`
  - `AAD_AUDIENCE`
  - `APPLICATIONINSIGHTS_CONNECTION_STRING`

**Endpoints**:
- `POST /api/upload` - Upload images
- `GET /api/images/{imageId}` - Retrieve image metadata
- `DELETE /api/images/{imageId}` - Delete images

#### 2. InkStainedWretchFunctions
**Purpose**: Domain registration, localization, and external API integrations

**Configuration**:
- Runtime: .NET Isolated
- Name format: `{baseName}-functions`
- Environment Variables:
  - `COSMOSDB_CONNECTION_STRING`
  - `AAD_TENANT_ID`
  - `AAD_AUDIENCE`
  - `APPLICATIONINSIGHTS_CONNECTION_STRING`

**Endpoints**:
- `GET /api/localizedtext/{culture}` - Get localized UI text
- `POST /api/domain-registrations` - Register domains
- `GET /api/domain-registrations` - List user domains
- External API integrations (Penguin Random House, Amazon)

#### 3. InkStainedWretchStripe
**Purpose**: Stripe payment processing and subscription management

**Configuration**:
- Runtime: .NET Isolated
- Name format: `{baseName}-stripe`
- Environment Variables:
  - `COSMOSDB_CONNECTION_STRING`
  - `STRIPE_API_KEY`
  - `AAD_TENANT_ID`
  - `AAD_AUDIENCE`
  - `APPLICATIONINSIGHTS_CONNECTION_STRING`

**Endpoints**:
- `POST /api/CreateStripeCheckoutSession` - Create checkout sessions
- `POST /api/CreateStripeCustomer` - Create Stripe customers
- `POST /api/CreateSubscription` - Create subscriptions
- `POST /api/WebHook` - Handle Stripe webhooks
- `GET /api/ListSubscription/{customerId}` - List subscriptions

## 📝 Setting Up GitHub Secrets

### Step-by-Step Guide

1. **Navigate to Repository Settings**
   - Go to your GitHub repository
   - Click "Settings" → "Secrets and variables" → "Actions"

2. **Add Azure Service Principal** (Required)
   ```bash
   # Create Service Principal and get credentials
   az ad sp create-for-rbac --name "github-actions-sp" \
     --role Contributor \
     --scopes /subscriptions/{subscription-id} \
     --sdk-auth
   ```
   - Copy the JSON output
   - Create secret: `AZURE_CREDENTIALS` with the JSON content

3. **Add Existing function-app Secrets** (If deploying existing function-app)
   - `AZURE_FUNCTIONAPP_NAME`: Your function app name
   - `AZURE_RESOURCE_GROUP`: Your resource group name
   - `AZURE_LOCATION`: Your Azure region (e.g., "East US")

4. **Add Ink Stained Wretches Infrastructure Secrets** (Required)
   - `ISW_RESOURCE_GROUP`: e.g., "InkStainedWretches-RG"
   - `ISW_LOCATION`: e.g., "West US 2"
   - `ISW_BASE_NAME`: e.g., "inkstainedwretches" (must be globally unique)

5. **Add Optional Infrastructure Secrets**
   - `ISW_DNS_ZONE_NAME`: Your custom domain (e.g., "yourdomain.com")

6. **Add Application Configuration Secrets** (Required)
   ```bash
   # Get Cosmos DB connection string
   az cosmosdb keys list --name {cosmos-account-name} \
     --resource-group {resource-group} \
     --type connection-strings \
     --query "connectionStrings[0].connectionString" -o tsv
   ```
   - `COSMOSDB_CONNECTION_STRING`: Cosmos DB connection string
   - `STRIPE_API_KEY`: From Stripe Dashboard → Developers → API Keys
   - `AAD_TENANT_ID`: From Azure Portal → Microsoft Entra ID → Tenant ID
   - `AAD_AUDIENCE`: From Azure Portal → App Registrations → Application ID

7. **Add Deployment Control Flags** (Optional)
   - `DEPLOY_IMAGE_API`: Set to "true" to deploy ImageAPI
   - `DEPLOY_ISW_FUNCTIONS`: Set to "true" to deploy InkStainedWretchFunctions
   - `DEPLOY_ISW_STRIPE`: Set to "true" to deploy InkStainedWretchStripe

## 🔄 Conditional Deployment Logic

The workflow uses intelligent conditional deployment:

### Resource-Level Conditions

Each resource in the Bicep template has a deployment condition:
```bicep
resource storageAccount ... = if (deployStorageAccount) { ... }
resource keyVault ... = if (deployKeyVault) { ... }
```

### Workflow-Level Conditions

Each workflow step checks for required secrets:
```bash
if [ -z "$REQUIRED_SECRET" ]; then
  echo "⚠️ Skipping deployment: Required secrets not configured"
  exit 0
fi
```

### Deployment Scenarios

| Scenario | Required Secrets | Result |
|----------|-----------------|--------|
| **Full Deployment** | All secrets configured | All resources and Function Apps deployed |
| **Infrastructure Only** | Infrastructure secrets only | Resources created, Function Apps skipped |
| **Partial Functions** | Specific `DEPLOY_*` flags set | Only selected Function Apps deployed |
| **No Ink Stained Wretches** | ISW secrets missing | Only existing function-app deployed |

## 🐛 Troubleshooting

### Common Issues

#### 1. Authentication Failures

**Error**: `Error: Login failed with Error: ...`

**Solution**:
- Verify `AZURE_CREDENTIALS` secret is correctly formatted JSON
- Check Service Principal has Contributor role
- Ensure subscription ID is correct

#### 2. Resource Name Conflicts

**Error**: `Resource name already exists`

**Solution**:
- Ensure `ISW_BASE_NAME` is globally unique
- Storage account names must be 3-24 characters, lowercase letters and numbers only
- Try a different base name or append a unique identifier

#### 3. Deployment Skipped

**Message**: `⚠️ Skipping deployment: Required secrets not configured`

**Solution**:
- Review the specific secret names mentioned in the warning
- Add the required secrets in GitHub repository settings
- Verify secret names match exactly (case-sensitive)

#### 4. Function App Deployment Fails

**Error**: `Error: Failed to deploy function app`

**Solution**:
- Ensure infrastructure deployment completed successfully first
- Verify the Function App exists in the resource group
- Check that `ISW_BASE_NAME` matches the infrastructure deployment
- Review Application Insights logs for detailed errors

#### 5. DNS Zone Issues

**Error**: `DNS Zone deployment failed`

**Solution**:
- Verify domain name format (no https://, no trailing slash)
- Ensure you own the domain
- Check if DNS Zone already exists in another resource group

### Viewing Deployment Logs

1. **GitHub Actions Logs**
   - Go to "Actions" tab in your repository
   - Click on the latest workflow run
   - Expand each step to view detailed logs

2. **Azure Portal Logs**
   - Navigate to Resource Group → Deployments
   - Click on the deployment to see detailed status
   - Review "Correlation ID" for tracking

3. **Function App Logs**
   - Navigate to Function App → Log stream
   - Review Application Insights → Logs for detailed diagnostics

### Debug Mode

Enable verbose logging by modifying the workflow:
```yaml
- name: 'Deploy Ink Stained Wretches Infrastructure'
  shell: bash
  run: |
    set -x  # Enable debug mode
    # ... rest of the script
```

## 🔒 Security Best Practices

### Secret Management

1. **Never commit secrets to code**
   - Always use GitHub Secrets
   - Use Azure Key Vault for runtime secrets
   - Rotate secrets regularly

2. **Use separate environments**
   - Development: Use test/sandbox accounts
   - Production: Use separate Service Principals with minimal permissions

3. **Limit Service Principal permissions**
   - Create separate Service Principals per environment
   - Use custom roles instead of Contributor when possible
   - Regularly audit access

### Infrastructure Security

1. **Storage Account**
   - Public access disabled by default
   - TLS 1.2 minimum
   - HTTPS only

2. **Key Vault**
   - RBAC authorization enabled
   - Soft delete enabled
   - Audit logging enabled

3. **Function Apps**
   - HTTPS only
   - FTPS only (no FTP)
   - Minimum TLS 1.2

## 📊 Monitoring and Observability

### Application Insights Integration

All Function Apps are automatically configured with Application Insights:

**Key Metrics**:
- Request rate and latency
- Failed requests
- Dependency calls
- Exception rates

**Access Logs**:
1. Navigate to Function App → Application Insights
2. Go to "Logs" to query telemetry
3. Use "Live Metrics" for real-time monitoring

### Example Queries

**Failed Requests**:
```kusto
requests
| where success == false
| summarize count() by resultCode, name
| order by count_ desc
```

**Slow Requests**:
```kusto
requests
| where duration > 5000
| project timestamp, name, duration, url
| order by duration desc
```

## 🔄 Updating Deployments

### Updating Infrastructure

To update infrastructure configuration:
1. Modify `infra/inkstainedwretches.bicep`
2. Commit and push to `main` branch
3. Workflow automatically redeploys infrastructure
4. Review deployment logs

### Updating Function Apps

To update Function App code:
1. Modify code in respective Function App project
2. Commit and push to `main` branch
3. Workflow automatically builds and deploys
4. Verify deployment in Azure Portal

### Rolling Back

To rollback a deployment:
1. Navigate to Azure Portal → Resource Group → Deployments
2. Find the previous successful deployment
3. Click "Redeploy" to rollback infrastructure
4. For code changes, revert the commit and push

## 📚 Additional Resources

- [Azure Functions Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

## 🤝 Support

For issues or questions:
1. Check this documentation first
2. Review GitHub Actions logs
3. Check Azure Portal deployment logs
4. Open an issue in the GitHub repository
5. Contact the development team

---

**Last Updated**: 2024
**Maintained by**: OnePageAuthor Development Team
