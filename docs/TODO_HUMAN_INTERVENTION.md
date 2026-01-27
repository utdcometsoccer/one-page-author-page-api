# Human Intervention To-Do List

**Created:** 2025-12-27  
**Last Updated:** 2025-12-30  
**Priority Focus:** Domain Registration Validation Testing  
**Status:** Active - Domain Registration Validation Phase

## Overview

This document outlines tasks that require human intervention, judgment, or access to external systems that cannot be automated by Copilot AI. Each task includes context, priority, and actionable steps.

**Recent Update (2025-12-30):**

- Standardized error handling completed (PR #203)
- Authentication validation completed and confirmed satisfactory
- Immediate focus is now on Domain Registration validation testing. Human intervention is required to configure environments, execute manual tests with real domains, and validate production functionality.

---

## 🟢 RECENT ACCOMPLISHMENTS

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

## 🔴 CRITICAL PRIORITY - Domain Registration Validation Testing

**Context:** Domain registration implementation is complete with Google Domains integration. Comprehensive automated tests are being created by Copilot AI. Human validation is required to test with real domains and production Google Domains API.

### 1. Test Domain Registration Flow ⚠️ **CRITICAL VALIDATION**

**Status:** ⚠️ **URGENT - START AFTER COPILOT TESTS COMPLETE**  
**Estimated Time:** 3-4 hours  
**Prerequisites:** Google Domains configured, test domain available, Copilot AI tests passing  
**Due Date:** January 8, 2026

**Context:**  
End-to-end validation of domain registration workflow with REAL Google Domains API and test domain. **This is the critical validation after automated tests pass.**

**Prerequisites:**

- [ ] Copilot AI domain registration tests are passing (115+ tests)
- [ ] Google Domains API access configured (see Task 2 below if needed)
- [ ] Test Stripe subscription is active
- [ ] Test domain available (e.g., cheap .xyz or .test domain)
- [ ] Azure DNS and Front Door configured

**Preparation:**

- [ ] Choose test domain (use `.test` or cheap domain for testing like `.xyz`)
- [ ] Ensure test Stripe subscription is active
- [ ] Have valid test contact information ready
- [ ] Budget approved for test domain cost

**Action Items:**

- [ ] Log into Azure Portal (<https://portal.azure.com>)
- [ ] Navigate to Microsoft Entra ID → App Registrations
- [ ] Verify/Create application registration for OnePageAuthor API
- [ ] Configure Redirect URIs for all environments:
  - Development: `https://localhost:7071/.auth/login/aad/callback`
  - Staging: `https://[staging-url]/.auth/login/aad/callback`
  - Production: `https://[production-url]/.auth/login/aad/callback`
- [ ] Add API Permissions:
  - Microsoft Graph: `User.Read` (Delegated)
  - Custom scopes as needed for API access
- [ ] Create Client Secret (if not exists):
  - Navigate to "Certificates & secrets"
  - Create new client secret
  - **IMPORTANT:** Save the secret value immediately (only shown once)
  - Document expiration date for renewal
- [ ] Configure Token Configuration:
  - Add optional claims if needed (email, name, etc.)
  - Configure ID token, Access token settings
- [ ] Document the following values:
  - Tenant ID: `________________________________________`
  - Client ID (Application ID): `________________________________________`
  - Client Secret: `________________________________________` (SECURE!)

**Validation:**

- [ ] Verify application shows in App Registrations
- [ ] Test authentication flow in development environment
- [ ] Verify tokens are issued correctly

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

#### Google Domains (Optional - if using Google Domains)

```
GOOGLE_APPLICATION_CREDENTIALS = [Path to service account JSON]
GOOGLE_CLOUD_PROJECT = [Google Cloud Project ID]
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

## 🔴 CRITICAL PRIORITY - Domain Registration Validation Testing

**Context:** Domain registration implementation is complete with Google Domains integration. Comprehensive automated tests are being created by Copilot AI. Human validation is required to test with real domains and production APIs.

### 6. Test Domain Registration Flow ⚠️ **CRITICAL VALIDATION**

**Status:** ⚠️ **URGENT - START AFTER COPILOT TESTS COMPLETE**  
**Estimated Time:** 3-4 hours  
**Prerequisites:** Google Domains configured, test domain available, Copilot AI tests passing  
**Due Date:** January 6, 2026

**Context:**  
The system can register domains through Google Domains API. This requires proper GCP setup.

**Action Items:**

- [ ] Log into Google Cloud Console (<https://console.cloud.google.com>)
- [ ] Create or select existing project for domain management
- [ ] Enable Google Domains API:
  - Navigate to APIs & Services → Library
  - Search for "Cloud Domains API"
  - Click "Enable"
- [ ] Create Service Account:
  - Navigate to IAM & Admin → Service Accounts
  - Click "Create Service Account"
  - Name: `domain-registration-service`
  - Grant roles:
    - `Cloud Domains Owner`
    - `DNS Administrator` (if managing DNS)
- [ ] Create and download JSON key:
  - Click on created service account
  - Go to "Keys" tab
  - Add Key → Create new key → JSON
  - **IMPORTANT:** Securely store the downloaded JSON file
- [ ] Configure billing:
  - Ensure billing is enabled for the project
  - Review pricing for domain registrations
- [ ] Test API access:

  ```bash
  gcloud auth activate-service-account --key-file=service-account-key.json
  gcloud domains registrations list
  ```

**Upload Service Account to Azure:**

- [ ] Option 1: Azure Key Vault
  - Upload JSON as secret
  - Reference in Function App configuration
- [ ] Option 2: App Configuration
  - Base64 encode the JSON
  - Store as environment variable

  ```bash
  base64 service-account-key.json
  ```

**Validation:**

- [ ] Verify API is enabled in GCP Console
- [ ] Test service account has proper permissions
- [ ] Function App can authenticate to Google Domains API

---

### 6. Test Domain Registration Flow

**Status:** ⏳ Required  
**Estimated Time:** 2-3 hours  
**Prerequisites:** Google Domains configured, test domain available

**Context:**  
End-to-end validation of domain registration workflow with REAL Google Domains API and test domain. **This is the critical validation after automated tests pass.**

**Prerequisites:**

- [ ] Copilot AI domain registration tests are passing (115+ tests)
- [ ] Google Domains API access configured (Task 5 - see below if needed)
- [ ] Test Stripe subscription is active
- [ ] Test domain available (e.g., cheap .xyz or .test domain)
- [ ] Azure DNS and Front Door configured

**Preparation:**

- [ ] Choose test domain (use `.test` or cheap domain for testing like `.xyz`)
- [ ] Ensure test Stripe subscription is active
- [ ] Have valid test contact information ready
- [ ] Budget approved for test domain cost

**Test Scenarios:**

#### Scenario 1: Successful Domain Registration

- [ ] Use Postman to POST to `/api/domain-registrations`
- [ ] Payload:

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

- [ ] Expected: 201 Created with registration ID
- [ ] Verify in Cosmos DB: Domain registration record created
- [ ] Verify in Google Domains Console: Domain registration initiated
- [ ] Document registration ID for further testing

#### Scenario 2: Retrieve Domain Registration

- [ ] GET `/api/domain-registrations/{registrationId}`
- [ ] Expected: 200 OK with full registration details
- [ ] Verify status reflects current state

#### Scenario 3: List User's Domains

- [ ] GET `/api/domain-registrations`
- [ ] Expected: 200 OK with array of user's domains
- [ ] Verify pagination works (if implemented)

#### Scenario 4: Update Domain Registration

- [ ] PUT `/api/domain-registrations/{registrationId}`
- [ ] Update auto-renew or contact information
- [ ] Expected: 200 OK with updated details

#### Scenario 5: Error Cases

- [ ] Try to register already-registered domain
  - Expected: 409 Conflict
- [ ] Try to register without subscription
  - Expected: 403 Forbidden
- [ ] Try with invalid domain name
  - Expected: 400 Bad Request
- [ ] Try with incomplete contact information
  - Expected: 400 Bad Request with validation errors

**Document Results:**

- [ ] Create comprehensive test report with findings
- [ ] Screenshot key API responses
- [ ] Note any issues or improvements needed
- [ ] Verify domain registration costs in Google Console
- [ ] **Update validation status in roadmap**

---

### 5. Configure Google Domains API Access (If Not Already Done)

**Status:** ⏳ Required (Verify/Configure if using Google Domains)  
**Estimated Time:** 1-2 hours  
**Prerequisites:** Google Cloud Platform account, billing enabled  
**Due Date:** Before Task 6 (Domain Testing)

---

## 🔴 CRITICAL PRIORITY - DNS Configuration Validation Testing

**Context:** DNS configuration with Azure DNS and Front Door is implemented. Comprehensive automated tests are being created by Copilot AI. Human validation is required to test in real Azure environments with actual domains.

### 9. Test DNS Zone Creation Workflow ⚠️ **CRITICAL VALIDATION**

**Status:** ⚠️ **URGENT - PARALLEL WITH DOMAIN TESTING**  
**Estimated Time:** 2-3 hours  
**Prerequisites:** Azure DNS configured, test domain available, Copilot AI tests passing  
**Due Date:** January 6, 2026

**Context:**  
Automated DNS configuration requires proper Azure DNS setup and permissions.

**Action Items:**

#### Create Resource Group (if not exists)

- [ ] Navigate to Azure Portal → Resource Groups
- [ ] Click "Create"
- [ ] Name: `rg-onepageauthor-dns` (or your naming convention)
- [ ] Region: Same as Function Apps (e.g., East US)
- [ ] Click "Review + Create"

#### Assign Managed Identity Permissions

- [ ] For each Function App that manages DNS:
  1. Navigate to Function App → Identity
  2. Enable System Assigned Managed Identity
  3. Click "Azure role assignments"
  4. Add role assignments:
     - Scope: Resource group
     - Resource group: DNS resource group
     - Role: `DNS Zone Contributor`
     - Role: `Reader` (for listing zones)

#### Verify DNS Zone Creation Capability

- [ ] Test DNS zone creation manually:
  - Navigate to DNS zones
  - Click "Create DNS zone"
  - Test with a domain you own
  - Verify nameservers are provided
  - Delete test zone if not needed

**Required Configuration Values:**

```
AZURE_SUBSCRIPTION_ID = [Your subscription GUID]
AZURE_DNS_RESOURCE_GROUP = rg-onepageauthor-dns
```

**Validation:**

- [ ] Managed identities are enabled
- [ ] Permissions are assigned correctly
- [ ] Can create DNS zones programmatically

---

### 10. Test Front Door Domain Addition Workflow ⚠️ **CRITICAL VALIDATION**

**Status:** ⚠️ **URGENT - AFTER DNS TESTING**  
**Estimated Time:** 3-4 hours  
**Prerequisites:** Front Door configured, DNS zone exists, Copilot AI tests passing  
**Due Date:** January 7, 2026

**Context:**  
Azure Front Door provides CDN and custom domain management for the platform.

**Action Items:**

#### Create Azure Front Door Profile (if not exists)

- [ ] Navigate to Azure Portal → Front Door and CDN profiles
- [ ] Click "Create"
- [ ] Select "Azure Front Door" (Standard or Premium)
- [ ] Configuration:
  - Name: `afd-onepageauthor-prod`
  - Resource group: Create new or use existing
  - Tier: Standard (or Premium for WAF)
- [ ] Configure Origin:
  - Origin type: Custom
  - Host name: Your Function App hostname
  - Origin host header: Same as host name
- [ ] Configure Endpoint:
  - Endpoint name: Unique name
  - Enable origin: Yes
- [ ] Click "Review + Create"

#### Assign Managed Identity Permissions

- [ ] For InkStainedWretchFunctions:
  1. Navigate to Function App → Identity
  2. Enable System Assigned Managed Identity
  3. Add role assignments:
     - Scope: Resource group (Front Door resource group)
     - Role: `CDN Profile Contributor`
     - Role: `CDN Endpoint Contributor`

#### Document Configuration

```
AZURE_SUBSCRIPTION_ID = [Subscription GUID]
AZURE_RESOURCE_GROUP_NAME = [Front Door resource group]
AZURE_FRONTDOOR_PROFILE_NAME = afd-onepageauthor-prod
```

**Validation:**

- [ ] Front Door profile is created and running
- [ ] Can access endpoint through Front Door URL
- [ ] Managed identity has permissions
- [ ] Test adding custom domain manually

---

### 9. Test DNS Zone Creation Workflow

**Status:** ⏳ Required  
**Estimated Time:** 1-2 hours  
**Prerequisites:** Azure DNS configured, test domain available

**Context:**  
Validate that DNS zones are automatically created for domain registrations in REAL Azure environment. **Critical validation after automated tests pass.**

**Prerequisites:**

- [ ] Copilot AI DNS tests are passing (90+ tests)
- [ ] Azure DNS resources configured (Task 7 - see below if needed)
- [ ] Test domain from Task 6 available
- [ ] Azure Front Door configured (Task 8 - see below if needed)

#### Scenario 1: Automatic DNS Zone Creation

- [ ] Create domain registration (from step 6)
- [ ] Verify DomainRegistrationTriggerFunction is triggered
- [ ] Check Azure DNS zones - verify new zone created
- [ ] Zone name should match domain (e.g., `example.com`)
- [ ] Verify NS records are created automatically
- [ ] Document nameservers provided

#### Scenario 2: Verify DNS Zone Records

- [ ] Navigate to Azure Portal → DNS zones → [your domain]
- [ ] Verify default records exist:
  - NS records (nameservers)
  - SOA record (start of authority)
- [ ] Test adding A record manually
- [ ] Test adding CNAME record manually
- [ ] Verify records are queryable:

  ```bash
  nslookup example.com [azure-nameserver]
  dig @[azure-nameserver] example.com
  ```

#### Scenario 3: Check DNS Zone Existence API

- [ ] Test `DnsZoneExistsAsync` method through test harness
- [ ] Expected: Returns true for created zones
- [ ] Expected: Returns false for non-existent domains

#### Scenario 4: DNS Zone Error Handling

- [ ] Try to create zone with invalid domain name
  - Expected: Error logged, graceful failure
- [ ] Try to create duplicate zone
  - Expected: Detects existing zone, no duplicate created
- [ ] Try with insufficient permissions
  - Expected: Clear error message about permissions

**Document Results:**

- [ ] Screenshot DNS zone in Azure Portal
- [ ] Document nameservers assigned
- [ ] Note any issues with zone creation
- [ ] Verify propagation time estimates
- [ ] **Update validation status in roadmap**

---

### 7. Configure Azure DNS Resources (If Not Already Done)

**Status:** ⏳ Required (Verify/Configure)  
**Estimated Time:** 1-2 hours  
**Prerequisites:** Azure subscription with DNS Zone capability  
**Due Date:** Before Task 9 (DNS Testing)

---

### 10. Test Front Door Domain Addition Workflow

**Status:** ⏳ Required  
**Estimated Time:** 2-3 hours  
**Prerequisites:** Front Door configured, DNS zone exists

**Context:**  
Validate automatic addition of custom domains to Azure Front Door in REAL production environment. **Final critical validation step.**

**Prerequisites:**

- [ ] Copilot AI Front Door tests are passing
- [ ] DNS zone exists for test domain (Task 9 complete)
- [ ] Azure Front Door configured (Task 8 - see below if needed)
- [ ] Domain registration complete (Task 6)

**Test Scenarios:**

#### Scenario 1: Add Domain to Front Door

- [ ] Trigger domain registration with DNS zone created
- [ ] Verify `AddDomainToFrontDoorAsync` is called
- [ ] Check Azure Portal → Front Door → Domains
- [ ] Verify new custom domain appears
- [ ] Status may be "Pending" initially

#### Scenario 2: Domain Validation

- [ ] Front Door requires domain ownership validation
- [ ] Navigate to custom domain details
- [ ] Copy validation TXT record details:
  - Name: `_dnsauth.example.com`
  - Value: `[validation-token]`
- [ ] Add TXT record to Azure DNS zone:
  - Can be manual or automated
- [ ] Wait for validation (may take 5-30 minutes)
- [ ] Verify domain status changes to "Approved"

#### Scenario 3: HTTPS Certificate

- [ ] After domain validated, configure HTTPS
- [ ] Option 1: Front Door Managed Certificate
  - Navigate to domain → HTTPS settings
  - Select "Front Door managed"
  - Certificate type: "Front Door managed certificate"
  - Minimum TLS: 1.2
- [ ] Option 2: Custom Certificate (from Key Vault)
  - Upload certificate to Key Vault
  - Grant Front Door access
  - Select certificate
- [ ] Wait for certificate provisioning (10-30 minutes)
- [ ] Verify HTTPS works: `https://example.com`

#### Scenario 4: Test Domain Routing

- [ ] Access custom domain through browser
- [ ] Verify routes to correct origin (Function App)
- [ ] Test various endpoints:
  - `https://example.com/api/authors/{author}/{domain}`
  - Should work with custom domain
- [ ] Verify response headers include Front Door information

#### Scenario 5: Error Cases

- [ ] Try to add already-added domain
  - Expected: Detects existing, no duplicate
- [ ] Try to add domain without DNS zone
  - Expected: Error or automatic DNS zone creation
- [ ] Try with invalid domain format
  - Expected: Validation error

**Document Results:**

- [ ] Screenshot Front Door configuration
- [ ] Document validation process and timing
- [ ] Note certificate provisioning time
- [ ] Test custom domain in browser with screenshots
- [ ] Screenshot working custom domain with HTTPS
- [ ] **Update validation status in roadmap**
- [ ] **Create validation summary report**

---

### 8. Configure Azure Front Door (If Not Already Done)

**Status:** ⏳ Required (Verify/Configure)  
**Estimated Time:** 2-3 hours  
**Prerequisites:** Azure subscription with Front Door capability  
**Due Date:** Before Task 10 (Front Door Testing)

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

### 🔴 Critical - Validation Testing (IMMEDIATE - This Week)

**Authentication Validation (January 3-4, 2026):**

1. ✅/⏳ Configure Azure Entra ID Application Registration (Verify)
2. ✅/⏳ Configure Environment Variables for All Function Apps (Verify)
3. ✅/⏳ Update GitHub Secrets for CI/CD Pipeline (Verify)
4. ⚠️ Test Authentication Flow End-to-End (CRITICAL VALIDATION)

**Domain Registration Validation (January 5-6, 2026):**
5. ✅/⏳ Configure Google Domains API Access (Verify if needed)
6. ⚠️ Test Domain Registration Flow (CRITICAL VALIDATION)

**DNS Configuration Validation (January 6-7, 2026):**
7. ✅/⏳ Configure Azure DNS Resources (Verify)
8. ✅/⏳ Configure Azure Front Door (Verify)
9. ⚠️ Test DNS Zone Creation Workflow (CRITICAL VALIDATION)
10. ⚠️ Test Front Door Domain Addition Workflow (CRITICAL VALIDATION)

### Medium Priority (Should Complete After Validation)

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
- Google Domains API: <https://cloud.google.com/domains/docs>
- Microsoft Entra ID: <https://learn.microsoft.com/entra>
- Internal documentation in `/docs` directory

---

**Last Updated:** 2025-12-30  
**Maintained By:** Development Team  
**Review Frequency:** After each major milestone  
**Next Review:** January 10, 2026 (after validation phase complete)
