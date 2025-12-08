# Cosmos DB and Application Insights Deployment Documentation

## Overview

The GitHub Actions workflow now supports conditional deployment of Azure Cosmos DB Account and Application Insights resources. These resources will only be deployed if:

1. The appropriate secrets are configured in the repository
2. The resources do not already exist in Azure

## Required Secrets

To enable Cosmos DB and Application Insights deployment, configure the following secrets in your GitHub repository:

### Cosmos DB Deployment Secrets

| Secret Name | Description | Required | Example |
|------------|-------------|----------|---------|
| `COSMOSDB_RESOURCE_GROUP` | Resource group name for Cosmos DB | ✅ Yes | `rg-cosmosdb-prod` |
| `COSMOSDB_ACCOUNT_NAME` | Cosmos DB account name (globally unique) | ✅ Yes | `one-page-author-db-account` |
| `COSMOSDB_LOCATION` | Azure region for deployment | ✅ Yes | `eastus`, `westus2`, `centralus` |
| `COSMOSDB_ENABLE_FREE_TIER` | Enable free tier (one per subscription) | ❌ Optional | `true` or `false` |

### Application Insights Deployment Secrets

| Secret Name | Description | Required | Example |
|------------|-------------|----------|---------|
| `APPINSIGHTS_NAME` | Application Insights resource name | ✅ Yes | `one-page-author-insights` |

**Note**: Application Insights uses the same resource group and location as Cosmos DB (`COSMOSDB_RESOURCE_GROUP` and `COSMOSDB_LOCATION`).

## How to Configure Secrets

1. Navigate to your GitHub repository
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with its corresponding value
5. Click **Add secret**

## Deployment Behavior

### Cosmos DB Account Deployment

The workflow will:

1. Check if `COSMOSDB_RESOURCE_GROUP`, `COSMOSDB_ACCOUNT_NAME`, and `COSMOSDB_LOCATION` secrets are configured
2. If missing, skip deployment with a warning message
3. If configured:
   - Create the resource group if it doesn't exist
   - Check if Cosmos DB Account already exists
   - If it doesn't exist, deploy using the `infra/cosmosdb.bicep` template
   - If it exists, skip deployment

**Cosmos DB Configuration**:
- **Capacity Mode**: Serverless (no pre-provisioned throughput required)
- **API**: Core (SQL) API
- **Consistency Level**: Session (default)
- **Automatic Failover**: Enabled
- **TLS Version**: 1.2 (minimum)
- **Zone Redundancy**: Enabled for high availability
- **Backup**: Periodic mode with geo-redundant storage

### Application Insights Deployment

The workflow will:

1. Check if `COSMOSDB_RESOURCE_GROUP`, `APPINSIGHTS_NAME`, and `COSMOSDB_LOCATION` secrets are configured
2. If missing, skip deployment with a warning message
3. If configured:
   - Ensure the resource group exists (same as Cosmos DB)
   - Check if Application Insights already exists
   - If it doesn't exist, deploy using the `infra/applicationinsights.bicep` template
   - If it exists, skip deployment

**Application Insights Configuration**:
- **Application Type**: Web
- **Retention**: 90 days
- **Public Access**: Enabled for ingestion and query

## Infrastructure Templates

### Cosmos DB Bicep Template

Location: `infra/cosmosdb.bicep`

This template creates:
- Azure Cosmos DB Account with serverless capacity
- Configured for Core (SQL) API
- Optimized for development and production workloads

Key outputs:
- `cosmosDbAccountName` - Account name
- `cosmosDbAccountId` - Resource ID
- `cosmosDbEndpoint` - Endpoint URI
- `cosmosDbPrimaryKey` - Primary access key (sensitive)
- `cosmosDbConnectionString` - Full connection string (sensitive)

### Application Insights Bicep Template

Location: `infra/applicationinsights.bicep`

This template creates:
- Application Insights resource for monitoring
- Configured for web application monitoring
- 90-day retention period

Key outputs:
- `appInsightsName` - Resource name
- `appInsightsId` - Resource ID
- `appInsightsInstrumentationKey` - Instrumentation key (sensitive)
- `appInsightsConnectionString` - Connection string (sensitive)

