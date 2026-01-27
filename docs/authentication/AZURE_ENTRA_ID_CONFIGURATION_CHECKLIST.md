# Azure Entra ID Configuration Checklist

## Overview

This checklist provides a step-by-step guide for configuring Microsoft Entra ID authentication for the OnePageAuthor API platform. Use this to ensure all configuration steps are completed correctly.

## Pre-Configuration

### Prerequisites

- [ ] Azure subscription with appropriate permissions
- [ ] Access to create App registrations in Entra ID
- [ ] Access to Azure Function Apps
- [ ] Understanding of OAuth 2.0 and OpenID Connect basics
- [ ] .NET 9.0 SDK installed (for local development)

### Documentation Review

- [ ] Read [ENTRA_CIAM_CONFIGURATION_GUIDE.md](ENTRA_CIAM_CONFIGURATION_GUIDE.md)
- [ ] Review [ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md)
- [ ] Bookmark [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md)

## Phase 1: Create Entra ID CIAM Tenant (Optional)

If using dedicated CIAM tenant:

- [ ] Navigate to Azure Portal → Microsoft Entra ID
- [ ] Click "Create a tenant"
- [ ] Choose "Customer Identity Access Management (CIAM)"
- [ ] Enter organization name: `_______________`
- [ ] Enter initial domain: `_______________.onmicrosoft.com`
- [ ] Choose data residency location: `_______________`
- [ ] Review and create tenant
- [ ] Switch to new tenant in Azure Portal
- [ ] Verify tenant by checking directory name in top-right

**Note**: If using existing workforce tenant, skip to Phase 2.

## Phase 2: Register API Application

### Create App Registration

- [ ] Navigate to Microsoft Entra ID → App registrations
- [ ] Click "New registration"
- [ ] Enter name: `OnePageAuthor API` (or your API name)
- [ ] Choose account type: "Accounts in this organizational directory only"
- [ ] Leave Redirect URI empty
- [ ] Click "Register"

### Record API Application Details

- [ ] Copy Application (client) ID: `_______________`
- [ ] Copy Directory (tenant) ID: `_______________`
- [ ] Store securely (password manager or Azure Key Vault)

### Configure API Scopes

- [ ] Go to "Expose an API"
- [ ] Click "Set" for Application ID URI (use default `api://{client-id}`)
- [ ] Click "Add a scope"
- [ ] Scope name: `access_as_user`
- [ ] Who can consent: "Admins and users"
- [ ] Admin consent display name: `Access API as user`
- [ ] Admin consent description: `Allows the app to access the API on behalf of the signed-in user`
- [ ] User consent display name: `Access your data`
- [ ] User consent description: `Allows the app to access your data on your behalf`
- [ ] State: Enabled
- [ ] Click "Add scope"

### Add App Roles (Optional - for RBAC)

If using role-based access control:

- [ ] Go to "App roles"
- [ ] Click "Create app role" for each role needed

**Example roles for Image Storage Tiers**:

- [ ] Role 1:
  - Display name: `Starter Tier`
  - Value: `ImageStorageTier.Starter`
  - Description: `Access to starter image storage tier`
  - Member types: Users/Groups
  - Enabled: Yes

- [ ] Role 2:
  - Display name: `Pro Tier`
  - Value: `ImageStorageTier.Pro`
  - Description: `Access to pro image storage tier`
  - Member types: Users/Groups
  - Enabled: Yes

- [ ] Role 3:
  - Display name: `Enterprise Tier`
  - Value: `ImageStorageTier.Enterprise`
  - Description: `Access to enterprise image storage tier`
  - Member types: Users/Groups
  - Enabled: Yes

## Phase 3: Register SPA Application

### Create SPA App Registration

- [ ] Navigate to Microsoft Entra ID → App registrations
- [ ] Click "New registration"
- [ ] Enter name: `Ink Stained Wretches SPA` (or your SPA name)
- [ ] Choose account type: "Accounts in this organizational directory only"
- [ ] Platform: "Single-page application (SPA)"
- [ ] Redirect URI: `https://inkstainedwretches.com/.auth/login/aad/callback`
- [ ] Click "Register"

