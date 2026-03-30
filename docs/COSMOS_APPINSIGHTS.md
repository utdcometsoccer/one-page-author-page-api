# Cosmos DB and Application Insights

> This document consolidates `COSMOS_APPINSIGHTS_DEPLOYMENT.md`, `QUICKSTART_COSMOS_APPINSIGHTS.md`, and `IMPLEMENTATION_SUMMARY_COSMOS_APPINSIGHTS.md` into a single reference.

## Overview

The GitHub Actions workflow supports conditional deployment of Azure Cosmos DB Account and Application Insights resources. These resources are only deployed when:

1. The appropriate secrets are configured in the repository
2. The resources do not already exist in Azure

Both resources are deployed via Bicep templates (`infra/cosmosdb.bicep` and `infra/applicationinsights.bicep`) and are integrated as conditional steps in `.github/workflows/main_onepageauthorapi.yml`.

---

## Quick Start

### Prerequisites

- GitHub repository with admin access
- Azure subscription with appropriate permissions
- Azure CLI installed (for local testing and post-deployment verification)

### Step 1: Configure GitHub Secrets

Navigate to **Settings** → **Secrets and variables** → **Actions** in your repository.

#### Minimum Required Secrets

| Secret | Example Value | Notes |
|--------|---------------|-------|
| `COSMOSDB_RESOURCE_GROUP` | `rg-data-prod` | Resource group for data services |
| `COSMOSDB_ACCOUNT_NAME` | `myapp-cosmosdb-prod` | Must be globally unique |
| `COSMOSDB_LOCATION` | `eastus` | Azure region |
| `APPINSIGHTS_NAME` | `myapp-insights-prod` | Application Insights name |

#### Optional Secrets (Cost Optimization)

| Secret | Value | When to Use |
|--------|-------|-------------|
| `COSMOSDB_ENABLE_FREE_TIER` | `true` | Development (one per subscription) |
| `COSMOSDB_ENABLE_ZONE_REDUNDANCY` | `true` | Production high-availability |

### Step 2: Trigger Deployment

Push to `main` or manually trigger via **Actions** → **Build and deploy Azure Functions and Infrastructure** → **Run workflow**.

### Step 3: Monitor Deployment

Expand these steps in the workflow run:

- **"Deploy Cosmos DB Account"**
- **"Deploy Application Insights"**

**Expected output (success):**

```
✓ Checking if Cosmos DB Account exists...
✓ Resource group exists: rg-data-prod
📦 Cosmos DB Account does not exist. Deploying...
✓ Cosmos DB Account deployed successfully
```

**Already exists:**

```
✓ Cosmos DB Account already exists. Skipping deployment.
```

**Secrets not configured:**

```
⚠️ Skipping Cosmos DB deployment: Required secrets not configured
   Required: COSMOSDB_RESOURCE_GROUP, COSMOSDB_ACCOUNT_NAME, COSMOSDB_LOCATION
```

### Step 4: Retrieve Connection Strings

After deployment, retrieve sensitive values using Azure CLI:

```bash
# Cosmos DB connection string
az cosmosdb keys list \
  --name <COSMOSDB_ACCOUNT_NAME> \
  --resource-group <COSMOSDB_RESOURCE_GROUP> \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" \
  -o tsv

# Application Insights connection string
az monitor app-insights component show \
  --app <APPINSIGHTS_NAME> \
  --resource-group <COSMOSDB_RESOURCE_GROUP> \
  --query "connectionString" \
  -o tsv
```

### Step 5: Update `COSMOSDB_CONNECTION_STRING`

Update the `COSMOSDB_CONNECTION_STRING` GitHub secret with the Cosmos DB connection string retrieved above. This secret is used by the Ink Stained Wretches infrastructure deployment.

---

## Configuration & Required Secrets

### Cosmos DB Deployment Secrets

| Secret Name | Description | Required | Example |
|------------|-------------|----------|---------|
| `COSMOSDB_RESOURCE_GROUP` | Resource group name | ✅ Yes | `rg-cosmosdb-prod` |
| `COSMOSDB_ACCOUNT_NAME` | Account name (globally unique) | ✅ Yes | `one-page-author-db-account` |
| `COSMOSDB_LOCATION` | Azure region | ✅ Yes | `eastus` |
| `COSMOSDB_ENABLE_FREE_TIER` | Enable free tier (one per subscription) | ❌ Optional | `true` or `false` |
| `COSMOSDB_ENABLE_ZONE_REDUNDANCY` | Enable zone redundancy for HA | ❌ Optional | `true` or `false` (default: `false`) |

### Application Insights Deployment Secrets

| Secret Name | Description | Required | Example |
|------------|-------------|----------|---------|
| `APPINSIGHTS_NAME` | Application Insights resource name | ✅ Yes | `one-page-author-insights` |

Application Insights shares the same resource group and location as Cosmos DB (`COSMOSDB_RESOURCE_GROUP` and `COSMOSDB_LOCATION`).

### Configuration Examples

**Development:**

```
COSMOSDB_RESOURCE_GROUP=rg-data-dev
COSMOSDB_ACCOUNT_NAME=onepageauthor-db-dev
COSMOSDB_LOCATION=westus2
COSMOSDB_ENABLE_FREE_TIER=true
COSMOSDB_ENABLE_ZONE_REDUNDANCY=false
APPINSIGHTS_NAME=onepageauthor-insights-dev
```

**Production:**