## Workflow Integration

The new deployment steps are integrated into the `.github/workflows/main_onepageauthorapi.yml` workflow:

1. **Azure Authentication** (existing step)
2. **Deploy Cosmos DB Account** (NEW) - Conditional deployment
3. **Deploy Application Insights** (NEW) - Conditional deployment
4. **Deploy function-app Infrastructure** (existing step)
5. ... (remaining deployment steps)

## Enabling/Disabling Deployment

### To Enable Deployment

Configure all required secrets as described above. The workflow will automatically deploy the resources on the next push to the `main` branch.

### To Disable Deployment

Remove or leave empty the required secrets. The workflow will skip deployment with a warning message:

```
⚠️ Skipping Cosmos DB deployment: Required secrets not configured
   Required: COSMOSDB_RESOURCE_GROUP, COSMOSDB_ACCOUNT_NAME, COSMOSDB_LOCATION
```

## Resource Group Strategy

The implementation allows you to:

1. **Use a separate resource group for Cosmos DB and Application Insights** - Specify a different value for `COSMOSDB_RESOURCE_GROUP` than your function app resource groups
2. **Co-locate resources** - Use the same resource group for all resources by using the same value for `COSMOSDB_RESOURCE_GROUP` and `ISW_RESOURCE_GROUP`

This flexibility allows you to organize resources according to your deployment and management preferences.

## Example Configuration

For a production environment:

```
# Cosmos DB & Application Insights
COSMOSDB_RESOURCE_GROUP=rg-data-prod
COSMOSDB_ACCOUNT_NAME=onepageauthor-db-prod
COSMOSDB_LOCATION=eastus
COSMOSDB_ENABLE_FREE_TIER=false
APPINSIGHTS_NAME=onepageauthor-insights-prod

# Function Apps (existing)
ISW_RESOURCE_GROUP=rg-functions-prod
ISW_LOCATION=eastus
ISW_BASE_NAME=onepageauthor-prod
```

For a development environment:

```
# Cosmos DB & Application Insights
COSMOSDB_RESOURCE_GROUP=rg-data-dev
COSMOSDB_ACCOUNT_NAME=onepageauthor-db-dev
COSMOSDB_LOCATION=westus2
COSMOSDB_ENABLE_FREE_TIER=true
APPINSIGHTS_NAME=onepageauthor-insights-dev

# Function Apps (existing)
ISW_RESOURCE_GROUP=rg-functions-dev
ISW_LOCATION=westus2
ISW_BASE_NAME=onepageauthor-dev
```

## Troubleshooting

### Deployment Fails with "Free tier already used"

Only one Cosmos DB account per subscription can use the free tier. Either:
- Set `COSMOSDB_ENABLE_FREE_TIER=false`
- Or use a different subscription

### Cosmos DB Account Name Already Taken

Cosmos DB account names must be globally unique across all Azure. Choose a different name in `COSMOSDB_ACCOUNT_NAME`.

### Resource Group Creation Fails

Ensure your Azure credentials (`AZURE_CREDENTIALS` secret) have permission to create resource groups in your subscription.

## Security Considerations

1. **Secrets Protection**: The Bicep templates output sensitive information (keys, connection strings) which are only visible in deployment logs for authorized users
2. **Public Network Access**: Both resources are configured with public network access enabled. For production, consider:
   - Implementing private endpoints
   - Configuring IP firewall rules
   - Using managed identities instead of connection strings
3. **TLS Version**: Minimum TLS 1.2 is enforced for Cosmos DB

## Cost Considerations

### Cosmos DB
- **Serverless mode**: Pay only for Request Units (RUs) consumed
- **Free tier**: 1000 RU/s and 25 GB storage (one per subscription)
- **Estimated cost**: $0.25 per million RUs for serverless mode

### Application Insights
- **First 5 GB/month**: Free
- **Additional data**: ~$2.30/GB
- **Estimated cost**: Minimal for typical development/testing workloads

## Support

For issues or questions:
- Review workflow run logs in GitHub Actions
- Check Azure Portal for resource deployment status
- Consult Azure documentation for service-specific issues
