# Role Assignment Permissions

> This document consolidates `PERMISSIONS_QUICK_FIX.md`, `SERVICE_PRINCIPAL_PERMISSIONS_FIX.md`, and `WORKFLOW_ROLE_ASSIGNMENT_FIX.md` into a single reference.

## Problem Summary

When deploying infrastructure via GitHub Actions, the workflow fails with:

```
ERROR: {"code": "InvalidTemplateDeployment", "message": "... The client '***' with object id
'3102a205-746e-4c19-b856-18fd3c3b31d8' does not have permission to perform action
'Microsoft.Authorization/roleAssignments/write' at scope '...Microsoft.KeyVault/vaults/***-kv/...'"
```

**Root cause**: The Bicep template `infra/inkstainedwretches.bicep` creates role assignments that grant Function Apps access to Key Vault. Creating role assignments requires the deploying service principal to hold the **User Access Administrator** or **Owner** role — the default **Contributor** role is not sufficient.

---

## Quick Fix

Run this script **once** before deploying:

### Windows (PowerShell)

```powershell
cd infra
./Grant-ServicePrincipalPermissions.ps1
```

### Linux/macOS (Bash)

```bash
cd infra
./Grant-ServicePrincipalPermissions.sh
```

This grants the GitHub Actions service principal the **User Access Administrator** role, enabling it to assign Key Vault roles to Function Apps during deployment.

### Requirements

Before running the script:

1. ✅ Install Azure CLI: `az version`
2. ✅ Login to Azure: `az login`
3. ✅ Have Owner or User Access Administrator permissions on the target scope
4. ✅ Install jq (Bash script only): `sudo apt-get install jq` or `brew install jq`

### After Running

1. **Re-run your GitHub Actions workflow** — the deployment should now succeed.
2. The script is **idempotent** — safe to run multiple times.
3. Wait up to 5 minutes for Azure role propagation if deployment still fails immediately after.

### Alternative Scopes

```bash
# Subscription scope (default, recommended)
./Grant-ServicePrincipalPermissions.sh

# Resource group scope only
./Grant-ServicePrincipalPermissions.sh -S resourcegroup -r YOUR_RESOURCE_GROUP_NAME
```

---

## Detailed Solution

### What Triggers the Error

The Bicep template creates four role assignment resources:

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

Specifically, these four assignments are required:

1. `imageApiKeyVaultAccess` — ImageAPI Function App → Key Vault
2. `functionsKeyVaultAccess` — InkStainedWretchFunctions → Key Vault
3. `stripeKeyVaultAccess` — InkStainedWretchStripe → Key Vault
4. `configKeyVaultAccess` — InkStainedWretchesConfig → Key Vault

### Script Usage

#### PowerShell (Windows)

```powershell
cd infra

# Subscription scope (recommended)
./Grant-ServicePrincipalPermissions.ps1

# Resource group scope
./Grant-ServicePrincipalPermissions.ps1 -Scope "resourcegroup" -ResourceGroupName "ISW_RESOURCE_GROUP_NAME"

# Custom service principal name
./Grant-ServicePrincipalPermissions.ps1 -ServicePrincipalName "github-actions-inkstainedwretches"
```

#### Bash (Linux/macOS)

```bash
cd infra

# Subscription scope (recommended)
./Grant-ServicePrincipalPermissions.sh

# Resource group scope
./Grant-ServicePrincipalPermissions.sh -S resourcegroup -r ISW_RESOURCE_GROUP_NAME

# Custom service principal name
./Grant-ServicePrincipalPermissions.sh -s github-actions-inkstainedwretches
```

### Verifying the Role Assignment

```bash
# At subscription scope
az role assignment list \
  --assignee 3102a205-746e-4c19-b856-18fd3c3b31d8 \
  --role "User Access Administrator" \
  --scope /subscriptions/3869f4ae-d40f-4bc2-9333-9744e204183b

# At resource group scope
az role assignment list \
  --assignee 3102a205-746e-4c19-b856-18fd3c3b31d8 \
  --role "User Access Administrator" \
  --resource-group YOUR_RESOURCE_GROUP_NAME
```

### Security Considerations

**Why User Access Administrator?**

The **User Access Administrator** role is the least-privileged role that allows creating role assignments. It:

- ✅ Allows creating/deleting role assignments only
- ✅ Does NOT grant full resource control (unlike Owner)
- ✅ Follows the principle of least privilege

**Scope Selection**:

- **Subscription scope** (recommended): Allows role assignments anywhere in the subscription; needed when deploying to multiple resource groups.
- **Resource group scope**: More restrictive; limits permission to a single resource group.

**Alternative — manual role assignments**:

If you prefer not to grant User Access Administrator to the service principal, you can remove role assignment resources from `inkstainedwretches.bicep` and manually assign Key Vault roles after each deployment using the existing `Assign-KeyVaultRole.ps1` / `Assign-KeyVaultRole.sh` scripts.

### Troubleshooting

| Issue | Solution |
|-------|----------|
| "Not logged in" | Run `az login` |
| "Service principal not found" | Verify: `az ad sp list --display-name github-actions-inkstainedwretches` |
| "Permission denied" | Your account needs Owner or User Access Administrator role |
| "Failed to create role assignment" | Confirm your account has the correct role at the right scope |
| Still fails after running | Wait 5 minutes for Azure role propagation |

---

## Workflow-Specific Context

### The Failing Workflow Step

The error occurs in the GitHub Actions step **"Deploy Ink Stained Wretches Infrastructure"** (`.github/workflows/main_onepageauthorapi.yml`, lines ~370–432).

### Alternative Approach: Remove Role Assignments from Bicep

A second approach (already implemented in one version of the template) is to **remove** the role assignment resources from `infra/inkstainedwretches.bicep` entirely, making the template deployable with standard Contributor permissions. After infrastructure deployment, administrators run the helper script manually:

```bash
cd infra
./Assign-KeyVaultRole.sh -k <key-vault-name>
```

The template then outputs the principal IDs needed:

- `keyVaultId`
- `imageApiFunctionPrincipalId`
- `inkStainedWretchFunctionsPrincipalId`
- `inkStainedWretchStripePrincipalId`
- `inkStainedWretchesConfigPrincipalId`

**Trade-offs**:

| Approach | Pros | Cons |
|----------|------|------|
| Grant User Access Administrator to service principal | Fully automated deployments | Elevated service principal permissions |
| Remove role assignments from Bicep | Standard Contributor is sufficient | Manual post-deployment step required |

For most teams, granting User Access Administrator at subscription scope and running `Grant-ServicePrincipalPermissions.sh` once is the recommended path.

### Related Files

- `infra/Grant-ServicePrincipalPermissions.ps1` / `.sh` — Scripts to grant permissions
- `infra/Assign-KeyVaultRole.ps1` / `.sh` — Scripts for manual Key Vault role assignment
- `infra/inkstainedwretches.bicep` — Bicep template containing role assignment resources
- `.github/workflows/main_onepageauthorapi.yml` — CI/CD workflow
- `docs/DEPLOYMENT_GUIDE.md` — Post-deployment steps and troubleshooting
- `infra/README.md` — Management scripts documentation
