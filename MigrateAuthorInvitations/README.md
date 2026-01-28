# Author Invitation Migration Tool

## Overview

This tool migrates existing pending author invitations to support the new multi-domain feature. It ensures that invitations created before the multi-domain update have their `DomainNames` array populated from the legacy `DomainName` field.

## When to Use This Tool

**You only need to run this tool if:**
1. You have existing pending invitations in your database created before the multi-domain feature was added
2. You want to ensure explicit data consistency in Cosmos DB

**You do NOT need to run this tool if:**
- You have no pending invitations
- All your invitations were created after the multi-domain feature was deployed
- The automatic migration in the `AuthorInvitation` entity is sufficient for your needs (recommended)

## Automatic Migration

The `AuthorInvitation` entity includes automatic migration logic that populates `DomainNames` from `DomainName` when reading old records. This means:

✅ **Existing invitations will work without running this tool**
✅ API endpoints will automatically see the domain in `DomainNames`
✅ Console application will automatically see the domain in `DomainNames`
✅ No breaking changes for existing pending invitations

## Explicit Migration

This tool performs an explicit one-time migration that:
- Updates all pending invitations to have `DomainNames` explicitly stored in the database
- Ensures data consistency without relying on automatic migration
- Provides a clear audit trail of migrated records

## Prerequisites

- .NET 10.0 SDK or later
- Azure Cosmos DB credentials
- Permission to update invitation records

## Configuration

Use the same configuration methods as `AuthorInvitationTool`:

### Option 1: User Secrets (Recommended)

```bash
dotnet user-secrets init
dotnet user-secrets set "CosmosDb:EndpointUri" "https://your-cosmos-account.documents.azure.com:443/"
dotnet user-secrets set "CosmosDb:PrimaryKey" "your-primary-key-here"
dotnet user-secrets set "CosmosDb:DatabaseId" "OnePageAuthor"
```

### Option 2: Environment Variables

```bash
export COSMOSDB_ENDPOINT_URI="https://your-cosmos-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-primary-key-here"
export COSMOSDB_DATABASE_ID="OnePageAuthor"
```

## Usage

```bash
cd MigrateAuthorInvitations
dotnet run
```

## What the Tool Does

1. **Fetches all pending invitations** from Cosmos DB
2. **Identifies invitations needing migration** (those with `DomainName` but no `DomainNames`)
3. **Shows a summary** of invitations to be migrated
4. **Asks for confirmation** before making changes
5. **Updates each invitation** to populate `DomainNames` from `DomainName`
6. **Reports results** showing successful migrations and any errors

## Example Output

```
═══════════════════════════════════════════════════════════
   Author Invitation Migration Tool
   Migrates existing invitations to support multiple domains
═══════════════════════════════════════════════════════════

Fetching all pending invitations...
Found 5 pending invitation(s)

Found 3 invitation(s) that need migration:

  ID: abc-123-def-456
    Email: author1@example.com
    DomainName: example.com
    DomainNames: 0 items

  ID: def-456-ghi-789
    Email: author2@example.com
    DomainName: another.com
    DomainNames: 0 items

Do you want to migrate these invitations? (y/n): y

Starting migration...

✅ Migrated: author1@example.com (abc-123-def-456)
✅ Migrated: author2@example.com (def-456-ghi-789)

═══════════════════════════════════════════════════════════
✅ Migration completed!
   Migrated: 2
═══════════════════════════════════════════════════════════
```

## Safety

- **Read-only preview**: The tool shows what will be migrated before making changes
- **User confirmation**: Requires explicit confirmation before updating any records
- **Error handling**: Continues processing if one record fails
- **Non-destructive**: Only adds data to `DomainNames`, doesn't remove or change existing fields
- **Idempotent**: Safe to run multiple times (already migrated records are skipped)

## Troubleshooting

### "COSMOSDB_ENDPOINT_URI is required"

Configure Cosmos DB credentials using user secrets or environment variables.

### "Found 0 invitation(s) that need migration"

All your invitations are already migrated or created with the new format. No action needed.

### Migration errors

Check the error message and logs. Common issues:
- Network connectivity to Cosmos DB
- Invalid partition key
- Insufficient permissions

## After Migration

After running this tool:
- ✅ All pending invitations will have `DomainNames` explicitly stored
- ✅ Future updates to these invitations will work correctly
- ✅ The API and console tool will work with the updated records
- ✅ No further migration needed

## Related Documentation

- [Author Invitation API Documentation](../InkStainedWretchFunctions/AUTHOR_INVITATION_API.md)
- [Author Invitation Tool README](../AuthorInvitationTool/README.md)
- [Implementation Summary](../IMPLEMENTATION_SUMMARY_MULTI_DOMAIN_INVITATIONS.md)
