# Microsoft Entra ID Environment Variables Guide

## Overview

This guide provides comprehensive documentation for all Microsoft Entra ID-related environment variables used in the OnePageAuthor API platform. Each section includes where to find the values, how to configure them, and their purpose.

## Table of Contents

1. [Required Environment Variables](#required-environment-variables)
2. [Optional Environment Variables](#optional-environment-variables)
3. [Configuration Methods](#configuration-methods)
4. [Variable Details](#variable-details)
5. [Validation & Testing](#validation--testing)
6. [Troubleshooting](#troubleshooting)

## Required Environment Variables

These variables MUST be configured for authentication to work:

| Variable | Required For | Default Value |
|----------|-------------|---------------|
| `AAD_TENANT_ID` | All authenticated functions | None |
| `AAD_AUDIENCE` | All authenticated functions | None |

## Optional Environment Variables

These variables can be used to customize authentication behavior:

| Variable | Purpose | Default Value |
|----------|---------|---------------|
| `AAD_CLIENT_ID` | Alternative to AAD_AUDIENCE | Value of AAD_AUDIENCE |
| `OPEN_ID_CONNECT_METADATA_URL` | Custom metadata endpoint | `https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration` |

## Configuration Methods

### Azure Portal (Production)

For deployed Function Apps:

1. Navigate to Azure Portal → Your Function App
2. Go to **Configuration** → **Application settings**
3. Click **New application setting**
4. Add each variable with its value
5. Click **Save**
6. Restart the Function App (if needed)

### User Secrets (Local Development - Recommended)

For local development, use user secrets (stored outside source control):

```bash
# Navigate to each Function App project
cd ImageAPI

# Set secrets
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id-here"
dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id-here"

# Repeat for other projects
cd ../InkStainedWretchFunctions
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id-here"
dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id-here"

cd ../InkStainedWretchStripe
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id-here"
dotnet user-secrets set "AAD_AUDIENCE" "your-api-client-id-here"
```

### local.settings.json (Local Development - Alternative)

⚠️ **WARNING**: Never commit `local.settings.json` to source control!

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

### Environment Variables (CI/CD)

For GitHub Actions or other CI/CD:

```yaml
# .github/workflows/deploy.yml
env:
  AAD_TENANT_ID: ${{ secrets.AAD_TENANT_ID }}
  AAD_AUDIENCE: ${{ secrets.AAD_AUDIENCE }}
```

Store the actual values in GitHub Secrets (Settings → Secrets and variables → Actions).

## Variable Details

### AAD_TENANT_ID

**Purpose**: Identifies your Microsoft Entra ID tenant. This ensures JWT tokens are issued by YOUR organization and not a different tenant.

**Format**: GUID (e.g., `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)

**Where to Find**:

#### Method 1: Azure Portal

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Search for and select **Microsoft Entra ID**
3. On the **Overview** page, find **Tenant ID**
4. Click the copy icon to copy the GUID

#### Method 2: App Registration

1. Navigate to **Microsoft Entra ID** → **App registrations**
2. Select your API app registration
3. On the **Overview** page, find **Directory (tenant) ID**
4. Click the copy icon

#### Method 3: Azure CLI

```bash
az account show --query tenantId -o tsv
```

#### Method 4: PowerShell

```powershell
(Get-AzContext).Tenant.Id
```

**Example Configuration**:

```bash
AAD_TENANT_ID=12345678-1234-1234-1234-123456789012
```

**Used By**:

- All Azure Function apps (ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe)
- JwtValidationService
- Program.cs authentication configuration
- EntraIdRoleManager (for role management)

**Validation**:

- Must be a valid GUID
- Must match the tenant where your app registrations exist
- Tokens from other tenants will be rejected

**Common Mistakes**:

- ❌ Using `common` or `organizations` instead of specific tenant ID
- ❌ Mixing up tenant IDs from different Entra ID directories
- ❌ Using tenant name instead of tenant ID

---

### AAD_AUDIENCE

**Purpose**: Specifies which application the JWT tokens are intended for. Tokens without this value in the `aud` claim will be rejected.

**Format**: GUID or URI (e.g., `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` or `api://your-api`)

**Where to Find**:

#### Method 1: Azure Portal - App Registration

1. Navigate to **Microsoft Entra ID** → **App registrations**
2. Select your **API app registration** (e.g., "OnePageAuthor API" or "InkStainedWretches API")
3. On the **Overview** page, find **Application (client) ID**
4. Click the copy icon to copy the GUID

#### Method 2: Application ID URI (Alternative)

1. In your API app registration, go to **Expose an API**
2. The **Application ID URI** is shown at the top
3. This can be used instead of the client ID if you prefer URI format
4. Common format: `api://{client-id}` or custom like `api://inkstainedwretches-api`

#### Method 3: Azure CLI

```bash
# Get by app display name
az ad app list --display-name "OnePageAuthor API" --query "[0].appId" -o tsv
```

#### Method 4: PowerShell

```powershell
Get-AzADApplication -DisplayName "OnePageAuthor API" | Select-Object -ExpandProperty AppId
```

**Example Configuration**:

Using Client ID (most common):

```bash
AAD_AUDIENCE=87654321-4321-4321-4321-210987654321
```

Using Application ID URI (alternative):

```bash
AAD_AUDIENCE=api://inkstainedwretches-api
```

**Used By**:

- All Azure Function apps (ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe)
- JwtValidationService for token validation
- TokenValidationParameters in Program.cs

**Validation**:

- Must match the `aud` claim in JWT tokens
- If using Application ID URI, ensure it matches what's configured in "Expose an API"
- Tokens with different audience will be rejected

**Important Notes**:

- ⚠️ Use the **API** app registration client ID, NOT the SPA client ID
- ⚠️ The audience must match what the SPA requests when acquiring tokens
- ⚠️ In MSAL, the SPA requests: `api://{audience}/access_as_user`

**Common Mistakes**:

- ❌ Using SPA client ID instead of API client ID
- ❌ Mismatch between AAD_AUDIENCE and the scope requested by SPA
- ❌ Using `api://` prefix when expecting just the GUID

**Testing the Audience**:
Decode a JWT token at <https://jwt.ms> and verify the `aud` claim matches your `AAD_AUDIENCE`:

```json
{
  "aud": "87654321-4321-4321-4321-210987654321",
  "iss": "https://login.microsoftonline.com/{tenant-id}/v2.0",
  "exp": 1234567890,
  ...
}
```

---

### AAD_CLIENT_ID

**Purpose**: Alternative to `AAD_AUDIENCE`. If both are set, `AAD_AUDIENCE` takes precedence.

**Format**: GUID (e.g., `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)

**Where to Find**: Same as `AAD_AUDIENCE` (API app registration client ID)

**When to Use**:

- If your organization prefers `CLIENT_ID` naming convention
- When migrating from configurations that use `CLIENT_ID`
- For backwards compatibility

**Example Configuration**:

```bash
AAD_CLIENT_ID=87654321-4321-4321-4321-210987654321
```

**Code Behavior**:

```csharp
// In Program.cs
var audience = configuration["AAD_AUDIENCE"] ?? configuration["AAD_CLIENT_ID"];
```

**Recommendation**: Use `AAD_AUDIENCE` for new configurations. It's more semantically correct (tokens are validated against the audience, not just any client ID).

---

### OPEN_ID_CONNECT_METADATA_URL

**Purpose**: Override the default OpenID Connect metadata endpoint. Used for custom identity providers or testing.

**Format**: URL (e.g., `https://custom-identity.com/.well-known/openid-configuration`)

**Default Value**:

```
https://login.microsoftonline.com/{AAD_TENANT_ID}/v2.0/.well-known/openid-configuration
```

**Where to Find**:

#### For Standard Entra ID

You don't need to set this - the default is automatically constructed using your `AAD_TENANT_ID`.

#### For Custom Identity Providers

If using a non-Microsoft identity provider:

1. Check the provider's documentation for their metadata endpoint
2. Usually follows format: `{authority}/.well-known/openid-configuration`
3. Test the URL in a browser - should return JSON with `issuer`, `jwks_uri`, etc.

**Example Configuration**:

```bash
OPEN_ID_CONNECT_METADATA_URL=https://custom-identity.example.com/.well-known/openid-configuration
```

**When to Use**:

- Testing with a mock identity provider
- Using a custom or third-party identity provider
- Special sovereign cloud configurations (government clouds)

**Testing the Metadata URL**:
Open the URL in a browser - should return JSON like:

```json
{
  "issuer": "https://login.microsoftonline.com/{tenant-id}/v2.0",
  "authorization_endpoint": "https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/authorize",
  "token_endpoint": "https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token",
  "jwks_uri": "https://login.microsoftonline.com/{tenant-id}/discovery/v2.0/keys",
  ...
}
```

**Common Mistakes**:

- ❌ Forgetting `/v2.0` in the URL for Microsoft Entra ID
- ❌ Using HTTP instead of HTTPS
- ❌ URL returns 404 or invalid JSON

---

## EntraIdRoleManager-Specific Variables

When using the EntraIdRoleManager console application to manage app roles:

### AAD_CLIENT_SECRET

**Purpose**: Service principal secret for authenticating to Microsoft Graph API.

**Format**: String (generated secret value)

**Where to Find**:

1. Navigate to **Microsoft Entra ID** → **App registrations**
2. Select your **API app registration**
3. Go to **Certificates & secrets**
4. Click **New client secret**
5. Add description: `EntraIdRoleManager Service Principal`
6. Choose expiration: **24 months** (maximum)
7. Click **Add**
8. **IMMEDIATELY COPY THE SECRET VALUE** - it cannot be retrieved later

**Example Configuration**:

```bash
AAD_CLIENT_SECRET=your-secret-value-here-copy-immediately
```

**Security Best Practices**:

- ✅ Store in Azure Key Vault for production use
- ✅ Use user secrets for local development
- ✅ Rotate secrets before expiration (set calendar reminder)
- ✅ Never commit to source control
- ✅ Use minimum required permissions (Application.ReadWrite.All, AppRoleAssignment.ReadWrite.All)

**Used By**:

- EntraIdRoleManager console application only
- Not needed by Azure Functions (they use user-assigned managed identities or delegated permissions)

**Permissions Required**:
The service principal needs these Microsoft Graph API permissions:

- `Application.ReadWrite.All` (Application permission)
- `AppRoleAssignment.ReadWrite.All` (Application permission)
- `Directory.Read.All` (Application permission)

**Grant Permissions**:

1. In app registration → **API permissions**
2. Add the permissions above
3. Click **Grant admin consent for {your tenant}**

---

## Configuration Validation

### Check if Variables are Set

#### In Azure Function App

1. Navigate to Function App → Configuration → Application settings
2. Verify `AAD_TENANT_ID` and `AAD_AUDIENCE` are present
3. Verify values are not empty or placeholder text

#### In Local Development

```bash
# Using user secrets
cd ImageAPI
dotnet user-secrets list

# Should show:
# AAD_TENANT_ID = 12345678-1234-1234-1234-123456789012
# AAD_AUDIENCE = 87654321-4321-4321-4321-210987654321
```

### Validate Configuration at Runtime

The Function Apps log configuration status at startup:

```
[Information] JWT authentication configured with tenant: 12345678-1234-1234-1234-123456789012, audience: 87654321-4321-4321-4321-210987654321
```

If missing:

```
[Warning] JWT authentication not configured: AAD_TENANT_ID or AAD_AUDIENCE not set
```

### Test Authentication

```bash
# Get a token from your SPA or use device code flow
ACCESS_TOKEN="your-token-here"

# Test an authenticated endpoint
curl -X GET "https://your-function.azurewebsites.net/api/domain-registrations" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

# Expected:
# - 200 OK with data = success ✅
# - 401 Unauthorized = authentication failed ❌
```

## Validation & Testing

### Automated Validation Script

Create a PowerShell script to validate configuration:

```powershell
# validate-auth-config.ps1

$tenantId = $env:AAD_TENANT_ID
$audience = $env:AAD_AUDIENCE

if ([string]::IsNullOrWhiteSpace($tenantId)) {
    Write-Error "AAD_TENANT_ID is not set"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($audience)) {
    Write-Error "AAD_AUDIENCE is not set"
    exit 1
}

# Validate GUID format
$guidRegex = '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'

if ($tenantId -notmatch $guidRegex) {
    Write-Error "AAD_TENANT_ID is not a valid GUID: $tenantId"
    exit 1
}

if ($audience -notmatch $guidRegex -and $audience -notlike "api://*") {
    Write-Error "AAD_AUDIENCE is not a valid GUID or URI: $audience"
    exit 1
}

# Test metadata endpoint
$metadataUrl = $env:OPEN_ID_CONNECT_METADATA_URL
if ([string]::IsNullOrWhiteSpace($metadataUrl)) {
    $metadataUrl = "https://login.microsoftonline.com/$tenantId/v2.0/.well-known/openid-configuration"
}

try {
    $response = Invoke-WebRequest -Uri $metadataUrl -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Metadata endpoint accessible: $metadataUrl"
        $metadata = $response.Content | ConvertFrom-Json
        Write-Host "   Issuer: $($metadata.issuer)"
        Write-Host "   JWKS URI: $($metadata.jwks_uri)"
    }
} catch {
    Write-Error "❌ Cannot access metadata endpoint: $metadataUrl"
    Write-Error $_.Exception.Message
    exit 1
}

Write-Host "✅ All authentication configuration validated successfully"
```

Run the script:

```powershell
# Load environment variables (adjust for your config method)
$env:AAD_TENANT_ID = "your-tenant-id"
$env:AAD_AUDIENCE = "your-audience"

# Run validation
.\validate-auth-config.ps1
```

### Manual Testing Checklist

- [ ] Variables set in all Function Apps (ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe)
- [ ] Variables set in local development environment
- [ ] Values are valid GUIDs
- [ ] Metadata endpoint is accessible
- [ ] Test token contains matching `aud` claim
- [ ] Authentication succeeds with valid token
- [ ] Authentication fails with invalid token (expected behavior)
- [ ] Authentication fails with no token (expected behavior)

## Troubleshooting

### Issue: "AAD_TENANT_ID or AAD_AUDIENCE not set"

**Symptoms:**

- Warning log at startup
- JWT authentication disabled
- All requests succeed without authentication (dangerous!)

**Resolution:**

1. Verify variables are set in Function App configuration
2. Check for typos in variable names (case-sensitive)
3. Restart Function App after setting variables
4. For local dev, verify user secrets or local.settings.json

### Issue: "Invalid issuer"

**Symptoms:**

- Token validation fails
- Error: "IDX10205: Issuer validation failed"

**Resolution:**

1. Verify `AAD_TENANT_ID` matches the tenant that issued the token
2. Decode token at <https://jwt.ms> and check `iss` claim
3. Ensure `iss` matches: `https://login.microsoftonline.com/{AAD_TENANT_ID}/v2.0`

### Issue: "Invalid audience"

**Symptoms:**

- Token validation fails
- Error: "IDX10214: Audience validation failed"

**Resolution:**

1. Verify `AAD_AUDIENCE` matches the `aud` claim in token
2. Check SPA is requesting correct scope: `api://{AAD_AUDIENCE}/access_as_user`
3. Verify API app registration has correct Application ID URI

### Issue: "Metadata endpoint not accessible"

**Symptoms:**

- Cannot validate tokens
- Timeout or 404 errors

**Resolution:**

1. Check network connectivity from Function App to `login.microsoftonline.com`
2. Verify firewall rules allow outbound HTTPS
3. If using custom metadata URL, verify it's correct and accessible
4. Check DNS resolution

### Issue: Variables Set But Not Working

**Symptoms:**

- Variables visible in portal
- Authentication still fails

**Resolution:**

1. Restart the Function App (configuration changes require restart)
2. Check for application settings with conflicting names
3. Verify no Azure Key Vault references are failing
4. Check Application Insights logs for startup errors

## Best Practices

### Security

- ✅ Use Azure Key Vault references for production: `@Microsoft.KeyVault(SecretUri=...)`
- ✅ Use managed identities instead of service principals when possible
- ✅ Rotate client secrets before expiration
- ✅ Use minimum required permissions
- ✅ Never commit secrets to source control

### Development

- ✅ Use user secrets for local development
- ✅ Document required variables in README
- ✅ Provide example values in comments
- ✅ Validate configuration at startup

### Operations

- ✅ Monitor for authentication failures in Application Insights
- ✅ Set up alerts for configuration issues
- ✅ Document variable values in secure location (password manager, Key Vault)
- ✅ Test configuration in staging before production

### Documentation

- ✅ Keep this guide updated with new variables
- ✅ Document where to find each value
- ✅ Include examples and common mistakes
- ✅ Link to related troubleshooting guides

## Related Documentation

- [ENTRA_CIAM_CONFIGURATION_GUIDE.md](ENTRA_CIAM_CONFIGURATION_GUIDE.md) - Complete Entra ID setup guide
- [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) - Token troubleshooting
- [AZURE_ENTRA_ID_CONFIGURATION_CHECKLIST.md](AZURE_ENTRA_ID_CONFIGURATION_CHECKLIST.md) - Setup checklist
- [../SECRETS_QUICK_REFERENCE.md](../../SECRETS_QUICK_REFERENCE.md) - All platform secrets reference

## Quick Reference Card

Print this and keep handy:

```
┌─────────────────────────────────────────────────────────────┐
│         Entra ID Environment Variables Quick Reference       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ AAD_TENANT_ID                                                │
│   Where: Azure Portal → Entra ID → Overview → Tenant ID     │
│   Format: GUID                                               │
│   Example: 12345678-1234-1234-1234-123456789012            │
│                                                              │
│ AAD_AUDIENCE                                                 │
│   Where: App registrations → API App → Application (client) ID │
│   Format: GUID or API URI                                    │
│   Example: 87654321-4321-4321-4321-210987654321            │
│                                                              │
│ Configuration:                                               │
│   Production: Function App → Configuration → App settings   │
│   Development: dotnet user-secrets set "KEY" "VALUE"         │
│                                                              │
│ Validation:                                                  │
│   Test: https://jwt.ms (decode token)                        │
│   Verify: iss and aud claims match your config              │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-13 | GitHub Copilot | Initial comprehensive environment variables guide |

---

**Need Help?** See [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) for authentication issues.
