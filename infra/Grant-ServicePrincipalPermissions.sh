#!/bin/bash
###############################################################################
# Grant-ServicePrincipalPermissions.sh
#
# Grants the User Access Administrator role to a service principal for managing
# role assignments. This permission is required for the service principal to
# create role assignments (e.g., assigning Key Vault roles to Function Apps).
#
# Usage:
#   ./Grant-ServicePrincipalPermissions.sh [-s ServicePrincipalName] [-S Scope] [-r ResourceGroupName] [-u SubscriptionId]
#
# Options:
#   -s    Service Principal name (default: github-actions-inkstainedwretches)
#   -S    Scope: "subscription" or "resourcegroup" (default: subscription)
#   -r    Resource Group name (required if Scope is resourcegroup)
#   -u    Subscription ID (optional - uses current subscription if not specified)
#   -h    Show help
#
# Examples:
#   ./Grant-ServicePrincipalPermissions.sh
#   ./Grant-ServicePrincipalPermissions.sh -S resourcegroup -r MyResourceGroup
#   ./Grant-ServicePrincipalPermissions.sh -s my-sp -u xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
#
# Author: OnePageAuthor Team
# Requires: Azure CLI installed and user authenticated
###############################################################################

set -e

# Default values
SERVICE_PRINCIPAL_NAME="github-actions-inkstainedwretches"
ROLE_NAME="User Access Administrator"
SCOPE="subscription"
RESOURCE_GROUP_NAME=""
SUBSCRIPTION_ID=""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Function to display usage
show_usage() {
    echo -e "${CYAN}Usage:${NC}"
    echo "  $0 [-s ServicePrincipalName] [-S Scope] [-r ResourceGroupName] [-u SubscriptionId]"
    echo ""
    echo -e "${CYAN}Options:${NC}"
    echo "  -s    Service Principal name (default: github-actions-inkstainedwretches)"
    echo "  -S    Scope: 'subscription' or 'resourcegroup' (default: subscription)"
    echo "  -r    Resource Group name (required if Scope is resourcegroup)"
    echo "  -u    Subscription ID (optional - uses current subscription if not specified)"
    echo "  -h    Show this help message"
    echo ""
    echo -e "${CYAN}Examples:${NC}"
    echo "  $0"
    echo "  $0 -S resourcegroup -r MyResourceGroup"
    echo "  $0 -s my-sp -u xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    exit 0
}

# Parse command line arguments
while getopts "s:S:r:u:h" opt; do
    case $opt in
        s) SERVICE_PRINCIPAL_NAME="$OPTARG" ;;
        S) SCOPE="$OPTARG" ;;
        r) RESOURCE_GROUP_NAME="$OPTARG" ;;
        u) SUBSCRIPTION_ID="$OPTARG" ;;
        h) show_usage ;;
        \?) 
            echo -e "${RED}Invalid option: -$OPTARG${NC}" >&2
            show_usage
            ;;
    esac
done

# Validate scope parameter
if [ "$SCOPE" != "subscription" ] && [ "$SCOPE" != "resourcegroup" ]; then
    echo -e "${RED}Error: Scope must be 'subscription' or 'resourcegroup'${NC}" >&2
    echo ""
    show_usage
fi

# Validate resource group is provided if scope is resourcegroup
if [ "$SCOPE" = "resourcegroup" ] && [ -z "$RESOURCE_GROUP_NAME" ]; then
    echo -e "${RED}Error: Resource group name is required when Scope is 'resourcegroup'${NC}" >&2
    echo ""
    show_usage
fi

# Function to print header
print_header() {
    echo -e "${CYAN}========================================${NC}"
    echo -e "${CYAN}Service Principal Permissions Script${NC}"
    echo -e "${CYAN}========================================${NC}"
    echo ""
    echo -e "${YELLOW}Service Principal: $SERVICE_PRINCIPAL_NAME${NC}"
    echo -e "${YELLOW}Role Name:        $ROLE_NAME${NC}"
    echo -e "${YELLOW}Scope:            $SCOPE${NC}"
    if [ "$SCOPE" = "resourcegroup" ] && [ -n "$RESOURCE_GROUP_NAME" ]; then
        echo -e "${YELLOW}Resource Group:   $RESOURCE_GROUP_NAME${NC}"
    fi
    echo ""
}

