# Key Vault Role Assignment Implementation Summary

## Overview

This implementation provides cross-platform scripts to assign the **Key Vault Secrets Officer** role to the service principal **github-actions-inkstainedwretches** for a specified Azure Key Vault.

## Files Created

### 1. infra/Assign-KeyVaultRole.ps1
PowerShell script for Windows users that:
- Accepts Key Vault name as required parameter
- Uses variables for service principal name (default: `github-actions-inkstainedwretches`)
- Uses variables for role name (default: `Key Vault Secrets Officer`)
- Checks if role assignment already exists (idempotent)
- Provides comprehensive error handling
- Includes colorful, informative output

### 2. infra/Assign-KeyVaultRole.sh
Bash script for Unix/Linux/macOS users that:
- Provides the same functionality as the PowerShell version
- Uses command-line flags for parameters
- Includes help functionality (`-h` flag)
- Made executable with proper permissions

### 3. infra/README.md (Updated)
Added documentation section for the management scripts including:
- Usage examples for both PowerShell and Bash
- Parameter descriptions
- Default values
- Requirements and prerequisites

## Key Features

### Variables Instead of Hardcoding
Both scripts use parameterized variables:
```powershell
# PowerShell
[string]$ServicePrincipalName = "github-actions-inkstainedwretches"
[string]$RoleName = "Key Vault Secrets Officer"
```

```bash
# Bash
SERVICE_PRINCIPAL_NAME="github-actions-inkstainedwretches"
ROLE_NAME="Key Vault Secrets Officer"
```

### Idempotency
The scripts check if the role assignment already exists before attempting to create it:
- Query existing role assignments for the service principal on the Key Vault
- If assignment exists, display success message and exit
- If assignment doesn't exist, create it

### Error Handling
Comprehensive validation and error handling:
- ✅ Verify Azure CLI is installed
- ✅ Verify user is authenticated (`az login`)
- ✅ Verify Key Vault exists and is accessible
- ✅ Verify service principal exists
- ✅ Handle multiple service principals with the same display name
- ✅ Provide actionable error messages

### User Experience
Clear, colorful output with progress indicators:
- Header with configuration summary
- Step-by-step progress messages
- Success/failure messages with color coding
- Helpful troubleshooting suggestions on error

## Usage Examples

### PowerShell (Windows)

```powershell
# Basic usage
.\Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault"

# With resource group
.\Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault" -ResourceGroupName "MyRG"

# Custom service principal and role
.\Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault" `
    -ServicePrincipalName "my-sp" `
    -RoleName "Key Vault Administrator"

# Get help
Get-Help .\Assign-KeyVaultRole.ps1 -Detailed
```

### Bash (Linux/macOS)

```bash
# Basic usage
./Assign-KeyVaultRole.sh -k mykeyvault

# With resource group
./Assign-KeyVaultRole.sh -k mykeyvault -r MyRG

# Custom service principal and role
./Assign-KeyVaultRole.sh -k mykeyvault -s my-sp -R "Key Vault Administrator"

# Get help
./Assign-KeyVaultRole.sh -h
```

## Prerequisites

1. **Azure CLI**: Must be installed and accessible in PATH
   - Install from: https://docs.microsoft.com/cli/azure/install-azure-cli

2. **Authentication**: User must be logged in to Azure
   ```bash
   az login
   ```

3. **Permissions**: User must have sufficient permissions to:
   - Read Key Vault information
   - Read service principal information
   - Assign roles (typically requires Owner or User Access Administrator role)

4. **Service Principal**: The service principal `github-actions-inkstainedwretches` must exist
   - Can be created with: `az ad sp create-for-rbac --name "github-actions-inkstainedwretches"`

## Security Considerations

### No Hardcoded Credentials
- Scripts use Azure CLI authentication
- No secrets or credentials stored in scripts
- Relies on user's existing Azure authentication

### Principle of Least Privilege
- Default role is "Key Vault Secrets Officer" (not Administrator)
- Allows managing secrets but not other Key Vault settings
- Can be customized via parameters if broader access is needed

### Safe Execution
- Idempotent design - safe to run multiple times
- Validates all prerequisites before making changes
- Clear error messages if permissions are insufficient

### Input Validation
- Required parameters are validated
- Azure resource existence is verified before attempting operations
- Service principal existence is verified

## Testing Performed

### Syntax Validation
- ✅ PowerShell: `Get-Command ./Assign-KeyVaultRole.ps1 -Syntax`
- ✅ Bash: `bash -n Assign-KeyVaultRole.sh`

### Help Functionality
- ✅ PowerShell: `Get-Help ./Assign-KeyVaultRole.ps1 -Detailed`
- ✅ Bash: `./Assign-KeyVaultRole.sh -h`

### Error Handling
- ✅ Missing required parameters show appropriate error messages
- ✅ Help text is displayed when parameters are missing

### Code Review
- ✅ Addressed feedback about error handling function extraction
- ✅ Improved code maintainability

## Integration with Existing Infrastructure

The scripts complement the existing infrastructure management tools in the `infra/` directory:

- **Bicep Templates**: Deploy resources (cosmosdb.bicep, inkstainedwretches.bicep, etc.)
- **Role Assignment Scripts**: Configure access to deployed resources
- **GitHub Actions Workflow**: Can integrate these scripts for automated setup

## Future Enhancements

Potential improvements for future iterations:

1. **Bulk Assignment**: Support assigning roles to multiple Key Vaults
2. **Configuration File**: Support reading parameters from a config file
3. **GitHub Actions Integration**: Create a workflow step that runs these scripts
4. **Validation Mode**: Add a dry-run flag to preview changes without applying them
5. **Role Removal**: Add complementary scripts to remove role assignments

## Conclusion

This implementation provides a production-ready solution for assigning the Key Vault Secrets Officer role to the github-actions-inkstainedwretches service principal. The scripts are:

- ✅ Cross-platform (PowerShell and Bash)
- ✅ Parameterized (no hardcoded values)
- ✅ Idempotent (safe to run multiple times)
- ✅ Well-documented (inline help and README)
- ✅ User-friendly (clear output and error messages)
- ✅ Secure (no credentials, principle of least privilege)
- ✅ Maintainable (clean code, error handling)

The scripts are ready for use in development, testing, and production environments.
