#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Assigns the Key Vault Secrets Officer role to a service principal if not already assigned.

.DESCRIPTION
    This script checks if the specified service principal has the Key Vault Secrets Officer role
    assigned for the specified Key Vault. If the role is not already assigned, it creates the
    role assignment.

.PARAMETER ServicePrincipalName
    The name of the service principal to assign the role to.
    Default: "github-actions-inkstainedwretches"

.PARAMETER RoleName
    The name of the Azure role to assign.
    Default: "Key Vault Secrets Officer"

.PARAMETER KeyVaultName
    The name of the Key Vault to assign permissions for.
    Required parameter.

.PARAMETER ResourceGroupName
    The name of the resource group containing the Key Vault.
    Optional - if not provided, the script will attempt to find the Key Vault.

.EXAMPLE
    ./Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault"

.EXAMPLE
    ./Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault" -ResourceGroupName "MyResourceGroup"

.EXAMPLE
    ./Assign-KeyVaultRole.ps1 -KeyVaultName "mykeyvault" -ServicePrincipalName "my-sp" -RoleName "Key Vault Administrator"

.NOTES
    Author: OnePageAuthor Team
    Requires: Azure CLI installed and user authenticated with sufficient permissions
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, HelpMessage = "Name of the Key Vault")]
    [ValidateNotNullOrEmpty()]
    [string]$KeyVaultName,

    [Parameter(Mandatory = $false, HelpMessage = "Name of the resource group containing the Key Vault")]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false, HelpMessage = "Name of the service principal")]
    [ValidateNotNullOrEmpty()]
    [string]$ServicePrincipalName = "github-actions-inkstainedwretches",

    [Parameter(Mandatory = $false, HelpMessage = "Name of the Azure role to assign")]
    [ValidateNotNullOrEmpty()]
    [string]$RoleName = "Key Vault Secrets Officer"
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Key Vault Role Assignment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Principal: $ServicePrincipalName" -ForegroundColor Yellow
Write-Host "Role Name:        $RoleName" -ForegroundColor Yellow
Write-Host "Key Vault Name:   $KeyVaultName" -ForegroundColor Yellow
Write-Host ""

try {
    # Check if Azure CLI is installed
    Write-Host "✓ Checking Azure CLI installation..." -ForegroundColor Gray
    $azVersion = az version --output json 2>$null | ConvertFrom-Json
    if (-not $azVersion) {
        throw "Azure CLI is not installed or not in PATH. Please install Azure CLI: https://docs.microsoft.com/cli/azure/install-azure-cli"
    }
    Write-Host "  Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Gray

    # Check if user is logged in
    Write-Host "✓ Checking Azure authentication..." -ForegroundColor Gray
    $account = az account show --output json 2>$null | ConvertFrom-Json
    if (-not $account) {
        throw "Not logged in to Azure. Please run 'az login' first."
    }
    Write-Host "  Subscription: $($account.name) ($($account.id))" -ForegroundColor Gray

    # Get Key Vault information
    Write-Host "✓ Retrieving Key Vault information..." -ForegroundColor Gray
    if ($ResourceGroupName) {
        $keyVault = az keyvault show --name $KeyVaultName --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
    }
    else {
        $keyVault = az keyvault show --name $KeyVaultName --output json 2>$null | ConvertFrom-Json
    }

    if (-not $keyVault) {
        throw "Key Vault '$KeyVaultName' not found. Please verify the name and ensure you have access."
    }

    $keyVaultId = $keyVault.id
    $keyVaultResourceGroup = $keyVault.resourceGroup
    Write-Host "  Key Vault ID: $keyVaultId" -ForegroundColor Gray
    Write-Host "  Resource Group: $keyVaultResourceGroup" -ForegroundColor Gray

    # Get service principal
    Write-Host "✓ Retrieving service principal information..." -ForegroundColor Gray
    $servicePrincipal = az ad sp list --display-name $ServicePrincipalName --output json 2>$null | ConvertFrom-Json
    
    if (-not $servicePrincipal -or $servicePrincipal.Count -eq 0) {
        throw "Service principal '$ServicePrincipalName' not found. Please verify the name."
    }

    # Handle multiple service principals with the same display name
    if ($servicePrincipal.Count -gt 1) {
        Write-Warning "Multiple service principals found with name '$ServicePrincipalName'. Using the first one."
        $servicePrincipal = $servicePrincipal[0]
    }

    $servicePrincipalId = $servicePrincipal.id
    $servicePrincipalAppId = $servicePrincipal.appId
    Write-Host "  Service Principal Object ID: $servicePrincipalId" -ForegroundColor Gray
    Write-Host "  Service Principal App ID: $servicePrincipalAppId" -ForegroundColor Gray

    # Check if role assignment already exists
    Write-Host "✓ Checking for existing role assignment..." -ForegroundColor Gray
    $existingAssignments = az role assignment list `
        --assignee $servicePrincipalId `
        --scope $keyVaultId `
        --role $RoleName `
        --output json 2>$null | ConvertFrom-Json

    if ($existingAssignments -and $existingAssignments.Count -gt 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "✓ Role assignment already exists!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "The service principal '$ServicePrincipalName' already has" -ForegroundColor Green
        Write-Host "the '$RoleName' role on Key Vault '$KeyVaultName'." -ForegroundColor Green
        Write-Host ""
        Write-Host "No action needed." -ForegroundColor Green
        Write-Host ""
        exit 0
    }

    # Create role assignment
    Write-Host ""
    Write-Host "→ Creating role assignment..." -ForegroundColor Yellow
    $roleAssignment = az role assignment create `
        --assignee $servicePrincipalId `
        --role $RoleName `
        --scope $keyVaultId `
        --output json 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create role assignment. Error: $roleAssignment"
    }

    $roleAssignmentObj = $roleAssignment | ConvertFrom-Json

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "✓ Role assignment created successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Details:" -ForegroundColor Green
    Write-Host "  Service Principal: $ServicePrincipalName" -ForegroundColor White
    Write-Host "  Role:             $RoleName" -ForegroundColor White
    Write-Host "  Key Vault:        $KeyVaultName" -ForegroundColor White
    Write-Host "  Scope:            $keyVaultId" -ForegroundColor White
    Write-Host ""
    Write-Host "The service principal can now manage secrets in the Key Vault." -ForegroundColor Green
    Write-Host ""

    exit 0
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "✗ Error occurred" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Message: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure that:" -ForegroundColor Yellow
    Write-Host "  1. You are logged in to Azure CLI (az login)" -ForegroundColor Yellow
    Write-Host "  2. You have sufficient permissions to assign roles" -ForegroundColor Yellow
    Write-Host "  3. The Key Vault '$KeyVaultName' exists" -ForegroundColor Yellow
    Write-Host "  4. The service principal '$ServicePrincipalName' exists" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