### Record SPA Application Details

- [ ] Copy Application (client) ID: `_______________`
- [ ] Verify Directory (tenant) ID matches API registration
- [ ] Store securely

### Configure SPA Authentication

- [ ] Go to "Authentication"
- [ ] Under "Single-page application", verify redirect URIs:
  - [ ] Production: `https://inkstainedwretches.com/.auth/login/aad/callback`
  - [ ] Development: `http://localhost:4280/.auth/login/aad/callback`
- [ ] Under "Implicit grant and hybrid flows":
  - [ ] Check "ID tokens"
  - [ ] Leave "Access tokens" unchecked
- [ ] Allow public client flows: No
- [ ] Click "Save"

### Add API Permissions

- [ ] Go to "API permissions"
- [ ] Click "Add a permission"
- [ ] Select "My APIs" tab
- [ ] Choose your API app registration (OnePageAuthor API)
- [ ] Select "Delegated permissions"
- [ ] Check `access_as_user` scope
- [ ] Click "Add permissions"
- [ ] (Optional) Click "Grant admin consent" to pre-consent for all users

## Phase 4: Configure Azure Function Apps

### Set Environment Variables

For each Function App (ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe):

#### Azure Portal Configuration

- [ ] Navigate to Function App in Azure Portal
- [ ] Go to Configuration → Application settings
- [ ] Add `AAD_TENANT_ID`:
  - Name: `AAD_TENANT_ID`
  - Value: `_______________` (from API app registration)
- [ ] Add `AAD_AUDIENCE`:
  - Name: `AAD_AUDIENCE`
  - Value: `_______________` (API client ID from API app registration)
- [ ] Click "Save"
- [ ] Wait for configuration to apply
- [ ] Restart Function App (if needed)

#### Repeat for All Function Apps

- [ ] ImageAPI configured
- [ ] InkStainedWretchFunctions configured
- [ ] InkStainedWretchStripe configured

### Verify Configuration in Logs

- [ ] Navigate to Function App → Log stream
- [ ] Look for startup log:

  ```
  JWT authentication configured with tenant: {tenant-id}, audience: {audience}
  ```

- [ ] If warning appears, verify environment variables are set correctly

## Phase 5: Configure Local Development

### User Secrets (Recommended)

For each project:

- [ ] ImageAPI:

  ```bash
  cd ImageAPI
  dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"
  dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id"
  dotnet user-secrets list  # Verify
  ```

- [ ] InkStainedWretchFunctions:

  ```bash
  cd InkStainedWretchFunctions
  dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"
  dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id"
  dotnet user-secrets list  # Verify
  ```

- [ ] InkStainedWretchStripe:

  ```bash
  cd InkStainedWretchStripe
  dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"
  dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id"
  dotnet user-secrets list  # Verify
  ```

### Verify local.settings.json is in .gitignore

- [ ] Check `.gitignore` includes `local.settings.json`
- [ ] Never commit `local.settings.json` with secrets

## Phase 6: Configure SPA Application (MSAL)

### Install MSAL Package

- [ ] Install MSAL library:

  ```bash
  npm install @azure/msal-browser @azure/msal-react
  ```

### Create MSAL Configuration

- [ ] Create `src/authConfig.js` (or equivalent):

  ```javascript
  export const msalConfig = {
    auth: {
      clientId: "your-spa-client-id",
      authority: "https://login.microsoftonline.com/your-tenant-id",
      redirectUri: "https://inkstainedwretches.com/.auth/login/aad/callback"
    },
    cache: {
      cacheLocation: "localStorage",
      storeAuthStateInCookie: false
    }
  };

  export const loginRequest = {
    scopes: ["api://your-api-client-id/access_as_user"]
  };
  ```

- [ ] Replace placeholders with actual values
- [ ] Verify scope format: `api://{api-client-id}/access_as_user`

### Initialize MSAL

