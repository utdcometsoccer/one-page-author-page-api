# GitHub Secrets Configuration Guide

This document provides a comprehensive mapping of GitHub Secrets to environment variables for all Function Apps in the OnePageAuthor API platform.

## Table of Contents
- [Overview](#overview)
- [Required vs Optional Secrets](#required-vs-optional-secrets)
- [InkStainedWretchFunctions Secrets](#inkstainedwretchfunctions-secrets)
- [ImageAPI Secrets](#imageapi-secrets)
- [InkStainedWretchStripe Secrets](#inkstainedwretchstripe-secrets)
- [Setting Up GitHub Secrets](#setting-up-github-secrets)

## Overview

The deployment workflow (`.github/workflows/main_onepageauthorapi.yml`) reads GitHub Secrets and conditionally passes them to the Bicep template (`infra/inkstainedwretches.bicep`). Only non-empty secrets are added as environment variables to the Function Apps, allowing for flexible deployment configurations.

## Required vs Optional Secrets

### Core Infrastructure Secrets (Required for Deployment)
| Secret Name | Purpose | Where to Find |
|-------------|---------|---------------|
| `ISW_RESOURCE_GROUP` | Azure Resource Group name | Azure Portal → Resource Groups |
| `ISW_LOCATION` | Azure region (e.g., "eastus", "westus2") | Azure regions list |
| `ISW_BASE_NAME` | Base name for all resources | Your chosen naming convention |

### Azure Authentication (Required for Deployment)
| Secret Name | Purpose | Where to Find |
|-------------|---------|---------------|
| `AZURE_CREDENTIALS` | Service Principal credentials for Azure CLI | See [Azure Login Action docs](https://github.com/Azure/login#configure-a-service-principal-with-a-secret) |

## InkStainedWretchFunctions Secrets

### Required Secrets
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `COSMOSDB_CONNECTION_STRING` | `CosmosDBConnection` | Cosmos DB connection for triggers | Azure Portal → Cosmos DB → Keys → Primary Connection String |
| `COSMOSDB_ENDPOINT_URI` | `COSMOSDB_ENDPOINT_URI` | Cosmos DB endpoint URL | Azure Portal → Cosmos DB → Keys → URI |
| `COSMOSDB_PRIMARY_KEY` | `COSMOSDB_PRIMARY_KEY` | Cosmos DB access key | Azure Portal → Cosmos DB → Keys → Primary Key |
| `COSMOSDB_DATABASE_ID` | `COSMOSDB_DATABASE_ID` | Database name | Your database name (typically "OnePageAuthorDb") |

### Optional Secrets - Azure AD Authentication
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `AAD_TENANT_ID` | `AAD_TENANT_ID` | Azure AD tenant ID | Azure Portal → Microsoft Entra ID → Overview → Tenant ID |
| `AAD_AUDIENCE` | `AAD_AUDIENCE` | Azure AD client ID | Azure Portal → Microsoft Entra ID → App registrations → Application ID |

### Optional Secrets - Domain Management
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `AZURE_SUBSCRIPTION_ID` | `AZURE_SUBSCRIPTION_ID` | Azure subscription for DNS/Front Door | Azure Portal → Subscriptions → Subscription ID |
| `AZURE_DNS_RESOURCE_GROUP` | `AZURE_DNS_RESOURCE_GROUP` | Resource group for DNS zones | Azure Portal → Resource Groups |
| `ISW_DNS_ZONE_NAME` | (Bicep parameter) | DNS zone to create | Your domain name |

### Optional Secrets - Google Domains Integration
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `GOOGLE_CLOUD_PROJECT_ID` | `GOOGLE_CLOUD_PROJECT_ID` | Google Cloud project ID | [Google Cloud Console](https://console.cloud.google.com) → Project ID |
| `GOOGLE_DOMAINS_LOCATION` | `GOOGLE_DOMAINS_LOCATION` | Location for domain operations | Default: "global" |

### Optional Secrets - Amazon Product Advertising API
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `AMAZON_PRODUCT_ACCESS_KEY` | `AMAZON_PRODUCT_ACCESS_KEY` | AWS access key ID | [AWS Console](https://console.aws.amazon.com) → Security Credentials |
| `AMAZON_PRODUCT_SECRET_KEY` | `AMAZON_PRODUCT_SECRET_KEY` | AWS secret access key | Created with Access Key (save immediately) |
| `AMAZON_PRODUCT_PARTNER_TAG` | `AMAZON_PRODUCT_PARTNER_TAG` | Amazon Associates tracking ID | [Amazon Associates](https://affiliate-program.amazon.com) |
| `AMAZON_PRODUCT_REGION` | `AMAZON_PRODUCT_REGION` | AWS region | Default: "us-east-1" |
| `AMAZON_PRODUCT_MARKETPLACE` | `AMAZON_PRODUCT_MARKETPLACE` | Target marketplace | Default: "www.amazon.com" |

### Optional Secrets - Penguin Random House API
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `PENGUIN_RANDOM_HOUSE_API_KEY` | `PENGUIN_RANDOM_HOUSE_API_KEY` | PRH API authentication key | [PRH Developer Portal](https://developer.penguinrandomhouse.com) |
| `PENGUIN_RANDOM_HOUSE_API_DOMAIN` | `PENGUIN_RANDOM_HOUSE_API_DOMAIN` | PRH API domain | Default: "PRH.US" |

## ImageAPI Secrets

### Required Secrets
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `COSMOSDB_ENDPOINT_URI` | `COSMOSDB_ENDPOINT_URI` | Cosmos DB endpoint URL | Azure Portal → Cosmos DB → Keys → URI |
| `COSMOSDB_PRIMARY_KEY` | `COSMOSDB_PRIMARY_KEY` | Cosmos DB access key | Azure Portal → Cosmos DB → Keys → Primary Key |
| `COSMOSDB_DATABASE_ID` | `COSMOSDB_DATABASE_ID` | Database name | Your database name |
| `COSMOSDB_CONNECTION_STRING` | `COSMOSDB_CONNECTION_STRING` | Full connection string | Azure Portal → Cosmos DB → Keys → Primary Connection String |
| `AZURE_STORAGE_CONNECTION_STRING` | `AZURE_STORAGE_CONNECTION_STRING` | Blob storage connection | Azure Portal → Storage Account → Access keys → Connection string |

### Optional Secrets - Azure AD Authentication
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `AAD_TENANT_ID` | `AAD_TENANT_ID` | Azure AD tenant ID | Azure Portal → Microsoft Entra ID → Tenant ID |
| `AAD_AUDIENCE` | `AAD_AUDIENCE` | Azure AD client ID | Azure Portal → Microsoft Entra ID → App registrations |
| `AAD_AUTHORITY` | `AAD_AUTHORITY` | Azure AD authority URL | Format: `https://login.microsoftonline.com/{tenant-id}/v2.0` |

## InkStainedWretchStripe Secrets

### Required Secrets
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `STRIPE_API_KEY` | `STRIPE_API_KEY` | Stripe secret key | [Stripe Dashboard](https://dashboard.stripe.com) → Developers → API keys |
| `STRIPE_WEBHOOK_SECRET` | `STRIPE_WEBHOOK_SECRET` | Webhook signing secret | Stripe Dashboard → Developers → Webhooks → Endpoint → Signing secret |
| `COSMOSDB_ENDPOINT_URI` | `COSMOSDB_ENDPOINT_URI` | Cosmos DB endpoint URL | Azure Portal → Cosmos DB → Keys → URI |
| `COSMOSDB_PRIMARY_KEY` | `COSMOSDB_PRIMARY_KEY` | Cosmos DB access key | Azure Portal → Cosmos DB → Keys → Primary Key |
| `COSMOSDB_DATABASE_ID` | `COSMOSDB_DATABASE_ID` | Database name | Your database name |
| `COSMOSDB_CONNECTION_STRING` | `COSMOSDB_CONNECTION_STRING` | Full connection string | Azure Portal → Cosmos DB → Keys |

### Optional Secrets - Azure AD Authentication
| GitHub Secret | Environment Variable | Purpose | Where to Find |
|---------------|---------------------|---------|---------------|
| `AAD_TENANT_ID` | `AAD_TENANT_ID` | Azure AD tenant ID | Azure Portal → Microsoft Entra ID → Tenant ID |
| `AAD_AUDIENCE` | `AAD_AUDIENCE` | Azure AD audience/client ID | Azure Portal → Microsoft Entra ID → App registrations |
| `AAD_CLIENT_ID` | `AAD_CLIENT_ID` | Azure AD client ID | Same as AAD_AUDIENCE in most cases |

## Setting Up GitHub Secrets

### Adding Secrets via GitHub UI
1. Navigate to your repository on GitHub
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter the secret name (exactly as shown in the tables above)
5. Enter the secret value
6. Click **Add secret**

### Adding Secrets via GitHub CLI
```bash
# Example: Add Cosmos DB endpoint URI
gh secret set COSMOSDB_ENDPOINT_URI --body "https://your-account.documents.azure.com:443/"

# Example: Add Stripe API key
gh secret set STRIPE_API_KEY --body "sk_test_your_stripe_key"

# Example: Add from file
gh secret set AZURE_CREDENTIALS < azure-credentials.json
```

## Deployment Behavior

### Conditional Configuration
- **If a secret is set**: The corresponding environment variable is added to the Function App
- **If a secret is NOT set**: The environment variable is omitted (not set to empty string)
- This allows features to gracefully degrade when optional integrations are not configured

### Example: Amazon API Integration
```bash
# If you set these secrets:
AMAZON_PRODUCT_ACCESS_KEY=AKIA...
AMAZON_PRODUCT_SECRET_KEY=...
AMAZON_PRODUCT_PARTNER_TAG=yourtag-20

# Then these environment variables will be available in InkStainedWretchFunctions:
AMAZON_PRODUCT_ACCESS_KEY=AKIA...
AMAZON_PRODUCT_SECRET_KEY=...
AMAZON_PRODUCT_PARTNER_TAG=yourtag-20
AMAZON_PRODUCT_REGION=us-east-1  # default value
AMAZON_PRODUCT_MARKETPLACE=www.amazon.com  # default value

# If you DON'T set these secrets:
# The Amazon API integration will be unavailable, but other features will work normally
```

## Security Best Practices

1. **Never commit secrets to source control**
   - Use GitHub Secrets for all sensitive values
   - Keep secrets out of code, comments, and commit messages

2. **Rotate secrets regularly**
   - Change API keys and passwords periodically
   - Update GitHub Secrets when you rotate credentials

3. **Use separate credentials per environment**
   - Development: Use test/sandbox credentials
   - Production: Use live credentials
   - Consider using different GitHub environments

4. **Principle of least privilege**
   - Grant minimal permissions to service principals
   - Only set secrets that are actually needed

5. **Monitor secret usage**
   - Review GitHub Actions logs for authentication issues
   - Set up alerts for failed deployments

## Troubleshooting

### Deployment Fails with "Missing Required Parameter"
**Problem**: Bicep deployment reports a required parameter is missing

**Solution**: 
1. Check that the secret name in GitHub matches exactly (case-sensitive)
2. Verify the secret has a non-empty value
3. Review the workflow file to ensure the secret is passed to the deployment

### Function App Missing Environment Variables
**Problem**: Function App doesn't have expected environment variables

**Solution**:
1. Verify the secret was set in GitHub before the deployment ran
2. Check workflow logs for "Secure parameters: [REDACTED]" message
3. Re-run the deployment workflow after setting missing secrets

### "Secret not found" in Workflow Logs
**Problem**: Workflow references a secret that doesn't exist

**Solution**:
1. Go to GitHub → Settings → Secrets and variables → Actions
2. Add the missing secret
3. Re-run the workflow

## Related Documentation
- [InkStainedWretchFunctions README](../InkStainedWretchFunctions/README.md)
- [ImageAPI README](../ImageAPI/README.md)
- [InkStainedWretchStripe README](../InkStainedWretchStripe/README.md)
- [Azure Functions Configuration Reference](https://learn.microsoft.com/en-us/azure/azure-functions/functions-app-settings)
- [GitHub Actions Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
