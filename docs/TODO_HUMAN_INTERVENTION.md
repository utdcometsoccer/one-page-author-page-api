# Human Intervention To-Do List

**Created:** 2025-12-27  
**Last Updated:** 2026-03-30  
**Priority Focus:** North America Launch — Production Configuration  
**Status:** 🟢 ALL BLOCKERS RESOLVED — Production Configuration Phase

## 🚀 LAUNCH CONTEXT

**Launch Target:** Q1 2026 (production configuration is the final step)  
**Platform Readiness:** 100% — All validation complete  
**Launch Documentation:** See [LAUNCH_READINESS_PLAN.md](LAUNCH_READINESS_PLAN.md) and [MINIMUM_VIABLE_LAUNCH.md](MINIMUM_VIABLE_LAUNCH.md)

### Launch Blocker Status

| Blocker | Status | ETA | Impact |
|---------|--------|-----|--------|
| Domain registration E2E test | ✅ Complete | Done | ~~LAUNCH BLOCKER~~ |
| DNS automation validation | ✅ Complete | Done | ~~LAUNCH BLOCKER~~ |
| Front Door integration test | ✅ Complete | Done | ~~LAUNCH BLOCKER~~ |

**All blockers resolved. Platform is ready for production configuration and launch.**

## Overview

This document outlines tasks that require human intervention, judgment, or access to external systems that cannot be automated by Copilot AI. Each task includes context, priority, and actionable steps.

**Recent Updates:**

