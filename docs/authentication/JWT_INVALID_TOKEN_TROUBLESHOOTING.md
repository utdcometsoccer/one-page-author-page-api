# JWT Invalid Token Troubleshooting Guide

## Overview

This comprehensive guide helps you diagnose and resolve JWT token validation issues in the OnePageAuthor API platform. It covers common errors, step-by-step troubleshooting procedures, and resolution strategies.

## Table of Contents

1. [Quick Diagnosis](#quick-diagnosis)
2. [Common JWT Validation Errors](#common-jwt-validation-errors)
3. [Step-by-Step Troubleshooting](#step-by-step-troubleshooting)
4. [Token Analysis Tools](#token-analysis-tools)
5. [Application Insights Queries](#application-insights-queries)
6. [Resolution Strategies](#resolution-strategies)
7. [Prevention Best Practices](#prevention-best-practices)

## Quick Diagnosis

### Symptoms Checklist

Check which symptoms you're experiencing:

- [ ] API returns 401 Unauthorized
- [ ] "Invalid or expired token" error message
- [ ] "Authorization header is required" error
- [ ] Token works in one environment but not another
- [ ] Token worked yesterday, stopped working today
- [ ] Some users can authenticate, others cannot
- [ ] Authentication works intermittently

### Quick Checks

Run through these quick checks first:

1. **Token Present?**
   ```bash
   # Check request headers
   curl -v https://your-api.azurewebsites.net/api/endpoint \
     -H "Authorization: Bearer YOUR_TOKEN"
   
   # Look for: Authorization: Bearer <token> in request headers
   ```

2. **Token Format Valid?**
   - JWT tokens have 3 parts separated by dots: `header.payload.signature`
   - Decode at https://jwt.ms to verify structure

3. **Token Expired?**
   - Check `exp` claim in decoded token
   - Compare to current Unix timestamp: https://www.unixtimestamp.com/

4. **Environment Variables Set?**
   ```bash
   # Check in Azure Portal
   Function App → Configuration → Application settings
   # Verify: AAD_TENANT_ID and AAD_AUDIENCE are present
   ```

5. **Correct Audience?**
   - Decode token, check `aud` claim
   - Compare to `AAD_AUDIENCE` environment variable

## Common JWT Validation Errors

### Error: "Authorization header is required"

**HTTP Status**: 401 Unauthorized

**Cause**: No Authorization header in request, or header is empty.

**Error Message**:
```json
{
  "error": "Authorization header is required"
}
```

**Resolution**:
1. Ensure request includes `Authorization` header
2. Format must be: `Authorization: Bearer <token>`
3. No extra spaces or typos
4. Header name is case-sensitive in some HTTP clients

**Example (Correct)**:
```bash
curl -X GET "https://api.example.com/endpoint" \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Example (Incorrect)**:
```bash
# Missing Authorization header
curl -X GET "https://api.example.com/endpoint"

# Wrong format (missing "Bearer")
curl -X GET "https://api.example.com/endpoint" \
  -H "Authorization: eyJhbGciOiJSUzI1NiIs..."

# Wrong header name
curl -X GET "https://api.example.com/endpoint" \
  -H "Auth: Bearer eyJhbGciOiJSUzI1NiIs..."
```

---

### Error: "Authorization header must start with 'Bearer '"

**HTTP Status**: 401 Unauthorized

**Cause**: Authorization header doesn't start with "Bearer " prefix.

**Resolution**:
Add "Bearer " prefix (with space after) to your token.

**Example (Correct)**:
```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Example (Incorrect)**:
```
Authorization: eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
Authorization: bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9... (lowercase)
Authorization: Bearer  eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9... (double space)
```

---

### Error: "Token has X segments, expected 3"

**HTTP Status**: 401 Unauthorized

**Cause**: Token is not a valid JWT format.

**Resolution**:

**If Token has 1 segment** (opaque token):
- Platform attempts introspection service
- Verify introspection endpoint is configured
- Check if using wrong type of token (e.g., refresh token instead of access token)

**If Token has 2 segments**:
- Malformed JWT
- Missing signature component
- Obtain a new token from identity provider

**If Token has more than 3 segments**:
- Token contains dots in payload (unlikely)
- Token was corrupted during transmission
- Obtain a new token

**Valid JWT Structure**:
```
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9    ← Header (Base64URL)
.
eyJpc3MiOiJodHRwczovL2xvZ2luLm1pY... ← Payload (Base64URL)
.
SflKxwRJSMeKKF2QT4fwpMeJf36POk...    ← Signature (Base64URL)
```

---

### Error: "Invalid or expired token"

**HTTP Status**: 401 Unauthorized

**Cause**: Token validation failed (expired, invalid signature, wrong audience, etc.)

**Resolution**:

1. **Check expiration**:
   - Decode token at https://jwt.ms
   - Look at `exp` claim (Unix timestamp)
   - Compare to current time
   - If expired, acquire new token

2. **Check signature**:
   - Signature is validated against Microsoft Entra ID public keys
   - Keys rotate periodically (see signing key rotation section below)
   - Check Application Insights for `SecurityTokenSignatureKeyNotFoundException`

3. **Check audience**:
   - Token `aud` claim must match `AAD_AUDIENCE` environment variable
   - If mismatch, either:
     - Update `AAD_AUDIENCE` to match token, OR
     - Configure SPA to request correct audience scope

4. **Check issuer**:
   - Token `iss` claim must match expected issuer
   - Expected: `https://login.microsoftonline.com/{tenant-id}/v2.0`
   - Verify `AAD_TENANT_ID` is correct

---

### Error: "IDX10503: Signature validation failed"

**Full Error**: `Microsoft.IdentityModel.Tokens.SecurityTokenSignatureKeyNotFoundException: IDX10503: Signature validation failed. Keys tried: 'kid1, kid2, ...' . Exceptions caught: '...'. token: '...'`

**HTTP Status**: 401 Unauthorized (or 500 Internal Server Error)

**Cause**: Azure AD has rotated signing keys, and the cached keys don't include the key used to sign this token.

**Resolution**:

#### Immediate Fix
Wait 2-3 minutes and retry. The platform automatically:
1. Detects the missing key (`kid` not found)
2. Refreshes metadata from OpenID Connect endpoint
3. Retries validation with new keys
4. Succeeds if token is valid

#### Verify Automatic Key Refresh is Configured

Check `Program.cs` in each Function App:

```csharp
.AddJwtBearer(options =>
{
    // ✅ Critical setting for key rotation
    options.RefreshOnIssuerKeyNotFound = true;

    // ✅ Automatic refresh configuration
    options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
        metadataAddress,
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever())
    {
        AutomaticRefreshInterval = TimeSpan.FromHours(6),
        RefreshInterval = TimeSpan.FromMinutes(30)
    };
});
```

#### If Still Failing

1. **Check connectivity to metadata endpoint**:
   ```bash
   curl https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration
   ```
   Should return JSON with `jwks_uri` property.

2. **Check JWKS endpoint**:
   ```bash
   curl https://login.microsoftonline.com/{tenant-id}/discovery/v2.0/keys
   ```
   Should return JSON with array of signing keys.

3. **Verify no firewall blocking**:
   - Function App must reach `login.microsoftonline.com`
   - Check NSG rules, firewall rules
   - Test from Function App console (Kudu)

4. **Restart Function App**:
   - Forces new ConfigurationManager instance
   - Fetches latest signing keys

See [../JWT_KEY_ROTATION_FIX.md](../JWT_KEY_ROTATION_FIX.md) for detailed information.

---

### Error: "IDX10205: Issuer validation failed"

**Full Error**: `IDX10205: Issuer validation failed. Issuer: '{issuer}'. Did not match: validationParameters.ValidIssuer: '{expected-issuer}'`

**Cause**: Token `iss` claim doesn't match expected issuer.

**Resolution**:

1. **Decode token** at https://jwt.ms and check `iss` claim
2. **Expected issuer format**: `https://login.microsoftonline.com/{tenant-id}/v2.0`
3. **Common mismatches**:
   - Using v1.0 endpoint instead of v2.0
   - Wrong tenant ID in `AAD_TENANT_ID`
   - Token from different tenant

**Fix**:
```bash
# Verify AAD_TENANT_ID matches tenant in token issuer
# Token iss: https://login.microsoftonline.com/12345678-1234-1234-1234-123456789012/v2.0
# AAD_TENANT_ID should be: 12345678-1234-1234-1234-123456789012
```

---

### Error: "IDX10214: Audience validation failed"

**Full Error**: `IDX10214: Audience validation failed. Audiences: '{token-aud}'. Did not match: validationParameters.ValidAudience: '{expected-aud}'`

**Cause**: Token `aud` claim doesn't match `AAD_AUDIENCE`.

**Resolution**:

1. **Decode token** and check `aud` claim
2. **Compare to `AAD_AUDIENCE`** environment variable
3. **Common mismatches**:
   - Using SPA client ID instead of API client ID
   - Mismatch between API app registration and SPA scope request
   - Token requested for wrong resource

**Fix Option 1**: Update AAD_AUDIENCE
```bash
# If token aud is: 87654321-4321-4321-4321-210987654321
# Set AAD_AUDIENCE to: 87654321-4321-4321-4321-210987654321
```

**Fix Option 2**: Update SPA scope request
```javascript
// In MSAL configuration
const loginRequest = {
  scopes: ["api://{AAD_AUDIENCE}/access_as_user"]
};
```

---

### Error: "IDX10223: Lifetime validation failed"

**Full Error**: `IDX10223: Lifetime validation failed. The token is expired.`

**Cause**: Token has expired (`exp` claim is in the past).

**Resolution**:

1. **Verify expiration**:
   - Decode token, check `exp` claim (Unix timestamp)
   - Convert timestamp: https://www.unixtimestamp.com/
   - Compare to current time

2. **Acquire new token**:
   ```javascript
   // In SPA with MSAL
   const tokenResponse = await msalInstance.acquireTokenSilent(loginRequest);
   const newAccessToken = tokenResponse.accessToken;
   ```

3. **Check clock skew**:
   - Default clock skew tolerance: 5 minutes
   - If token just expired, may need to wait a few seconds
   - Ensure server clocks are synchronized (NTP)

4. **Check token lifetime configuration**:
   - Default access token lifetime: 60-90 minutes
   - Configure in Azure Portal if needed (rare)

**Prevention**:
- Implement token refresh before expiration
- Use MSAL's `acquireTokenSilent` which automatically handles refresh

---

## Step-by-Step Troubleshooting

### Procedure 1: Basic Token Validation

Follow these steps to validate a token:

#### Step 1: Decode the Token

1. Copy the full JWT token (without "Bearer " prefix)
2. Go to https://jwt.ms
3. Paste the token
4. Review the decoded claims

#### Step 2: Verify Token Structure

Check the decoded token has all required claims:

```json
{
  "aud": "87654321-4321-4321-4321-210987654321",  ← Must match AAD_AUDIENCE
  "iss": "https://login.microsoftonline.com/12345678-1234-1234-1234-123456789012/v2.0",  ← Must match tenant
  "exp": 1234567890,  ← Must be in future
  "iat": 1234564290,  ← Should be in past
  "nbf": 1234564290,  ← Should be in past (not before)
  "sub": "user-id",  ← User identifier
  "oid": "user-object-id",  ← User object ID
  "scp": "access_as_user",  ← Scope (delegated permission)
  "roles": ["ImageStorageTier.Pro"]  ← Optional app roles
}
```

#### Step 3: Verify Claims Match Configuration

| Claim | Should Match | Where to Find Expected Value |
|-------|--------------|------------------------------|
| `aud` | `AAD_AUDIENCE` | Function App → Configuration → AAD_AUDIENCE |
| `iss` | `https://login.microsoftonline.com/{AAD_TENANT_ID}/v2.0` | Function App → Configuration → AAD_TENANT_ID |
| `exp` | Future timestamp | Current time < exp |
| `scp` or `roles` | Expected permissions | Verify user has correct scopes/roles |

#### Step 4: Test with cURL

```bash
# Replace placeholders
FUNCTION_URL="https://your-function.azurewebsites.net/api/endpoint"
ACCESS_TOKEN="your-jwt-token-here"

# Make request
curl -v -X GET "$FUNCTION_URL" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json"

# Check response:
# - HTTP 200 = Success ✅
# - HTTP 401 = Unauthorized ❌
# - HTTP 500 = Server error ❌
```

#### Step 5: Check Application Insights Logs

```kql
// Recent authentication failures
traces
| where timestamp > ago(1h)
| where message contains "JWT" or message contains "token" or message contains "authentication"
| where severityLevel >= 2
| order by timestamp desc
| project timestamp, message, severityLevel
| take 20
```

### Procedure 2: Diagnose Signing Key Issues

If you see `SecurityTokenSignatureKeyNotFoundException`:

#### Step 1: Verify Key Rotation Configuration

Check Program.cs:
```csharp
options.RefreshOnIssuerKeyNotFound = true;  // Should be true
```

#### Step 2: Check Metadata Endpoint

```bash
TENANT_ID="your-tenant-id"
curl "https://login.microsoftonline.com/$TENANT_ID/v2.0/.well-known/openid-configuration" | jq
```

Should return:
```json
{
  "issuer": "https://login.microsoftonline.com/{tenant-id}/v2.0",
  "jwks_uri": "https://login.microsoftonline.com/{tenant-id}/discovery/v2.0/keys",
  ...
}
```

#### Step 3: Check JWKS Endpoint

```bash
curl "https://login.microsoftonline.com/$TENANT_ID/discovery/v2.0/keys" | jq
```

Should return array of keys with `kid` values:
```json
{
  "keys": [
    {
      "kid": "Uo1a_QUweTFqiqL_HSeSjQIoRj0",
      "use": "sig",
      "kty": "RSA",
      "n": "...",
      "e": "AQAB"
    },
    ...
  ]
}
```

#### Step 4: Check Token Kid

Decode token header (first part before first dot):
```bash
TOKEN="eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IlVvMWFfUVV3ZVRGcWlxTF9IU2VTalFJb1JqMCJ9.eyJ..."
HEADER=$(echo "$TOKEN" | cut -d'.' -f1)
echo "$HEADER" | base64 -d 2>/dev/null | jq
```

Should show:
```json
{
  "alg": "RS256",
  "typ": "JWT",
  "kid": "Uo1a_QUweTFqiqL_HSeSjQIoRj0"  ← Token signing key ID
}
```

#### Step 5: Verify Kid Exists in JWKS

Check if the token's `kid` is in the JWKS response from Step 3.

**If yes**: Key is available, issue is with caching. Wait and retry.

**If no**: Key has been rotated but token was signed with old key. Options:
- Wait for token to expire and acquire new token
- Force token refresh in SPA
- If token is valid but key rotated, platform will auto-refresh

### Procedure 3: Diagnose Audience Mismatch

#### Step 1: Get Token Audience

Decode token, find `aud` claim:
```json
{
  "aud": "87654321-4321-4321-4321-210987654321",
  ...
}
```

#### Step 2: Get Expected Audience

Check Function App configuration:
```bash
# In Azure Portal
Function App → Configuration → Application settings → AAD_AUDIENCE

# Or via Azure CLI
az functionapp config appsettings list \
  --name your-function-app \
  --resource-group your-rg \
  --query "[?name=='AAD_AUDIENCE'].value" -o tsv
```

#### Step 3: Compare Values

```bash
TOKEN_AUD="87654321-4321-4321-4321-210987654321"  # From token
EXPECTED_AUD="12345678-abcd-efgh-ijkl-987654321012"  # From config

if [ "$TOKEN_AUD" = "$EXPECTED_AUD" ]; then
  echo "✅ Audience matches"
else
  echo "❌ Audience mismatch!"
  echo "Token aud: $TOKEN_AUD"
  echo "Expected:  $EXPECTED_AUD"
fi
```

#### Step 4: Determine Correct Value

**Option A**: Update Function App configuration to match token
- Use if token `aud` is correct
- Update `AAD_AUDIENCE` to match token `aud`

**Option B**: Update SPA to request correct audience
- Use if Function App `AAD_AUDIENCE` is correct
- Update MSAL scope to: `api://{AAD_AUDIENCE}/access_as_user`

#### Step 5: Verify App Registration

1. Go to Azure Portal → App registrations
2. Find API app registration
3. Check **Expose an API** → Application ID URI
4. Should match `api://{client-id}` format
5. Client ID should match `AAD_AUDIENCE`

## Token Analysis Tools

### jwt.ms (Microsoft)

**URL**: https://jwt.ms

**Features**:
- Decode JWT tokens
- View all claims
- No token leaves your browser (client-side only)
- Trusted Microsoft tool

**Usage**:
1. Copy JWT token (without "Bearer ")
2. Paste into jwt.ms
3. Review decoded claims

### jwt.io (Auth0)

**URL**: https://jwt.io

**Features**:
- Decode JWT tokens
- Verify signatures (with public key)
- Edit and re-encode tokens (for testing)

⚠️ **Warning**: Be cautious with production tokens on third-party sites.

### Browser Developer Tools

**Check Requests**:
```javascript
// In browser console, intercept fetch requests
const originalFetch = window.fetch;
window.fetch = function(...args) {
  const [url, options] = args;
  console.log('Fetch URL:', url);
  console.log('Headers:', options?.headers);
  return originalFetch.apply(this, args);
};
```

**View Local Storage (MSAL cache)**:
1. Open Developer Tools (F12)
2. Go to Application tab
3. Expand Local Storage
4. Find your domain
5. Look for keys starting with `msal.`

### Postman

**Import Token**:
1. Authorization tab → Type: OAuth 2.0
2. Configure grant type: Authorization Code (PKCE)
3. Get New Access Token
4. Use token in requests

**Decode Token**:
- Postman automatically decodes JWT in Authorization tab

## Application Insights Queries

### Authentication Success Rate

```kql
// Calculate authentication success rate over last 24 hours
let totalRequests = requests
| where timestamp > ago(24h)
| where name contains "authenticated" or url contains "/api/"
| count;
let successfulAuth = requests
| where timestamp > ago(24h)
| where resultCode !in (401, 403)
| where name contains "authenticated" or url contains "/api/"
| count;
let successRate = todouble(successfulAuth) / todouble(totalRequests) * 100;
print 
    TotalRequests = totalRequests,
    SuccessfulAuth = successfulAuth,
    SuccessRate = strcat(round(successRate, 2), "%")
```

### Recent Authentication Failures

```kql
// Get recent 401 Unauthorized responses with details
requests
| where timestamp > ago(1h)
| where resultCode == 401
| order by timestamp desc
| project 
    timestamp,
    name,
    url,
    resultCode,
    duration,
    operation_Id
| take 50
```

### JWT Validation Errors

```kql
// Find JWT-specific validation errors
traces
| where timestamp > ago(24h)
| where message contains "JWT" 
    or message contains "token validation"
    or message contains "IDX"
| where severityLevel >= 2  // Warning or Error
| order by timestamp desc
| project 
    timestamp,
    severityLevel,
    message,
    operation_Id
| take 100
```

### Signing Key Rotation Events

```kql
// Detect signing key rotation issues
traces
| where timestamp > ago(7d)
| where message contains "SecurityTokenSignatureKeyNotFoundException"
    or message contains "IDX10503"
    or message contains "Signature validation failed"
| summarize Count = count(), 
    FirstOccurrence = min(timestamp),
    LastOccurrence = max(timestamp) 
    by message
| order by Count desc
```

### Token Expiration Failures

```kql
// Find expired token errors
traces
| where timestamp > ago(24h)
| where message contains "IDX10223" 
    or message contains "token is expired"
    or message contains "Lifetime validation failed"
| order by timestamp desc
| project 
    timestamp,
    message,
    operation_Id
| take 50
```

### Audience Validation Failures

```kql
// Find audience mismatch errors
traces
| where timestamp > ago(24h)
| where message contains "IDX10214" 
    or message contains "Audience validation failed"
| order by timestamp desc
| project 
    timestamp,
    message,
    operation_Id
| take 50
```

### User-Specific Authentication Issues

```kql
// Track authentication for specific user
let userId = "user-id-here";
union traces, requests
| where timestamp > ago(24h)
| where message contains userId or customDimensions.UserId == userId
| where message contains "authentication" or message contains "JWT" or resultCode == 401
| order by timestamp asc
| project 
    timestamp,
    itemType,
    message = coalesce(message, name),
    resultCode = coalesce(resultCode, ""),
    duration = coalesce(duration, 0.0)
```

## Resolution Strategies

### Strategy 1: Token Refresh

**When to Use**: Token expired or about to expire

**Implementation (MSAL)**:
```javascript
async function getAccessToken() {
  const account = msalInstance.getAllAccounts()[0];
  
  try {
    // Try silent acquisition first (uses cache/refresh token)
    const response = await msalInstance.acquireTokenSilent({
      scopes: ["api://{api-client-id}/access_as_user"],
      account: account
    });
    return response.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      // Silent acquisition failed, require user interaction
      const response = await msalInstance.acquireTokenPopup({
        scopes: ["api://{api-client-id}/access_as_user"]
      });
      return response.accessToken;
    }
    throw error;
  }
}
```

### Strategy 2: Configuration Validation Script

**PowerShell Script**:
```powershell
# validate-jwt-config.ps1

param(
    [Parameter(Mandatory=$true)]
    [string]$FunctionAppName,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup
)

Write-Host "Validating JWT configuration for $FunctionAppName..." -ForegroundColor Yellow

# Get app settings
$settings = az functionapp config appsettings list `
    --name $FunctionAppName `
    --resource-group $ResourceGroup | ConvertFrom-Json

$tenantId = ($settings | Where-Object { $_.name -eq "AAD_TENANT_ID" }).value
$audience = ($settings | Where-Object { $_.name -eq "AAD_AUDIENCE" }).value

# Validate settings exist
if ([string]::IsNullOrEmpty($tenantId)) {
    Write-Host "❌ AAD_TENANT_ID is not set" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrEmpty($audience)) {
    Write-Host "❌ AAD_AUDIENCE is not set" -ForegroundColor Red
    exit 1
}

Write-Host "✅ AAD_TENANT_ID: $tenantId" -ForegroundColor Green
Write-Host "✅ AAD_AUDIENCE: $audience" -ForegroundColor Green

# Test metadata endpoint
$metadataUrl = "https://login.microsoftonline.com/$tenantId/v2.0/.well-known/openid-configuration"
Write-Host "Testing metadata endpoint: $metadataUrl" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri $metadataUrl -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Metadata endpoint accessible" -ForegroundColor Green
        $metadata = $response.Content | ConvertFrom-Json
        Write-Host "   Issuer: $($metadata.issuer)" -ForegroundColor Cyan
        Write-Host "   JWKS URI: $($metadata.jwks_uri)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ Cannot access metadata endpoint" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ All validations passed" -ForegroundColor Green
```

### Strategy 3: Automated Token Testing

**Python Script**:
```python
#!/usr/bin/env python3
"""Test JWT token validation"""

import requests
import jwt
import json
from datetime import datetime

def test_token(api_url, access_token):
    """Test API with JWT token"""
    
    # Decode token (without verification, just to inspect)
    decoded = jwt.decode(access_token, options={"verify_signature": False})
    
    print("Token Claims:")
    print(f"  Issuer: {decoded.get('iss')}")
    print(f"  Audience: {decoded.get('aud')}")
    print(f"  Expires: {datetime.fromtimestamp(decoded.get('exp'))}")
    print(f"  User: {decoded.get('oid')} / {decoded.get('sub')}")
    
    # Check if expired
    if decoded.get('exp') < datetime.now().timestamp():
        print("❌ Token is expired!")
        return False
    
    # Test API call
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }
    
    print(f"\nCalling API: {api_url}")
    response = requests.get(api_url, headers=headers)
    
    print(f"Status Code: {response.status_code}")
    
    if response.status_code == 200:
        print("✅ Authentication successful!")
        return True
    elif response.status_code == 401:
        print("❌ Authentication failed (401 Unauthorized)")
        print(f"Response: {response.text}")
        return False
    else:
        print(f"❌ Unexpected status code: {response.status_code}")
        print(f"Response: {response.text}")
        return False

if __name__ == "__main__":
    # Configuration
    API_URL = "https://your-function.azurewebsites.net/api/endpoint"
    ACCESS_TOKEN = "your-jwt-token-here"
    
    test_token(API_URL, ACCESS_TOKEN)
```

## Prevention Best Practices

### 1. Implement Proactive Token Refresh

Don't wait for tokens to expire:

```javascript
// Check token expiration and refresh proactively
function isTokenExpiringSoon(tokenResponse) {
  const expiresOn = tokenResponse.expiresOn;
  const now = new Date();
  const fiveMinutes = 5 * 60 * 1000;
  
  return (expiresOn.getTime() - now.getTime()) < fiveMinutes;
}

async function getValidToken() {
  let tokenResponse = await msalInstance.acquireTokenSilent(loginRequest);
  
  if (isTokenExpiringSoon(tokenResponse)) {
    // Proactively refresh
    tokenResponse = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      forceRefresh: true
    });
  }
  
  return tokenResponse.accessToken;
}
```

### 2. Set Up Monitoring Alerts

Configure Application Insights alerts:

**Alert 1: High Authentication Failure Rate**
```kql
requests
| where timestamp > ago(5m)
| where resultCode == 401
| summarize Count = count()
| where Count > 10  // Alert if more than 10 failures in 5 minutes
```

**Alert 2: Signing Key Rotation Issues**
```kql
traces
| where timestamp > ago(5m)
| where message contains "SecurityTokenSignatureKeyNotFoundException"
| summarize Count = count()
| where Count > 5
```

### 3. Validate Configuration on Startup

Add validation in Program.cs:

```csharp
var tenantId = configuration["AAD_TENANT_ID"];
var audience = configuration["AAD_AUDIENCE"] ?? configuration["AAD_CLIENT_ID"];

if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(audience))
{
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogWarning(
        "JWT authentication not configured: AAD_TENANT_ID={TenantId}, AAD_AUDIENCE={Audience}",
        tenantId ?? "(not set)",
        audience ?? "(not set)");
}
else
{
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "JWT authentication configured with tenant: {TenantId}, audience: {Audience}",
        tenantId,
        audience);
}
```

### 4. Document Expected Token Format

Create a reference token structure:

```json
{
  "// Expected Token Claims": {
    "aud": "API client ID from AAD_AUDIENCE",
    "iss": "https://login.microsoftonline.com/{AAD_TENANT_ID}/v2.0",
    "exp": "Future Unix timestamp",
    "iat": "Past Unix timestamp",
    "nbf": "Past Unix timestamp",
    "sub": "User identifier",
    "oid": "User object ID in Entra ID",
    "tid": "Tenant ID (should match AAD_TENANT_ID)",
    "scp": "access_as_user",
    "roles": ["Optional app roles"]
  }
}
```

### 5. Test in Pre-Production

Always test authentication configuration in staging before production:

```bash
# Test script
#!/bin/bash

ENVIRONMENTS=("dev" "staging" "prod")
TOKEN="$1"

for env in "${ENVIRONMENTS[@]}"; do
  echo "Testing $env environment..."
  response=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "Authorization: Bearer $TOKEN" \
    "https://${env}-api.azurewebsites.net/api/health")
  
  if [ "$response" -eq 200 ]; then
    echo "✅ $env: Authentication successful"
  else
    echo "❌ $env: Authentication failed (HTTP $response)"
  fi
done
```

## Related Documentation

- [ENTRA_ID_EXCEPTIONS_REFERENCE.md](ENTRA_ID_EXCEPTIONS_REFERENCE.md) - Complete exception reference
- [ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md) - Environment variable details
- [APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md) - More KQL queries
- [../JWT_KEY_ROTATION_FIX.md](../JWT_KEY_ROTATION_FIX.md) - Key rotation implementation
- [../REFRESH_ON_ISSUER_KEY_NOT_FOUND.md](../REFRESH_ON_ISSUER_KEY_NOT_FOUND.md) - RefreshOnIssuerKeyNotFound configuration

## Support Resources

- [Microsoft Entra ID Documentation](https://learn.microsoft.com/en-us/entra/identity-platform/)
- [JWT Token Reference](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens)
- [Token Validation](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens#validate-tokens)
- [MSAL.js Documentation](https://learn.microsoft.com/en-us/entra/msal/javascript/)

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-13 | GitHub Copilot | Initial comprehensive JWT troubleshooting guide |

---

**Still Having Issues?** Check [ENTRA_ID_EXCEPTIONS_REFERENCE.md](ENTRA_ID_EXCEPTIONS_REFERENCE.md) for complete exception details or [APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md) for advanced monitoring.