- [ ] Initialize MSAL in app entry point
- [ ] Wrap app with MsalProvider (React) or equivalent
- [ ] Test sign-in flow

## Phase 7: Verify Program.cs Configuration

For each Function App project:

### Check JWT Authentication Configuration

- [ ] Open `Program.cs`
- [ ] Verify configuration reads environment variables:

  ```csharp
  var tenantId = configuration["AAD_TENANT_ID"];
  var audience = configuration["AAD_AUDIENCE"] ?? configuration["AAD_CLIENT_ID"];
  ```

- [ ] Verify JWT Bearer authentication is configured:

  ```csharp
  builder.Services
      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options => { ... });
  ```

- [ ] Verify RefreshOnIssuerKeyNotFound is enabled:

  ```csharp
  options.RefreshOnIssuerKeyNotFound = true;
  ```

- [ ] Verify ConfigurationManager with refresh intervals:

  ```csharp
  options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
      metadataAddress,
      new OpenIdConnectConfigurationRetriever(),
      new HttpDocumentRetriever())
  {
      AutomaticRefreshInterval = TimeSpan.FromHours(6),
      RefreshInterval = TimeSpan.FromMinutes(30)
  };
  ```

### Verify for All Function Apps

- [ ] ImageAPI/Program.cs
- [ ] InkStainedWretchFunctions/Program.cs
- [ ] InkStainedWretchStripe/Program.cs

## Phase 8: Testing

### Test Authentication Flow

#### Test SPA Sign-In

- [ ] Navigate to SPA application
- [ ] Click sign-in button
- [ ] Verify redirect to Microsoft sign-in page
- [ ] Sign in with test user
- [ ] Verify redirect back to application
- [ ] Check browser local storage for MSAL tokens

#### Test API Authentication

- [ ] Obtain access token from SPA
- [ ] Test authenticated endpoint:

  ```bash
  curl -X GET "https://your-function.azurewebsites.net/api/domain-registrations" \
    -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
  ```

- [ ] Expected: HTTP 200 OK with data

#### Test Authentication Failures

- [ ] Test without token:

  ```bash
  curl -X GET "https://your-function.azurewebsites.net/api/domain-registrations"
  ```

  - [ ] Expected: HTTP 401 Unauthorized

- [ ] Test with invalid token:

  ```bash
  curl -X GET "https://your-function.azurewebsites.net/api/domain-registrations" \
    -H "Authorization: Bearer invalid-token"
  ```

  - [ ] Expected: HTTP 401 Unauthorized

### Validate JWT Token

- [ ] Copy access token from browser (local storage or network tab)
- [ ] Go to <https://jwt.ms>
- [ ] Paste token
- [ ] Verify claims:
  - [ ] `aud` matches `AAD_AUDIENCE`
  - [ ] `iss` matches `https://login.microsoftonline.com/{AAD_TENANT_ID}/v2.0`
  - [ ] `exp` is in future
  - [ ] `scp` contains `access_as_user`
  - [ ] `roles` contains expected roles (if using RBAC)

## Phase 9: Monitoring Setup

### Configure Application Insights

- [ ] Verify Application Insights is connected to Function Apps
- [ ] Note Instrumentation Key / Connection String

### Create Authentication Dashboard

- [ ] Navigate to Application Insights → Workbooks
- [ ] Create new workbook: "Authentication Monitoring"
- [ ] Add sections from [APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md):
  - [ ] Authentication Success Rate
  - [ ] Recent Failures
  - [ ] JWT Validation Errors
  - [ ] User Activity

### Set Up Alerts

- [ ] Create alert: High Authentication Failure Rate
  - Threshold: > 20% failure rate
  - Action: Email DevOps team
- [ ] Create alert: Signing Key Rotation Issues
  - Threshold: > 10 errors in 10 minutes
  - Action: Email + Create incident
- [ ] Create alert: Brute Force Detection
  - Threshold: > 20 failures from single IP
  - Action: Email Security team

## Phase 10: Documentation

