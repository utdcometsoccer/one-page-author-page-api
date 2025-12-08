# Quick Start Guide: Cosmos DB and Application Insights Deployment

This guide helps you quickly enable automated deployment of Cosmos DB and Application Insights in your GitHub Actions workflow.

## Prerequisites

- GitHub repository with admin access
- Azure subscription with appropriate permissions
- Azure CLI installed (for local testing)

## Step 1: Configure GitHub Secrets

Navigate to your repository: **Settings** → **Secrets and variables** → **Actions**

### Minimum Required Secrets

| Secret | Value | Notes |
|--------|-------|-------|
| `COSMOSDB_RESOURCE_GROUP` | `rg-data-prod` | Resource group for data services |
| `COSMOSDB_ACCOUNT_NAME` | `myapp-cosmosdb-prod` | Must be globally unique |
| `COSMOSDB_LOCATION` | `eastus` | Azure region |
| `APPINSIGHTS_NAME` | `myapp-insights-prod` | Application Insights name |

### Optional Secrets (Cost Optimization)

| Secret | Value | When to Use |
|--------|-------|-------------|
| `COSMOSDB_ENABLE_FREE_TIER` | `true` | Development (one per subscription) |
| `COSMOSDB_ENABLE_ZONE_REDUNDANCY` | `true` | Production (increases cost for HA) |

## Step 2: Trigger Deployment

Push to `main` branch or manually trigger the workflow:

```bash
git push origin main
```

Or in GitHub: **Actions** → **Build and deploy Azure Functions and Infrastructure** → **Run workflow**

## Step 3: Monitor Deployment

1. Go to **Actions** tab in your repository
2. Click on the running workflow
3. Expand the deployment steps:
   - "Deploy Cosmos DB Account"
   - "Deploy Application Insights"

### Expected Output

**Success:**
```
✓ Checking if Cosmos DB Account exists...
✓ Resource group exists: rg-data-prod
📦 Cosmos DB Account does not exist. Deploying...
✓ Cosmos DB Account deployed successfully
```

**Already Exists:**
```
✓ Checking if Cosmos DB Account exists...
✓ Cosmos DB Account already exists. Skipping deployment.
```

**Skipped:**
```
⚠️ Skipping Cosmos DB deployment: Required secrets not configured
```

## Step 4: Retrieve Connection Strings

After deployment, retrieve sensitive values using Azure CLI:

### Cosmos DB Connection String

```bash
az cosmosdb keys list \
  --name <COSMOSDB_ACCOUNT_NAME> \
  --resource-group <COSMOSDB_RESOURCE_GROUP> \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" \
  -o tsv
```

### Application Insights Connection String

```bash
az monitor app-insights component show \
  --app <APPINSIGHTS_NAME> \
  --resource-group <COSMOSDB_RESOURCE_GROUP> \
  --query "connectionString" \
  -o tsv
```

## Step 5: Update Application Secrets

Update the `COSMOSDB_CONNECTION_STRING` secret in GitHub with the value from Step 4. This is used by the Ink Stained Wretches infrastructure deployment.

## Configuration Examples

### Development Setup
```bash
# Minimal cost, single subscription free tier
COSMOSDB_RESOURCE_GROUP=rg-dev
COSMOSDB_ACCOUNT_NAME=myapp-dev
COSMOSDB_LOCATION=westus2
COSMOSDB_ENABLE_FREE_TIER=true
APPINSIGHTS_NAME=myapp-insights-dev
```

### Production Setup
```bash
# High availability with zone redundancy
COSMOSDB_RESOURCE_GROUP=rg-prod
COSMOSDB_ACCOUNT_NAME=myapp-prod
COSMOSDB_LOCATION=eastus
COSMOSDB_ENABLE_FREE_TIER=false
COSMOSDB_ENABLE_ZONE_REDUNDANCY=true
APPINSIGHTS_NAME=myapp-insights-prod
```

### Staging Setup
```bash
# Cost-optimized without zone redundancy
COSMOSDB_RESOURCE_GROUP=rg-staging
COSMOSDB_ACCOUNT_NAME=myapp-staging
COSMOSDB_LOCATION=eastus2
COSMOSDB_ENABLE_FREE_TIER=false
APPINSIGHTS_NAME=myapp-insights-staging
```

## Troubleshooting

### Issue: "Free tier already used"

**Solution:** Only one Cosmos DB free tier per subscription. Set `COSMOSDB_ENABLE_FREE_TIER=false`.

### Issue: "Account name not available"

**Solution:** Cosmos DB names must be globally unique. Try a different name.

### Issue: "Resource group not found"

**Solution:** The workflow creates the resource group automatically. Ensure your Azure credentials have permission.

### Issue: "Deployment failed"

**Solution:** Check workflow logs for specific error. Common causes:
- Invalid Azure credentials
- Insufficient permissions
- Resource quotas exceeded
- Invalid location/region

## Verify Deployment

### In Azure Portal

1. Navigate to **Resource Groups**
2. Find your resource group (e.g., `rg-data-prod`)
3. Verify resources:
   - Cosmos DB account
   - Application Insights

### Using Azure CLI

```bash
# List resources in resource group
az resource list \
  --resource-group <COSMOSDB_RESOURCE_GROUP> \
  --output table

# Check Cosmos DB status
az cosmosdb show \
  --name <COSMOSDB_ACCOUNT_NAME> \
  --resource-group <COSMOSDB_RESOURCE_GROUP> \
  --query "provisioningState"

# Check Application Insights status
az monitor app-insights component show \
  --app <APPINSIGHTS_NAME> \
  --resource-group <COSMOSDB_RESOURCE_GROUP> \
  --query "provisioningState"
```

## Next Steps

1. **Create Database and Containers**: Use Azure Portal or data seeding tools
2. **Configure Function Apps**: Update function app settings with connection strings
3. **Run Data Seeders**: Initialize data using seeder projects
4. **Monitor Application**: Use Application Insights for telemetry

## Cost Monitoring

Track costs in Azure Portal:

1. **Cost Management + Billing** → **Cost analysis**
2. Filter by resource group: `COSMOSDB_RESOURCE_GROUP`
3. View daily/monthly costs

**Expected Monthly Costs (Serverless):**
- Cosmos DB (free tier): $0
- Cosmos DB (low usage): $5-20
- Application Insights (low usage): $0-5
- **Total Development**: ~$0-10/month
- **Total Production**: ~$50-200/month (depends on usage)

## Support

- Full documentation: [`docs/COSMOS_APPINSIGHTS_DEPLOYMENT.md`](../docs/COSMOS_APPINSIGHTS_DEPLOYMENT.md)
- Implementation details: [`IMPLEMENTATION_SUMMARY_COSMOS_APPINSIGHTS.md`](../IMPLEMENTATION_SUMMARY_COSMOS_APPINSIGHTS.md)
- Azure documentation: [https://docs.microsoft.com/azure](https://docs.microsoft.com/azure)

## Cleanup

To remove deployed resources:

```bash
# Delete resource group (removes all resources)
az group delete \
  --name <COSMOSDB_RESOURCE_GROUP> \
  --yes \
  --no-wait

# Or delete individual resources
az cosmosdb delete \
  --name <COSMOSDB_ACCOUNT_NAME> \
  --resource-group <COSMOSDB_RESOURCE_GROUP> \
  --yes

az monitor app-insights component delete \
  --app <APPINSIGHTS_NAME> \
  --resource-group <COSMOSDB_RESOURCE_GROUP>
```
