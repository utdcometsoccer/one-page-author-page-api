# Migrate DomainRegistration LastUpdatedAt

This is a one-time migration script to populate the `LastUpdatedAt` field for existing `DomainRegistration` records in Cosmos DB.

## Purpose

When the `LastUpdatedAt` field was added to the `DomainRegistration` entity, existing records in the database did not have this field populated. This migration script:

1. Queries all `DomainRegistration` records that don't have `LastUpdatedAt` set
2. Sets `LastUpdatedAt` to the value of `CreatedAt` (the date of the first request)
3. Saves the updated records back to Cosmos DB

## Prerequisites

- .NET 10.0 SDK
- Access to the Cosmos DB instance containing DomainRegistration records
- Cosmos DB credentials (endpoint URI, primary key, database ID)

## Configuration

Configure the following settings either through:

- User Secrets (recommended for local development)
- Environment variables
- `appsettings.json` (not recommended for sensitive data)

Required settings:

```
COSMOSDB_ENDPOINT_URI=<your-cosmos-endpoint>
COSMOSDB_PRIMARY_KEY=<your-primary-key>
COSMOSDB_DATABASE_ID=<your-database-id>
```

### Setting User Secrets (Recommended)

```bash
cd MigrateDomainRegistrationLastUpdatedAt
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthor"
```

## Running the Migration

```bash
cd MigrateDomainRegistrationLastUpdatedAt
dotnet run
```

## What It Does

The migration:

1. Connects to your Cosmos DB database
2. Queries for all `DomainRegistration` records where `LastUpdatedAt` is not set
3. For each record found:
   - Sets `LastUpdatedAt = CreatedAt`
   - Updates the record in Cosmos DB
4. Logs progress and any errors
5. Reports final statistics (records updated, errors encountered)

## Safety

- This migration is **idempotent** - it only updates records that don't have `LastUpdatedAt` set
- Running it multiple times is safe; it will only process records that still need migration
- Each record is updated individually with error handling
- Failed updates are logged but don't stop the migration

## Verification

After running the migration, all `DomainRegistration` records should have:

- `LastUpdatedAt` equal to `CreatedAt` (for records that existed before the field was added)
- Both timestamps present and valid

You can verify by querying the API endpoints that return `DomainRegistration` data and checking that `LastUpdatedAt` is populated.
