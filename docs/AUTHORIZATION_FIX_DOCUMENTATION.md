# Authorization Fix Documentation

## Issue: 401 Unauthorized from Ink Stained Wretches APIs

### Problem Statement

All authenticated APIs in the InkStainedWretchFunctions project were returning `401 Unauthorized` errors in production environments, despite:

- JWT authentication working correctly in development
- CORS being properly configured
- Valid JWT tokens being provided by clients

### Root Cause Analysis

The issue was caused by **double authentication requirements** in the Azure Functions configuration:

1. **Azure Functions Host-Level Authentication**: Functions were configured with `AuthorizationLevel.Function`, which requires:
   - An Azure Functions host key (also called function key or x-functions-key)
   - This key must be passed either as:
     - Query parameter: `?code=<function-key>`
     - HTTP header: `x-functions-key: <function-key>`

2. **Application-Level JWT Authentication**: Functions also manually validate JWT Bearer tokens using:
   - `JwtAuthenticationHelper.ValidateJwtTokenAsync()`
   - Requires: `Authorization: Bearer <jwt-token>` header

**The Problem**: Clients needed to provide BOTH:

- Azure Functions host key (for `AuthorizationLevel.Function`)
- JWT Bearer token (for application-level authentication)

In production environments, only JWT tokens were being provided, causing the 401 errors at the Azure Functions host level before the JWT validation code could even execute.

### Solution

Changed `AuthorizationLevel.Function` to `AuthorizationLevel.Anonymous` for all endpoints that implement manual JWT token validation. This ensures:

- ✅ Only JWT Bearer tokens are required for authentication
- ✅ JWT validation logic remains fully functional and provides security
- ✅ No Azure Functions host keys needed by clients
- ✅ Consistent authentication experience across environments

### Files Modified

#### InkStainedWretchFunctions/DomainRegistrationFunction.cs

Changed 4 endpoints from `AuthorizationLevel.Function` to `AuthorizationLevel.Anonymous`:

- `CreateDomainRegistration` (POST /api/domain-registrations)
- `GetDomainRegistrations` (GET /api/domain-registrations)
- `GetDomainRegistrationById` (GET /api/domain-registrations/{registrationId})
- `UpdateDomainRegistration` (PUT /api/domain-registrations/{registrationId})

#### InkStainedWretchFunctions/PenguinRandomHouseFunction.cs

Changed 2 endpoints from `AuthorizationLevel.Function` to `AuthorizationLevel.Anonymous`:

- `SearchPenguinAuthors` (GET /api/penguin/authors/{authorName})
- `GetPenguinTitlesByAuthor` (GET /api/penguin/authors/{authorKey}/titles)

#### InkStainedWretchFunctions/AmazonProductFunction.cs

Changed 1 endpoint from `AuthorizationLevel.Function` to `AuthorizationLevel.Anonymous`:

- `SearchAmazonBooksByAuthor` (GET /api/amazon/books/author/{authorName})

### Already Correct Configurations

The following functions were already correctly configured and did NOT require changes:

#### Functions with `[Authorize]` Attribute (ASP.NET Core Authorization)

These use `AuthorizationLevel.Anonymous` with declarative authorization:

- `CreateTestimonial` (POST /api/testimonials)
- `UpdateTestimonial` (PUT /api/testimonials/{id})
- `DeleteTestimonial` (DELETE /api/testimonials/{id})

#### Functions with Manual JWT Validation Already Using Anonymous

These already used `AuthorizationLevel.Anonymous`:

- `GetAuthors` (GET /api/authors/{secondLevelDomain}/{topLevelDomain})
- `GetStateProvinces` (GET /api/stateprovinces/{culture})
- `GetStateProvincesByCountry` (GET /api/stateprovinces/{countryCode}/{culture})

### Security Implications

This change **does not reduce security**:

✅ **Security is maintained** because:

