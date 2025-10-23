# Configuration Masking and Logging Standardization

## Overview
All applications in the OnePageAuthor API repository have been updated with standardized configuration masking and logging. This ensures sensitive values are properly masked in logs while providing clear visibility into what configuration values are loaded.

## Standardized Masking Functions

### Helper Functions Added to All Applications
```csharp
// Helper function to mask sensitive configuration values
static string MaskSensitiveValue(string? value, string notSetText = "(not set)")
{
    if (string.IsNullOrWhiteSpace(value)) return notSetText;
    if (value.Length < 8) return "(set)";
    return $"{value[..4]}****{value[^4..]}";
}

// Helper function to mask URLs (show more of the beginning for readability)
static string MaskUrl(string? value, string notSetText = "(not set)")
{
    if (string.IsNullOrWhiteSpace(value)) return notSetText;
    if (value.Length < 12) return "(set)";
    return $"{value[..8]}****{value[^4..]}";
}
```

### Masking Rules
- **Short values (< 8 chars)**: Show "(set)" to avoid revealing too much
- **Sensitive values (8+ chars)**: Show first 4 chars + "****" + last 4 chars  
- **URLs (12+ chars)**: Show first 8 chars + "****" + last 4 chars (more context for debugging)
- **Missing values**: Show "(not set)" for optional values
- **Database IDs**: Not masked (not sensitive, useful for debugging)
- **Domain names**: Not masked (not sensitive, useful for debugging)

## Applications Updated

### ✅ Production Azure Functions
1. **ImageAPI** (`ImageAPI/Program.cs`)
   ```
   Azure AD Tenant ID configured: (not set)
   Azure AD Audience configured: (not set)  
   Azure AD Authority configured: (not set)
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Primary Key configured: C2y6****Jw==
   Cosmos DB Database ID configured: OnePageAuthorDB
   Azure Storage Connection String configured: Defa****key1
   ```

2. **InkStainedWretchFunctions** (`InkStainedWretchFunctions/Program.cs`)
   ```
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Primary Key configured: C2y6****Jw==
   Cosmos DB Database ID configured: OnePageAuthorDB
   ```

3. **InkStainedWretchStripe** (`InkStainedWretchStripe/Program.cs`)
   ```
   Stripe API key configured: sk_t****_1N2
   Azure AD Tenant ID configured: (not set)
   Azure AD Audience configured: (not set)
   Azure AD Authority configured: (not set)
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Primary Key configured: C2y6****Jw==
   Cosmos DB Database ID configured: OnePageAuthorDB
   ```

4. **function-app** (`function-app/Program.cs`)
   ```
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Primary Key configured: C2y6****Jw==
   Cosmos DB Database ID configured: OnePageAuthorDB
   ```

### ✅ Management/Utility Applications
5. **EntraIdRoleManager** (`EntraIdRoleManager/Program.cs`)
   ```
   Configuration (masked for security):
     Management App ID: 0816****333a
     Target App ID: f2b0****db73
     Tenant ID: 5c6d****461e
     Cosmos DB Endpoint: https:****com/
     Cosmos DB Database ID: OnePageAuthorDB
   ```

6. **SeedImageStorageTiers** (`SeedImageStorageTiers/Program.cs`)
   ```
   Configuration (masked for security):
     Cosmos DB Endpoint: https:****com/
     Cosmos DB Database ID: OnePageAuthorDB
   ```

### ✅ Data Seeder Applications
7. **SeedAPIData** (`SeedAPIData/Program.cs`)
   ```
   Starting API Data Seeding...
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Database ID configured: OnePageAuthorDB
   ```

8. **SeedInkStainedWretchesLocale** (`SeedInkStainedWretchesLocale/Program.cs`)
   ```
   Starting InkStainedWretches Locale Seeding...
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Database ID configured: OnePageAuthorDB
   ```

9. **OnePageAuthor.DataSeeder** (`OnePageAuthor.DataSeeder/Program.cs`)
   ```
   Starting StateProvince Data Seeding...
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Database ID configured: OnePageAuthorDB
   ```

10. **IntegrationTestAuthorDataService** (`IntegrationTestAuthorDataService/Program.cs`)
    ```
    Starting Integration Test for Author Data Service...
    Cosmos DB Endpoint configured: https:****com/
    Cosmos DB Database ID configured: OnePageAuthorDB
    Author Domain configured: example.com
    ```

## Configuration Values Logged

### Always Masked
- **Cosmos DB Primary Keys**: `MaskSensitiveValue()` - Shows `C2y6****Jw==`
- **Azure Storage Connection Strings**: `MaskSensitiveValue()` - Shows connection string secrets masked
- **Stripe API Keys**: `MaskSensitiveValue()` - Shows `sk_t****_1N2`
- **Azure AD Client IDs**: `MaskSensitiveValue()` - Shows `0816****333a`
- **Azure AD Client Secrets**: `MaskSensitiveValue()` - Shows first/last 4 chars
- **Azure AD Tenant IDs**: `MaskSensitiveValue()` - Shows `5c6d****461e`

### URL Masked (More Context)
- **Cosmos DB Endpoints**: `MaskUrl()` - Shows `https:****com/`
- **Azure AD Authority URLs**: `MaskUrl()` - Shows `https:****v2.0`

### Never Masked (Not Sensitive)
- **Database IDs**: Shown in full (useful for debugging, not sensitive)
- **Domain Names**: Shown in full (not sensitive)
- **Application Names**: Shown in full

### Optional Values
- **Azure AD Settings**: Show "(not set)" when not configured
- All optional authentication parameters handled gracefully

## Security Benefits
1. **No Secrets in Logs**: All sensitive values are properly masked
2. **Debugging Friendly**: Enough context to verify configuration without exposing secrets
3. **Consistent Format**: Same masking pattern across all applications
4. **Clear Status**: Easy to see what's configured vs. missing
5. **Production Safe**: Safe to enable in production environments

## Implementation Pattern
All applications now follow this consistent pattern:
1. Load configuration with proper validation (from previous update)
2. Apply standardized masking functions
3. Log configuration status with appropriate masking
4. Continue with application startup

This provides excellent visibility into application configuration while maintaining security best practices.