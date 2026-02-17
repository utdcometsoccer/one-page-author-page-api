# API Categorization Documentation

**Last Updated:** 2026-02-17  
**Total APIs:** 54 Azure Functions across 4 projects

## Table of Contents

- [Quick Reference](#quick-reference)
- [Categorization by Project](#categorization-by-project)
  - [1. ImageAPI - Image Management](#1-imageapi---image-management)
  - [2. InkStainedWretchFunctions - Core Platform APIs](#2-inkstainedwretchfunctions---core-platform-apis)
  - [3. InkStainedWretchStripe - Payment Processing](#3-inkstainedwretchstripe---payment-processing)
  - [4. function-app - Public Content APIs](#4-function-app---public-content-apis)
- [Categorization by Characteristics](#categorization-by-characteristics)
  - [By HTTP Method](#by-http-method)
  - [By Authentication](#by-authentication)
  - [By Data Modification](#by-data-modification)
  - [By Synchronicity](#by-synchronicity)
- [Summary Statistics](#summary-statistics)

---

## Quick Reference

### Authentication Quick Reference

| Category | Count | Examples |
|----------|-------|----------|
| **JWT Token Required** | 24 | Upload, Delete, CreateDomainRegistration |
| **[Authorize] Attribute** | 12 | CreateTestimonial, UpdateSubscription |
| **No Authentication** | 18 | GetTestimonials, GetLanguages, WebHook |
| **Stripe Signature** | 1 | WebHook (Stripe signature validation) |
| **Rate Limited** | 1 | CreateLead (10 req/min per IP) |

### Side Effects Quick Reference

| Category | Count | Examples |
|----------|-------|----------|
| **Read-Only (No Side Effects)** | 35 | GetTestimonials, GetAuthors, ListSubscription |
| **Creates Data** | 11 | Upload, CreateLead, CreateSubscription |
| **Updates Data** | 5 | UpdateTestimonial, UpdateSubscription |
| **Deletes Data** | 2 | Delete, DeleteTestimonial |
| **External API Calls** | 4 | SearchPenguinAuthors, SearchAmazonBooksByAuthor |

---

## Categorization by Project

### 1. ImageAPI - Image Management

**Project:** `ImageAPI/`  
**Total Functions:** 4  
**Primary Purpose:** User image upload, management, and retrieval  
**Authentication:** All require JWT Bearer token with `RequireScope.Read` policy

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **WhoAmI** | GET | `/api/whoami` | JWT Required ([Authorize]) | Read-only | Async | RequireScope.Read |
| **Upload** | POST | `/api/images/upload` | JWT Required ([Authorize]) | Creates (blob storage, DB) | Async | RequireScope.Read + Tier limits |
| **User** | GET | `/api/images/user` | JWT Required ([Authorize]) | Read-only | Async | RequireScope.Read |
| **Delete** | DELETE | `/api/images/{id}` | JWT Required ([Authorize]) | Deletes (blob storage, DB) | Async | RequireScope.Read + Owner only |

**Special Notes:**
- All functions validate subscription tier limits (Starter/Pro/Elite)
- Upload enforces file size, storage quota, bandwidth, and file count limits
- Delete enforces ownership - users can only delete their own images
- User returns only images belonging to authenticated user

---

### 2. InkStainedWretchFunctions - Core Platform APIs

**Project:** `InkStainedWretchFunctions/`  
**Total Functions:** 35  
**Primary Purpose:** Core platform functionality, domain management, localization, third-party integrations

#### 2.1 Testimonial Management

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **GetTestimonials** | GET | `/api/testimonials` | None | Read-only | Async | Public |
| **CreateTestimonial** | POST | `/api/testimonials` | [Authorize] | Creates | Async | Authenticated users |
| **UpdateTestimonial** | PUT | `/api/testimonials/{id}` | [Authorize] | Updates | Async | Authenticated users |
| **DeleteTestimonial** | DELETE | `/api/testimonials/{id}` | [Authorize] | Deletes | Async | Authenticated users |

#### 2.2 External API Integrations

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **SearchPenguinAuthors** | GET | `/api/penguin/authors/{authorName}` | JWT Required | Read-only (external) | Async | Authenticated users |
| **GetPenguinTitlesByAuthor** | GET | `/api/penguin/authors/{authorKey}/titles` | JWT Required | Read-only (external) | Async | Authenticated users |
| **SearchAmazonBooksByAuthor** | GET | `/api/amazon/books/author/{authorName}` | JWT Required | Read-only (external) | Async | Authenticated users |
| **GetPersonFacts** | GET | `/api/wikipedia/{language}/{personName}` | None | Read-only (external) | Async | Public |

#### 2.3 Localization & Reference Data

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **LocalizedText** | GET | `/api/localizedtext/{culture}` | None | Read-only | Async | Public |
| **GetCountriesByLanguage** | GET | `/api/countries/{language}` | None | Read-only | Async | Public |
| **GetLanguages** | GET | `/api/languages/{language}` | None | Read-only | Async | Public |
| **GetStateProvinces** | GET | `/api/stateprovinces/{culture}` | JWT Required | Read-only | Async | Authenticated users |
| **GetStateProvincesByCountry** | GET | `/api/stateprovinces/{countryCode}/{culture}` | JWT Required | Read-only | Async | Authenticated users |

#### 2.4 Domain Registration

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **CreateDomainRegistration** | POST | `/api/domain-registrations` | JWT Required | Creates (DB, triggers) | Async | Authenticated users |
| **GetDomainRegistrations** | GET | `/api/domain-registrations` | JWT Required | Read-only | Async | Authenticated users |
| **GetDomainRegistrationById** | GET | `/api/domain-registrations/{registrationId}` | JWT Required | Read-only | Async | Authenticated users |
| **UpdateDomainRegistration** | PUT | `/api/domain-registrations/{registrationId}` | JWT Required | Updates | Async | Authenticated + Active subscription |
| **DomainRegistrationTrigger** | CosmosDB Trigger | N/A (trigger) | N/A | Creates (WHMCS, Front Door) | Async | System |
| **CreateDnsZone** | CosmosDB Trigger | N/A (trigger) | N/A | Creates (Azure DNS) | Async | System |

**Special Notes:**
- DomainRegistrationTrigger: Automatically registers domains via WHMCS and adds to Azure Front Door
- CreateDnsZone: Automatically creates Azure DNS zones for new domain registrations
- Both triggers listen to DomainRegistrations container changes

#### 2.5 Author Invitations

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **CreateAuthorInvitation** | POST | `/api/author-invitations` | [Authorize] | Creates | Async | Authenticated users |
| **ListAuthorInvitations** | GET | `/api/author-invitations` | [Authorize] | Read-only | Async | Authenticated users |
| **GetAuthorInvitation** | GET | `/api/author-invitations/{id}` | [Authorize] | Read-only | Async | Authenticated users |
| **UpdateAuthorInvitation** | PUT | `/api/author-invitations/{id}` | [Authorize] | Updates | Async | Authenticated users |
| **ResendAuthorInvitation** | POST | `/api/author-invitations/{id}/resend` | [Authorize] | Updates (LastEmailSentAt) | Async | Authenticated users |

#### 2.6 Lead Capture & Marketing

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **CreateLead** | POST | `/api/leads` | None | Creates | Async | Public (rate limited) |
| **CreateReferral** | POST | `/api/referrals` | None | Creates | Async | Public |
| **GetReferralStats** | GET | `/api/referrals/{userId}` | None | Read-only | Async | Public |

**Special Notes:**
- CreateLead: Rate limited to 10 requests per minute per IP address
- Validates email format and required fields

#### 2.7 Platform Data & Statistics

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **GetPlatformStats** | GET | `/api/stats/platform` | None | Read-only | Async | Public (cached 1hr) |
| **GetAuthors** | GET | `/api/authors/{secondLevelDomain}/{topLevelDomain}` | JWT Required | Read-only | Async | Author.Read scope |
| **GetExperiments** | GET | `/api/experiments` | None | Read-only | Async | Public |

**Special Notes:**
- GetPlatformStats: Cached with Cache-Control header (max-age=3600)
- GetAuthors: Requires specific `Author.Read` scope claim

#### 2.8 Test Functions (Development Only)

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **TestScenario1** | POST | `/test/scenario1` | Function level | Test harness | Async | Function key |
| **TestScenario3** | POST | `/test/scenario3` | Function level | Test harness | Async | Function key |
| **TestFrontDoor** | POST | `/test/frontdoor` | Function level | Test harness | Async | Function key |
| **TestDnsZone** | POST | `/test/dns` | Function level | Test harness | Async | Function key |
| **TestCreateDnsZone** | POST | N/A | Function level | Test harness | Async | Function key |

---

### 3. InkStainedWretchStripe - Payment Processing

**Project:** `InkStainedWretchStripe/`  
**Total Functions:** 13  
**Primary Purpose:** Stripe integration for subscriptions, payments, and billing

#### 3.1 Customer & Checkout Management

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **CreateStripeCustomer** | POST | `/api/CreateStripeCustomer` | JWT Required | Creates (Stripe) | Async | Authenticated users |
| **CreateStripeCheckoutSession** | POST | `/api/CreateStripeCheckoutSession` | JWT Required | Creates (Stripe) | Async | Authenticated users |
| **GetStripeCheckoutSession** | GET | `/api/GetStripeCheckoutSession/{sessionId}` | JWT Required | Read-only | Async | Authenticated users |

#### 3.2 Subscription Management

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **CreateSubscription** | POST | `/api/CreateSubscription` | JWT Required | Creates (Stripe) | Async | Authenticated users |
| **UpdateSubscription** | POST | `/api/UpdateSubscription/{subscriptionId}` | [Authorize] | Updates (Stripe) | Async | Authenticated users |
| **CancelSubscription** | POST | `/api/CancelSubscription/{subscriptionId}` | JWT Required | Updates (Stripe) | Async | Authenticated users |
| **ListSubscription** | GET | `/api/ListSubscription/{customerId}` | [Authorize] | Read-only | Async | Authenticated users |
| **FindSubscription** | GET | `/api/FindSubscription` | [Authorize] | Read-only | Async | Authenticated users |

**Query Parameters for FindSubscription:**
- `email` (required)
- `domain` (required)

**Query Parameters for ListSubscription:**
- `status` (optional)
- `limit` (optional)
- `startingAfter` (optional)
- `expandLatestInvoicePaymentIntent` (optional)

#### 3.3 Pricing & Billing

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **GetStripePriceInformation** | POST | `/api/GetStripePriceInformation` | JWT Required | Read-only | Async | Authenticated users |
| **InvoicePreview** | POST | `/api/InvoicePreview` | [Authorize] | Read-only (preview) | Async | Authenticated users |

#### 3.4 Webhooks & Health

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **WebHook** | POST | `/api/WebHook` | Stripe-Signature | Creates/Updates (processes events) | Async | Stripe webhook |
| **StripeHealth** | GET | `/api/stripe/health` | None | Read-only | Sync | Public |

**Special Notes:**
- WebHook: Validates Stripe-Signature header for security
- WebHook processes events: checkout.session.completed, customer.subscription.created, customer.subscription.updated, customer.subscription.deleted, invoice.payment_succeeded, invoice.payment_failed
- StripeHealth: Only synchronous function in the codebase

---

### 4. function-app - Public Content APIs

**Project:** `function-app/`  
**Total Functions:** 3  
**Primary Purpose:** Public-facing author data and content retrieval

| Function | HTTP Method | Route | Auth | Side Effects | Sync/Async | Role Access |
|----------|-------------|-------|------|--------------|------------|-------------|
| **GetLocaleData** | GET | `/api/GetLocaleData/{languageName}/{regionName?}` | None | Read-only | Async* | Public |
| **GetAuthorData** | GET | `/api/GetAuthorData/{topLevelDomain}/{secondLevelDomain}/{languageName}/{regionName?}` | None | Read-only | Async* | Public |
| **GetSitemap** | GET | `/api/sitemap.xml/{topLevelDomain}/{secondLevelDomain}` | None | Read-only | Async | Public |

**Special Notes:**
- GetLocaleData: Returns localized UI text for specified language and region
- GetAuthorData: Returns complete author profile with books, articles, social links
- GetSitemap: Returns XML sitemap with Content-Type: application/xml
- *Uses `.GetAwaiter().GetResult()` pattern (blocking async call)

---

## Categorization by Characteristics

### By HTTP Method

#### GET (Read Operations) - 28 Functions

**Public (No Auth):**
- GetTestimonials, GetPersonFacts, LocalizedText, GetCountriesByLanguage, GetLanguages, GetPlatformStats, GetExperiments, GetLocaleData, GetAuthorData, GetSitemap, StripeHealth, GetReferralStats

**JWT Required:**
- WhoAmI, User, GetStateProvinces, GetStateProvincesByCountry, SearchPenguinAuthors, GetPenguinTitlesByAuthor, SearchAmazonBooksByAuthor, GetDomainRegistrations, GetDomainRegistrationById, GetAuthors, GetStripeCheckoutSession

**[Authorize] Attribute:**
- ListAuthorInvitations, GetAuthorInvitation, ListSubscription, FindSubscription

#### POST (Create Operations) - 18 Functions

**Public (No Auth):**
- CreateLead, CreateReferral

**JWT Required:**
- Upload, CreateDomainRegistration, CreateStripeCustomer, CreateStripeCheckoutSession, CreateSubscription, CancelSubscription, GetStripePriceInformation

**[Authorize] Attribute:**
- CreateTestimonial, CreateAuthorInvitation, ResendAuthorInvitation, UpdateSubscription, InvoicePreview

**Stripe Signature:**
- WebHook

**Test Functions:**
- TestScenario1, TestScenario3, TestFrontDoor, TestDnsZone, TestCreateDnsZone

#### PUT (Update Operations) - 3 Functions

**[Authorize] Attribute:**
- UpdateTestimonial, UpdateAuthorInvitation

**JWT Required:**
- UpdateDomainRegistration

#### DELETE (Delete Operations) - 2 Functions

**JWT Required:**
- Delete (images)

**[Authorize] Attribute:**
- DeleteTestimonial

#### Cosmos DB Triggers - 2 Functions

**System Triggers:**
- DomainRegistrationTrigger (registers domains via WHMCS & Front Door)
- CreateDnsZone (creates Azure DNS zones)

---

### By Authentication

#### No Authentication Required (18 Functions)

**Public APIs:**
- GetTestimonials, GetPersonFacts, GetExperiments, GetCountriesByLanguage, LocalizedText, GetLanguages, GetPlatformStats, GetLocaleData, GetAuthorData, GetSitemap, StripeHealth, CreateLead (rate limited), CreateReferral, GetReferralStats

**Special Authentication:**
- WebHook (Stripe-Signature header)

**System Triggers:**
- DomainRegistrationTrigger
- CreateDnsZone

**Test Functions:**
- TestScenario1, TestScenario3, TestFrontDoor, TestDnsZone, TestCreateDnsZone

#### JWT Token Required - Manual Validation (24 Functions)

**ImageAPI (4):**
- WhoAmI, Upload, User, Delete

**InkStainedWretchFunctions (10):**
- SearchPenguinAuthors, GetPenguinTitlesByAuthor, SearchAmazonBooksByAuthor, GetStateProvinces, GetStateProvincesByCountry, CreateDomainRegistration, GetDomainRegistrations, GetDomainRegistrationById, UpdateDomainRegistration, GetAuthors

**InkStainedWretchStripe (10):**
- CreateStripeCustomer, CreateStripeCheckoutSession, GetStripeCheckoutSession, CreateSubscription, CancelSubscription, GetStripePriceInformation

#### [Authorize] Attribute (12 Functions)

**ImageAPI (4):**
- All ImageAPI functions also have [Authorize] policy

**InkStainedWretchFunctions (5):**
- CreateTestimonial, UpdateTestimonial, DeleteTestimonial, CreateAuthorInvitation, ListAuthorInvitations, GetAuthorInvitation, UpdateAuthorInvitation, ResendAuthorInvitation

**InkStainedWretchStripe (3):**
- UpdateSubscription, ListSubscription, FindSubscription, InvoicePreview

---

### By Data Modification

#### Read-Only (No Side Effects) - 35 Functions

**Pure Read Operations:**
- WhoAmI, User, GetTestimonials, GetPersonFacts, GetExperiments, GetCountriesByLanguage, LocalizedText, GetLanguages, GetStateProvinces, GetStateProvincesByCountry, GetPlatformStats, GetDomainRegistrations, GetDomainRegistrationById, GetAuthors, ListAuthorInvitations, GetAuthorInvitation, GetLocaleData, GetAuthorData, GetSitemap, GetStripeCheckoutSession, ListSubscription, FindSubscription, InvoicePreview (preview only), GetStripePriceInformation, StripeHealth, GetReferralStats

**External API Reads (No Local Side Effects):**
- SearchPenguinAuthors, GetPenguinTitlesByAuthor, SearchAmazonBooksByAuthor, GetPersonFacts

#### Creates Data (11 Functions)

**Local Storage:**
- Upload (blob storage + DB)
- CreateTestimonial (DB)
- CreateLead (DB)
- CreateReferral (DB)
- CreateAuthorInvitation (DB)
- CreateDomainRegistration (DB + triggers)

**External Services:**
- CreateStripeCustomer (Stripe)
- CreateStripeCheckoutSession (Stripe)
- CreateSubscription (Stripe)
- DomainRegistrationTrigger (WHMCS + Front Door)
- CreateDnsZone (Azure DNS)

#### Updates Data (5 Functions)

**Local Updates:**
- UpdateTestimonial (DB)
- UpdateAuthorInvitation (DB)
- UpdateDomainRegistration (DB)
- ResendAuthorInvitation (DB - LastEmailSentAt)

**External Updates:**
- UpdateSubscription (Stripe)

#### Deletes Data (2 Functions)

**Local Deletes:**
- Delete (blob storage + DB)
- DeleteTestimonial (DB)

#### Mixed Operations (1 Function)

**Complex Processing:**
- WebHook (processes various Stripe events - creates, updates based on event type)
- CancelSubscription (Stripe update)

---

### By Synchronicity

#### Asynchronous (53 Functions)

**All functions are async/await pattern EXCEPT:**
- StripeHealth (synchronous)

**Note:** Some functions use `.GetAwaiter().GetResult()` blocking pattern:
- GetLocaleData
- GetAuthorData

These are technically blocking calls but wrap async operations.

#### Synchronous (1 Function)

- **StripeHealth** - Simple health check endpoint

---

## Summary Statistics

### Overall Statistics

| Metric | Count |
|--------|-------|
| **Total Functions** | **54** |
| HTTP-Triggered Functions | 49 |
| Cosmos DB Triggers | 2 |
| Test Functions | 5 |
| Production Functions | 49 |

### By Project

| Project | Function Count | Primary Purpose |
|---------|---------------|-----------------|
| **ImageAPI** | 4 | Image management (upload, delete, retrieve) |
| **InkStainedWretchFunctions** | 35 | Core platform (domains, localization, integrations) |
| **InkStainedWretchStripe** | 13 | Payment processing and subscriptions |
| **function-app** | 3 | Public author data and content |

### By HTTP Method

| HTTP Method | Count | Percentage |
|-------------|-------|------------|
| GET | 28 | 52% |
| POST | 18 | 33% |
| PUT | 3 | 6% |
| DELETE | 2 | 4% |
| Triggers | 2 | 4% |

### By Authentication Type

| Auth Type | Count | Percentage |
|-----------|-------|------------|
| JWT Token Required | 24 | 44% |
| [Authorize] Attribute | 12 | 22% |
| No Authentication | 18 | 33% |
| Stripe Signature | 1 | 2% |

**Note:** Some functions have both JWT validation AND [Authorize] attribute (ImageAPI functions)

### By Side Effects

| Side Effect Type | Count | Percentage |
|-----------------|-------|------------|
| Read-Only | 35 | 65% |
| Creates Data | 11 | 20% |
| Updates Data | 5 | 9% |
| Deletes Data | 2 | 4% |
| Mixed/Complex | 1 | 2% |

### By Synchronicity

| Pattern | Count | Percentage |
|---------|-------|------------|
| Async/Await | 53 | 98% |
| Synchronous | 1 | 2% |

### Special Features

| Feature | Count | Functions |
|---------|-------|-----------|
| **Rate Limited** | 1 | CreateLead (10 req/min per IP) |
| **Cached Responses** | 1 | GetPlatformStats (1 hour cache) |
| **External API Calls** | 4 | Penguin Random House (2), Amazon (1), Wikipedia (1) |
| **Cosmos DB Triggers** | 2 | DomainRegistrationTrigger, CreateDnsZone |
| **Webhook Handlers** | 1 | WebHook (Stripe events) |
| **Subscription Tier Validation** | 4 | All ImageAPI functions |

---

## Authentication & Authorization Details

### JWT Token Claims Used

**Common Claims:**
- `oid` - Object ID (user identifier)
- `upn` - User Principal Name
- `name` - Display name
- `email` - Email address
- `roles` - User roles
- Various scope claims for authorization

### Authorization Policies

| Policy | Description | Used By |
|--------|-------------|---------|
| **RequireScope.Read** | Read scope required | All ImageAPI functions (4) |
| **Author.Read** | Author read scope | GetAuthors |
| **[Authorize]** | Generic authorization | 12 functions |

### Subscription Tier Limits (ImageAPI)

| Tier | Storage | Bandwidth | Max File Size | Max Files |
|------|---------|-----------|---------------|-----------|
| **Starter (Free)** | 5 GB | 25 GB | 5 MB | 20 |
| **Pro ($9.99/mo)** | 250 GB | 1 TB | 10 MB | 500 |
| **Elite ($19.99/mo)** | 2 TB | 10 TB | 25 MB | 2000 |

---

## External Integrations

### Third-Party APIs

| Service | Functions | Purpose |
|---------|-----------|---------|
| **Stripe** | 13 functions | Payment processing, subscriptions, billing |
| **Penguin Random House** | 2 functions | Author search, book titles lookup |
| **Amazon Product Advertising** | 1 function | Book search by author |
| **Wikipedia** | 1 function | Person facts and biography |
| **WHMCS** | 1 trigger | Domain registration service |

### Azure Services

| Service | Functions | Purpose |
|---------|-----------|---------|
| **Azure Blob Storage** | 2 functions | Image upload and deletion |
| **Azure Cosmos DB** | Most functions | NoSQL database (triggers and CRUD) |
| **Azure Front Door** | 1 trigger | CDN and domain routing |
| **Azure DNS** | 1 trigger | DNS zone management |

---

## Error Handling & Response Codes

### Common HTTP Status Codes

| Code | Meaning | Common Scenarios |
|------|---------|------------------|
| 200 OK | Success | Successful read/update/delete |
| 201 Created | Resource created | Upload, CreateLead, CreateTestimonial |
| 400 Bad Request | Invalid input | Missing parameters, validation failures |
| 401 Unauthorized | Auth failure | Invalid/missing JWT token |
| 402 Payment Required | Quota exceeded | Bandwidth limit exceeded (ImageAPI) |
| 403 Forbidden | Access denied | Tier limit reached, not owner |
| 404 Not Found | Resource not found | Image not found, author not found |
| 429 Too Many Requests | Rate limit | CreateLead rate limiting |
| 500 Internal Server Error | Server error | Unexpected exceptions |
| 507 Insufficient Storage | Storage full | Storage quota exceeded (ImageAPI) |

---

## Security Considerations

### Authentication Methods

1. **JWT Bearer Tokens**
   - Validated against Microsoft Entra ID
   - Claims extracted for user identification
   - Scopes/policies enforced

2. **[Authorize] Attribute**
   - ASP.NET Core authorization
   - Integrated with authentication middleware

3. **Stripe Webhook Signature**
   - Validates `Stripe-Signature` header
   - Prevents unauthorized webhook calls

4. **Rate Limiting**
   - IP-based rate limiting on CreateLead
   - Prevents abuse of public endpoints

### Data Access Controls

- **User Isolation:** Users can only access their own data (images, invitations)
- **Ownership Validation:** Delete operations verify ownership
- **Subscription Validation:** Tier limits enforced on image operations
- **Scope Validation:** Specific scopes required for sensitive operations

---

## Performance Optimizations

### Caching

| Function | Cache Duration | Header |
|----------|---------------|--------|
| **GetPlatformStats** | 1 hour | Cache-Control: public, max-age=3600 |
| **GetTestimonials** | 15 minutes | Cache-Control: public, max-age=900 |

### Async Patterns

- **98% async:** Nearly all functions use async/await for I/O operations
- **Non-blocking:** Database, blob storage, and external API calls are asynchronous
- **Scalability:** Supports high concurrency without thread blocking

---

## Development & Testing

### Test Functions (Function-Level Auth)

These functions are for development/testing only and require function keys:

1. **TestScenario1** - End-to-end test scenario 1
2. **TestScenario3** - End-to-end test scenario 3
3. **TestFrontDoor** - Front Door integration testing
4. **TestDnsZone** - DNS zone creation testing
5. **TestCreateDnsZone** - DNS zone creation test harness

**Security:** These should NOT be exposed in production or should use strong function keys.

---

## Migration Notes

### Deprecated Patterns

Some functions use `.GetAwaiter().GetResult()` instead of proper async/await:
- GetLocaleData
- GetAuthorData

**Recommendation:** Refactor to use proper async/await to avoid potential deadlocks.

---

## Document Maintenance

This document should be updated when:
- New Azure Functions are added
- Authentication/authorization changes
- API routes or HTTP methods change
- Side effects of functions change
- New external integrations are added

**Update Process:**
1. Analyze new/changed function code
2. Update relevant sections
3. Regenerate summary statistics
4. Update "Last Updated" date at top of document

---

**End of API Categorization Documentation**
