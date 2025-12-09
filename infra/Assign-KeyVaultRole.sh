#!/bin/bash
###############################################################################
# Assign-KeyVaultRole.sh
#
# Assigns the Key Vault Secrets Officer role to a service principal if not
# already assigned.
#
# Usage:
#   ./Assign-KeyVaultRole.sh -k <KeyVaultName> [-r ResourceGroupName] [-s ServicePrincipalName] [-R RoleName]
#
# Options:
#   -k    Key Vault name (required)
#   -r    Resource Group name (optional)
#   -s    Service Principal name (default: github-actions-inkstainedwretches)
#   -R    Role name (default: Key Vault Secrets Officer)
#   -h    Show help
#
# Examples:
#   ./Assign-KeyVaultRole.sh -k mykeyvault
#   ./Assign-KeyVaultRole.sh -k mykeyvault -r MyResourceGroup
#   ./Assign-KeyVaultRole.sh -k mykeyvault -s my-sp -R "Key Vault Administrator"
#
# Author: OnePageAuthor Team
# Requires: Azure CLI installed and user authenticated
###############################################################################

set -e

# Default values (using variables to avoid hardcoding)
SERVICE_PRINCIPAL_NAME="github-actions-inkstainedwretches"
ROLE_NAME="Key Vault Secrets Officer"
KEY_VAULT_NAME=""
RESOURCE_GROUP_NAME=""

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
    echo "  $0 -k <KeyVaultName> [-r ResourceGroupName] [-s ServicePrincipalName] [-R RoleName]"
    echo ""
    echo -e "${CYAN}Options:${NC}"
    echo "  -k    Key Vault name (required)"
    echo "  -r    Resource Group name (optional)"
    echo "  -s    Service Principal name (default: github-actions-inkstainedwretches)"
    echo "  -R    Role name (default: Key Vault Secrets Officer)"
    echo "  -h    Show this help message"
    echo ""
    echo -e "${CYAN}Examples:${NC}"
    echo "  $0 -k mykeyvault"
    echo "  $0 -k mykeyvault -r MyResourceGroup"
    echo "  $0 -k mykeyvault -s my-sp -R \"Key Vault Administrator\""
    exit 0
}

# Parse command line arguments
while getopts "k:r:s:R:h" opt; do
    case $opt in
        k) KEY_VAULT_NAME="$OPTARG" ;;
        r) RESOURCE_GROUP_NAME="$OPTARG" ;;
        s) SERVICE_PRINCIPAL_NAME="$OPTARG" ;;
        R) ROLE_NAME="$OPTARG" ;;
        h) show_usage ;;
        \?) 
            echo -e "${RED}Invalid option: -$OPTARG${NC}" >&2
            show_usage
            ;;
    esac
done

# Validate required parameters
if [ -z "$KEY_VAULT_NAME" ]; then
    echo -e "${RED}Error: Key Vault name is required${NC}" >&2
    echo ""
    show_usage
fi

# Function to print header
print_header() {
    echo -e "${CYAN}========================================${NC}"
    echo -e "${CYAN}Key Vault Role Assignment Script${NC}"
    echo -e "${CYAN}========================================${NC}"
    echo ""
    echo -e "${YELLOW}Service Principal: $SERVICE_PRINCIPAL_NAME${NC}"
    echo -e "${YELLOW}Role Name:        $ROLE_NAME${NC}"
    echo -e "${YELLOW}Key Vault Name:   $KEY_VAULT_NAME${NC}"
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
    
    local subscription_name=$(echo "$account" | jq -r '.name')
    local subscription_id=$(echo "$account" | jq -r '.id')
    echo -e "${GRAY}  Subscription: $subscription_name ($subscription_id)${NC}"
}

# Function to get Key Vault information
get_keyvault_info() {
    echo -e "${GRAY}✓ Retrieving Key Vault information...${NC}"
    
    local kv_json
    if [ -n "$RESOURCE_GROUP_NAME" ]; then
        kv_json=$(az keyvault show --name "$KEY_VAULT_NAME" --resource-group "$RESOURCE_GROUP_NAME" --output json 2>/dev/null)
    else
        kv_json=$(az keyvault show --name "$KEY_VAULT_NAME" --output json 2>/dev/null)
    fi
    
    if [ $? -ne 0 ] || [ -z "$kv_json" ]; then
        echo -e "${RED}✗ Key Vault '$KEY_VAULT_NAME' not found.${NC}" >&2
        echo -e "${YELLOW}  Please verify the name and ensure you have access.${NC}" >&2
        exit 1
    fi
    
    KEY_VAULT_ID=$(echo "$kv_json" | jq -r '.id')
    KEY_VAULT_RESOURCE_GROUP=$(echo "$kv_json" | jq -r '.resourceGroup')
    
    echo -e "${GRAY}  Key Vault ID: $KEY_VAULT_ID${NC}"
    echo -e "${GRAY}  Resource Group: $KEY_VAULT_RESOURCE_GROUP${NC}"
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

# Function to check existing role assignment
check_existing_assignment() {
    echo -e "${GRAY}✓ Checking for existing role assignment...${NC}"
    
    local assignments=$(az role assignment list \
        --assignee "$SERVICE_PRINCIPAL_ID" \
        --scope "$KEY_VAULT_ID" \
        --role "$ROLE_NAME" \
        --output json 2>/dev/null)
    
    if [ $? -eq 0 ] && [ -n "$assignments" ] && [ "$assignments" != "[]" ]; then
        echo ""
        echo -e "${GREEN}========================================${NC}"
        echo -e "${GREEN}✓ Role assignment already exists!${NC}"
        echo -e "${GREEN}========================================${NC}"
        echo ""
        echo -e "${GREEN}The service principal '$SERVICE_PRINCIPAL_NAME' already has${NC}"
        echo -e "${GREEN}the '$ROLE_NAME' role on Key Vault '$KEY_VAULT_NAME'.${NC}"
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
    
    local assignment_result=$(az role assignment create \
        --assignee "$SERVICE_PRINCIPAL_ID" \
        --role "$ROLE_NAME" \
        --scope "$KEY_VAULT_ID" \
        --output json 2>&1)
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}✗ Failed to create role assignment.${NC}" >&2
        echo -e "${RED}Error: $assignment_result${NC}" >&2
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
    echo -e "${WHITE}  Key Vault:        $KEY_VAULT_NAME${NC}"
    echo -e "${WHITE}  Scope:            $KEY_VAULT_ID${NC}"
    echo ""
    echo -e "${GREEN}The service principal can now manage secrets in the Key Vault.${NC}"
    echo ""
}

# Main execution
main() {
    print_header
    
    # Perform checks and operations
    check_azure_cli
    check_azure_auth
    get_keyvault_info
    get_service_principal_info
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
    echo -e "${YELLOW}  2. You have sufficient permissions to assign roles${NC}"
    echo -e "${YELLOW}  3. The Key Vault '$KEY_VAULT_NAME' exists${NC}"
    echo -e "${YELLOW}  4. The service principal '$SERVICE_PRINCIPAL_NAME' exists${NC}"
    echo ""
    exit 1
}

# Trap errors
trap 'handle_error' ERR

# Run main function
main
