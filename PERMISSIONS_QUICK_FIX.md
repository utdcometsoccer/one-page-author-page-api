# Quick Fix: Service Principal Permissions Error

## The Error

```
ERROR: "code": "InvalidTemplateDeployment"
"The client '***' with object id '3102a205-746e-4c19-b856-18fd3c3b31d8' 
does not have permission to perform action 'Microsoft.Authorization/roleAssignments/write'"
```

## The Solution (One Command)

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

## What Does This Do?

Grants your GitHub Actions service principal the **User Access Administrator** role, allowing it to:
- Assign Key Vault roles to Function Apps during deployment
- Create other role assignments defined in Bicep templates

## Requirements

Before running the script:
1. ✅ Install Azure CLI: `az version`
2. ✅ Login to Azure: `az login`
3. ✅ Have Owner or User Access Administrator permissions

## After Running

1. **Re-run your GitHub Actions workflow** - The deployment should now succeed
2. The script is **idempotent** - safe to run multiple times
3. No further action needed unless you create new service principals

## More Information

- **Full documentation**: [docs/SERVICE_PRINCIPAL_PERMISSIONS_FIX.md](docs/SERVICE_PRINCIPAL_PERMISSIONS_FIX.md)
- **Script details**: [infra/README.md](infra/README.md#grant-serviceprincipalPermissionsps1--grant-serviceprincipalPermissionssh)

## Alternative Scopes

**Subscription scope (default, recommended):**
```bash
./Grant-ServicePrincipalPermissions.sh
```

**Resource group scope only:**
```bash
./Grant-ServicePrincipalPermissions.sh -S resourcegroup -r YOUR_RESOURCE_GROUP_NAME
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Not logged in" | Run `az login` |
| "Service principal not found" | Verify name: `az ad sp list --display-name github-actions-inkstainedwretches` |
| "Permission denied" | Your account needs Owner or User Access Administrator role |
| Still fails after running | Wait 5 minutes for Azure role propagation |

---

**Note**: This is a **one-time setup** required before the first deployment. The permission allows your GitHub Actions service principal to configure access control for Azure resources during automated deployments.
