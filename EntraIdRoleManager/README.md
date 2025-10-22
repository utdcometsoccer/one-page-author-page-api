# Entra ID Role Manager

This console application manages Entra ID App Roles for Image Storage Tiers.

## Purpose

This tool performs a one-time migration to move from Cosmos DB-based ImageStorageTierMembership to Microsoft Entra ID App Roles:

1. Creates an Entra ID App Role for each ImageStorageTier in Cosmos DB
2. Migrates existing users from ImageStorageTierMemberships to the appropriate Entra ID roles
3. Provides a foundation for role-based access control using JWT tokens

## Prerequisites

- Azure AD application with appropriate permissions:
  - `Application.ReadWrite.All` (to create/update app roles)
  - `AppRoleAssignment.ReadWrite.All` (to assign users to roles)
- Service Principal credentials (Client ID, Client Secret, Tenant ID)
- Cosmos DB access to read ImageStorageTiers and ImageStorageTierMemberships

## Configuration

Set the following environment variables or user secrets:

```
COSMOSDB_ENDPOINT_URI=<your-cosmos-endpoint>
COSMOSDB_PRIMARY_KEY=<your-cosmos-key>
COSMOSDB_DATABASE_ID=<your-database-id>
AAD_TENANT_ID=<your-tenant-id>
AAD_CLIENT_ID=<your-client-id>
AAD_CLIENT_SECRET=<your-client-secret>
```

## Usage

```bash
dotnet run --project EntraIdRoleManager
```

## What It Does

1. **Reads ImageStorageTiers**: Fetches all tier definitions from Cosmos DB (Starter, Pro, Elite, etc.)
2. **Creates App Roles**: For each tier, creates an app role like `ImageStorageTier.Starter` in Azure AD
3. **Migrates Memberships**: Reads all ImageStorageTierMembership records and assigns users to corresponding roles
4. **Idempotent**: Safe to run multiple times - skips roles/assignments that already exist

## App Role Format

Roles are created with the following format:
- **Display Name**: `ImageStorageTier.{TierName}` (e.g., `ImageStorageTier.Starter`)
- **Value**: `ImageStorageTier.{TierName}` (used in JWT claims)
- **Description**: Includes tier details (cost, storage, bandwidth)

## After Running

After running this tool:
1. Users will have app role assignments in Azure AD
2. JWT tokens will include `roles` claims with values like `ImageStorageTier.Starter`
3. The ImageAPI can read these roles directly from JWT claims
4. ImageStorageTierMembership records are preserved but no longer used at runtime

## Notes

- The tool waits 10 seconds after creating roles for Azure AD propagation
- Duplicate assignment attempts are safely ignored
- All operations are logged for auditing
- The tool will fail fast with descriptive errors if configuration is missing
