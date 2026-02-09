# Authorization Configuration Audit Report

**Generated:** 2025-12-27  
**Scope:** All Azure Function Apps  
**Purpose:** Validate consistent authorization configuration across the platform

---

## Executive Summary

This report analyzes the authorization configuration of all Azure Functions across the platform to ensure:

1. Consistent use of `AuthorizationLevel` settings
2. Proper JWT authentication on protected endpoints
3. No security gaps or misconfigurations

### Key Findings

✅ **Good Practices Found:**

- Domain registration endpoints correctly use `Anonymous` + JWT validation
- Stripe webhook properly uses `Anonymous` (webhooks require signature validation, not JWT)
- Public endpoints appropriately use `Anonymous` without JWT requirements
- Several endpoints use declarative `[Authorize]` attribute (ASP.NET Core authorization)

⚠️ **Recommendations:**

- **ImageAPI:** All endpoints use `AuthorizationLevel.Function` with JWT validation - should change to `Anonymous`
- **InkStainedWretchesConfig:** Config endpoints use `AuthorizationLevel.Function` without JWT - keep as-is for internal use
- **InkStainedWretchStripe:** Mixed usage - some use `Function` + JWT (should change to `Anonymous`), some correctly use `[Authorize]`
- **Test Harness Functions:** Correctly use `AuthorizationLevel.Function` for test-only endpoints

---

## Detailed Analysis by Function App

### 1. ImageAPI

**Status:** ⚠️ NEEDS UPDATE  
**Issue:** Using `AuthorizationLevel.Function` with JWT validation (double authentication)

| Function | Authorization Level | JWT Validation | Recommendation |
|----------|-------------------|----------------|----------------|
| `Upload` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |
| `Delete` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |
| `User` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |
| `WhoAmI` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |

**Rationale:**  
All four ImageAPI endpoints perform JWT token validation using `JwtAuthenticationHelper.ValidateJwtTokenAsync()`. They should use `AuthorizationLevel.Anonymous` to rely solely on JWT authentication, eliminating the need for Azure Functions host keys.

**Impact:** Medium  
**Priority:** High  
**Estimated Effort:** 15 minutes

**Code Changes Required:**

```csharp
// Before:
[HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req

// After:
[HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req
```

---

### 2. InkStainedWretchStripe

**Status:** ⚠️ MIXED CONFIGURATION  
**Issue:** Inconsistent authorization approaches

#### Endpoints Using `AuthorizationLevel.Function` + JWT Validation

| Function | Authorization Level | JWT Validation | Recommendation |
|----------|-------------------|----------------|----------------|
| `CreateStripeCheckoutSession` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |
| `CreateStripeCustomer` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |
| `CreateSubscription` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |
| `GetStripePriceInformation` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |
| `CancelSubscription` | `Function` | ✅ Yes (JwtAuthenticationHelper) | Change to `Anonymous` |
| `GetStripeCheckoutSession` | `Function` | ❌ No | **Review Required** - Add JWT or keep Function-level |

**Recommendation for `GetStripeCheckoutSession`:**  
Currently uses `AuthorizationLevel.Function` without JWT validation. This endpoint retrieves checkout session details. Options:

1. Add JWT validation for user context
2. Keep Function-level auth if only called internally
3. Validate session belongs to requesting user

#### Endpoints Using `[Authorize]` Attribute (Correct)

| Function | Authorization Level | Declarative Auth | Status |
|----------|-------------------|-----------------|---------|
| `ListSubscription` | `Function` | ✅ `[Authorize]` | ✅ Correct |
| `UpdateSubscription` | `Function` | ✅ `[Authorize]` | ✅ Correct |
| `InvoicePreview` | `Function` | ✅ `[Authorize]` | ✅ Correct |
| `FindSubscription` | `Function` | ✅ `[Authorize]` | ✅ Correct |

**Note:** These endpoints use ASP.NET Core declarative authorization with `[Authorize]` attribute. This is configured at the app level and is a cleaner approach. The `AuthorizationLevel.Function` setting doesn't add security value here but doesn't hurt either.

