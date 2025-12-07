# GitHub Actions Workflow Update

## Summary

This document describes the updates made to the GitHub Actions workflows for the OnePageAuthor API platform.

## Changes Made

### 1. Removed Obsolete Workflow
- **Deleted**: `.github/workflows/main_authorpageapi.yml`
  - This workflow used outdated authentication method with hard-coded client IDs
  - It was targeting a specific function app name `authorpageapi` that may no longer be in use
  - The workflow was not functioning properly

### 2. Updated Main Workflow
- **Updated**: `.github/workflows/main_onepageauthorapi.yml`
  - Fixed .NET version from `10.0.x` to `9.0.x` (matching the actual project version)
  - Added conditional checks for all Azure deployment steps
  - Added infrastructure deployment step using Bicep templates
  - Workflow now gracefully skips Azure deployment if required secrets are missing

### 3. New Infrastructure as Code
- **Created**: `infra/functionapp.bicep`
  - Bicep template for deploying Azure Function App infrastructure
  - Includes:
    - Storage Account (for Function App runtime)
    - App Service Plan (Consumption Y1 tier)
    - Function App (.NET 9 isolated worker)
  - Configured with security best practices (HTTPS only, TLS 1.2, disabled public blob access)

### 4. Updated .gitignore
- Added `infra/*.json` to ignore compiled Bicep templates

## How the Workflow Works

The updated workflow (`main_onepageauthorapi.yml`) performs the following steps:

1. **Build Phase** (Always runs):
   - Checks out code
   - Sets up .NET 9.0.x
   - Builds and publishes the function-app project
   - Creates a zip package of the published output

2. **Deploy Phase** (Only if secrets are configured):
   - Logs into Azure (requires `AZURE_CREDENTIALS`, `AZURE_FUNCTIONAPP_NAME`, `AZURE_RESOURCE_GROUP`)
   - Deploys infrastructure if Function App doesn't exist (also requires `AZURE_LOCATION`)
   - Deploys the function code using config-zip

### Conditional Logic

The workflow uses GitHub Actions conditional expressions to skip deployment steps if secrets are missing:

```yaml
if: ${{ secrets.AZURE_CREDENTIALS != '' && secrets.AZURE_FUNCTIONAPP_NAME != '' && secrets.AZURE_RESOURCE_GROUP != '' }}
```

This allows the workflow to:
- ✅ Run successfully even without Azure secrets (build-only mode)
- ✅ Deploy automatically when secrets are configured
- ✅ Create infrastructure automatically if it doesn't exist

## Required GitHub Secrets

To enable full deployment functionality, configure the following secrets in your GitHub repository:

### Required for Deployment
| Secret Name | Description | Example |
|------------|-------------|---------|
| `AZURE_CREDENTIALS` | Azure service principal credentials (JSON) | `{"clientId":"...","clientSecret":"...","subscriptionId":"...","tenantId":"..."}` |
| `AZURE_FUNCTIONAPP_NAME` | Name of the Azure Function App | `onepageauthorapi` |
| `AZURE_RESOURCE_GROUP` | Azure resource group name | `onepageauthor-rg` |

### Required for Infrastructure Creation
| Secret Name | Description | Example |
|------------|-------------|---------|
| `AZURE_LOCATION` | Azure region for resources | `eastus`, `westus2`, `centralus` |

### Creating Azure Service Principal

To create the `AZURE_CREDENTIALS` secret value:

```bash
# Login to Azure
az login

# Create service principal with contributor role
az ad sp create-for-rbac \
  --name "github-actions-onepageauthor" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
  --sdk-auth

# The output JSON should be saved as the AZURE_CREDENTIALS secret
```

### Adding Secrets to GitHub

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with its corresponding value

## Infrastructure Details

The Bicep template (`infra/functionapp.bicep`) creates:

### Storage Account
- **SKU**: Standard_LRS (Locally Redundant Storage)
- **Kind**: StorageV2
- **Security**: HTTPS only, TLS 1.2 minimum, no public blob access

### App Service Plan
- **SKU**: Y1 (Consumption/Dynamic)
- **Tier**: Dynamic (pay-per-execution)

### Function App
- **Runtime**: .NET 9 isolated worker
- **Functions Version**: v4
- **Configuration**:
  - `FUNCTIONS_WORKER_RUNTIME`: dotnet-isolated
  - `WEBSITE_RUN_FROM_PACKAGE`: 1
  - `FUNCTIONS_EXTENSION_VERSION`: ~4
- **Security**: HTTPS only, TLS 1.2 minimum, FTPS only

### Automatic Infrastructure Check

The workflow automatically checks if the Function App exists before attempting to deploy infrastructure:

```bash
if ! az functionapp show --name ${{ secrets.AZURE_FUNCTIONAPP_NAME }} \
                        --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} &> /dev/null; then
  # Deploy infrastructure
else
  # Skip - already exists
fi
```

## Testing the Workflow

### Build-Only Mode (No Secrets)
```bash
git push origin main
```
The workflow will:
- ✅ Build the function-app project
- ✅ Create the deployment package
- ⏭️ Skip Azure deployment steps

### Full Deployment Mode (With Secrets)
```bash
git push origin main
```
The workflow will:
- ✅ Build the function-app project
- ✅ Create the deployment package
- ✅ Login to Azure
- ✅ Deploy infrastructure (if needed)
- ✅ Deploy function code

## Troubleshooting

### Build Fails
- Ensure .NET 9.0.x SDK is available in the build environment
- Check that `function-app/function-app.csproj` exists and is valid

### Infrastructure Deployment Fails
- Verify `AZURE_LOCATION` secret is set and valid
- Ensure service principal has permissions to create resources
- Check that resource group exists

### Code Deployment Fails
- Verify Function App exists or infrastructure deployment succeeded
- Ensure service principal has permissions to deploy to the Function App
- Check that the zip package was created successfully

## Migration from Old Workflow

If you were using the old `main_authorpageapi.yml` workflow:

1. The old workflow used specific client IDs and tenant IDs as secrets
2. The new workflow uses `AZURE_CREDENTIALS` (service principal JSON)
3. You'll need to configure the new secrets as described above
4. The old workflow deployed to a hard-coded app name `authorpageapi`
5. The new workflow uses the `AZURE_FUNCTIONAPP_NAME` secret for flexibility

## Benefits of This Update

1. **Flexible**: Works with or without Azure secrets
2. **Self-Healing**: Automatically creates infrastructure if it doesn't exist
3. **Secure**: Uses service principal authentication instead of hard-coded credentials
4. **Modern**: Uses Bicep for infrastructure as code
5. **Maintainable**: Single workflow file with clear conditional logic
6. **Up-to-Date**: Uses correct .NET version (9.0.x) matching the project

## Future Enhancements

Potential improvements for future iterations:

- Add environment-specific deployments (dev, staging, production)
- Implement blue-green deployment strategy
- Add automated testing before deployment
- Include health check after deployment
- Add notification on deployment success/failure
- Implement infrastructure drift detection