```
COSMOSDB_RESOURCE_GROUP=rg-data-prod
COSMOSDB_ACCOUNT_NAME=onepageauthor-db-prod
COSMOSDB_LOCATION=eastus
COSMOSDB_ENABLE_FREE_TIER=false
COSMOSDB_ENABLE_ZONE_REDUNDANCY=true
APPINSIGHTS_NAME=onepageauthor-insights-prod
```

### Enabling/Disabling Deployment

- **Enable**: Configure all required secrets. The workflow deploys automatically on the next push to `main`.
- **Disable**: Remove or leave empty the required secrets. The workflow skips deployment with a warning.

---

## Implementation Details

### Infrastructure Templates

#### `infra/cosmosdb.bicep`

Creates an Azure Cosmos DB Account with:

- **Capacity Mode**: Serverless (pay-per-use)
- **API**: Core (SQL)
- **Consistency Level**: Session
- **Automatic Failover**: Enabled
- **Zone Redundancy**: Optional (disabled by default)
- **TLS Version**: 1.2 minimum
- **Backup**: Periodic mode with geo-redundant storage

**Parameters**:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `cosmosDbAccountName` | — | Globally unique account name (required) |
| `location` | — | Azure region (required) |
| `enableAutomaticFailover` | `true` | Enable failover |
| `enableFreeTier` | `false` | Enable free tier |
| `capacityMode` | `Serverless` | Serverless or Provisioned |
| `minimalTlsVersion` | `Tls12` | Minimum TLS version |

**Outputs**: `cosmosDbAccountName`, `cosmosDbAccountId`, `cosmosDbEndpoint`, `cosmosDbPrimaryKey` (sensitive), `cosmosDbConnectionString` (sensitive)

#### `infra/applicationinsights.bicep`

Creates an Application Insights resource with:

- **Application Type**: Web
- **Retention**: 90 days
- **Public Access**: Enabled for ingestion and query

**Parameters**:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `appInsightsName` | — | Resource name (required) |
| `location` | — | Azure region (required) |
| `applicationType` | `web` | Application type |
| `retentionInDays` | `90` | Data retention period |
| `workspaceResourceId` | — | Optional Log Analytics workspace |

**Outputs**: `appInsightsName`, `appInsightsId`, `appInsightsInstrumentationKey` (sensitive), `appInsightsConnectionString` (sensitive)

### Workflow Integration

The deployment steps are positioned after Azure authentication and before function app deployments:

```
1.  Checkout code
2.  Setup .NET environment
3.  Build all Function Apps
4.  Zip all Function App outputs
5.  Login to Azure
6.  → Deploy Cosmos DB Account          ← NEW (conditional)
7.  → Deploy Application Insights       ← NEW (conditional)
8.  Deploy function-app Infrastructure  (existing)
9.  Deploy function-app code            (existing)
10. Deploy Ink Stained Wretches Infrastructure (existing)
11. Deploy ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe (existing)
```

Both steps use `continue-on-error: true` so a skip or failure doesn't block the rest of the workflow.

### Deployment Scenarios

| Scenario | Secrets Configured | Result |
|----------|--------------------|--------|
| Deploy everything | All secrets | Both Cosmos DB and App Insights deploy |
| Cosmos DB only | Cosmos DB secrets only | Only Cosmos DB deploys |
| App Insights only | App Insights + shared secrets | Only App Insights deploys |
| Skip both | No secrets | Both steps skip with warning |
| Resources exist | All secrets | Both steps detect and skip |

### Backward Compatibility

- ✅ Existing workflow functionality unchanged
- ✅ Existing secrets continue to work
- ✅ New steps skip gracefully if not configured
- ✅ `continue-on-error: true` prevents workflow failures

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Free tier already used" | Set `COSMOSDB_ENABLE_FREE_TIER=false` (only one per subscription) |
| "Account name not available" | Cosmos DB names are globally unique — choose a different name |
| "Resource group creation fails" | Ensure Azure credentials have permission to create resource groups |
| "Deployment failed" | Check workflow logs; common causes: invalid credentials, insufficient permissions, quota exceeded, invalid region |
| Connection string not updating | Retrieve it manually (Step 4) and update `COSMOSDB_CONNECTION_STRING` secret |

### Verify Deployment via Azure CLI

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

### Security Considerations

- Sensitive outputs (keys, connection strings) are **not** displayed in workflow logs
- TLS 1.2 minimum is enforced for Cosmos DB
- Public network access is enabled by default; consider private endpoints and IP firewall rules for production
- GitHub Actions secrets are only accessible to authorized repository collaborators

### Cost Considerations

| Resource | Estimate |
|----------|----------|
| Cosmos DB (free tier) | $0 |
| Cosmos DB (serverless, low usage) | $5–20/month |
| Application Insights (< 5 GB/month) | $0 |
| Application Insights (additional data) | ~$2.30/GB |
| **Development total** | ~$0–10/month |
| **Production total** | ~$50–200/month (usage-dependent) |

### Cleanup

```bash
# Delete entire resource group (removes all resources)
az group delete --name <COSMOSDB_RESOURCE_GROUP> --yes --no-wait

# Or delete resources individually
az cosmosdb delete --name <COSMOSDB_ACCOUNT_NAME> --resource-group <COSMOSDB_RESOURCE_GROUP> --yes
az monitor app-insights component delete --app <APPINSIGHTS_NAME> --resource-group <COSMOSDB_RESOURCE_GROUP>
```