# Function to check Azure CLI
check_azure_cli() {
    echo -e "${GRAY}✓ Checking Azure CLI installation...${NC}"
    if ! command -v az &> /dev/null; then
        echo -e "${RED}✗ Azure CLI is not installed or not in PATH.${NC}" >&2
        echo -e "${YELLOW}  Please install Azure CLI: https://docs.microsoft.com/cli/azure/install-azure-cli${NC}" >&2
        exit 1
    fi
    
    local az_version=$(az version --query '"azure-cli"' -o tsv 2>/dev/null)
    echo -e "${GRAY}  Azure CLI version: $az_version${NC}"
}

# Function to check Azure authentication
check_azure_auth() {
    echo -e "${GRAY}✓ Checking Azure authentication...${NC}"
    local account=$(az account show --output json 2>/dev/null)
    if [ $? -ne 0 ] || [ -z "$account" ]; then
        echo -e "${RED}✗ Not logged in to Azure.${NC}" >&2
        echo -e "${YELLOW}  Please run 'az login' first.${NC}" >&2
        exit 1
    fi
    
    # Set subscription if provided
    if [ -n "$SUBSCRIPTION_ID" ]; then
        echo -e "${GRAY}  Switching to subscription: $SUBSCRIPTION_ID${NC}"
        if ! az account set --subscription "$SUBSCRIPTION_ID" 2>/dev/null; then
            echo -e "${RED}✗ Failed to set subscription to '$SUBSCRIPTION_ID'.${NC}" >&2
            echo -e "${YELLOW}  Please verify the subscription ID.${NC}" >&2
            exit 1
        fi
        account=$(az account show --output json 2>/dev/null)
    fi
    
    CURRENT_SUBSCRIPTION_ID=$(echo "$account" | jq -r '.id')
    CURRENT_SUBSCRIPTION_NAME=$(echo "$account" | jq -r '.name')
    echo -e "${GRAY}  Subscription: $CURRENT_SUBSCRIPTION_NAME ($CURRENT_SUBSCRIPTION_ID)${NC}"
}

# Function to get service principal information
get_service_principal_info() {
    echo -e "${GRAY}✓ Retrieving service principal information...${NC}"
    
    local sp_json=$(az ad sp list --display-name "$SERVICE_PRINCIPAL_NAME" --output json 2>/dev/null)
    if [ $? -ne 0 ] || [ -z "$sp_json" ] || [ "$sp_json" = "[]" ]; then
        echo -e "${RED}✗ Service principal '$SERVICE_PRINCIPAL_NAME' not found.${NC}" >&2
        echo -e "${YELLOW}  Please verify the name.${NC}" >&2
        exit 1
    fi
    
    # Count number of service principals found
    local sp_count=$(echo "$sp_json" | jq '. | length')
    if [ "$sp_count" -gt 1 ]; then
        echo -e "${YELLOW}⚠ Warning: Multiple service principals found with name '$SERVICE_PRINCIPAL_NAME'.${NC}"
        echo -e "${YELLOW}  Using the first one.${NC}"
    fi
    
    SERVICE_PRINCIPAL_ID=$(echo "$sp_json" | jq -r '.[0].id')
    SERVICE_PRINCIPAL_APP_ID=$(echo "$sp_json" | jq -r '.[0].appId')
    
    echo -e "${GRAY}  Service Principal Object ID: $SERVICE_PRINCIPAL_ID${NC}"
    echo -e "${GRAY}  Service Principal App ID: $SERVICE_PRINCIPAL_APP_ID${NC}"
}

# Function to build scope path
build_scope_path() {
    if [ "$SCOPE" = "subscription" ]; then
        SCOPE_PATH="/subscriptions/$CURRENT_SUBSCRIPTION_ID"
    else
        # Verify resource group exists
        echo -e "${GRAY}✓ Verifying resource group...${NC}"
        local rg_json=$(az group show --name "$RESOURCE_GROUP_NAME" --output json 2>/dev/null)
        if [ $? -ne 0 ] || [ -z "$rg_json" ]; then
            echo -e "${RED}✗ Resource group '$RESOURCE_GROUP_NAME' not found.${NC}" >&2
            exit 1
        fi
        SCOPE_PATH="/subscriptions/$CURRENT_SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP_NAME"
    fi
    
    echo -e "${GRAY}  Scope path: $SCOPE_PATH${NC}"
}