#### Public Endpoints (Correct)

| Function | Authorization Level | Purpose | Status |
|----------|-------------------|---------|---------|
| `WebHook` | `Anonymous` | Stripe webhook (signature validation) | ✅ Correct |
| `StripeHealthFunction` | `Anonymous` | Health check endpoint | ✅ Correct |

**Impact:** Medium-High  
**Priority:** High  
**Estimated Effort:** 30 minutes

---

### 3. InkStainedWretchFunctions

**Status:** ✅ MOSTLY CORRECT  
**Issue:** Already fixed in recent update

#### Protected Endpoints Using JWT (Correct Configuration)

| Function | Authorization Level | JWT Validation | Status |
|----------|-------------------|----------------|---------|
| `CreateDomainRegistration` | `Anonymous` | ✅ Yes (JwtValidationService) | ✅ Correct |
| `GetDomainRegistrations` | `Anonymous` | ✅ Yes (JwtValidationService) | ✅ Correct |
| `GetDomainRegistrationById` | `Anonymous` | ✅ Yes (JwtValidationService) | ✅ Correct |
| `UpdateDomainRegistration` | `Anonymous` | ✅ Yes (JwtValidationService) | ✅ Correct |
| `SearchPenguinAuthors` | `Anonymous` | ✅ Yes (JwtAuthenticationHelper) | ✅ Correct |
| `GetPenguinTitlesByAuthor` | `Anonymous` | ✅ Yes (JwtAuthenticationHelper) | ✅ Correct |
| `SearchAmazonBooksByAuthor` | `Anonymous` | ✅ Yes (JwtAuthenticationHelper) | ✅ Correct |

#### Public Endpoints (No Authentication Required - Correct)

| Function | Authorization Level | Purpose | Status |
|----------|-------------------|---------|---------|
| `GetAuthors` | `Anonymous` | Public author data | ✅ Correct |
| `GetStateProvinces` | `Anonymous` | Public geographic data | ✅ Correct |
| `GetStateProvincesByCountry` | `Anonymous` | Public geographic data | ✅ Correct |
| `GetLanguages` | `Anonymous` | Public language data | ✅ Correct |
| `GetCountriesByLanguage` | `Anonymous` | Public country data | ✅ Correct |
| `LocalizedText` | `Anonymous` | Public localization data | ✅ Correct |
| `GetTestimonials` | `Anonymous` | Public testimonials | ✅ Correct |
| `GetExperiments` | `Anonymous` | Public A/B test data | ✅ Correct |
| `GetPlatformStats` | `Anonymous` | Public platform statistics | ✅ Correct |
| `ReferralFunction` (POST/GET) | `Anonymous` | Public referral tracking | ✅ Correct |
| `LeadCaptureFunction` | `Anonymous` | Public lead capture | ✅ Correct |
| `GetPersonFacts` | `Anonymous` | Public Wikipedia data | ✅ Correct |

#### Admin Endpoints with `[Authorize]` (Correct)

| Function | Authorization Level | Declarative Auth | Status |
|----------|-------------------|-----------------|---------|
| `CreateTestimonial` | `Anonymous` | ✅ `[Authorize]` | ✅ Correct |
| `UpdateTestimonial` | `Anonymous` | ✅ `[Authorize]` | ✅ Correct |
| `DeleteTestimonial` | `Anonymous` | ✅ `[Authorize]` | ✅ Correct |

#### Test Harness Endpoints (Correct)

| Function | Authorization Level | Purpose | Status |
|----------|-------------------|---------|---------|
| `EndToEndTestFunction` (Scenario 1) | `Function` | Internal testing | ✅ Correct |
| `EndToEndTestFunction` (Scenario 3) | `Function` | Internal testing | ✅ Correct |
| `TestHarnessFunction` (FrontDoor) | `Function` | Internal testing | ✅ Correct |
| `TestHarnessFunction` (DNS) | `Function` | Internal testing | ✅ Correct |
| `TestCreateDnsZoneFunction` | `Function` | Internal testing | ✅ Correct |

