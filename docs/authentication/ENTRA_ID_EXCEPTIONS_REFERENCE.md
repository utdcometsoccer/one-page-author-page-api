# Microsoft Entra ID Exceptions Reference

## Overview

This document provides a comprehensive reference of all Microsoft Entra ID-related exceptions that can occur in the OnePageAuthor API platform, along with their causes, resolutions, and prevention strategies.

## Table of Contents

1. [Token Validation Exceptions](#token-validation-exceptions)
2. [Authentication Exceptions](#authentication-exceptions)
3. [Configuration Exceptions](#configuration-exceptions)
4. [Network & Connectivity Exceptions](#network--connectivity-exceptions)
5. [Authorization Exceptions](#authorization-exceptions)
6. [Quick Reference Table](#quick-reference-table)

## Token Validation Exceptions

### SecurityTokenSignatureKeyNotFoundException

**Namespace**: `Microsoft.IdentityModel.Tokens`

**Error Code**: IDX10503

**Full Message**: 
```
IDX10503: Signature validation failed. Keys tried: 'kid1, kid2, ...'. 
Exceptions caught: '...'. token: '...'
```

**Cause**:
- Azure AD has rotated signing keys
- Token signed with a key not in the cached keyset
- ConfigurationManager hasn't refreshed signing keys yet

**When It Occurs**:
- Immediately after Azure AD rotates signing keys (typically monthly)
- When token from older key arrives before cache refresh
- Network issues preventing metadata refresh

**Impact**: 
- HTTP 401 Unauthorized
- Authentication failures until keys refresh
- Users may need to retry their request

**Resolution**:

1. **Automatic (Recommended)**:
   ```csharp
   // In Program.cs
   options.RefreshOnIssuerKeyNotFound = true;
   ```
   Platform automatically refreshes keys and retries validation.

2. **Manual Verification**:
   ```bash
   # Check JWKS endpoint for available keys
   curl https://login.microsoftonline.com/{tenant-id}/discovery/v2.0/keys
   ```

3. **Force Refresh** (if automatic doesn't work):
   - Restart Azure Function App
   - ConfigurationManager will fetch latest keys on startup

**Prevention**:
- ✅ Ensure `RefreshOnIssuerKeyNotFound = true` in all Program.cs files
- ✅ Configure `AutomaticRefreshInterval = TimeSpan.FromHours(6)`
- ✅ Configure `RefreshInterval = TimeSpan.FromMinutes(30)`
- ✅ Monitor Application Insights for key rotation events

**Related Documentation**: 
- [JWT_KEY_ROTATION_FIX.md](../JWT_KEY_ROTATION_FIX.md)
- [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md)

---

### SecurityTokenInvalidSignatureException

**Namespace**: `Microsoft.IdentityModel.Tokens`

**Error Code**: IDX10511

**Full Message**:
```
IDX10511: Signature validation failed. Unable to match key: kid: '{kid}'.
```

**Cause**:
- Token signature is invalid or tampered with
- Token not issued by expected authority
- Wrong public key used for validation

**When It Occurs**:
- Token has been modified after issuance
- Using token from wrong tenant/authority
- Man-in-the-middle attack attempt

**Impact**:
- HTTP 401 Unauthorized
- Complete authentication failure
- Security concern if frequent

**Resolution**:
1. Verify token is from correct tenant
2. Check token hasn't been modified
3. Acquire new token from identity provider
4. If persistent, check for network security issues

**Prevention**:
- ✅ Use HTTPS for all communications
- ✅ Never log or expose full JWT tokens
- ✅ Validate token source and authority
- ✅ Monitor for patterns of signature failures (potential attack)

---

### SecurityTokenExpiredException

**Namespace**: `Microsoft.IdentityModel.Tokens`

**Error Code**: IDX10223

**Full Message**:
```
IDX10223: Lifetime validation failed. The token is expired. 
ValidTo: '{expiry-time}', Current time: '{current-time}'.
```

**Cause**:
- Token's `exp` claim is in the past
- Client hasn't refreshed token before expiration
- Clock skew between client and server (rare)

**When It Occurs**:
- After token lifetime expires (typically 60-90 minutes)
- Client doesn't implement token refresh
- Long-running operations without token refresh

**Impact**:
- HTTP 401 Unauthorized
- User must re-authenticate or refresh token

**Resolution**:
1. **Client-side (MSAL)**:
   ```javascript
   // Automatically handles refresh
   const response = await msalInstance.acquireTokenSilent(loginRequest);
   const newToken = response.accessToken;
   ```

2. **Check token expiration**:
   - Decode token at https://jwt.ms
   - Check `exp` claim (Unix timestamp)
   - Compare to current time

**Prevention**:
- ✅ Implement proactive token refresh (before expiration)
- ✅ Use MSAL's `acquireTokenSilent` with automatic refresh
- ✅ Monitor token expiration in client app
- ✅ Handle 401 responses with token refresh retry

**Related Documentation**:
- [MSAL_CIAM_BEST_PRACTICES.md](MSAL_CIAM_BEST_PRACTICES.md)

---

### SecurityTokenInvalidAudienceException

**Namespace**: `Microsoft.IdentityModel.Tokens`

**Error Code**: IDX10214

**Full Message**:
```
IDX10214: Audience validation failed. Audiences: '{token-aud}'. 
Did not match: validationParameters.ValidAudience: '{expected-aud}'.
```

**Cause**:
- Token `aud` claim doesn't match `AAD_AUDIENCE` configuration
- Token requested for different API/resource
- Misconfiguration in SPA or API

**When It Occurs**:
- SPA requests wrong scope
- API `AAD_AUDIENCE` incorrectly configured
- Using token intended for different resource

**Impact**:
- HTTP 401 Unauthorized
- Authentication fails until configuration fixed

**Resolution**:

1. **Decode token** and check `aud` claim
2. **Compare to API configuration**:
   ```bash
   # Token aud: 87654321-4321-4321-4321-210987654321
   # AAD_AUDIENCE should match
   ```

3. **Fix Option 1 - Update API**:
   ```bash
   az functionapp config appsettings set \
     --name your-function-app \
     --resource-group your-rg \
     --settings AAD_AUDIENCE="87654321-4321-4321-4321-210987654321"
   ```

4. **Fix Option 2 - Update SPA scope**:
   ```javascript
   const loginRequest = {
     scopes: ["api://87654321-4321-4321-4321-210987654321/access_as_user"]
   };
   ```

**Prevention**:
- ✅ Document correct audience value
- ✅ Validate configuration on deployment
- ✅ Use consistent client IDs across environments
- ✅ Test with real tokens before deployment

**Related Documentation**:
- [ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md)

---

### SecurityTokenInvalidIssuerException

**Namespace**: `Microsoft.IdentityModel.Tokens`

**Error Code**: IDX10205

**Full Message**:
```
IDX10205: Issuer validation failed. Issuer: '{actual-issuer}'. 
Did not match: validationParameters.ValidIssuer: '{expected-issuer}'.
```

**Cause**:
- Token `iss` claim doesn't match expected issuer
- Wrong tenant ID in configuration
- Token from different Azure AD tenant
- Using v1.0 endpoint instead of v2.0 (or vice versa)

**When It Occurs**:
- `AAD_TENANT_ID` misconfigured
- Token from different tenant
- Mixing v1.0 and v2.0 endpoints

**Impact**:
- HTTP 401 Unauthorized
- All authentication fails until corrected

**Resolution**:

1. **Check token issuer**:
   - Decode token at https://jwt.ms
   - Find `iss` claim
   - Should be: `https://login.microsoftonline.com/{tenant-id}/v2.0`

2. **Verify tenant ID matches**:
   ```bash
   # From token iss claim
   ACTUAL_TENANT="12345678-1234-1234-1234-123456789012"
   
   # From configuration
   CONFIGURED_TENANT=$(az functionapp config appsettings list \
     --name your-app --resource-group your-rg \
     --query "[?name=='AAD_TENANT_ID'].value" -o tsv)
   
   # Should match
   ```

3. **Update configuration**:
   ```bash
   az functionapp config appsettings set \
     --name your-function-app \
     --resource-group your-rg \
     --settings AAD_TENANT_ID="12345678-1234-1234-1234-123456789012"
   ```

**Prevention**:
- ✅ Use specific tenant ID, not `common` or `organizations`
- ✅ Validate tenant ID on deployment
- ✅ Use v2.0 endpoints consistently
- ✅ Document which tenant is used for each environment

---

### SecurityTokenNoExpirationException

**Namespace**: `Microsoft.IdentityModel.Tokens`

**Error Code**: IDX10225

**Full Message**:
```
IDX10225: Lifetime validation failed. The token is missing an Expiration Time. 
Tokentype: '{token-type}'.
```

**Cause**:
- Token missing `exp` claim
- Malformed token
- Custom token generation without expiration

**When It Occurs**:
- Rare in standard Entra ID tokens
- Custom token generators
- Token corruption

**Impact**:
- HTTP 401 Unauthorized
- Token rejected as invalid

**Resolution**:
1. Acquire new token from Entra ID
2. Verify token structure
3. Check token generation code if using custom tokens

**Prevention**:
- ✅ Use standard Entra ID token endpoints
- ✅ Don't create custom JWT tokens for Entra ID validation
- ✅ Validate token structure in tests

---

## Authentication Exceptions

### ArgumentNullException (Authorization Header)

**Namespace**: `System`

**Error Message**:
```
Authorization header is required
```

**Cause**:
- No `Authorization` header in HTTP request
- Header value is null or empty

**When It Occurs**:
- Client forgets to include Authorization header
- Header stripped by proxy/gateway
- Client not sending token

**Impact**:
- HTTP 401 Unauthorized
- Request rejected before token validation

**Resolution**:

1. **Check request includes header**:
   ```bash
   curl -v https://api.example.com/endpoint \
     -H "Authorization: Bearer YOUR_TOKEN"
   
   # Look for: > Authorization: Bearer ... in output
   ```

2. **Verify header format**:
   ```
   Correct:   Authorization: Bearer eyJhbGc...
   Incorrect: authorization: Bearer eyJhbGc... (lowercase - may work but non-standard)
   Incorrect: Bearer eyJhbGc... (missing header name)
   ```

**Prevention**:
- ✅ Include Authorization header in all authenticated requests
- ✅ Check HTTP client configuration
- ✅ Verify proxies/gateways don't strip headers
- ✅ Test with tools like Postman or curl first

---

### ArgumentException (Invalid Authorization Format)

**Error Message**:
```
Authorization header must start with 'Bearer '
```

**Cause**:
- Authorization header doesn't start with "Bearer " prefix
- Wrong authentication scheme
- Malformed header value

**When It Occurs**:
- Using different auth scheme (Basic, Digest, etc.)
- Missing "Bearer " prefix
- Extra spaces or typos

**Impact**:
- HTTP 401 Unauthorized
- Token not extracted for validation

**Resolution**:
1. **Correct format**:
   ```
   Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
                 ^^^^^^ (note space after Bearer)
   ```

2. **Common mistakes**:
   ```
   Wrong: Authorization: eyJhbGc... (missing "Bearer")
   Wrong: Authorization: bearer eyJhbGc... (lowercase)
   Wrong: Authorization: Bearer  eyJhbGc... (double space)
   ```

**Prevention**:
- ✅ Use proper header format: `Bearer <token>`
- ✅ Validate in HTTP client code
- ✅ Use MSAL's built-in header formatting
- ✅ Test authorization header before deployment

---

## Configuration Exceptions

### ConfigurationErrorsException (Missing Configuration)

**Error Message**:
```
JWT authentication not configured: AAD_TENANT_ID or AAD_AUDIENCE not set
```

**Cause**:
- `AAD_TENANT_ID` or `AAD_AUDIENCE` environment variables not set
- Empty or whitespace-only values
- Configuration not loaded

**When It Occurs**:
- First deployment without configuration
- Environment variables not migrated between environments
- Configuration deleted accidentally

**Impact**:
- JWT authentication disabled
- All requests succeed without authentication (DANGEROUS!)
- Or all requests fail with 401

**Resolution**:

1. **Set required environment variables**:
   ```bash
   az functionapp config appsettings set \
     --name your-function-app \
     --resource-group your-rg \
     --settings \
       AAD_TENANT_ID="your-tenant-id" \
       AAD_AUDIENCE="your-api-client-id"
   ```

2. **Verify configuration**:
   ```bash
   az functionapp config appsettings list \
     --name your-function-app \
     --resource-group your-rg \
     --query "[?name=='AAD_TENANT_ID' || name=='AAD_AUDIENCE']"
   ```

3. **Restart Function App** (if needed):
   ```bash
   az functionapp restart \
     --name your-function-app \
     --resource-group your-rg
   ```

**Prevention**:
- ✅ Validate configuration in deployment pipeline
- ✅ Use infrastructure as code (ARM templates, Bicep)
- ✅ Document required configuration
- ✅ Add configuration validation on startup
- ✅ Monitor startup logs for configuration warnings

**Related Documentation**:
- [ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md)

---

## Network & Connectivity Exceptions

### HttpRequestException (Metadata Endpoint)

**Error Message**:
```
Unable to retrieve OpenID Connect configuration from: 
https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration
```

**Cause**:
- Network connectivity issues
- Firewall blocking `login.microsoftonline.com`
- DNS resolution failure
- Proxy configuration issues

**When It Occurs**:
- Function App cannot reach Microsoft endpoints
- Network Security Groups blocking outbound traffic
- DNS issues in VNet
- First-time metadata fetch

**Impact**:
- Authentication fails completely
- Tokens cannot be validated
- Service outage

**Resolution**:

1. **Test connectivity**:
   ```bash
   # From Function App console (Kudu)
   curl https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration
   ```

2. **Check NSG rules**:
   - Ensure outbound HTTPS (443) allowed
   - Allow `login.microsoftonline.com`
   - Check VNet service endpoints

3. **Verify DNS**:
   ```bash
   nslookup login.microsoftonline.com
   ```

4. **Check proxy settings**:
   - If using proxy, configure in Function App
   - Verify proxy allows Microsoft endpoints

**Prevention**:
- ✅ Allow outbound HTTPS to `login.microsoftonline.com`
- ✅ Configure NSGs to allow Microsoft identity endpoints
- ✅ Test network connectivity before deployment
- ✅ Monitor metadata endpoint accessibility
- ✅ Configure retry policies for transient failures

---

### TimeoutException (Metadata Request)

**Error Message**:
```
The request timed out while fetching OpenID Connect configuration
```

**Cause**:
- Slow network connection
- Microsoft endpoints experiencing issues
- Network congestion
- Overly aggressive timeout settings

**When It Occurs**:
- Network latency to Microsoft endpoints
- High load on identity services
- Geographic distance from Azure regions
- Network infrastructure issues

**Impact**:
- Authentication delays
- Intermittent failures
- Poor user experience

**Resolution**:

1. **Increase timeout** (if reasonable):
   ```csharp
   options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
       metadataAddress,
       new OpenIdConnectConfigurationRetriever(),
       new HttpDocumentRetriever { 
           MaxResponseContentBufferSize = 1024 * 1024,
           Timeout = TimeSpan.FromSeconds(30)  // Increase if needed
       });
   ```

2. **Check Microsoft service status**:
   - Visit: https://status.azure.com/
   - Check Azure AD service health

3. **Test from different network**:
   - Verify issue is not local network problem

**Prevention**:
- ✅ Use appropriate timeout values (10-30 seconds)
- ✅ Implement retry logic
- ✅ Monitor metadata endpoint response times
- ✅ Deploy to Azure regions close to identity services

---

## Authorization Exceptions

### UnauthorizedAccessException

**Error Message**:
```
User does not have required permissions to access this resource
```

**Cause**:
- User lacks required app roles
- User lacks required scopes
- Authorization policy not met
- Token doesn't contain expected claims

**When It Occurs**:
- Role-based access control (RBAC) denies access
- Scope-based authorization fails
- Custom authorization logic rejects request

**Impact**:
- HTTP 403 Forbidden
- Access denied to resource
- User sees permission error

**Resolution**:

1. **Check user roles**:
   ```kql
   // In Application Insights
   traces
   | where message contains "roles" and operation_Id == "operation-id-here"
   ```

2. **Decode token and verify roles**:
   ```json
   {
     "roles": ["ImageStorageTier.Pro", "Admin"],
     "scp": "access_as_user"
   }
   ```

3. **Assign required roles** (if appropriate):
   - Azure Portal → App registrations → Enterprise applications
   - Assign user to required app roles

**Prevention**:
- ✅ Clearly document required permissions
- ✅ Provide meaningful error messages
- ✅ Implement role assignment during user onboarding
- ✅ Test with users of different roles

---

## Quick Reference Table

| Exception | Error Code | HTTP Status | Typical Cause | Quick Fix |
|-----------|------------|-------------|---------------|-----------|
| SecurityTokenSignatureKeyNotFoundException | IDX10503 | 401 | Key rotation | Wait & retry (auto-refreshes) |
| SecurityTokenExpiredException | IDX10223 | 401 | Token expired | Acquire new token |
| SecurityTokenInvalidAudienceException | IDX10214 | 401 | Wrong audience | Fix AAD_AUDIENCE or SPA scope |
| SecurityTokenInvalidIssuerException | IDX10205 | 401 | Wrong tenant | Fix AAD_TENANT_ID |
| ArgumentNullException | N/A | 401 | Missing header | Add Authorization header |
| ArgumentException | N/A | 401 | Wrong format | Use "Bearer <token>" format |
| ConfigurationErrorsException | N/A | 401/500 | Missing config | Set AAD_TENANT_ID & AAD_AUDIENCE |
| HttpRequestException | N/A | 500 | Network issue | Check connectivity |
| UnauthorizedAccessException | N/A | 403 | Insufficient permissions | Assign required roles |

## Monitoring & Detection

### Application Insights Query

Monitor for all authentication exceptions:

```kql
union exceptions, traces
| where timestamp > ago(24h)
| where message contains "JWT" 
    or message contains "token"
    or message contains "IDX"
    or message contains "SecurityToken"
    or message contains "Authorization"
| extend ExceptionType = case(
    message contains "IDX10503", "Signing Key Not Found",
    message contains "IDX10223", "Token Expired",
    message contains "IDX10214", "Invalid Audience",
    message contains "IDX10205", "Invalid Issuer",
    message contains "ArgumentNull", "Missing Header",
    "Other Auth Exception"
)
| summarize Count = count() by ExceptionType, bin(timestamp, 1h)
| render timechart
```

## Related Documentation

- [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) - Troubleshooting guide
- [APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md) - KQL queries
- [ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md) - Configuration guide
- [ENTRA_CIAM_CONFIGURATION_GUIDE.md](ENTRA_CIAM_CONFIGURATION_GUIDE.md) - Setup guide

## External Resources

- [Microsoft Identity Platform Error Codes](https://learn.microsoft.com/en-us/entra/identity-platform/reference-error-codes)
- [JWT Token Validation](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens#validate-tokens)
- [Azure Monitor Alerts](https://learn.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-overview)

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-13 | GitHub Copilot | Initial comprehensive exceptions reference |

---

**Need Help?** See [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) for step-by-step troubleshooting procedures.
