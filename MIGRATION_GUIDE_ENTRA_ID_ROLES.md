# Migration Guide: From ImageStorageTierMembership to Entra ID Roles

This guide explains how to migrate from Cosmos DB-based ImageStorageTierMembership to Microsoft Entra ID App Roles for image storage tier management.

## Overview

The image storage tier system has been refactored to use Microsoft Entra ID App Roles instead of Cosmos DB memberships. This provides several benefits:
- **Centralized Identity Management**: Tier assignments are managed in Azure AD alongside user identities
- **JWT Token Integration**: Tier information is included directly in JWT tokens, eliminating database lookups
- **Better Security**: Role-based access control (RBAC) through standard Azure AD mechanisms
- **Simplified Architecture**: Reduced database complexity by separating tier assignment from usage tracking

## Architecture Changes

### Before
```
User Authentication → Cosmos DB Lookup (ImageStorageTierMembership) → Tier Determination
                                    ↓
                            Usage Tracking (Same Entity)
```

### After
```
User Authentication → JWT Roles Claim → Tier Determination
                                    ↓
                    Cosmos DB Lookup (ImageStorageUsage) → Usage Tracking Only
```

## Migration Steps

### Step 1: Prerequisites

Ensure you have:
- Azure AD application with appropriate permissions:
  - `Application.ReadWrite.All` (to create/update app roles)
  - `AppRoleAssignment.ReadWrite.All` (to assign users to roles)
- Service Principal credentials (Client ID, Client Secret, Tenant ID)
- Cosmos DB access credentials
- Backup of existing ImageStorageTierMembership data

### Step 2: Run the EntraIdRoleManager

The EntraIdRoleManager console application performs the migration:

1. Configure environment variables or user secrets:
   ```bash
   dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "<your-endpoint>"
   dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "<your-key>"
   dotnet user-secrets set "COSMOSDB_DATABASE_ID" "<your-database>"
   dotnet user-secrets set "AAD_TENANT_ID" "<your-tenant-id>"
   dotnet user-secrets set "AAD_CLIENT_ID" "<your-client-id>"
   dotnet user-secrets set "AAD_CLIENT_SECRET" "<your-client-secret>"
   ```

2. Run the migration:
   ```bash
   cd EntraIdRoleManager
   dotnet run
   ```

3. The tool will:
   - Read all ImageStorageTiers from Cosmos DB
   - Create app roles in Azure AD (e.g., `ImageStorageTier.Starter`, `ImageStorageTier.Pro`)
   - Read all ImageStorageTierMemberships
   - Assign users to corresponding app roles
   - Skip existing assignments (idempotent)

### Step 3: Verify App Roles in Azure Portal

1. Navigate to Azure Portal → Azure Active Directory → App registrations
2. Find your application
3. Go to "App roles" section
4. Verify roles are created with format: `ImageStorageTier.{TierName}`
5. Check "Enterprise applications" → your app → "Users and groups" to verify assignments

### Step 4: Update Token Configuration (if needed)

Ensure your Azure AD app registration includes roles in the token:
1. Navigate to Token configuration
2. Add optional claim "roles" if not already present
3. Ensure "roles" claim is included in both ID and Access tokens

### Step 5: Deploy Updated ImageAPI

The ImageAPI has been updated to:
- Accept user ClaimsPrincipal to read roles from JWT
- Use ImageStorageTierService to determine tier from roles
- Automatically assign Starter (or lowest cost) tier to users without roles
- Track usage separately in ImageStorageUsage entity

Deploy the updated ImageAPI code to your environment.

### Step 6: Migrate Usage Data

If you need to migrate existing usage data from ImageStorageTierMembership to ImageStorageUsage:

