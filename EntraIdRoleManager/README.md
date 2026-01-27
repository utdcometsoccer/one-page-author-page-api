# Entra ID Role Manager for Personal Microsoft Account Apps

This console application manages user authorization for Azure AD applications configured for **Personal Microsoft Account** users. Since Personal Microsoft Account apps cannot create app roles or use Service Principal role assignments, this tool configures a **pure Cosmos DB authorization system**.

## Purpose

This tool verifies and configures a **Cosmos DB-only authorization system** for Personal Microsoft Account applications:

1. Verifies the target application is configured for Personal Microsoft Account users
2. Confirms existing ImageStorageTierMembership records in Cosmos DB
3. Provides statistics on current user tier distribution
4. Documents the authorization strategy for the ImageAPI

## Architecture

### Personal Microsoft Account Limitations

Personal Microsoft Account applications have significant limitations compared to enterprise applications:

- **No App Roles**: Cannot create or define app roles in the application
- **No Service Principals**: Cannot create Service Principals for the application
- **No JWT Role Claims**: JWT tokens will not contain `roles` claims
- **No Role Assignments**: Cannot assign users to roles programmatically

### Pure Cosmos DB Authorization

Given these limitations, the authorization system uses:

1. **User Identity**: Extract user ID from JWT token claims (`oid` or `sub`)
2. **Direct Lookup**: Query ImageStorageTierMembership records in Cosmos DB by UserProfileId
3. **Auto-Assignment**: Create default "Starter" tier membership for new users who don't have existing records

### Two-Application Setup

1. **Management Application**: This console app with permissions to read app configuration and Cosmos DB
2. **Target Application**: The ImageAPI app configured for Personal Microsoft Account users (`signInAudience: "PersonalMicrosoftAccount"`)

## Prerequisites

### 1. Management Application Registration

Create an App Registration in Azure AD for this management tool:

- **Name**: `OnePageAuthor-RoleManager` (or similar)
- **Account types**: `Accounts in this organizational directory only (Single tenant)`
- **API Permissions**:

  - `Microsoft Graph` → `Application.ReadWrite.All` (Application permission)
  - `Microsoft Graph` → `AppRoleAssignment.ReadWrite.All` (Application permission)
  - **Grant admin consent** for these permissions

- **Certificates & secrets**: Create a client secret

### 2. Target Application Registration

Your existing ImageAPI app registration should be configured for Microsoft Account users:

- **Account types**: `Personal Microsoft accounts only`
- **Authentication**: Configure redirect URIs as needed
- This app will receive the roles created by the management tool

### 3. Additional Requirements

- Cosmos DB access to read ImageStorageTiers and ImageStorageTierMemberships
- Both applications must be in the same Azure AD tenant

## ⚙️ Configuration

### Required Settings

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB account endpoint | Azure Portal → Cosmos DB → Keys → URI | Read ImageStorageTiers and existing memberships |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary access key | Azure Portal → Cosmos DB → Keys → Primary Key | Authenticate database operations |
| `COSMOSDB_DATABASE_ID` | Database name | Your database name (e.g., "OnePageAuthorDb") | Identify target database |
| `AAD_TENANT_ID` | Azure AD tenant ID | Azure Portal → Microsoft Entra ID → Overview → Tenant ID | Connect to correct Azure AD tenant |
| `AAD_MANAGEMENT_CLIENT_ID` | Management app client ID | Azure Portal → App registrations → [Management App] → Application ID | Authenticate this console app |
| `AAD_MANAGEMENT_CLIENT_SECRET` | Management app client secret | Azure Portal → App registrations → [Management App] → Certificates & secrets | Authenticate this console app |
| `AAD_TARGET_CLIENT_ID` | Target ImageAPI app client ID | Azure Portal → App registrations → [ImageAPI App] → Application ID | Create roles in the target application |

### Why These Settings Are Needed

<details>
<summary>🗄️ Cosmos DB Configuration</summary>

**Purpose**: Read ImageStorageTier definitions and existing user memberships to configure app roles.

| Variable | Why It's Needed |
|----------|-----------------|
| `COSMOSDB_ENDPOINT_URI` | Connect to database to read tier configurations (Starter, Pro, Elite) |
| `COSMOSDB_PRIMARY_KEY` | Authenticate read operations against the database |
| `COSMOSDB_DATABASE_ID` | Identify which database contains the ImageStorageTiers container |

</details>

<details>
<summary>🔐 Azure AD Configuration</summary>

**Purpose**: Connect to Microsoft Graph API to manage app roles and read application configurations.

| Variable | Why It's Needed |
|----------|-----------------|
| `AAD_TENANT_ID` | Identifies your Azure AD tenant for Graph API operations |
| `AAD_MANAGEMENT_CLIENT_ID` | The Application ID of this management tool's app registration |
| `AAD_MANAGEMENT_CLIENT_SECRET` | Client secret for authenticating as the management app |
| `AAD_TARGET_CLIENT_ID` | The ImageAPI app where roles will be created |

</details>

### Setting Up User Secrets (Recommended)

Navigate to the EntraIdRoleManager directory and configure user secrets:

