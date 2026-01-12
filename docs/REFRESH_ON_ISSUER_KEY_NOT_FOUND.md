# RefreshOnIssuerKeyNotFound Configuration

## Overview

This document describes the `RefreshOnIssuerKeyNotFound` configuration option that has been enabled across all Azure Function apps in the OnePageAuthor API platform to prevent authentication failures during Azure AD signing key rotations.

## Problem Statement

The platform was experiencing `SecurityTokenSignatureKeyNotFoundException` errors during JWT token validation. This exception occurs when:

1. Azure Active Directory (now Microsoft Entra ID) rotates its signing keys
2. The cached signing keys in the application are outdated
3. A token signed with the new key arrives before the cache is updated
4. Token validation fails because the signing key is not found in the cached keys

## Solution

Enabled the `RefreshOnIssuerKeyNotFound` option in the JWT Bearer authentication configuration for all three Azure Function apps.

### How It Works

When `RefreshOnIssuerKeyNotFound` is set to `true`:

1. **Normal validation attempt**: The JWT Bearer handler attempts to validate the token using cached signing keys
2. **Key not found**: If the signing key used to sign the token is not found in the cache, a `SecurityTokenSignatureKeyNotFoundException` would normally be thrown
3. **Automatic refresh**: Instead of failing immediately, the handler:
   - Refreshes the OpenID Connect metadata from the authority endpoint
   - Updates the cached signing keys
   - Retries the token validation
4. **Success or failure**: The validation either succeeds with the refreshed keys or fails with an appropriate error

### Benefits

- **Resilience during key rotations**: Automatic recovery from signing key rotations without manual intervention
- **No downtime**: Users experience seamless authentication even during Azure AD key updates
- **Reduced support burden**: Fewer authentication-related incidents and support tickets
- **Security maintained**: All token validation rules remain enforced; only the key discovery mechanism is enhanced

## Implementation Details

### Files Modified

The following files were updated to include `options.RefreshOnIssuerKeyNotFound = true`:

1. **ImageAPI/Program.cs**
2. **InkStainedWretchFunctions/Program.cs**
3. **InkStainedWretchStripe/Program.cs**

### Code Example

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ... other configuration ...
        
        // Enable automatic metadata refresh when signing key is not found
        // This helps prevent SecurityTokenSignatureKeyNotFoundException when Azure AD rotates keys
        options.RefreshOnIssuerKeyNotFound = true;
        
        // Configure automatic refresh of signing keys from OpenID Connect metadata
        if (!string.IsNullOrWhiteSpace(authority))
        {
            var metadataAddress = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever())
            {
                // Refresh metadata every 6 hours (default is 24 hours)
                AutomaticRefreshInterval = TimeSpan.FromHours(6),
                // Minimum time between refreshes to prevent hammering the endpoint
                RefreshInterval = TimeSpan.FromMinutes(30)
            };
        }
        
        // ... token validation parameters ...
    });