- **2026-03-30:** DNS automation and Front Door integration validated and confirmed complete — all blockers resolved
- **2026-03-30:** End-to-end domain registration validated and confirmed complete
- **2026-02-11:** Launch readiness analysis complete - Domain validation identified as sole blocker
- **2025-12-30:** Standardized error handling completed (PR #203)
- **2025-12-30:** Authentication validation completed and confirmed satisfactory

---

## 🟢 RECENT ACCOMPLISHMENTS

### DNS and Front Door Validation ✅ COMPLETE (2026-03-30)

- ✅ Azure DNS zone creation validated in real Azure environment
- ✅ DNS zone auto-creation triggered correctly from domain registration
- ✅ Nameservers assigned and stored in DomainRegistration entity
- ✅ Azure Front Door domain addition confirmed working
- ✅ HTTPS certificate provisioning confirmed
- ✅ Custom domain routing to author pages validated

**Impact:** All domain infrastructure is fully operational. Custom domains now work end-to-end from registration through DNS to live site.

### Domain Registration E2E Validation ✅ COMPLETE (2026-03-30)

- ✅ End-to-end domain registration workflow validated with real domains
- ✅ WHMCS API integration tested and operational
- ✅ Domain registration records created in Cosmos DB
- ✅ All domain registration API endpoints verified (POST, GET, PUT)
- ✅ Error cases validated (unavailable domain, missing subscription, invalid input)
- ✅ Queue-based architecture (Service Bus + WhmcsWorkerService) confirmed working

**Impact:** Domain registration is fully operational end-to-end. This was the primary launch blocker and is now resolved.

### Authentication System Validation ✅ COMPLETE (2025-12-30)

- ✅ JWT authentication validated and operational
- ✅ Authorization configurations verified
- ✅ Microsoft Entra ID integration tested
- ✅ Production authentication flows confirmed satisfactory

**Impact:** Security foundation validated and operational

### Standardized Error Handling ✅ COMPLETE (2025-12-30)

- ✅ Implemented consistent error response format across all APIs  
- ✅ Deployed to production successfully
- ✅ Client integration improved

**Impact:** Significantly improved error debugging and client integration

---

## 🟢 COMPLETED - Domain Registration Validation Testing

**Context:** Domain registration implementation and end-to-end validation is complete.

### 1. Test Domain Registration Flow ✅ **COMPLETE**

**Status:** ✅ **COMPLETE (2026-03-30)**  
**Estimated Time:** 3-4 hours  
**Prerequisites:** WHMCS configured, test domain available, Copilot AI tests passing  
**Due Date:** ~~January 8, 2026~~ **Completed March 30, 2026**

**Context:**  
End-to-end validation of domain registration workflow with WHMCS and test domain completed successfully.

**Prerequisites:**

- [x] Copilot AI domain registration tests are passing (115+ tests)
- [x] WHMCS API access configured
- [x] Test Stripe subscription is active
- [x] Test domain available (e.g., cheap .xyz or .test domain)
- [x] Azure DNS and Front Door configured

**Preparation:**

- [x] Choose test domain (use `.test` or cheap domain for testing like `.xyz`)
- [x] Ensure test Stripe subscription is active
- [x] Have valid test contact information ready
- [x] Budget approved for test domain cost

**Action Items:**

- [x] Log into Azure Portal (<https://portal.azure.com>)
- [x] Navigate to Microsoft Entra ID → App Registrations
- [x] Verify/Create application registration for OnePageAuthor API
- [x] Configure Redirect URIs for all environments:
  - Development: `https://localhost:7071/.auth/login/aad/callback`
  - Staging: `https://[staging-url]/.auth/login/aad/callback`
  - Production: `https://[production-url]/.auth/login/aad/callback`
- [x] Add API Permissions:
  - Microsoft Graph: `User.Read` (Delegated)
  - Custom scopes as needed for API access
- [x] Create Client Secret (if not exists):
  - Navigate to "Certificates & secrets"
  - Create new client secret
  - **IMPORTANT:** Save the secret value immediately (only shown once)
  - Document expiration date for renewal
- [x] Configure Token Configuration:
  - Add optional claims if needed (email, name, etc.)
  - Configure ID token, Access token settings
- [x] Document the following values:
  - Tenant ID: `________________________________________`
  - Client ID (Application ID): `________________________________________`
  - Client Secret: `________________________________________` (SECURE!)

**Validation:**

- [x] Verify application shows in App Registrations
- [x] Test authentication flow in development environment
- [x] Verify tokens are issued correctly

---

### 2. Configure Environment Variables for All Function Apps

**Status:** ⏳ Required  
**Estimated Time:** 30-45 minutes per environment  
**Prerequisites:** Azure Portal access to Function Apps

**Context:**  
All Azure Function apps require proper environment variables for authentication to work correctly.

**Function Apps to Configure:**

1. ImageAPI
2. InkStainedWretchFunctions
3. InkStainedWretchStripe
4. function-app
5. InkStainedWretchesConfig

**Action Items for Each Function App:**

- [ ] Navigate to Azure Portal → Function App → Configuration → Application Settings
- [ ] Add/Verify the following settings:

#### Authentication Settings (CRITICAL)

```
AAD_TENANT_ID = [Your Entra ID Tenant GUID]
AAD_AUDIENCE = [Your Application/Client ID]
AAD_CLIENT_ID = [Same as AAD_AUDIENCE or separate if using different client]
```

#### Optional OpenID Connect Override

```
OPEN_ID_CONNECT_METADATA_URL = https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration
```

#### Cosmos DB Settings

```
COSMOSDB_ENDPOINT_URI = https://[your-account].documents.azure.com:443/
COSMOSDB_PRIMARY_KEY = [Your Cosmos DB Primary Key]
COSMOSDB_DATABASE_ID = OnePageAuthor
```

#### Stripe Settings (for InkStainedWretchStripe)

```
STRIPE_API_KEY = sk_live_... or sk_test_...
STRIPE_WEBHOOK_SECRET = whsec_...
STRIPE_PUBLISHABLE_KEY = pk_live_... or pk_test_...
```

#### Azure Resources (for Domain/DNS functions)

```
AZURE_SUBSCRIPTION_ID = [Azure Subscription GUID]
AZURE_RESOURCE_GROUP_NAME = [Resource Group for Front Door]
AZURE_DNS_RESOURCE_GROUP = [Resource Group for DNS Zones]
AZURE_FRONTDOOR_PROFILE_NAME = [Front Door Profile Name]
```

**Validation:**

- [ ] Click "Save" after adding settings
- [ ] Restart Function App
- [ ] Check Application Insights for startup logs
- [ ] Verify no configuration validation errors

---

### 3. Update GitHub Secrets for CI/CD Pipeline

**Status:** ⏳ Required  
**Estimated Time:** 15-30 minutes  
**Prerequisites:** GitHub repository admin access

**Context:**  
GitHub Actions workflows need updated secrets for deployment and testing.

**Action Items:**

- [ ] Navigate to GitHub Repository → Settings → Secrets and variables → Actions
- [ ] Add/Update the following secrets:

#### Azure Deployment

```
AZURE_CREDENTIALS = [Service Principal JSON]
AZURE_SUBSCRIPTION_ID = [Subscription GUID]
AZURE_RESOURCE_GROUP = [Resource Group Name]
```

#### Function App Publishing Profiles (per app)

```
IMAGEAPI_PUBLISH_PROFILE = [Download from Azure Portal]
INKSTAINEDWRETCHFUNCTIONS_PUBLISH_PROFILE = [Download from Azure Portal]
INKSTAINEDWRETCHSTRIPE_PUBLISH_PROFILE = [Download from Azure Portal]
FUNCTIONAPP_PUBLISH_PROFILE = [Download from Azure Portal]
```

#### Entra ID Configuration

```
AAD_TENANT_ID = [Tenant GUID]
AAD_CLIENT_ID = [Application ID]
AAD_CLIENT_SECRET = [Client Secret]
```

#### Cosmos DB

```
COSMOSDB_ENDPOINT_URI = [Cosmos DB URI]
COSMOSDB_PRIMARY_KEY = [Primary Key]
```

#### Stripe

```
STRIPE_API_KEY = [Secret Key]
STRIPE_WEBHOOK_SECRET = [Webhook Signing Secret]
```

**Validation:**

- [ ] Trigger a GitHub Actions workflow manually
- [ ] Verify deployment succeeds
- [ ] Check workflow logs for any secret-related errors

---

### 4. Test Authentication Flow End-to-End ⚠️ **CRITICAL VALIDATION**

**Status:** ⚠️ **URGENT - START AFTER COPILOT TESTS COMPLETE**  
**Estimated Time:** 2-3 hours  
**Prerequisites:** All above configurations complete, Copilot AI tests passing  
**Due Date:** January 4, 2026

**Context:**  
Comprehensive manual testing of authentication across all endpoints to ensure everything works in REAL production environments. **This is the critical validation step after automated tests pass.**

**Prerequisites:**

- [ ] Copilot AI authentication tests are passing (100+ tests)
- [ ] Environment variables configured in all Function Apps
- [ ] GitHub Secrets updated
- [ ] Real Microsoft Entra ID tokens available

**Test Scenarios:**

#### Scenario 1: Token Acquisition

- [ ] Use Postman or similar tool to obtain JWT token
- [ ] Method 1: Use OAuth 2.0 Authorization Code Flow
  - Auth URL: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize`
  - Token URL: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token`
  - Scopes: `api://{client-id}/.default` or custom scopes
- [ ] Method 2: Use Azure CLI

  ```bash
  az login
  az account get-access-token --resource api://{client-id}
  ```

- [ ] Verify token structure using jwt.io
- [ ] Confirm claims: `aud`, `iss`, `exp`, `upn`/`preferred_username`

#### Scenario 2: Test Protected Endpoints

Test each protected endpoint with valid token:

**ImageAPI:**

- [ ] `POST /api/upload` - Upload an image (with valid JWT)
- [ ] `GET /api/images/{imageId}` - Retrieve image metadata
- [ ] `DELETE /api/images/{imageId}` - Delete an image
- [ ] Expected: 200 OK responses

**InkStainedWretchFunctions:**

- [ ] `POST /api/domain-registrations` - Create domain registration
- [ ] `GET /api/domain-registrations` - List user's domains
- [ ] `GET /api/domain-registrations/{id}` - Get specific domain
- [ ] `PUT /api/domain-registrations/{id}` - Update domain
- [ ] Expected: 200/201 OK responses

**InkStainedWretchStripe:**

- [ ] `POST /api/CreateStripeCustomer` - Create customer
- [ ] `POST /api/CreateStripeCheckoutSession` - Create checkout
- [ ] `GET /api/ListSubscription/{customerId}` - List subscriptions
- [ ] Expected: 200 OK responses

#### Scenario 3: Test Unauthorized Access

- [ ] Test same endpoints WITHOUT Authorization header
- [ ] Expected: 401 Unauthorized
- [ ] Verify error message is clear and actionable

#### Scenario 4: Test Invalid/Expired Tokens

- [ ] Test with malformed token
- [ ] Test with token from wrong tenant
- [ ] Test with expired token
- [ ] Expected: 401 Unauthorized with appropriate error messages

**Document Results:**

- [ ] Create test report with screenshots
- [ ] Document any issues or unexpected behavior
- [ ] Update authentication documentation with findings

---

## 🟢 COMPLETED - Domain Registration Validation Testing

**Context:** Domain registration implementation and end-to-end validation is complete.

### 6. Test Domain Registration Flow ✅ **COMPLETE**

**Status:** ✅ **COMPLETE (2026-03-30)**  
**Estimated Time:** 3-4 hours  
**Prerequisites:** WHMCS configured, test domain available, Copilot AI tests passing  
**Due Date:** ~~January 6, 2026~~ **Completed March 30, 2026**

**Context:**  
The system registers domains through WHMCS. WHMCS setup and validation are complete.

**Action Items:**

- [x] Log into your WHMCS admin panel
- [x] Configure domain registrar settings in WHMCS
- [x] Set up WHMCS API credentials in Azure Function App settings
- [x] Test API access via WHMCS API

**Validation:**

- [x] Verify WHMCS API credentials are configured
- [x] Test API endpoint can be reached
- [x] Function App can authenticate to WHMCS API

---

### 6. Test Domain Registration Flow ✅ **COMPLETE**

**Status:** ✅ **COMPLETE (2026-03-30)**  
**Estimated Time:** 2-3 hours  
**Prerequisites:** WHMCS configured, test domain available

**Context:**  
End-to-end validation of domain registration workflow with WHMCS and test domain completed successfully.

**Prerequisites:**

- [x] Copilot AI domain registration tests are passing (115+ tests)
- [x] WHMCS API access configured
- [x] Test Stripe subscription is active
- [x] Test domain available (e.g., cheap .xyz or .test domain)
- [x] Azure DNS and Front Door configured

**Preparation:**

- [x] Choose test domain (use `.test` or cheap domain for testing like `.xyz`)
- [x] Ensure test Stripe subscription is active
- [x] Have valid test contact information ready
- [x] Budget approved for test domain cost

**Test Scenarios:**

#### Scenario 1: Successful Domain Registration

- [x] Use Postman to POST to `/api/domain-registrations`
- [x] Payload:

  ```json
  {
    "DomainName": "test-example.com",
    "RegistrationPeriodYears": 1,
    "AutoRenew": false,
    "PrivacyProtection": true,
    "ContactInfo": {
      "FirstName": "Test",
      "LastName": "User",
      "Email": "test@example.com",
      "Phone": "+1.5555555555",
      "Address": {
        "Street": "123 Test St",
        "City": "Austin",
        "State": "TX",
        "PostalCode": "78701",
        "Country": "US"
      }
    }
  }
  ```

- [x] Expected: 201 Created with registration ID
- [x] Verify in Cosmos DB: Domain registration record created
- [x] Document registration ID for further testing

#### Scenario 2: Retrieve Domain Registration

- [x] GET `/api/domain-registrations/{registrationId}`
- [x] Expected: 200 OK with full registration details
- [x] Verify status reflects current state

#### Scenario 3: List User's Domains

- [x] GET `/api/domain-registrations`
- [x] Expected: 200 OK with array of user's domains
- [x] Verify pagination works (if implemented)

#### Scenario 4: Update Domain Registration

- [x] PUT `/api/domain-registrations/{registrationId}`
- [x] Update auto-renew or contact information
- [x] Expected: 200 OK with updated details

#### Scenario 5: Error Cases

- [x] Try to register already-registered domain
  - Expected: 409 Conflict
- [x] Try to register without subscription
  - Expected: 403 Forbidden
- [x] Try with invalid domain name
  - Expected: 400 Bad Request
- [x] Try with incomplete contact information
  - Expected: 400 Bad Request with validation errors

**Document Results:**

- [x] Create comprehensive test report with findings
- [x] Screenshot key API responses
- [x] Note any issues or improvements needed
- [x] **Update validation status in roadmap**

---

### 5. Configure WHMCS API Access (If Not Already Done)

**Status:** ✅ Complete  
**Estimated Time:** 1-2 hours  
**Prerequisites:** WHMCS account, billing enabled  
**Due Date:** ~~Before Task 6 (Domain Testing)~~ **Complete**

---

## 🟢 COMPLETE - DNS Configuration Validation Testing

**Context:** DNS configuration with Azure DNS and Front Door is implemented and fully validated (2026-03-30).

### 9. Test DNS Zone Creation Workflow ✅ **COMPLETE (2026-03-30)**

**Status:** ✅ **COMPLETE (2026-03-30)**  

**What Was Validated:**
- Azure DNS resource group configured
- Managed identity permissions assigned correctly
- DNS zone creation via programmatic API confirmed working
- NS records returned and stored correctly

---

### 10. Test Front Door Domain Addition Workflow ✅ **COMPLETE (2026-03-30)**

**Status:** ✅ **COMPLETE (2026-03-30)**  

**What Was Validated:**
- Azure Front Door profile created and running
- Managed identity permissions confirmed correct
- Custom domain addition after DNS zone creation confirmed working
- HTTPS certificate provisioning confirmed
- Routing rules directing traffic to author pages confirmed

---

### 9. Test DNS Zone Creation Workflow ✅ **COMPLETE (2026-03-30)**

**Status:** ✅ **COMPLETE (2026-03-30)**

**Summary:** DNS zone creation validated in real Azure environment. All scenarios confirmed working.

---

### 7. Configure Azure DNS Resources ✅ **COMPLETE**

**Status:** ✅ Complete — Azure DNS resources configured and validated.

---

### 10. Test Front Door Domain Addition Workflow ✅ **COMPLETE (2026-03-30)**

**Status:** ✅ **COMPLETE (2026-03-30)**

**Summary:** Front Door integration validated in real Azure environment. Domain addition, HTTPS certificate provisioning, and routing all confirmed working.

---

### 8. Configure Azure Front Door ✅ **COMPLETE**

**Status:** ✅ Complete — Azure Front Door configured and validated.

---

## 🟡 MEDIUM PRIORITY - Infrastructure & Configuration

### 11. Review and Update Application Insights

**Status:** ⏳ Recommended  
**Estimated Time:** 30-60 minutes

**Action Items:**

- [ ] Verify Application Insights is connected to all Function Apps
- [ ] Configure alerting rules:
  - 401/403 authentication errors (threshold: 10 in 5 minutes)
  - 500 server errors (threshold: 5 in 5 minutes)
  - High latency (threshold: >2 seconds average)
  - Domain registration failures
  - DNS zone creation failures
- [ ] Set up availability tests for critical endpoints
- [ ] Configure smart detection
- [ ] Review and organize workbooks for monitoring

---

### 12. Update Documentation

**Status:** ⏳ Recommended  
**Estimated Time:** 2-4 hours

**Action Items:**

- [ ] Update `AUTHORIZATION_FIX_DOCUMENTATION.md` with latest findings
- [ ] Create/update `DOMAIN_REGISTRATION_GUIDE.md`
- [ ] Create/update `DNS_CONFIGURATION_GUIDE.md`
- [ ] Update `README.md` with configuration instructions
- [ ] Document common troubleshooting scenarios
- [ ] Add architecture diagrams for domain registration flow
- [ ] Update API documentation with authentication examples

---

### 13. Security Review

**Status:** ⏳ Recommended  
**Estimated Time:** 2-3 hours

**Action Items:**

- [ ] Review RBAC permissions on Azure resources
- [ ] Audit service accounts and their permissions
- [ ] Review secret expiration dates
- [ ] Implement secret rotation policy
- [ ] Review API rate limiting (if any)
- [ ] Verify CORS configuration is appropriate
- [ ] Check for any exposed secrets in code/config
- [ ] Review Application Insights data retention settings
- [ ] Ensure PII is not logged

---

### 14. Cost Optimization Review

**Status:** 🟢 Nice to Have  
**Estimated Time:** 1-2 hours

**Action Items:**

- [ ] Review Cosmos DB RU consumption
- [ ] Analyze Azure Functions execution costs
- [ ] Review Front Door bandwidth usage
- [ ] Check DNS zone query costs
- [ ] Review Application Insights data ingestion
- [ ] Identify opportunities for cost optimization
- [ ] Set up cost alerts and budgets

---

## 🟢 LOW PRIORITY - Enhancements

### 15. Set Up Monitoring Dashboard

**Status:** 🟢 Nice to Have  
**Estimated Time:** 2-3 hours

**Action Items:**

- [ ] Create Azure Dashboard for system overview
- [ ] Add key metrics:
  - Authentication success/failure rate
  - Domain registration status
  - DNS zone creation success rate
  - API response times
  - Error rates by endpoint
- [ ] Share dashboard with team
- [ ] Set up automated reports

---

### 16. Create Runbook for Common Issues

**Status:** 🟢 Nice to Have  
**Estimated Time:** 2-4 hours

**Action Items:**

- [ ] Document common authentication issues and solutions
- [ ] Document domain registration troubleshooting
- [ ] Document DNS configuration issues
- [ ] Create step-by-step resolution guides
- [ ] Include diagnostic queries and commands
- [ ] Add escalation procedures

---

## Summary of Human Tasks

### ✅ All Validation Testing Complete (2026-03-30)

**Authentication Validation:**

1. ✅ Configure Azure Entra ID Application Registration (Complete)
2. ✅ Configure Environment Variables for All Function Apps (Verify in production)
3. ✅ Update GitHub Secrets for CI/CD Pipeline (Verify)
4. ✅ Test Authentication Flow End-to-End (Complete)

**Domain Registration Validation:**
5. ✅ Configure WHMCS API Access (Complete)
6. ✅ Test Domain Registration Flow (COMPLETE - 2026-03-30)

**DNS Configuration Validation:**
7. ✅ Configure Azure DNS Resources (Complete)
8. ✅ Configure Azure Front Door (Complete)
9. ✅ Test DNS Zone Creation Workflow (COMPLETE - 2026-03-30)
10. ✅ Test Front Door Domain Addition Workflow (COMPLETE - 2026-03-30)

### ⚠️ Current Priority (Stage 2: Production Configuration)

1. Review and Update Application Insights
2. Update Documentation
3. Security Review
4. Cost Optimization Review

### Low Priority (Nice to Have)

1. Set Up Monitoring Dashboard
2. Create Runbook for Common Issues

---

## Notes for Execution

### Time Estimates

- **Critical Validation Tasks:** 10-15 hours total (This Week)
  - Authentication validation: 3-4 hours
  - Domain registration validation: 3-4 hours
  - DNS configuration validation: 4-6 hours
- **Medium Priority:** 7-12 hours total (Next Week)
- **Low Priority:** 4-7 hours total (As Time Allows)
- **Total Estimated Time:** 21-34 hours

### Recommended Approach

1. **Week 1 (Dec 30 - Jan 5):** Focus on critical validation
   - Start with Authentication validation (Tasks 1-4)
   - Parallel: Begin Domain Registration validation (Tasks 5-6)
   - Follow with DNS validation (Tasks 7-10)
2. **Week 2 (Jan 6-12):** Address findings and medium priority items
3. **Ongoing:** Low priority items as time allows

### Success Criteria for Validation Phase

- ✅ All automated tests passing (300+ tests from Copilot AI)
- ✅ Authentication works in production with real Entra ID tokens
- ✅ Domain registration workflow completes end-to-end with real domain
- ✅ DNS zones are created automatically for new domains
- ✅ Domains are added to Front Door automatically with HTTPS
- ✅ All manual validation tests documented with screenshots
- ✅ Validation summary report created
- ✅ Roadmap updated with validation results
- ✅ Any issues discovered are documented and prioritized

### Support Resources

- Azure Documentation: <https://docs.microsoft.com/azure>
- WHMCS API: <https://developers.whmcs.com/api-reference/>
- Microsoft Entra ID: <https://learn.microsoft.com/entra>
- Internal documentation in `/docs` directory

---

**Last Updated:** 2026-03-30  
**Maintained By:** Development Team  
**Review Frequency:** After each major milestone  
**Next Review:** After DNS and Front Door validation complete
