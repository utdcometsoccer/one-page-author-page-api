# Configuration Validation Summary

## Overview
All applications in the OnePageAuthor API repository have been updated to implement consistent configuration validation patterns. Required configuration values now throw `InvalidOperationException` with clear error messages when missing, while optional values use appropriate fallbacks.

## Standardized Configuration Keys

### Required Settings
All applications now use standardized environment variable names:

**Cosmos DB (Required for all apps)**
- `COSMOSDB_ENDPOINT_URI` - Cosmos DB endpoint URL
- `COSMOSDB_PRIMARY_KEY` - Cosmos DB primary access key  
- `COSMOSDB_DATABASE_ID` - Cosmos DB database name

**Azure Storage (Required for ImageAPI only)**
- `AZURE_STORAGE_CONNECTION_STRING` - Azure Blob Storage connection string

**Stripe (Required for InkStainedWretchStripe only)**
- `STRIPE_API_KEY` - Stripe API key for payment processing

**Azure AD/Entra ID (Required for EntraIdRoleManager only)**
- `AAD_TENANT_ID` - Azure AD tenant ID
- `AAD_MANAGEMENT_CLIENT_ID` - Management app client ID
- `AAD_MANAGEMENT_CLIENT_SECRET` - Management app client secret
- `AAD_TARGET_CLIENT_ID` - Target app client ID

### Optional Settings
These settings have reasonable defaults or fallback behavior:

**Azure AD/Entra ID (Optional for ImageAPI, InkStainedWretchStripe)**
- `AAD_TENANT_ID` - Optional for authentication
- `AAD_AUDIENCE` / `AAD_CLIENT_ID` - Optional for JWT validation
- `AAD_AUTHORITY` - Optional, auto-constructed from tenant ID

## Applications Updated

### ✅ Production Applications
1. **ImageAPI** (`ImageAPI/Program.cs`)
   - ✅ Added validation for `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
   - ✅ Added validation for `AZURE_STORAGE_CONNECTION_STRING`
   - ✅ Removed null-forgiving operators (!)
   - ✅ Optional Azure AD settings remain optional with proper fallbacks

2. **InkStainedWretchFunctions** (`InkStainedWretchFunctions/Program.cs`)
   - ✅ Added validation for `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
   - ✅ Removed null-forgiving operators (!)

3. **InkStainedWretchStripe** (`InkStainedWretchStripe/Program.cs`)
   - ✅ Added validation for `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
   - ✅ Existing `STRIPE_API_KEY` validation maintained
   - ✅ Removed null-forgiving operators (!)
   - ✅ Optional Azure AD settings remain optional with proper masking in logs

4. **function-app** (`function-app/Program.cs`)
   - ✅ Already had proper validation - verified consistency

### ✅ Management/Utility Applications
5. **EntraIdRoleManager** (`EntraIdRoleManager/Program.cs`)
   - ✅ Already had proper validation - verified consistency

6. **SeedImageStorageTiers** (`SeedImageStorageTiers/Program.cs`)
   - ✅ Already had proper validation - verified consistency

### ✅ Data Seeder Applications
All seeder applications updated to use standardized keys with backward compatibility:

7. **SeedLocalizationData** (`SeedLocalizationData/Program.cs`)
   - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy `EndpointUri`, `PrimaryKey`, `DatabaseId`
   - ✅ Added environment variable support

8. **OnePageAuthor.DataSeeder** (`OnePageAuthor.DataSeeder/Program.cs`)
   - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
   - ✅ Removed unsafe emulator defaults
   - ✅ Added helpful error messages with emulator information

9. **SeedLocales** (`SeedLocales/Program.cs`)
   - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
   - ✅ Added environment variable support

10. **SeedInkStainedWretchesLocale** (`SeedInkStainedWretchesLocale/Program.cs`)
    - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
    - ✅ Added environment variable support

11. **SeedAPIData** (`SeedAPIData/Program.cs`)
    - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
    - ✅ Added environment variable support

12. **IntegrationTestAuthorDataService** (`IntegrationTestAuthorDataService/Program.cs`)
    - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
    - ✅ Added proper validation (was using empty strings as defaults)
    - ✅ Added environment variable support

## Validation Pattern
All applications now use the consistent pattern:

```csharp
// For required settings
var requiredValue = config["REQUIRED_KEY"] ?? throw new InvalidOperationException("REQUIRED_KEY is required");

// For optional settings with fallback
var optionalValue = config["OPTIONAL_KEY"] ?? "default_value";

// For backward compatibility in seeder apps
var value = config["NEW_KEY"] ?? config["LEGACY_KEY"] ?? throw new InvalidOperationException("NEW_KEY is required");
```

## Error Messages
All error messages are clear and actionable:
- Specify the exact environment variable name needed
- For development tools, include helpful information about emulator settings
- No sensitive information is logged

## Benefits
1. **Production Safety** - No more silent failures due to missing configuration
2. **Consistency** - Standardized configuration keys across all applications
3. **Developer Experience** - Clear error messages with actionable guidance
4. **Backward Compatibility** - Seeder apps support both new and legacy configuration keys
5. **Security** - No hardcoded secrets or unsafe defaults in production applications

## Testing
All applications should be tested to ensure:
1. They fail fast with clear error messages when required configuration is missing
2. They work correctly when proper configuration is provided
3. Optional settings behave appropriately with and without values
4. Seeder applications work with both new and legacy configuration key formats