Create a simple migration script:
```csharp
var memberships = await membershipRepository.GetAllAsync(); // You'll need to implement GetAllAsync
foreach (var membership in memberships)
{
    var usage = new ImageStorageUsage
    {
        id = membership.UserProfileId,
        UserProfileId = membership.UserProfileId,
        StorageUsedInBytes = membership.StorageUsedInBytes,
        BandwidthUsedInBytes = membership.BandwidthUsedInBytes,
        LastUpdated = DateTime.UtcNow
    };
    await usageRepository.AddAsync(usage);
}
```

### Step 7: Test the Migration

1. **Test Role Assignment**: 
   - Authenticate as a migrated user
   - Check JWT token contains `roles` claim with `ImageStorageTier.{TierName}`
   - Verify image upload works with role-based tier

2. **Test Default Tier Assignment**:
   - Authenticate as a new user (no role assigned)
   - Attempt image upload
   - Verify user is automatically assigned to Starter tier
   - Check logs for tier assignment

3. **Test Usage Tracking**:
   - Upload an image
   - Verify ImageStorageUsage is created/updated
   - Check storage and bandwidth values are tracked correctly

### Step 8: Monitoring and Verification

Monitor the following:
- Application logs for tier determination
- Usage tracking updates in Cosmos DB
- Any errors related to missing roles or tier configuration

### Step 9: Cleanup (Optional)

After confirming everything works:
1. Keep ImageStorageTierMembership data for historical reference
2. Consider archiving old membership records
3. Update documentation to reflect new architecture

## Rollback Procedure

If you need to rollback:

1. Redeploy previous version of ImageAPI
2. App roles in Azure AD are harmless and can remain
3. Remove assignments if needed via Azure Portal or PowerShell

## Automatic Tier Assignment Logic

When a user doesn't have an `ImageStorageTier.*` role:
1. System checks for "Starter" tier by name
2. If found, user is automatically assigned this tier for the request
3. If not found, system assigns lowest cost tier
4. No database write occurs - assignment is request-scoped only

**Note**: Automatic tier assignment does NOT update Azure AD. It only affects the current request. You should create a separate process to assign actual roles to new users.

## New User Onboarding

For new users, you have two options:

### Option 1: Automatic Default Assignment (Recommended)
Configure a process to automatically assign new users to the Starter role when they register:
```csharp
// In your user registration flow
var graphClient = new GraphServiceClient(credential);
var roleAssignment = new AppRoleAssignment
{
    PrincipalId = Guid.Parse(userId),
    ResourceId = Guid.Parse(servicePrincipalId),
    AppRoleId = starterRoleId
};
await graphClient.ServicePrincipals[servicePrincipalId].AppRoleAssignedTo.PostAsync(roleAssignment);
```

### Option 2: Request-Scoped Default
Let the ImageStorageTierService handle defaults automatically. Users without roles will use Starter tier but won't have persistent role assignment.

## Troubleshooting

### User has no tier
**Symptom**: Log shows "User has no tier role, assigning default tier"
**Solution**: This is expected for new users. Ensure Starter tier exists in Cosmos DB.

### Role not found in database
**Symptom**: "User has role ImageStorageTier.X but tier not found in database"
**Solution**: Ensure tier names in Azure AD match exactly with Cosmos DB tier names.

### Usage not tracked
**Symptom**: ImageStorageUsage not created or updated
**Solution**: Check Cosmos DB container exists. Verify ImageStorageUsagesContainerManager is registered.

### Migration fails with permission errors
**Symptom**: "Insufficient privileges to complete the operation"
**Solution**: Verify service principal has required permissions in Azure AD.

## Benefits of New Architecture

1. **Performance**: No database lookup for tier determination - read from JWT
2. **Scalability**: Reduced Cosmos DB queries
3. **Security**: Centralized role management through Azure AD
4. **Auditability**: Role assignments are tracked in Azure AD audit logs
5. **Consistency**: Single source of truth for user roles

## Support

For issues or questions:
1. Check application logs for detailed error messages
2. Verify Azure AD app role configuration
3. Ensure JWT tokens include roles claim
4. Review Cosmos DB for usage tracking data