### Document Configuration

- [ ] Record all client IDs and tenant IDs in secure location
- [ ] Document redirect URIs for each environment
- [ ] Document API scopes and their purposes
- [ ] Document app roles and assignment process

### Update Project Documentation

- [ ] Update README.md with authentication setup steps
- [ ] Add environment variables to configuration guide
- [ ] Document troubleshooting steps
- [ ] Add links to this checklist

### Create Runbooks

- [ ] Create runbook for adding new users
- [ ] Create runbook for assigning roles
- [ ] Create runbook for troubleshooting authentication issues
- [ ] Create runbook for rotating client secrets (if using service principals)

## Phase 11: Security Review

### Validate Security Configuration

- [ ] Verify HTTPS is enforced for all endpoints
- [ ] Verify tokens are not logged
- [ ] Verify client secrets (if any) are stored securely
- [ ] Verify no secrets in source control
- [ ] Check `.gitignore` includes sensitive files

### Review App Registrations

- [ ] API app registration has minimal permissions
- [ ] SPA app registration has only required API permissions
- [ ] No unnecessary redirect URIs configured
- [ ] Token lifetime is appropriate (default is good)

### Test with Security Tools

- [ ] Test with OWASP ZAP or similar
- [ ] Verify no JWT tokens in URL parameters
- [ ] Verify no credentials in error messages
- [ ] Check for proper CORS configuration

## Phase 12: Deploy & Verify

### Deployment

- [ ] Deploy Function Apps to staging environment
- [ ] Test authentication in staging
- [ ] Deploy to production
- [ ] Verify configuration in production

### Post-Deployment Verification

- [ ] Check Application Insights for startup logs
- [ ] Verify no authentication errors in logs
- [ ] Test with real user accounts
- [ ] Monitor for 24 hours

### Rollback Plan

- [ ] Document previous configuration
- [ ] Have rollback commands ready
- [ ] Test rollback procedure in staging

## Completion Checklist

### All Phases Complete

- [ ] Phase 1: Entra ID tenant (if applicable)
- [ ] Phase 2: API app registration
- [ ] Phase 3: SPA app registration
- [ ] Phase 4: Function App configuration
- [ ] Phase 5: Local development setup
- [ ] Phase 6: SPA/MSAL configuration
- [ ] Phase 7: Program.cs verification
- [ ] Phase 8: Testing
- [ ] Phase 9: Monitoring
- [ ] Phase 10: Documentation
- [ ] Phase 11: Security review
- [ ] Phase 12: Deployment

### Final Verification

- [ ] All Function Apps authenticate successfully
- [ ] SPA sign-in works correctly
- [ ] API calls with valid tokens succeed
- [ ] API calls without tokens fail appropriately
- [ ] Monitoring and alerts are working
- [ ] Documentation is complete and accessible
- [ ] Team trained on troubleshooting procedures

## Troubleshooting

If you encounter issues during configuration:

- [ ] Check [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md)
- [ ] Review [ENTRA_ID_EXCEPTIONS_REFERENCE.md](ENTRA_ID_EXCEPTIONS_REFERENCE.md)
- [ ] Use queries from [APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md)
- [ ] Verify environment variables in [ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md)

## Related Documentation

- [ENTRA_CIAM_CONFIGURATION_GUIDE.md](ENTRA_CIAM_CONFIGURATION_GUIDE.md) - Detailed configuration guide
- [ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md) - Environment variable reference
- [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) - Troubleshooting guide
- [APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md) - Monitoring queries
- [MSAL_CIAM_BEST_PRACTICES.md](MSAL_CIAM_BEST_PRACTICES.md) - MSAL best practices

## Support

For additional help:

- Review complete documentation in `docs/authentication/`
- Check Application Insights logs
- Consult Microsoft Entra ID documentation: <https://learn.microsoft.com/en-us/entra/>

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-13 | GitHub Copilot | Initial comprehensive configuration checklist |

---

**Print this checklist and check off items as you complete them to ensure thorough configuration!**