# Function to check existing role assignment
check_existing_assignment() {
    echo -e "${GRAY}✓ Checking for existing role assignment...${NC}"
    
    local assignments=$(az role assignment list \
        --assignee "$SERVICE_PRINCIPAL_ID" \
        --scope "$SCOPE_PATH" \
        --role "$ROLE_NAME" \
        --output json 2>/dev/null)
    
    if [ $? -eq 0 ] && [ -n "$assignments" ] && [ "$assignments" != "[]" ]; then
        echo ""
        echo -e "${GREEN}========================================${NC}"
        echo -e "${GREEN}✓ Role assignment already exists!${NC}"
        echo -e "${GREEN}========================================${NC}"
        echo ""
        echo -e "${GREEN}The service principal '$SERVICE_PRINCIPAL_NAME' already has${NC}"
        echo -e "${GREEN}the '$ROLE_NAME' role at the specified scope.${NC}"
        echo ""
        echo -e "${GREEN}No action needed.${NC}"
        echo ""
        exit 0
    fi
}

# Function to create role assignment
create_role_assignment() {
    echo ""
    echo -e "${YELLOW}→ Creating role assignment...${NC}"
    echo -e "${YELLOW}  This will allow the service principal to create role assignments${NC}"
    echo -e "${YELLOW}  (e.g., assigning Key Vault roles to Function Apps)${NC}"
    echo ""
    
    local assignment_result=$(az role assignment create \
        --assignee "$SERVICE_PRINCIPAL_ID" \
        --role "$ROLE_NAME" \
        --scope "$SCOPE_PATH" \
        --output json 2>&1)
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}✗ Failed to create role assignment.${NC}" >&2
        echo -e "${RED}Error: $assignment_result${NC}" >&2
        echo ""
        echo -e "${YELLOW}Please ensure you have Owner or User Access Administrator permissions.${NC}" >&2
        exit 1
    fi
    
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✓ Role assignment created successfully!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo -e "${GREEN}Details:${NC}"
    echo -e "${WHITE}  Service Principal: $SERVICE_PRINCIPAL_NAME${NC}"
    echo -e "${WHITE}  Role:             $ROLE_NAME${NC}"
    echo -e "${WHITE}  Scope:            $SCOPE_PATH${NC}"
    echo ""
    echo -e "${GREEN}The service principal can now create role assignments at this scope.${NC}"
    echo -e "${GREEN}This includes assigning Key Vault roles to Function Apps during deployment.${NC}"
    echo ""
}

# Main execution
main() {
    print_header
    
    # Perform checks and operations
    check_azure_cli
    check_azure_auth
    get_service_principal_info
    build_scope_path
    check_existing_assignment
    create_role_assignment
}

# Function to handle errors
handle_error() {
    echo ""
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}✗ Error occurred${NC}"
    echo -e "${RED}========================================${NC}"
    echo ""
    echo -e "${YELLOW}Please ensure that:${NC}"
    echo -e "${YELLOW}  1. You are logged in to Azure CLI (az login)${NC}"
    echo -e "${YELLOW}  2. You have Owner or User Access Administrator permissions${NC}"
    echo -e "${YELLOW}  3. The service principal '$SERVICE_PRINCIPAL_NAME' exists${NC}"
    if [ "$SCOPE" = "resourcegroup" ]; then
        echo -e "${YELLOW}  4. The resource group '$RESOURCE_GROUP_NAME' exists${NC}"
    fi
    echo ""
    echo -e "${YELLOW}Common issues:${NC}"
    echo -e "${YELLOW}  - Insufficient permissions: You need Owner or User Access Administrator role${NC}"
    echo -e "${YELLOW}  - Wrong scope: Ensure you're targeting the correct subscription/resource group${NC}"
    echo ""
    exit 1
}

# Trap errors
trap 'handle_error' ERR

# Run main function
main