- JWT token validation is performed by `JwtAuthenticationHelper.ValidateJwtTokenAsync()`
- JWT tokens are validated against Microsoft Entra ID (Azure AD)
- Token signature, issuer, audience, and expiration are all verified
- Invalid or missing tokens result in 401 Unauthorized responses
- Token validation requires proper configuration of:
  - `AAD_TENANT_ID` (Azure AD tenant ID)
  - `AAD_AUDIENCE` (application client ID)

⚠️ **What changed**:

- Removed requirement for Azure Functions host keys
- Functions are now accessible without function keys
- JWT tokens provide the authentication and authorization

### Configuration Requirements

For JWT authentication to work properly, the following environment variables must be configured:

| Variable | Required | Description |
|----------|----------|-------------|
| `AAD_TENANT_ID` | ✅ Yes | Microsoft Entra ID tenant GUID |
| `AAD_AUDIENCE` | ✅ Yes | Application/Client ID for the API |
| `AAD_CLIENT_ID` | ⚪ Optional | Alternative to AAD_AUDIENCE |
| `OPEN_ID_CONNECT_METADATA_URL` | ⚪ Optional | Override default OpenID metadata URL |

**Default metadata URL** (if not specified):

```
https://login.microsoftonline.com/{AAD_TENANT_ID}/v2.0/.well-known/openid-configuration
```

### Testing

#### Local Development Testing

1. **Start the function app**:

   ```bash
   cd InkStainedWretchFunctions
   func start
   ```

2. **Obtain a JWT token** from Microsoft Entra ID

3. **Test an endpoint**:

   ```bash
   curl -X GET "https://localhost:7071/api/domain-registrations" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

4. **Expected behavior**:
   - ✅ Valid JWT: Returns data (200 OK)
   - ❌ Invalid JWT: Returns 401 Unauthorized with error message
   - ❌ Missing JWT: Returns 401 Unauthorized

#### Production Testing

After deployment to Azure:

```bash
curl -X GET "https://your-function-app.azurewebsites.net/api/domain-registrations" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Migration Guide for Other APIs

If other Azure Functions in the solution have the same issue, follow this pattern:

1. **Identify functions with `AuthorizationLevel.Function`**:

   ```bash
   grep -r "AuthorizationLevel.Function" --include="*.cs"
   ```

2. **Check if they manually validate JWT tokens**:

   ```csharp
   var (user, errorResult) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, ...);
   ```

3. **If yes, change to `AuthorizationLevel.Anonymous`**:

   ```csharp
   // Before:
   [HttpTrigger(AuthorizationLevel.Function, "get", Route = "...")] 
   
   // After:
   [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "...")]
   ```

4. **If no JWT validation, consider adding it or keep function-level security**

### When to Use Each Authorization Level

| Authorization Level | Use Case | Security Provided By |
|---------------------|----------|---------------------|
| `AuthorizationLevel.Anonymous` | Public endpoints or endpoints with custom auth logic | Your custom authentication code (e.g., JWT validation) |
| `AuthorizationLevel.Function` | Internal/admin endpoints requiring Azure Functions keys | Azure Functions host keys |
| `AuthorizationLevel.Admin` | System admin operations | Azure Functions master key |

### Best Practices

1. **Don't mix authorization levels**: If using JWT tokens, use `AuthorizationLevel.Anonymous` and handle all auth in code
2. **Validate tokens on every protected endpoint**: Always call `JwtAuthenticationHelper.ValidateJwtTokenAsync()`
3. **Log authentication failures**: Include debugging information for troubleshooting
4. **Configure environment variables properly**: Ensure `AAD_TENANT_ID` and `AAD_AUDIENCE` are set
5. **Use declarative authorization when possible**: Consider `[Authorize]` attribute for cleaner code

### References

- [Azure Functions HTTP triggers - Authorization levels](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=isolated-process%2Cfunctionsv2&pivots=programming-language-csharp#http-auth)
- [Microsoft identity platform and OAuth 2.0 authorization code flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow)
- [Validate tokens from Microsoft identity platform](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens#validate-tokens)

---

**Document Created**: 2024-12-23  
**Issue**: #401 Unauthorized from all the Ink Stained Wretches API's  
**Status**: ✅ Resolved