```bash
cd EntraIdRoleManager
dotnet user-secrets init

# Cosmos DB Configuration
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-cosmos-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthorDb"

# Azure AD Configuration
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"

# Management App (this console app)
dotnet user-secrets set "AAD_MANAGEMENT_CLIENT_ID" "your-management-app-client-id"
dotnet user-secrets set "AAD_MANAGEMENT_CLIENT_SECRET" "your-management-app-client-secret"

# Target App (ImageAPI app configured for Microsoft Account users)
dotnet user-secrets set "AAD_TARGET_CLIENT_ID" "your-imageapi-app-client-id"

# Verify configuration
dotnet user-secrets list
```

### How to Obtain Configuration Values

1. **Cosmos DB Values**:
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to your Cosmos DB account
   - Click "Keys" in the left sidebar
   - Copy URI and Primary Key

2. **Tenant ID**:
   - Azure Portal → Microsoft Entra ID (Azure Active Directory)
   - Overview page → Tenant ID

3. **Management App Client ID/Secret**:
   - Azure Portal → Microsoft Entra ID → App registrations
   - Select your management app (e.g., "OnePageAuthor-RoleManager")
   - **Client ID**: Application (client) ID on Overview page
   - **Client Secret**: Certificates & secrets → New client secret → Copy value immediately

4. **Target App Client ID**:
   - Azure Portal → Microsoft Entra ID → App registrations
   - Select your ImageAPI app
   - Copy Application (client) ID from Overview page

### Environment Variables Alternative

You can also set these as environment variables:

```bash
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-cosmos-key"
export COSMOSDB_DATABASE_ID="OnePageAuthorDb"
export AAD_TENANT_ID="your-tenant-id"
export AAD_MANAGEMENT_CLIENT_ID="management-app-client-id"
export AAD_MANAGEMENT_CLIENT_SECRET="management-app-client-secret"
export AAD_TARGET_CLIENT_ID="target-app-client-id"
```

## Usage

```bash
# From the repository root
dotnet run --project EntraIdRoleManager

# Or from the EntraIdRoleManager directory
cd EntraIdRoleManager
dotnet run

```

## What It Does

1. **Connects to Target App**: Uses management app credentials to connect to Microsoft Graph
2. **Reads ImageStorageTiers**: Fetches all tier definitions from Cosmos DB (Starter, Pro, Elite, etc.)
3. **Creates App Roles**: For each tier, creates an app role like `ImageStorageTier.Starter` in the **target application**
4. **Preserves Memberships**: Keeps existing ImageStorageTierMembership records in Cosmos DB for authorization
5. **Sets Up Hybrid System**: Configures the ImageAPI to use both JWT roles and Cosmos DB fallback
6. **Microsoft Account Support**: Works specifically with Personal Microsoft Account applications
7. **Idempotent**: Safe to run multiple times - skips roles that already exist

**Note**: This tool **does NOT assign users to roles** because Personal Microsoft Account apps cannot use Service Principal role assignments. Instead, it relies on the existing Cosmos DB membership system.

## Verification

After running the tool, verify the setup:

1. **Check App Roles**: Azure Portal → App registrations → [Target App] → App roles
2. **Check Role Assignments**: Azure Portal → Enterprise applications → [Target App] → Users and groups
3. **Test JWT Token**: Authenticate with the target app and verify `roles` claim contains values like `ImageStorageTier.Starter`

## App Role Format

Roles are created with the following format:

- **Display Name**: `ImageStorageTier.{TierName}` (e.g., `ImageStorageTier.Starter`)
- **Value**: `ImageStorageTier.{TierName}` (used in JWT claims)
- **Description**: Includes tier details (cost, storage, bandwidth)

## After Running

After running this tool:

1. **Target Application**: Has app roles defined for each storage tier
2. **User Assignments**: Users are assigned to appropriate roles based on their Cosmos DB memberships
3. **JWT Tokens**: When users authenticate with the target app, JWT tokens include `roles` claims like `ImageStorageTier.Starter`
4. **ImageAPI Integration**: The ImageAPI can read these roles directly from JWT claims
5. **Cosmos DB**: ImageStorageTierMembership records are preserved but no longer used at runtime

## Microsoft Account Integration

This setup enables:

- **Personal Microsoft Account Users**: Can authenticate and receive appropriate storage tier roles
- **Role-Based Authorization**: ImageAPI functions can authorize based on JWT roles instead of Cosmos DB lookups
- **Simplified Architecture**: Removes dependency on ImageStorageTierMembership for runtime authorization

## Troubleshooting

### Common Issues

1. **"Service principal not found"**:

   - The tool will automatically create the Service Principal if it doesn't exist
   - Verify the target app Client ID is correct
   - Ensure the management app has sufficient permissions to create Service Principals

2. **"Insufficient privileges"**:

   - Verify management app has `Application.ReadWrite.All` and `AppRoleAssignment.ReadWrite.All` permissions
   - Ensure admin consent has been granted

3. **"Application not found"**:

   - Check that both apps are in the same Azure AD tenant
   - Verify Client IDs are correct

4. **User assignment failures**:

   - Ensure user IDs in Cosmos DB match Azure AD user object IDs
   - For Microsoft Account users, verify they exist in the tenant

## Notes

- The tool waits 10 seconds after creating roles for Azure AD propagation
- Duplicate assignment attempts are safely ignored
- All operations are logged for auditing
- The tool will fail fast with descriptive errors if configuration is missing
- Supports both organizational and Microsoft Account users through proper target app configuration