**Impact:** None (already correct)  
**Priority:** N/A  
**Estimated Effort:** 0 minutes (already fixed)

---

### 4. function-app

**Status:** ✅ CORRECT  
**Issue:** None - all endpoints properly configured

| Function | Authorization Level | Purpose | Status |
|----------|-------------------|---------|---------|
| `GetAuthorData` | `Anonymous` | Public author data | ✅ Correct |
| `GetLocaleData` | `Anonymous` | Public localization | ✅ Correct |
| `GetSitemap` | `Anonymous` | Public sitemap | ✅ Correct |

**Impact:** None  
**Priority:** N/A  
**Estimated Effort:** 0 minutes

---

### 5. InkStainedWretchesConfig

**Status:** ✅ CORRECT  
**Issue:** None - internal configuration endpoints appropriately secured

| Function | Authorization Level | Purpose | Status |
|----------|-------------------|---------|---------|
| `GetApplicationConfig` | `Function` | Internal config retrieval | ✅ Correct |
| `GetPenguinApiKey` | `Function` | Internal API key retrieval | ✅ Correct |

**Rationale:**  
These are internal configuration endpoints that should NOT be publicly accessible. Using `AuthorizationLevel.Function` is appropriate as these are called by other Azure services or internal tools with function keys. **No JWT validation needed.**

**Impact:** None  
**Priority:** N/A  
**Estimated Effort:** 0 minutes

---

## Summary of Recommendations

### High Priority Changes Required

#### 1. ImageAPI (4 endpoints)

**Change:** `AuthorizationLevel.Function` → `Anonymous`  
**Affected Functions:**

- `Upload`
- `Delete`
- `User`
- `WhoAmI`

**Reason:** All use JWT validation, double authentication not needed

---

#### 2. InkStainedWretchStripe (5 endpoints)

**Change:** `AuthorizationLevel.Function` → `Anonymous`  
**Affected Functions:**

- `CreateStripeCheckoutSession`
- `CreateStripeCustomer`
- `CreateSubscription`
- `GetStripePriceInformation`
- `CancelSubscription`

**Reason:** All use JWT validation, double authentication not needed

---

#### 3. InkStainedWretchStripe - Review Required (1 endpoint)

**Function:** `GetStripeCheckoutSession`  
**Current:** `AuthorizationLevel.Function`, no JWT validation  
**Action:** Review code to determine if:

1. JWT validation should be added
2. Function-level auth is intentional for internal use
3. Session ownership validation exists

---

### Correct Configurations (No Changes Needed)

✅ **InkStainedWretchFunctions:** Already updated in recent fix  
✅ **function-app:** Public endpoints appropriately configured  
✅ **InkStainedWretchesConfig:** Internal endpoints correctly secured  
✅ **Test Harness Functions:** Appropriately use Function-level auth  
✅ **Stripe Webhook:** Correctly uses Anonymous (signature validation)  
✅ **Endpoints with `[Authorize]` attribute:** Declarative auth working correctly

---

## Implementation Checklist

### Phase 1: ImageAPI (High Priority)

- [ ] Update `Upload.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Update `Delete.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Update `User.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Update `WhoAmI.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Test all endpoints with valid JWT tokens
- [ ] Test all endpoints with invalid/missing tokens
- [ ] Deploy to development environment
- [ ] Validate production deployment

### Phase 2: InkStainedWretchStripe (High Priority)

- [ ] Update `CreateStripeCheckoutSession.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Update `CreateStripeCustomer.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Update `CreateSubscription.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Update `GetStripePriceInformation.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Update `CancelSubscription.cs` - Change to `AuthorizationLevel.Anonymous`
- [ ] Review `GetStripeCheckoutSession.cs` - Determine appropriate auth strategy
- [ ] Test all endpoints with valid JWT tokens
- [ ] Test all endpoints with invalid/missing tokens
- [ ] Test webhook endpoint still works (uses signature validation)
- [ ] Deploy to development environment
- [ ] Validate production deployment

