# Microsoft Entra ID CIAM Configuration Guide

## Overview

This guide provides complete, step-by-step instructions for configuring Microsoft Entra ID Customer Identity Access Management (CIAM) for the InkStainedWretches API platform. CIAM is optimized for customer-facing applications and provides a simplified configuration experience compared to traditional Azure AD workforce tenants.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Create Entra ID CIAM Tenant](#create-entra-id-ciam-tenant)
3. [Register Applications](#register-applications)
4. [Configure API Application](#configure-api-application)
5. [Configure SPA Application](#configure-spa-application)
6. [Configure Authentication in Azure Functions](#configure-authentication-in-azure-functions)
7. [Testing & Validation](#testing--validation)
8. [Troubleshooting](#troubleshooting)

## Prerequisites

Before starting configuration, ensure you have:

- ✅ Azure subscription with appropriate permissions
- ✅ Access to create and manage Azure resources
- ✅ Understanding of OAuth 2.0 and OpenID Connect concepts
- ✅ Administrative access to your domain (for custom domains, optional)
- ✅ Development environment set up with .NET 9.0 SDK

## Create Entra ID CIAM Tenant

### Why CIAM?

Microsoft Entra ID CIAM (Customer Identity Access Management) is purpose-built for customer-facing applications. Unlike workforce tenants, CIAM offers:

- ✅ **Simplified user management** - Self-service sign-up and profile management
- ✅ **Optimized sign-in experience** - Streamlined authentication flows
- ✅ **Built-in security** - Automatic bot protection and risk detection
- ✅ **Scalability** - Designed for millions of consumer identities
- ✅ **Customizable branding** - White-label sign-in experiences

### Step 1: Create the CIAM Tenant

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Search for "Microsoft Entra ID"
3. Select **Create a tenant**
4. Choose **Customer Identity Access Management (CIAM)**
5. Provide tenant configuration:
   - **Organization name**: `Ink Stained Wretches` (or your organization name)
   - **Initial domain name**: `inkstainedwretches` (becomes inkstainedwretches.onmicrosoft.com)
   - **Location/Data residency**: Choose your preferred region (e.g., United States, Europe)
6. Review and create the tenant
7. Wait for tenant provisioning (typically 1-2 minutes)

### Step 2: Switch to the New Tenant

1. In Azure Portal, click on your profile (top right)
2. Select **Switch directory**
3. Choose your new CIAM tenant
4. Verify you're in the correct tenant by checking the directory name

## Register Applications

You need TWO app registrations:
1. **API Application** - Represents your Azure Functions backend
2. **SPA Application** - Represents your Single Page Application (front-end)

### Register the API Application

This represents your Azure Functions backend API.

#### Step 1: Create App Registration

1. Navigate to **Microsoft Entra ID** → **App registrations**
2. Click **New registration**
3. Configure the registration:
   - **Name**: `InkStainedWretches API`
   - **Supported account types**: 
     - Choose **Accounts in this organizational directory only (Single tenant)**
     - For CIAM, select **Accounts in this tenant directory**
   - **Redirect URI**: Leave blank (not needed for APIs)
4. Click **Register**

#### Step 2: Record Application Details

After creation, record these values (you'll need them later):

| Value | Location | Environment Variable |
|-------|----------|---------------------|
| Application (client) ID | Overview page | `AAD_AUDIENCE` or `AAD_CLIENT_ID` |
| Directory (tenant) ID | Overview page | `AAD_TENANT_ID` |

#### Step 3: Configure API Scopes (Expose an API)

1. In your API app registration, go to **Expose an API**
2. Click **Add a scope** (if Application ID URI not set, click **Set** first)
   - Application ID URI will be: `api://{client-id}`
   - Or customize: `api://inkstainedwretches-api`
3. Add the default scope:
   - **Scope name**: `access_as_user`
   - **Who can consent**: **Admins and users**
   - **Admin consent display name**: `Access API as user`
   - **Admin consent description**: `Allows the app to access the API on behalf of the signed-in user`
   - **User consent display name**: `Access your data`
   - **User consent description**: `Allows the app to access your data on your behalf`
   - **State**: **Enabled**
4. Click **Add scope**

#### Step 4: Add App Roles (Optional - for RBAC)

If using role-based access control (e.g., ImageStorageTier roles):

1. Go to **App roles** → **Create app role**
2. Add roles as needed:

**Example: Image Storage Tier Roles**

| Display name | Value | Description | Member types |
|-------------|-------|-------------|--------------|
| Starter Tier | `ImageStorageTier.Starter` | Access to starter image storage tier | Users/Groups |
| Pro Tier | `ImageStorageTier.Pro` | Access to pro image storage tier | Users/Groups |
| Enterprise Tier | `ImageStorageTier.Enterprise` | Access to enterprise image storage tier | Users/Groups |

3. Users assigned to roles will receive these in their JWT token's `roles` claim

### Register the SPA Application

This represents your React/Angular/Vue Single Page Application.

#### Step 1: Create App Registration

1. Navigate to **Microsoft Entra ID** → **App registrations**
2. Click **New registration**
3. Configure the registration:
   - **Name**: `Ink Stained Wretches SPA` (or your SPA name)
   - **Supported account types**: 
     - **Accounts in this organizational directory only**
   - **Redirect URI**: 
     - Platform: **Single-page application (SPA)**
     - URI: `https://inkstainedwretches.com/auth-callback/`
     - For local development, add the appropriate localhost URL based on your frontend framework:
     
     | Framework/Platform | Default Port | Redirect URI |
     |-------------------|--------------|--------------|
     | Vite | 5173 | `http://localhost:5173/auth-callback/` |
     | Create React App | 3000 | `http://localhost:3000/auth-callback/` |
     | Next.js | 3000 | `http://localhost:3000/auth-callback/` |
     | Angular | 4200 | `http://localhost:4200/auth-callback/` |
     | Vue CLI | 8080 | `http://localhost:8080/auth-callback/` |
     | Custom/Other | varies | `http://localhost:{PORT}/auth-callback/` |
4. Click **Register**

#### Step 2: Record Application Details

Record these values for your SPA configuration:

| Value | Location | Purpose |
|-------|----------|---------|
| Application (client) ID | Overview page | MSAL configuration (`clientId`) |
| Directory (tenant) ID | Overview page | MSAL authority URL |

#### Step 3: Configure Authentication

1. Go to **Authentication**
2. Under **Single-page application** section, verify redirect URIs:
   - Production: `https://inkstainedwretches.com/auth-callback/`
   - Development: Add the appropriate localhost URL for your frontend framework (see registration table above)
3. Under **Implicit grant and hybrid flows**:
   - ✅ **ID tokens** (for sign-in)
   - ⚪ **Access tokens** (NOT needed for SPA - MSAL handles this)
4. **Allow public client flows**: **No**
5. Click **Save**

#### Step 4: Configure API Permissions

Your SPA needs permission to call your API:

1. Go to **API permissions**
2. Click **Add a permission**
3. Select **My APIs** tab
4. Choose **InkStainedWretches API** (your API app registration)
5. Select **Delegated permissions**
6. Check **access_as_user** scope
7. Click **Add permissions**
8. (Optional) Click **Grant admin consent** if you want to pre-consent for all users

**Note**: In CIAM, user claims like `email` and `preferred_username` are automatically included in tokens based on the user profile. Unlike Azure AD B2C, CIAM does not require explicit token configuration for standard claims.

## Configure API Application

### Configure in Azure Portal

#### Enable ID Tokens

1. In your API app registration, go to **Authentication**
2. Under **Implicit grant and hybrid flows**:
   - ✅ **ID tokens** (for hybrid flows)
3. Click **Save**

**Note**: CIAM token lifetimes are managed at the tenant level with sensible defaults optimized for customer-facing applications:
- **Access token**: 60-90 minutes (default)
- **Refresh token**: 14 days (default)  
- **ID token**: 60 minutes (default)

Unlike traditional Azure AD or Azure AD B2C, CIAM does not expose token lifetime configuration through the "Token configuration" UI in individual app registrations. Token policies are simplified and managed centrally for the entire CIAM tenant.

### Configure Service Principal for Automation (Optional)

If you need to manage roles programmatically (e.g., using EntraIdRoleManager tool):

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Add description: `EntraIdRoleManager Service Principal`
4. Choose expiration: `24 months` (maximum)
5. Click **Add**
6. **IMMEDIATELY COPY THE SECRET VALUE** - it won't be shown again
7. Store securely in Azure Key Vault or user secrets

#### Required API Permissions for Service Principal

To manage app roles and assignments:

1. Go to **API permissions** (in the API app registration)
2. Add **Microsoft Graph** permissions:
   - `Application.ReadWrite.All` (Type: Application) - To create/update app roles
   - `AppRoleAssignment.ReadWrite.All` (Type: Application) - To assign users to roles
   - `Directory.Read.All` (Type: Application) - To read user information
3. Click **Grant admin consent for {your tenant}**

**Security Note**: Only grant these permissions to service principals that absolutely need them, and rotate secrets regularly.

## Configure SPA Application

### MSAL Configuration

In your React/Angular/Vue application, configure MSAL (Microsoft Authentication Library):

#### React Example (with @azure/msal-react)

```javascript
import { PublicClientApplication, LogLevel } from "@azure/msal-browser";

export const msalConfig = {
  auth: {
    clientId: "{YOUR_SPA_CLIENT_ID}",
    authority: "https://login.microsoftonline.com/{YOUR_TENANT_ID}",
    redirectUri: "https://inkstainedwretches.com/auth-callback/",
    // CIAM-specific
    knownAuthorities: ["login.microsoftonline.com"]
  },
  cache: {
    cacheLocation: "localStorage", // "sessionStorage" for more security
    storeAuthStateInCookie: false // Set to true if supporting IE11
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Info,
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        console.log(message);
      }
    }
  }
};

// Scopes for API access
export const loginRequest = {
  scopes: ["api://{YOUR_API_CLIENT_ID}/access_as_user"]
};

export const msalInstance = new PublicClientApplication(msalConfig);
```

#### Key Configuration Notes

- **authority**: Use your specific tenant ID, not `common` or `organizations`
- **scopes**: Must match the scope you exposed in the API app registration
- **redirectUri**: Must match exactly what you configured in Azure Portal
- **cacheLocation**: `localStorage` for persistence across sessions, `sessionStorage` for single session

### Acquire Access Tokens

When calling the API:

```javascript
import { msalInstance, loginRequest } from './msalConfig';

async function callApi() {
  try {
    // Try to acquire token silently (from cache)
    const account = msalInstance.getAllAccounts()[0];
    const tokenResponse = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account: account
    });
    
    const accessToken = tokenResponse.accessToken;
    
    // Call API with token
    const response = await fetch('https://your-api.azurewebsites.net/api/endpoint', {
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      }
    });
    
    return await response.json();
  } catch (error) {
    if (error.name === 'InteractionRequiredAuthError') {
      // Token expired or needs user interaction
      const tokenResponse = await msalInstance.acquireTokenPopup(loginRequest);
      const accessToken = tokenResponse.accessToken;
      // Retry API call with new token
    }
    throw error;
  }
}
```

## Configure Authentication in Azure Functions

### Environment Variables

Configure these environment variables in all Azure Function apps:

#### Required Variables

| Variable | Value | Where to Find |
|----------|-------|---------------|
| `AAD_TENANT_ID` | Your tenant GUID | Azure Portal → Microsoft Entra ID → Overview → Tenant ID |
| `AAD_AUDIENCE` | API Client ID | Azure Portal → App registrations → API App → Application (client) ID |

#### Optional Variables

| Variable | Value | Purpose |
|----------|-------|---------|
| `AAD_CLIENT_ID` | API Client ID | Alternative to AAD_AUDIENCE |
| `OPEN_ID_CONNECT_METADATA_URL` | Custom metadata URL | Override default metadata endpoint |

### Configure in Azure Portal

For each Function App:

1. Navigate to your Function App in Azure Portal
2. Go to **Configuration** → **Application settings**
3. Add new application settings:
   - Name: `AAD_TENANT_ID`, Value: `{your-tenant-id}`
   - Name: `AAD_AUDIENCE`, Value: `{your-api-client-id}`
4. Click **Save**
5. Restart the Function App if needed

### Configure Local Development

#### Using User Secrets (Recommended)

For each Azure Function project:

```bash
cd ImageAPI
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id-here"
dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id-here"

cd ../InkStainedWretchFunctions
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id-here"
dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id-here"

cd ../InkStainedWretchStripe
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id-here"
dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id-here"
```

#### Using local.settings.json

Alternatively, add to `local.settings.json` (⚠️ **DO NOT COMMIT THIS FILE**):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AAD_TENANT_ID": "your-tenant-id-here",
    "AAD_AUDIENCE": "your-api-client-id-here"
  }
}
```

### Verify Program.cs Configuration

Each Function App should have JWT authentication configured in `Program.cs`:

```csharp
// ImageAPI/Program.cs, InkStainedWretchFunctions/Program.cs, InkStainedWretchStripe/Program.cs

var tenantId = configuration["AAD_TENANT_ID"];
var audience = configuration["AAD_AUDIENCE"] ?? configuration["AAD_CLIENT_ID"];

if (!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(audience))
{
    var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = audience,
                ValidIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0"
            };

            // ✅ Critical for handling key rotation
            options.RefreshOnIssuerKeyNotFound = true;

            // ✅ Configure automatic key refresh
            var metadataAddress = $"{authority}/.well-known/openid-configuration";
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever())
            {
                AutomaticRefreshInterval = TimeSpan.FromHours(6),
                RefreshInterval = TimeSpan.FromMinutes(30)
            };
        });

    builder.Services.AddAuthorization();
}
```

**Key Points:**
- ✅ `RefreshOnIssuerKeyNotFound = true` - Essential for handling signing key rotation
- ✅ `AutomaticRefreshInterval` - Proactively refreshes keys every 6 hours
- ✅ `RefreshInterval` - Rate limits metadata endpoint requests (30 minutes minimum)

## Testing & Validation

### Step 1: Test SPA Authentication

1. Navigate to your SPA application
2. Click sign-in
3. You should be redirected to Entra ID CIAM sign-in page
4. Sign in with a test user
5. After successful sign-in, you should be redirected back to your app
6. Check browser developer tools → Application → Local Storage → MSAL cache
7. Verify tokens are present

### Step 2: Test API Authentication

#### Using cURL

```bash
# Replace with your actual values
TENANT_ID="your-tenant-id"
CLIENT_ID="your-api-client-id"
FUNCTION_URL="https://your-function-app.azurewebsites.net/api/domain-registrations"

# First, get an access token (you'll need to get this from your SPA or use device code flow)
ACCESS_TOKEN="your-access-token-here"

# Test the API endpoint
curl -X GET "$FUNCTION_URL" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json"
```

**Expected Results:**
- ✅ **Valid token**: HTTP 200 OK with data
- ❌ **Invalid token**: HTTP 401 Unauthorized with error message
- ❌ **Missing token**: HTTP 401 Unauthorized

#### Using Postman

1. Create a new request
2. Set method and URL
3. Go to **Authorization** tab
4. Type: **OAuth 2.0**
5. Configure token request:
   - **Grant Type**: Authorization Code (with PKCE)
   - **Auth URL**: `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/authorize`
   - **Access Token URL**: `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token`
   - **Client ID**: Your SPA client ID
   - **Scope**: `api://{api-client-id}/access_as_user`
6. Click **Get New Access Token**
7. Sign in when prompted
8. Use the token in your request

### Step 3: Verify JWT Token Contents

Use https://jwt.ms to decode your access token and verify:

✅ **Issuer (`iss`)**: `https://login.microsoftonline.com/{tenant-id}/v2.0`
✅ **Audience (`aud`)**: Your API client ID
✅ **Expiration (`exp`)**: Future timestamp
✅ **Issued At (`iat`)**: Past timestamp
✅ **Subject (`sub`)** or **Object ID (`oid`)**: User identifier
✅ **Roles (`roles`)**: Any app roles assigned to the user
✅ **Scopes (`scp`)**: `access_as_user`

### Step 4: Monitor Application Insights

Check Application Insights for authentication events:

```kql
// Authentication success
traces
| where message contains "User authenticated successfully"
| order by timestamp desc
| take 10

// Authentication failures
traces
| where message contains "JWT validation failed" or message contains "Invalid token"
| order by timestamp desc
| take 10
```

## Troubleshooting

### Issue: SPA Not Redirecting to Sign-In

**Symptoms:**
- Clicking sign-in does nothing
- No redirect to Microsoft sign-in page

**Resolution:**
1. Check browser console for errors
2. Verify `msalConfig.auth.clientId` matches SPA app registration
3. Verify `msalConfig.auth.authority` uses correct tenant ID
4. Check `redirectUri` matches app registration exactly (including trailing slashes)
5. Verify no ad-blockers or privacy extensions are blocking redirects

### Issue: "AADSTS50011: Invalid redirect URI"

**Symptoms:**
- Error after sign-in
- Redirect fails

**Resolution:**
1. Go to SPA app registration → Authentication
2. Verify redirect URI matches exactly (case-sensitive, trailing slashes matter)
3. Ensure URI is registered under **Single-page application** platform
4. For local development, ensure the appropriate localhost redirect URI for your frontend framework is added (see framework-specific ports in the registration section above)

### Issue: API Returns 401 Unauthorized

**Symptoms:**
- Valid user signed in to SPA
- API calls return 401

**Resolution:**
1. Verify `AAD_TENANT_ID` and `AAD_AUDIENCE` are set in Function App configuration
2. Verify token is being sent: `Authorization: Bearer <token>`
3. Decode token at https://jwt.ms and verify `aud` claim matches `AAD_AUDIENCE`
4. Check Application Insights logs for specific error
5. See [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) for detailed troubleshooting

### Issue: "IDX10503: Signature validation failed"

**Symptoms:**
- `SecurityTokenSignatureKeyNotFoundException`
- Token signature cannot be validated

**Resolution:**
This occurs during signing key rotation. Verify:
1. `RefreshOnIssuerKeyNotFound = true` is set in Program.cs
2. ConfigurationManager is configured with refresh intervals
3. Function App has internet access to `login.microsoftonline.com`
4. See [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) for detailed resolution

### Issue: Token Contains Wrong Audience

**Symptoms:**
- Token validation fails
- `aud` claim doesn't match expected value

**Resolution:**
1. In SPA, verify you're requesting correct scope: `api://{api-client-id}/access_as_user`
2. Verify API app registration Application ID URI is set correctly
3. Ensure SPA has API permission granted
4. In API, verify `AAD_AUDIENCE` matches the `aud` claim in token

### Common Mistakes to Avoid

❌ **Using `common` authority** - Always use your specific tenant ID
❌ **Mixing client IDs** - Keep SPA client ID and API client ID separate
❌ **Not granting API permissions** - SPA must have delegated permission to API
❌ **Forgetting redirect URI** - Must match exactly in app registration
❌ **Not handling token refresh** - Implement acquireTokenSilent with fallback to acquireTokenPopup
❌ **Hardcoding secrets** - Never commit client secrets, use Azure Key Vault or user secrets
❌ **Not setting RefreshOnIssuerKeyNotFound** - Critical for production reliability

## Next Steps

After completing configuration:

1. ✅ Review [AUTHENTICATION_AUTHORIZATION_SEPARATION.md](AUTHENTICATION_AUTHORIZATION_SEPARATION.md) to understand the architecture
2. ✅ Read [MSAL_CIAM_BEST_PRACTICES.md](MSAL_CIAM_BEST_PRACTICES.md) for SPA implementation best practices
3. ✅ Set up monitoring with [APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md)
4. ✅ Configure role-based access control if needed (see [../MIGRATION_GUIDE_ENTRA_ID_ROLES.md](../MIGRATION_GUIDE_ENTRA_ID_ROLES.md))
5. ✅ Review [AUTHENTICATION_LOGGING.md](AUTHENTICATION_LOGGING.md) for observability

## Additional Resources

- [Microsoft Entra ID CIAM Documentation](https://learn.microsoft.com/en-us/entra/external-id/customers/)
- [MSAL.js Documentation](https://learn.microsoft.com/en-us/entra/identity-platform/msal-js-initializing-client-applications)
- [JWT Token Reference](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens)
- [OAuth 2.0 Authorization Code Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow)

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-13 | GitHub Copilot | Initial CIAM-focused configuration guide |

---

**Need Help?** Refer to [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) for detailed troubleshooting steps.
