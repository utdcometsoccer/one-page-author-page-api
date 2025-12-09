#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Grants the User Access Administrator role to a service principal for managing role assignments.

.DESCRIPTION
    This script assigns the User Access Administrator role to the specified service principal at
    either the subscription or resource group scope. This permission is required for the service
    principal to create role assignments (e.g., assigning Key Vault roles to Function Apps).

.PARAMETER ServicePrincipalName
    The name of the service principal to grant permissions to.
    Default: "github-actions-inkstainedwretches"

.PARAMETER Scope
    The scope for the role assignment: "subscription" or "resourcegroup".
    Default: "subscription"

.PARAMETER ResourceGroupName
    The name of the resource group (required if Scope is "resourcegroup").
    Optional parameter.

.PARAMETER SubscriptionId
    The subscription ID (optional - uses current subscription if not specified).
    Optional parameter.

.EXAMPLE
    ./Grant-ServicePrincipalPermissions.ps1

.EXAMPLE
    ./Grant-ServicePrincipalPermissions.ps1 -Scope "resourcegroup" -ResourceGroupName "MyResourceGroup"

.EXAMPLE
    ./Grant-ServicePrincipalPermissions.ps1 -ServicePrincipalName "my-sp" -SubscriptionId "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"

.NOTES
    Author: OnePageAuthor Team
    Requires: Azure CLI installed and user authenticated with Owner or User Access Administrator permissions
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false, HelpMessage = "Name of the service principal")]
    [ValidateNotNullOrEmpty()]
    [string]$ServicePrincipalName = "github-actions-inkstainedwretches",

    [Parameter(Mandatory = $false, HelpMessage = "Scope for role assignment: subscription or resourcegroup")]
    [ValidateSet("subscription", "resourcegroup")]
    [string]$Scope = "subscription",

    [Parameter(Mandatory = $false, HelpMessage = "Resource group name (required if Scope is resourcegroup)")]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false, HelpMessage = "Subscription ID (optional)")]
    [string]$SubscriptionId
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Role name constant
$RoleName = "User Access Administrator"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Service Principal Permissions Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Principal: $ServicePrincipalName" -ForegroundColor Yellow
Write-Host "Role Name:        $RoleName" -ForegroundColor Yellow
Write-Host "Scope:            $Scope" -ForegroundColor Yellow
if ($Scope -eq "resourcegroup" -and $ResourceGroupName) {
    Write-Host "Resource Group:   $ResourceGroupName" -ForegroundColor Yellow
}
Write-Host ""

try {
    # Validate parameters
    if ($Scope -eq "resourcegroup" -and [string]::IsNullOrWhiteSpace($ResourceGroupName)) {
        throw "ResourceGroupName is required when Scope is 'resourcegroup'"
    }

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

    # Use provided subscription ID or current subscription
    if (-not [string]::IsNullOrWhiteSpace($SubscriptionId)) {
        Write-Host "  Switching to subscription: $SubscriptionId" -ForegroundColor Gray
        az account set --subscription $SubscriptionId 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to set subscription to '$SubscriptionId'. Please verify the subscription ID."
        }
        $account = az account show --output json 2>$null | ConvertFrom-Json
    }

    $currentSubscriptionId = $account.id
    $currentSubscriptionName = $account.name
    Write-Host "  Subscription: $currentSubscriptionName ($currentSubscriptionId)" -ForegroundColor Gray

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

    # Build scope path
    $scopePath = ""
    if ($Scope -eq "subscription") {
        $scopePath = "/subscriptions/$currentSubscriptionId"
    }
    else {
        # Verify resource group exists
        Write-Host "✓ Verifying resource group..." -ForegroundColor Gray
        $resourceGroup = az group show --name $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        if (-not $resourceGroup) {
            throw "Resource group '$ResourceGroupName' not found."
        }
        $scopePath = "/subscriptions/$currentSubscriptionId/resourceGroups/$ResourceGroupName"
    }

    Write-Host "  Scope path: $scopePath" -ForegroundColor Gray

    # Check if role assignment already exists
    Write-Host "✓ Checking for existing role assignment..." -ForegroundColor Gray
    $existingAssignments = az role assignment list `
        --assignee $servicePrincipalId `
        --scope $scopePath `
        --role $RoleName `
        --output json 2>$null | ConvertFrom-Json

    if ($existingAssignments -and $existingAssignments.Count -gt 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "✓ Role assignment already exists!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "The service principal '$ServicePrincipalName' already has" -ForegroundColor Green
        Write-Host "the '$RoleName' role at the specified scope." -ForegroundColor Green
        Write-Host ""
        Write-Host "No action needed." -ForegroundColor Green
        Write-Host ""
        exit 0
    }

    # Create role assignment
    Write-Host ""
    Write-Host "→ Creating role assignment..." -ForegroundColor Yellow
    Write-Host "  This will allow the service principal to create role assignments" -ForegroundColor Yellow
    Write-Host "  (e.g., assigning Key Vault roles to Function Apps)" -ForegroundColor Yellow
    Write-Host ""
    
    $roleAssignment = az role assignment create `
        --assignee $servicePrincipalId `
        --role $RoleName `
        --scope $scopePath `
        --output json 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create role assignment. Error: $roleAssignment`n`nPlease ensure you have Owner or User Access Administrator permissions."
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
    Write-Host "  Scope:            $scopePath" -ForegroundColor White
    Write-Host ""
    Write-Host "The service principal can now create role assignments at this scope." -ForegroundColor Green
    Write-Host "This includes assigning Key Vault roles to Function Apps during deployment." -ForegroundColor Green
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
    Write-Host "  2. You have Owner or User Access Administrator permissions" -ForegroundColor Yellow
    Write-Host "  3. The service principal '$ServicePrincipalName' exists" -ForegroundColor Yellow
    if ($Scope -eq "resourcegroup") {
        Write-Host "  4. The resource group '$ResourceGroupName' exists" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "  - Insufficient permissions: You need Owner or User Access Administrator role" -ForegroundColor Yellow
    Write-Host "  - Wrong scope: Ensure you're targeting the correct subscription/resource group" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
