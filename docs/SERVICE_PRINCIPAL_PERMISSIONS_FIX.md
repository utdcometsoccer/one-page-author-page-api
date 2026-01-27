# Service Principal Role Assignment Permission Error - Solution

## Problem Statement

When deploying infrastructure using the GitHub Actions workflow, the following error occurs:

```
ERROR: "code": "InvalidTemplateDeployment", 
"message": "Deployment failed with multiple errors: 'Authorization failed for template resource 
'5134459b-8075-51ed-b49c-320c76569b49' of type 'Microsoft.Authorization/roleAssignments'. 
The client '***' with object id '3102a205-746e-4c19-b856-18fd3c3b31d8' does not have permission 
to perform action 'Microsoft.Authorization/roleAssignments/write' at scope 
'/subscriptions/3869f4ae-d40f-4bc2-9333-9744e204183b/resourceGroups/***/providers/Microsoft.KeyVault/vaults/***-kv/providers/Microsoft.Authorization/roleAssignments/...'"
```

## Root Cause

The Bicep template `inkstainedwretches.bicep` creates role assignments to grant Function Apps access to Key Vault:

```bicep
// Grant ImageAPI access to Key Vault
resource imageApiKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployKeyVault && deployImageApi && deployStorageAccount) {
  name: guid(keyVault.id, imageApiFunctionApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: imageApiFunctionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
```

To create these role assignments, the service principal executing the deployment needs the **User Access Administrator** role (or **Owner** role) at the subscription or resource group scope.

## Solution

Two scripts have been created to grant the required permissions to the service principal:

1. **Grant-ServicePrincipalPermissions.ps1** (PowerShell for Windows)
2. **Grant-ServicePrincipalPermissions.sh** (Bash for Linux/macOS)

These scripts assign the **User Access Administrator** role to the service principal, allowing it to create role assignments during deployment.

## Usage

### Option 1: PowerShell (Windows)

```powershell
cd infra

# Grant permissions at subscription scope (recommended)
./Grant-ServicePrincipalPermissions.ps1

# Or, grant permissions at resource group scope only
./Grant-ServicePrincipalPermissions.ps1 -Scope "resourcegroup" -ResourceGroupName "ISW_RESOURCE_GROUP_NAME"

# With custom service principal name
./Grant-ServicePrincipalPermissions.ps1 -ServicePrincipalName "github-actions-inkstainedwretches"
```

### Option 2: Bash (Linux/macOS)

```bash
cd infra

# Grant permissions at subscription scope (recommended)
./Grant-ServicePrincipalPermissions.sh

# Or, grant permissions at resource group scope only
./Grant-ServicePrincipalPermissions.sh -S resourcegroup -r ISW_RESOURCE_GROUP_NAME

# With custom service principal name
./Grant-ServicePrincipalPermissions.sh -s github-actions-inkstainedwretches
```

## Prerequisites

1. **Azure CLI**: Must be installed and accessible

   ```bash
   az version
   ```

2. **Azure Authentication**: You must be logged in

   ```bash
   az login
   ```

3. **Sufficient Permissions**: Your user account must have:
   - **Owner** role, OR
   - **User Access Administrator** role

   at the target scope (subscription or resource group)

4. **jq (for Bash script)**: JSON processor for parsing Azure CLI output

   ```bash
   # Ubuntu/Debian
   sudo apt-get install jq
   
   # macOS
   brew install jq
   
   # Other: https://stedolan.github.io/jq/download/
   ```

5. **Service Principal Exists**: The service principal must already be created
   - Default name: `github-actions-inkstainedwretches`
   - Object ID shown in error: `3102a205-746e-4c19-b856-18fd3c3b31d8`

## Verification

After running the script successfully, you can verify the role assignment:

### Verify at Subscription Scope

```bash
az role assignment list \
  --assignee 3102a205-746e-4c19-b856-18fd3c3b31d8 \
  --role "User Access Administrator" \
  --scope /subscriptions/3869f4ae-d40f-4bc2-9333-9744e204183b
```

### Verify at Resource Group Scope

```bash
az role assignment list \
  --assignee 3102a205-746e-4c19-b856-18fd3c3b31d8 \
  --role "User Access Administrator" \
  --resource-group YOUR_RESOURCE_GROUP_NAME
```

## Next Steps

After granting the permissions:

1. **Re-run the GitHub Actions workflow**: The deployment should now succeed
2. **Verify Key Vault role assignments**: Check that Function Apps have Key Vault access
3. **Test Function App functionality**: Ensure they can access Key Vault secrets

## Security Considerations

### Why User Access Administrator?

The **User Access Administrator** role is the least privileged role that allows creating role assignments. It:

- ✅ Allows creating/deleting role assignments only
- ✅ Does NOT grant full control over resources (unlike Owner)
- ✅ Follows the principle of least privilege
- ✅ Is sufficient for Bicep template deployments that assign roles

### Scope Selection

**Subscription Scope (Recommended)**:

- Allows the service principal to create role assignments anywhere in the subscription
- Required if deploying to multiple resource groups
- More flexible for infrastructure changes

**Resource Group Scope**:

- Limits role assignment permissions to a specific resource group
- More restrictive security posture
- Requires the service principal to have this role in each resource group where it deploys

### Alternative: Manual Role Assignments

If you prefer not to grant User Access Administrator to the service principal, you can:

1. Remove role assignment resources from `inkstainedwretches.bicep`
2. Manually assign Key Vault roles to Function Apps after deployment
3. Use the existing `Assign-KeyVaultRole.ps1` / `Assign-KeyVaultRole.sh` scripts

However, this approach requires manual intervention after each deployment.

## Troubleshooting

### Error: "Not logged in to Azure"

**Solution**: Run `az login` first

### Error: "Service principal not found"

**Solution**: Verify the service principal name matches the one used in GitHub Actions

```bash
az ad sp list --display-name github-actions-inkstainedwretches
```

### Error: "Failed to create role assignment"

**Solution**: Ensure your account has Owner or User Access Administrator role

```bash
az role assignment list --assignee YOUR_USER_EMAIL --role "Owner"
az role assignment list --assignee YOUR_USER_EMAIL --role "User Access Administrator"
```

### Script runs but deployment still fails

**Solution**:

1. Wait a few minutes for role propagation (can take up to 5 minutes)
2. Verify the role assignment was created (see Verification section)
3. Check the exact scope matches what the Bicep template expects

## Related Files

- **Scripts**: `infra/Grant-ServicePrincipalPermissions.ps1`, `infra/Grant-ServicePrincipalPermissions.sh`
- **Bicep Template**: `infra/inkstainedwretches.bicep` (lines 468-509 contain role assignments)
- **GitHub Workflow**: `.github/workflows/main_onepageauthorapi.yml` (line 370-432 for ISW deployment)
- **Documentation**: `infra/README.md`, `docs/GITHUB_SECRETS_REFERENCE.md`

## Summary

The error occurs because the service principal lacks permission to create role assignments. The solution is to grant the **User Access Administrator** role to the service principal at the subscription or resource group scope using the provided scripts. This is a one-time setup required before deploying infrastructure with role assignments.