### Phase 3: Testing & Validation

- [ ] Run comprehensive authentication tests
- [ ] Verify no regression in existing functionality
- [ ] Update API documentation
- [ ] Update client integration guides
- [ ] Notify client developers of changes (if function keys were being used)

### Phase 4: Documentation Updates

- [ ] Update `AUTHORIZATION_FIX_DOCUMENTATION.md`
- [ ] Update API documentation with examples
- [ ] Create migration guide for any clients using function keys
- [ ] Update runbooks and troubleshooting guides

---

## Authorization Best Practices

### When to Use Each Authorization Level

| Authorization Level | Use Case | Security Provided By |
|---------------------|----------|---------------------|
| `Anonymous` | Public endpoints OR endpoints with custom auth (JWT) | Your authentication code |
| `Function` | Internal/admin endpoints requiring Azure Functions keys | Azure Functions host |
| `Admin` | System admin operations | Azure Functions master key |

### JWT Authentication Pattern

✅ **Recommended Pattern:**

```csharp
[Function("ProtectedEndpoint")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
{
    // Validate JWT token
    var (user, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(
        req, _jwtValidationService, _logger);
    
    if (authError != null)
        return authError;
    
    // Proceed with authenticated user
    // ...
}
```

✅ **Alternative Pattern (Declarative):**

```csharp
[Function("ProtectedEndpoint")]
[Authorize] // ASP.NET Core authorization
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
{
    // User already authenticated by middleware
    var user = req.HttpContext.User;
    // ...
}
```

❌ **Anti-Pattern (Double Authentication):**

```csharp
// DON'T DO THIS:
[HttpTrigger(AuthorizationLevel.Function, "get")] // Requires function key
public async Task<IActionResult> Run(HttpRequest req)
{
    // ALSO requires JWT token
    var (user, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(...);
    // Clients must provide BOTH function key AND JWT token!
}
```

---

## Testing Recommendations

### Unit Tests

- [ ] Test JWT validation logic
- [ ] Test expired token rejection
- [ ] Test invalid signature rejection
- [ ] Test missing Authorization header
- [ ] Test malformed tokens

### Integration Tests

- [ ] Test each protected endpoint with valid JWT
- [ ] Test each protected endpoint without JWT (expect 401)
- [ ] Test each protected endpoint with invalid JWT (expect 401)
- [ ] Test public endpoints work without JWT

### Security Tests

- [ ] Verify tokens from wrong tenant are rejected
- [ ] Verify tokens with wrong audience are rejected
- [ ] Verify expired tokens are rejected
- [ ] Verify tampered tokens are rejected
- [ ] Verify no secrets are logged

---

## Monitoring & Observability

### Metrics to Track

- Authentication success/failure rate
- 401 Unauthorized response rate by endpoint
- Invalid token types (expired, wrong tenant, etc.)
- Average token validation time

### Application Insights Queries

#### Authentication Failure Rate

```kql
requests
| where resultCode == 401
| summarize FailureCount = count() by name, bin(timestamp, 5m)
| order by timestamp desc
```

#### Token Validation Errors

```kql
traces
| where message contains "JWT" or message contains "token"
| where severityLevel >= 2
| summarize Count = count() by message
| order by Count desc
```

---

## References

- **Authorization Fix Documentation:** `AUTHORIZATION_FIX_DOCUMENTATION.md`
- **Azure Functions HTTP Authorization:** <https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger#http-auth>
- **Microsoft Entra ID Token Validation:** <https://learn.microsoft.com/entra/identity-platform/access-tokens#validate-tokens>

---

**Report Generated:** 2025-12-27  
**Last Updated:** 2025-12-27  
**Next Review:** After implementing recommendations  
**Status:** ⚠️ Action Required - 10 endpoints need updates