```

## Technical Details

### Property Location

- **Namespace**: `Microsoft.AspNetCore.Authentication.JwtBearer`
- **Class**: `JwtBearerOptions`
- **Property**: `RefreshOnIssuerKeyNotFound`
- **Type**: `bool`
- **Default Value**: `true` (set explicitly for documentation and clarity)

### Related Configuration

The platform also uses `ConfigurationManager` with custom refresh intervals:

- **AutomaticRefreshInterval**: 6 hours (reduced from default 24 hours)
- **RefreshInterval**: 30 minutes (minimum time between refreshes)

These settings work together with `RefreshOnIssuerKeyNotFound` to provide:
1. **Proactive refresh**: Keys are automatically refreshed every 6 hours
2. **Reactive refresh**: Keys are refreshed on-demand when validation fails due to missing key
3. **Rate limiting**: Minimum 30 minutes between refreshes prevents endpoint overload

## Additional Remediation Strategies

While `RefreshOnIssuerKeyNotFound` addresses the immediate issue, consider these additional strategies:

### 1. Monitoring and Alerting

Configure Application Insights alerts for:
- `SecurityTokenSignatureKeyNotFoundException` occurrences
- JWT validation failure rates
- Metadata endpoint response times

### 2. Retry Policies

Implement retry policies with exponential backoff for transient failures when fetching metadata:

```csharp
options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    metadataAddress,
    new OpenIdConnectConfigurationRetriever(),
    new HttpDocumentRetriever { MaxResponseContentBufferSize = 1024 * 1024 })
{
    AutomaticRefreshInterval = TimeSpan.FromHours(6),
    RefreshInterval = TimeSpan.FromMinutes(30),
    // Consider adding custom error handling
};
```

### 3. Token Caching Strategy

Review and optimize token caching strategies:
- Implement distributed caching for multi-instance deployments
- Set appropriate cache expiration times
- Consider token revocation scenarios

### 4. Metadata Endpoint Performance

Monitor the OpenID Connect metadata endpoint:
- Track latency and availability
- Implement fallback mechanisms if primary endpoint is slow
- Consider caching metadata responses with appropriate TTL

### 5. Logging Enhancements

Add detailed logging for key refresh events:

```csharp
services.AddLogging(builder =>
{
    builder.AddFilter("Microsoft.AspNetCore.Authentication.JwtBearer", LogLevel.Debug);
});
```

## Testing

### Unit Testing

The configuration change does not require new unit tests as it modifies startup configuration rather than business logic. Existing JWT authentication integration tests validate the overall authentication flow.

### Manual Testing

To test the configuration:

1. **Obtain a valid JWT token** from Microsoft Entra ID
2. **Make authenticated API calls** to verify normal operation
3. **Simulate key rotation** (not recommended in production)
4. **Monitor logs** for key refresh events

### Expected Behavior

- ✅ Valid tokens with current keys: Authentication succeeds immediately
- ✅ Valid tokens with new keys: Brief delay for metadata refresh, then authentication succeeds
- ❌ Invalid tokens: Authentication fails with appropriate error message
- ❌ Expired tokens: Authentication fails with token lifetime error

## Impact and Risks

### Positive Impact

- ✅ Improved resilience to Azure AD key rotations
- ✅ Reduced authentication failures
- ✅ Better user experience during key updates
- ✅ No code changes required in authentication logic

### Potential Risks

- ⚠️ **Increased latency**: First request after key rotation may experience additional latency (typically <1 second)
- ⚠️ **Metadata endpoint dependency**: Increased reliance on availability of OpenID Connect metadata endpoint
- ⚠️ **Network traffic**: Additional HTTP requests to metadata endpoint during key refreshes

### Mitigation Strategies

1. **Monitor metadata endpoint**: Set up alerts for slow or failed metadata requests
2. **Implement caching**: Existing `ConfigurationManager` provides intelligent caching
3. **Rate limiting**: The 30-minute minimum refresh interval prevents excessive requests
4. **Fallback mechanisms**: Consider implementing custom key resolvers if metadata endpoint has issues

## Related Documentation

- [Microsoft Docs - JwtBearerOptions.RefreshOnIssuerKeyNotFound](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.jwtbeareroptions.refreshonissuerkeynotfound)
- [Microsoft Docs - Azure AD Key Rollover](https://learn.microsoft.com/en-us/entra/identity-platform/signing-key-rollover)
- [Microsoft Docs - Token Validation](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens#validating-tokens)
- [Project Documentation - 401 Unauthorized Resolution](./401_UNAUTHORIZED_RESOLUTION.md)
- [Project Documentation - Authorization Fix Documentation](../AUTHORIZATION_FIX_DOCUMENTATION.md)

## Troubleshooting

### Issue: Still seeing SecurityTokenSignatureKeyNotFoundException

**Possible Causes:**
1. Network connectivity issues preventing metadata refresh
2. OpenID Connect metadata endpoint unavailable
3. Incorrect authority or metadata URL configuration
4. Certificate trust issues with the metadata endpoint

**Resolution Steps:**
1. Verify `AAD_AUTHORITY` or `AAD_TENANT_ID` configuration is correct
2. Check network connectivity to `https://login.microsoftonline.com`
3. Review Application Insights logs for metadata fetch errors
4. Verify SSL/TLS certificates are trusted

### Issue: High latency on first authentication after key rotation

**Expected Behavior:**
This is normal. The first request after a key rotation will experience additional latency while fetching updated metadata.

**Resolution:**
This is working as designed. Subsequent requests will use the cached updated keys and experience normal latency.

### Issue: Too many requests to metadata endpoint

**Possible Causes:**
1. Multiple instances fetching metadata independently
2. Incorrect `RefreshInterval` configuration
3. Key rotation happening more frequently than expected

**Resolution Steps:**
1. Verify `RefreshInterval` is set to at least 30 minutes
2. Review metadata endpoint access logs
3. Consider implementing distributed caching for metadata across instances

## Version History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0 | 2026-01-12 | GitHub Copilot | Initial documentation for RefreshOnIssuerKeyNotFound configuration |

## Contact

For questions or issues related to this configuration:
- Review this documentation first
- Check Application Insights for error details
- Refer to [Microsoft Entra ID documentation](https://learn.microsoft.com/en-us/entra/identity-platform/)
- Contact the development team with specific error messages and logs
