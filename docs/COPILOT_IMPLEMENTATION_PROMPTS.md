# Copilot Implementation Prompts — New Consolidated Architecture

*Generated: 2026-03-04*

This document contains ready-to-paste GitHub Copilot prompts for implementing every API endpoint using the new consolidated single-Function-App architecture. See the [Architecture Overview](#architecture-overview) section before using these prompts.

---

## Architecture Overview

The new architecture consolidates all four Azure Function App projects (`function-app`, `ImageAPI`, `InkStainedWretchFunctions`, `InkStainedWretchStripe`) into **one Function App project** with the following rules:

| Request type | Behavior |
|-------------|----------|
| **GET** (no side effects) | Implemented exactly as today — read from Cosmos DB / external API and return response directly |
| **POST / PUT / DELETE** (with side effects) | Function returns `202 Accepted` immediately after enqueueing a message onto Azure Service Bus. A separate **Service Bus–triggered function** method dequeues the message and executes the operation. |
| **Completion notification** | After a queue-triggered function completes its operation, it sends a notification via **Azure Notification Hub** so the front end can update state without polling. |

**Application Logic Rule:** All business logic lives in `OnePageAuthorLib` (the Ink Stained Wretches Library). Function methods are thin controllers (validate JWT → enqueue or query → return).

**Project layout after consolidation:**

```
InkStainedWretchFunctions/        ← single consolidated function app
  Http/                           ← HTTP-triggered thin controllers
  Queue/                          ← Service Bus–triggered processors
  Timers/                         ← Timer-triggered jobs (if any)
OnePageAuthorLib/                 ← all business logic
  services/
  nosql/
  interfaces/
  api/
  Authentication/
```

---

## Table of Contents

1. [Consolidation Scaffold Prompt](#1-consolidation-scaffold-prompt)
2. [Author Endpoints](#2-author-endpoints)
3. [Domain Registration Endpoints](#3-domain-registration-endpoints)
4. [Admin / Management Endpoints](#4-admin--management-endpoints)
5. [Author Invitation Endpoints](#5-author-invitation-endpoints)
6. [Testimonial Endpoints](#6-testimonial-endpoints)
7. [Lead Capture Endpoint](#7-lead-capture-endpoint)
8. [Referral Endpoints](#8-referral-endpoints)
9. [External Integration Endpoints](#9-external-integration-endpoints)
10. [Reference Data Endpoints](#10-reference-data-endpoints)
11. [Image Management Endpoints](#11-image-management-endpoints)
12. [Stripe / Subscription Endpoints](#12-stripe--subscription-endpoints)
13. [Stripe Webhook](#13-stripe-webhook)
14. [Cosmos DB Change-Feed Triggers](#14-cosmos-db-change-feed-triggers)
15. [Notification Hub Integration](#15-notification-hub-integration)
16. [Service Registration Updates](#16-service-registration-updates)

---

## 1. Consolidation Scaffold Prompt

Use this prompt first to scaffold the consolidated project structure before implementing individual endpoints.

---

**Prompt:**

```
I am consolidating four Azure Function App projects (function-app, ImageAPI,
InkStainedWretchFunctions, InkStainedWretchStripe) into a single Azure Functions
v4 isolated-worker project targeting net10.0.

Please create the new project scaffold:

1. Create a new Azure Functions v4 isolated-worker project at
   `InkStainedWretchFunctions/` (reuse the existing project — remove the old
   function files, keep the project file and Program.cs).

2. Update `InkStainedWretchFunctions/InkStainedWretchFunctions.csproj` to add
   these additional package references that are currently only in ImageAPI and
   InkStainedWretchStripe:
   - Azure.Storage.Blobs
   - Stripe.net
   - Azure.Messaging.NotificationHubs (for Azure Notification Hub)
   - Azure.Messaging.ServiceBus (already present, verify)

3. Update `InkStainedWretchFunctions/Program.cs` to call ALL service registration
   extension methods from `OnePageAuthorLib/ServiceFactory.cs` that were previously
   split across the four Program.cs files. Ensure each service is registered exactly
   once.

4. Add a sub-folder structure inside InkStainedWretchFunctions/:
   - `Http/Authors/`         ← GetAuthors (moved from existing)
   - `Http/DomainRegistration/`
   - `Http/AuthorInvitation/`
   - `Http/Testimonials/`
   - `Http/Leads/`
   - `Http/Referrals/`
   - `Http/ExternalIntegrations/`
   - `Http/ReferenceData/`
   - `Http/Images/`
   - `Http/Stripe/`
   - `Queue/`               ← Service Bus–triggered processors
   - `Triggers/`            ← Cosmos DB change-feed triggers

5. Do NOT remove existing function files yet — just create the folder structure
   and update the project file.

Constraints:
- Target framework: net10.0
- All business logic must stay in OnePageAuthorLib/
- Follow existing naming conventions: PascalCase classes, _camelCase fields,
  Async suffix on async methods, I-prefix on interfaces.
- Use constructor injection for all services.
```

---

## 2. Author Endpoints

### 2.1 GetAuthors (GET — no change needed)

**Current source:** `InkStainedWretchFunctions/GetAuthors.cs`  
**Architecture rule:** GET with no side effects — keep as-is, just move to `Http/Authors/`.

**Prompt:**

```
Move the existing `GetAuthors` HTTP-triggered function from
`InkStainedWretchFunctions/GetAuthors.cs` into
`InkStainedWretchFunctions/Http/Authors/GetAuthors.cs`.

No logic changes. Only update the namespace to match the new folder structure.
Verify it:
- Uses [Function("GetAuthors")] attribute
- Route: "authors/{secondLevelDomain?}/{topLevelDomain?}"
- Calls IScopeValidationService to validate "Author.Read" scope
- Uses IJwtValidationService to validate JWT
- Calls IAuthorDataService for data access
- Returns appropriate HTTP status codes: 200, 401, 403, 404, 500

Update the corresponding test file
`OnePageAuthor.Test/InkStainedWretchFunctions/GetAuthorsTests.cs` to use the
new namespace.
```

---

## 3. Domain Registration Endpoints

### 3.1 CreateDomainRegistration (POST — enqueue pattern)

**Current source:** `InkStainedWretchFunctions/DomainRegistrationFunction.cs`  
**New behavior:** Return 202 immediately; queue message processed asynchronously.

**Prompt:**

```
Implement the CreateDomainRegistration endpoint using the Service Bus enqueue
pattern in the consolidated InkStainedWretchFunctions project.

FILE 1: `InkStainedWretchFunctions/Http/DomainRegistration/CreateDomainRegistrationFunction.cs`

[Function("CreateDomainRegistration")]
HTTP POST /api/domain-registrations
Authorization: Bearer JWT required (use IJwtValidationService)

Steps:
1. Validate JWT token using IJwtValidationService. Return 401 if invalid.
2. Deserialize request body as CreateDomainRegistrationRequest:
   { DomainName: string, RegistrationPeriodYears: int, AutoRenew: bool,
     PrivacyProtection: bool, ContactInfo: { FirstName, LastName, Email,
     Phone, Address: { Street, City, State, PostalCode, Country } } }
3. Validate required fields (DomainName non-empty, ContactInfo non-null).
   Return 400 with validation errors if invalid.
4. Call IDomainRegistrationService.CreatePendingAsync(request, user) to
   create a Pending document in Cosmos DB and return a DomainRegistrationResponse.
5. Enqueue a DomainRegistrationQueueMessage (domain registration ID + domain name)
   onto the Azure Service Bus queue named "domain-registration-commands" using
   IServiceBusPublisher.EnqueueAsync(queueMessage).
6. Return HTTP 202 Accepted with body:
   { "id": "<registration id>", "status": "Pending",
     "message": "Domain registration queued for processing." }

FILE 2: `InkStainedWretchFunctions/Queue/DomainRegistrationQueueProcessor.cs`

[Function("ProcessDomainRegistration")]
Service Bus trigger on queue "domain-registration-commands"

Steps:
1. Deserialize DomainRegistrationQueueMessage from the Service Bus message.
2. Load the full DomainRegistration document from IDomainRegistrationRepository.
3. Call IDomainRegistrationService.ProcessRegistrationAsync(registration) which:
   a. Registers domain via IWhmcsQueueService (enqueues to the WHMCS Service Bus
      queue for the WhmcsWorkerService to process)
   b. Creates Azure DNS zone via IDnsZoneService
   c. Updates domain registration status to "Processing" in Cosmos DB
4. Send a completion notification via INotificationHubService.SendAsync(
     userId, "DomainRegistrationQueued",
     new { RegistrationId = id, Status = "Processing" })
5. Complete or abandon the Service Bus message based on success/failure.

FILE 3: `OnePageAuthorLib/interfaces/IServiceBusPublisher.cs`
Define:
  Task EnqueueAsync<T>(T message, string queueName, CancellationToken ct = default);

FILE 4: `OnePageAuthorLib/services/ServiceBusPublisher.cs`
Implement IServiceBusPublisher using Azure.Messaging.ServiceBus.ServiceBusClient.
Serialize messages as JSON using System.Text.Json.

Constraints:
- All business logic in OnePageAuthorLib
- Use constructor injection
- Use ILogger<T> for logging
- Follow existing error handling patterns (try/catch with structured logging)
- Register IServiceBusPublisher in ServiceFactory.cs via AddServiceBusPublisher()
  extension method
```

---

### 3.2 GetDomainRegistrations / GetDomainRegistrationById / UpdateDomainRegistration

**Prompt:**

```
Move the GET and PUT domain registration endpoints from the existing
DomainRegistrationFunction.cs to the consolidated project.

FILE: `InkStainedWretchFunctions/Http/DomainRegistration/DomainRegistrationFunction.cs`

Implement three functions in this file:

1. [Function("GetDomainRegistrations")]
   GET /api/domain-registrations
   - Validate JWT
   - Call IDomainRegistrationRepository.GetByUserEmailAsync(userEmail)
   - Return 200 with array of DomainRegistrationResponse

2. [Function("GetDomainRegistrationById")]
   GET /api/domain-registrations/{registrationId}
   - Validate JWT
   - Call IDomainRegistrationRepository.GetByIdAsync(registrationId)
   - Return 200 with single DomainRegistrationResponse, or 404

3. [Function("UpdateDomainRegistration")]
   PUT /api/domain-registrations/{registrationId}
   PUT is a side-effect operation — use the enqueue pattern:
   - Validate JWT
   - Deserialize DomainRegistrationUpdateRequest from body
   - Enqueue a DomainRegistrationUpdateQueueMessage onto "domain-registration-commands"
   - Return 202 Accepted immediately

   Add a corresponding queue processor in
   `InkStainedWretchFunctions/Queue/DomainRegistrationUpdateQueueProcessor.cs`
   that performs the actual update in Cosmos DB and sends a
   Notification Hub notification on completion.

All logic delegates to IDomainRegistrationRepository and IDomainRegistrationService
in OnePageAuthorLib. Do not duplicate business logic in function files.
```

---

## 4. Admin / Management Endpoints

### 4.1 AdminGetIncompleteDomainRegistrations (GET)

**Prompt:**

```
Move AdminGetIncompleteDomainRegistrations to the consolidated project.

FILE: `InkStainedWretchFunctions/Http/DomainRegistration/AdminDomainRegistrationFunction.cs`

[Function("AdminGetIncompleteDomainRegistrations")]
GET /api/management/domain-registrations
Authorization: Bearer JWT + Admin role (use IRoleChecker.HasRole(user, "Admin"))

Steps:
1. Validate JWT using IJwtValidationService. Return 401 if invalid.
2. Check Admin role using IRoleChecker. Return 403 if not admin.
3. Parse optional "maxResults" query parameter (default: 50, max: 200).
4. Call IDomainRegistrationRepository.GetIncompleteAsync(maxResults).
5. Return 200 with array of DomainRegistrationResponse.

Use the existing implementation in AdminDomainRegistrationFunction.cs as
reference. Keep all logic in the function file thin — delegate to repository.
```

---

### 4.2 AdminCompleteDomainRegistration (POST — enqueue pattern)

**Prompt:**

```
Implement AdminCompleteDomainRegistration using the enqueue pattern.

FILE: `InkStainedWretchFunctions/Http/DomainRegistration/AdminDomainRegistrationFunction.cs`
(add to same file as AdminGetIncompleteDomainRegistrations)

[Function("AdminCompleteDomainRegistration")]
POST /api/management/domain-registrations/{registrationId}/complete
Authorization: Bearer JWT + Admin role

Steps:
1. Validate JWT and Admin role. Return 401/403 as appropriate.
2. Verify the registration exists; return 404 if not found.
3. Enqueue an AdminCompleteDomainRegistrationMessage (registrationId, adminUserId)
   onto "domain-registration-commands" using IServiceBusPublisher.
4. Return 202 Accepted with { "id": registrationId, "status": "Completing",
   "message": "Domain completion queued for processing." }

FILE: `InkStainedWretchFunctions/Queue/AdminDomainCompletionQueueProcessor.cs`

[Function("ProcessAdminDomainCompletion")]
Service Bus trigger on queue "domain-registration-commands"
(use message subject / label filtering to route to correct processor)

Steps:
1. Register domain via IWhmcsService.RegisterDomainAsync(registration)
2. Create Azure DNS zone via IDnsZoneService
3. Update name servers in WHMCS via IWhmcsService.UpdateNameServersAsync(...)
4. Add domain to Azure Front Door via IFrontDoorService
5. Update registration status to "Active" in Cosmos DB
6. Send notification via INotificationHubService.SendAsync(
     adminUserId, "AdminDomainCompletionFinished",
     new { RegistrationId = id, Status = "Active" })

Follow the existing logic in AdminDomainRegistrationFunction.cs as reference.
All service calls go through interfaces in OnePageAuthorLib.
```

---

## 5. Author Invitation Endpoints

### 5.1 CreateAuthorInvitation (POST — enqueue pattern)

**Prompt:**

```
Implement CreateAuthorInvitation using the enqueue pattern.

FILE: `InkStainedWretchFunctions/Http/AuthorInvitation/AuthorInvitationFunction.cs`

[Function("CreateAuthorInvitation")]
POST /api/author-invitations
Authorization: [Authorize] + Bearer JWT

Steps:
1. Validate JWT. Return 401 if invalid.
2. Deserialize CreateAuthorInvitationRequest:
   { EmailAddress: string, DomainName: string, DomainNames: string[], Notes: string }
3. Validate EmailAddress is non-empty. Return 400 otherwise.
4. Create a pending invitation record in Cosmos DB using
   IAuthorInvitationService.CreatePendingAsync(request, createdByUserId).
5. Enqueue an AuthorInvitationCreatedMessage (invitationId) onto
   "author-invitation-commands" queue.
6. Return 202 Accepted with { "id": invitationId, "status": "Pending",
   "message": "Invitation queued for sending." }

FILE: `InkStainedWretchFunctions/Queue/AuthorInvitationQueueProcessor.cs`

[Function("ProcessAuthorInvitation")]
Service Bus trigger on "author-invitation-commands"

Steps:
1. Load invitation from IAuthorInvitationRepository.
2. Call IEmailService.SendInvitationEmailAsync(invitation).
3. Update invitation status (EmailSent = true, LastEmailSentAt = now).
4. Send Notification Hub notification:
   INotificationHubService.SendAsync(userId, "AuthorInvitationSent",
     new { InvitationId = id, EmailAddress = invitation.EmailAddress })

Preserve all existing business rules in IAuthorInvitationService.
Do not duplicate logic from OnePageAuthorLib/services/AuthorInvitationService.cs.
```

---

### 5.2 Remaining Author Invitation Endpoints (GET, PUT, Resend)

**Prompt:**

```
Add the remaining author invitation HTTP endpoints to
`InkStainedWretchFunctions/Http/AuthorInvitation/AuthorInvitationFunction.cs`.

All use [Authorize] + Bearer JWT.

1. [Function("ListAuthorInvitations")]
   GET /api/author-invitations
   - Call IAuthorInvitationService.GetAllAsync(userId)
   - Return 200 with array of AuthorInvitationResponse

2. [Function("GetAuthorInvitationById")]
   GET /api/author-invitations/{id}
   - Call IAuthorInvitationService.GetByIdAsync(id)
   - Return 200 or 404

3. [Function("UpdateAuthorInvitation")]
   PUT /api/author-invitations/{id}
   Enqueue pattern:
   - Deserialize UpdateAuthorInvitationRequest { DomainNames, Notes, ExpiresAt }
   - Enqueue AuthorInvitationUpdateMessage onto "author-invitation-commands"
   - Return 202 Accepted
   Queue processor: update invitation in Cosmos DB, send Notification Hub event.

4. [Function("ResendAuthorInvitation")]
   POST /api/author-invitations/{id}/resend
   Enqueue pattern:
   - Enqueue AuthorInvitationResendMessage onto "author-invitation-commands"
   - Return 202 Accepted
   Queue processor: call IEmailService.SendInvitationEmailAsync, update
   LastEmailSentAt, send Notification Hub event "AuthorInvitationResent".

Update existing unit tests in
`OnePageAuthor.Test/InkStainedWretchFunctions/AuthorInvitationFunctionTests.cs`
to verify 202 responses for mutation endpoints and that IServiceBusPublisher
was called with correct message types.
```

---

## 6. Testimonial Endpoints

### 6.1 GetTestimonials (GET — no change)

**Prompt:**

```
Move GetTestimonials to the consolidated project.

FILE: `InkStainedWretchFunctions/Http/Testimonials/TestimonialFunction.cs`

[Function("GetTestimonials")]
GET /api/testimonials
Public (no auth)
Query params: limit (int), featured (bool), locale (string)
Cache-Control: public, max-age=900

Logic is unchanged from the current implementation. Delegate to ITestimonialRepository.
Return 200 with { testimonials: [...], total: N }.
```

---

### 6.2 CreateTestimonial / UpdateTestimonial / DeleteTestimonial (POST/PUT/DELETE — enqueue)

**Prompt:**

```
Add mutation testimonial endpoints to
`InkStainedWretchFunctions/Http/Testimonials/TestimonialFunction.cs`
using the enqueue pattern.

All mutation endpoints require [Authorize] + Bearer JWT.

1. [Function("CreateTestimonial")]
   POST /api/testimonials
   - Deserialize CreateTestimonialRequest { AuthorName, Quote, Rating, Featured, Locale }
   - Enqueue TestimonialCreateMessage onto "testimonial-commands"
   - Return 202 Accepted with { "message": "Testimonial creation queued." }

2. [Function("UpdateTestimonial")]
   PUT /api/testimonials/{id}
   - Enqueue TestimonialUpdateMessage onto "testimonial-commands"
   - Return 202 Accepted

3. [Function("DeleteTestimonial")]
   DELETE /api/testimonials/{id}
   - Enqueue TestimonialDeleteMessage onto "testimonial-commands"
   - Return 202 Accepted

FILE: `InkStainedWretchFunctions/Queue/TestimonialQueueProcessor.cs`

[Function("ProcessTestimonialCommand")]
Service Bus trigger on "testimonial-commands"

Route messages by a "CommandType" property in the message body:
- "Create": call ITestimonialRepository.CreateAsync(testimonial)
- "Update": call ITestimonialRepository.UpdateAsync(id, fields)
- "Delete": call ITestimonialRepository.DeleteAsync(id)
Send Notification Hub event after each operation.
```

---

## 7. Lead Capture Endpoint

**Prompt:**

```
Implement CreateLead using the enqueue pattern with rate limiting preserved.

FILE: `InkStainedWretchFunctions/Http/Leads/LeadCaptureFunction.cs`

[Function("CreateLead")]
POST /api/leads
Public (no auth)
Rate limit: 10 requests / IP / minute (use IRateLimitService)

Steps:
1. Check rate limit using IRateLimitService.IsRequestAllowedAsync(ipAddress, "leads").
   Return 429 Too Many Requests if exceeded.
2. Deserialize CreateLeadRequest { Email, Source, Name, Phone, Message }.
3. Validate Email is non-empty. Return 400 otherwise.
4. Enqueue LeadCreatedMessage onto "lead-commands" queue.
5. Return 202 Accepted with { "message": "Lead received." }

FILE: `InkStainedWretchFunctions/Queue/LeadQueueProcessor.cs`

[Function("ProcessLeadCommand")]
Service Bus trigger on "lead-commands"

Steps:
1. Call ILeadService.CreateLeadAsync(request, ipAddress).
   - Handles duplicate detection (returns existing record silently)
2. No Notification Hub event needed (no authenticated user to notify).

Preserve all existing unit tests in
`OnePageAuthor.Test/InkStainedWretchFunctions/LeadCaptureFunctionTests.cs`.
Update mock for IServiceBusPublisher.
```

---

## 8. Referral Endpoints

### 8.1 CreateReferral (POST — enqueue)

**Prompt:**

```
Implement CreateReferral using the enqueue pattern.

FILE: `InkStainedWretchFunctions/Http/Referrals/ReferralFunction.cs`

[Function("CreateReferral")]
POST /api/referrals
Public (no auth)

Steps:
1. Deserialize CreateReferralRequest { UserId, Source, ReferredEmail }.
2. Validate UserId. Return 400 if empty.
3. Enqueue ReferralCreatedMessage onto "referral-commands".
4. Return 202 Accepted.

FILE: `InkStainedWretchFunctions/Queue/ReferralQueueProcessor.cs`
[Function("ProcessReferralCommand")]
- Call IReferralService.CreateReferralAsync(request).

No Notification Hub event (anonymous users).
```

---

### 8.2 GetReferralStats (GET — no change)

**Prompt:**

```
Move GetReferralStats to the consolidated project (no logic change).

FILE: `InkStainedWretchFunctions/Http/Referrals/ReferralFunction.cs`

[Function("GetReferralStats")]
GET /api/referrals/{userId}
Public (no auth)
- Call IReferralService.GetReferralStatsAsync(userId)
- Return 200 with stats object, or 404 if userId not found
```

---

## 9. External Integration Endpoints

### 9.1 GetTLDPricing (GET — no change)

**Prompt:**

```
Move GetTLDPricing to the consolidated project (GET, no side effects).

FILE: `InkStainedWretchFunctions/Http/ExternalIntegrations/ExternalIntegrationFunctions.cs`

[Function("GetTLDPricing")]
GET /api/whmcs/tld-pricing
Authorization: Bearer JWT required
Query params: clientId (optional), currencyId (optional)

- Validate JWT
- Call IWhmcsService.GetTLDPricingAsync(clientId, currencyId)
- Return 200 with WHMCS JSON (unmodified), or 502 on WHMCS error

Reference existing GetTLDPricingFunction.cs for exact implementation.
Preserve existing unit tests in GetTLDPricingFunctionTests.cs.
```

---

### 9.2 Amazon and Penguin Random House (GET — no change)

**Prompt:**

```
Move Amazon and Penguin Random House endpoints to the consolidated project.
All are GET requests with no side effects; keep logic unchanged.

FILE: `InkStainedWretchFunctions/Http/ExternalIntegrations/ExternalIntegrationFunctions.cs`
(add to same file)

1. [Function("SearchAmazonBooksByAuthor")]
   GET /api/amazon/books/author/{authorName}
   Authorization: Bearer JWT
   Query: page (optional int)
   - Validate JWT
   - Call IAmazonProductService.SearchBooksByAuthorAsync(authorName, page)
   - Return 200 (Amazon JSON), 401, 404, 500, 502

2. [Function("SearchPenguinAuthors")]
   GET /api/penguin/authors/{authorName}
   Authorization: Bearer JWT
   - Validate JWT
   - Call IPenguinRandomHouseService.SearchAuthorsAsync(authorName)
   - Return 200, 401, 404, 500, 502

3. [Function("GetPenguinTitlesByAuthor")]
   GET /api/penguin/authors/{authorKey}/titles
   Authorization: Bearer JWT
   - Validate JWT
   - Call IPenguinRandomHouseService.GetTitlesByAuthorKeyAsync(authorKey)
   - Return 200, 401, 404, 500, 502
```

---

### 9.3 GetPersonFacts (GET — no change)

**Prompt:**

```
Move GetPersonFacts to the consolidated project.

FILE: `InkStainedWretchFunctions/Http/ExternalIntegrations/ExternalIntegrationFunctions.cs`

[Function("GetPersonFacts")]
GET /api/wikipedia/{language}/{personName}
Public (no auth)
- Call IWikipediaService.GetPersonFactsAsync(language, personName)
- Return 200 with Wikipedia extract JSON, or 404, 500, 502

Reference existing GetPersonFacts.cs. Preserve unit tests in
GetPersonFactsTests.cs. Update namespace.
```

---

## 10. Reference Data Endpoints

**Prompt:**

```
Move all reference data GET endpoints to the consolidated project.
These are all pure GET requests with no side effects.

FILE: `InkStainedWretchFunctions/Http/ReferenceData/ReferenceDataFunctions.cs`

Implement the following functions. Reference the existing source files for
exact implementation. Only update namespaces and constructor injection.

1. [Function("GetPlatformStats")]
   GET /api/stats/platform
   Public, Cache-Control: public, max-age=3600
   Calls IPlatformStatsService.GetPlatformStatsAsync()
   Tests: GetPlatformStatsTests.cs

2. [Function("GetExperiments")]
   GET /api/experiments
   Public
   Query: page (required), userId (optional)
   Calls IExperimentService.GetExperimentsAsync(request)
   Tests: GetExperimentsTests.cs

3. [Function("GetLanguages")]
   GET /api/languages/{language}
   Public
   Calls ILanguageService.GetLanguagesByRequestLanguageAsync(language)

4. [Function("GetCountriesByLanguage")]
   GET /api/countries/{language}
   Public
   Calls ICountryService.GetCountriesByLanguageAsync(language)

5. [Function("GetStateProvinces")]
   GET /api/stateprovinces/{culture}
   Authorization: Bearer JWT
   Calls IStateProvinceService.GetStateProvincesAsync(culture)

6. [Function("GetStateProvincesByCountry")]
   GET /api/stateprovinces/{countryCode}/{culture}
   Authorization: Bearer JWT
   Calls IStateProvinceService.GetStateProvincesByCountryAsync(countryCode, culture)

7. [Function("LocalizedText")]
   GET /api/localizedtext/{culture}
   Public
   Calls ILocaleDataService.GetLocalizedTextAsync(culture)

8. [Function("GetAuthorData")]
   GET /GetAuthorData/{tld}/{sld}/{language}/{region?}
   Public
   Calls IAuthorDataService.GetAuthorWithDataAsync(tld, sld, language, region)

9. [Function("GetLocaleData")]
   GET /GetLocaleData/{language}/{region?}
   Public
   Calls ILocaleDataService.GetLocalesAsync(language, region)

10. [Function("GetSitemap")]
    GET /sitemap.xml/{tld}/{sld}
    Public
    Content-Type: application/xml
    Calls IDomainRegistrationRepository to build sitemap
```

---

## 11. Image Management Endpoints

**Prompt:**

```
Migrate the ImageAPI functions into the consolidated project.

FILE: `InkStainedWretchFunctions/Http/Images/ImageFunctions.cs`

All three endpoints use [Authorize(Policy = "RequireScope.Read")] + Bearer JWT.
Register the "RequireScope.Read" authorization policy in Program.cs.

1. [Function("Upload")]
   POST /api/images/upload
   Content-Type: multipart/form-data
   Enqueue pattern:
   a. Validate JWT and authorization policy.
   b. Read the uploaded file from the multipart form data.
   c. Validate file size against user's subscription tier
      (call IImageUploadService.ValidateUploadAsync(file, userId)).
      Return 400/402/403/507 as appropriate on validation failure.
   d. Enqueue ImageUploadMessage (file bytes, userId, fileName, contentType)
      onto "image-commands".
   e. Return 202 Accepted with { "message": "Image upload queued.",
      "correlationId": "<generated guid>" }

   FILE: `InkStainedWretchFunctions/Queue/ImageQueueProcessor.cs`
   [Function("ProcessImageCommand")]
   Service Bus trigger on "image-commands"
   - Call IImageUploadService.UploadImageAsync(message)
   - Send Notification Hub event "ImageUploaded" with { id, url, correlationId }

2. [Function("Delete")]
   DELETE /api/Delete?id={imageId}
   Enqueue pattern:
   a. Validate JWT.
   b. Enqueue ImageDeleteMessage (imageId, userId) onto "image-commands".
   c. Return 202 Accepted.
   Queue processor: call IImageDeleteService.DeleteImageAsync(imageId, userId).
   Notification Hub event: "ImageDeleted" with { id }

3. [Function("WhoAmI")]
   GET /api/whoami
   Authorization: [Authorize(Policy = "RequireScope.Read")] + JWT
   GET request — no enqueue:
   - Return authenticated user claims/profile
   - No side effects, return 200 directly

Note on file size: Azure Service Bus has a 256 KB message size limit (standard)
or 100 MB (premium). If image file bytes exceed the limit, upload the file to
a staging Azure Blob container first, then enqueue a message containing the
Blob URI rather than the raw bytes. Add this logic to IImageUploadService.
```

---

## 12. Stripe / Subscription Endpoints

### 12.1 GET Stripe Endpoints (no change)

**Prompt:**

```
Move the read-only Stripe endpoints to the consolidated project.

FILE: `InkStainedWretchFunctions/Http/Stripe/StripeFunctions.cs`

All require Bearer JWT (use IJwtValidationService).

1. [Function("ListSubscription")]
   GET /api/ListSubscription/{customerId}
   Authorization: Function key + [Authorize] + JWT
   Calls IListSubscriptions.ListAsync(customerId)
   Return 200 with array of Stripe Subscription objects.

2. [Function("FindSubscription")]
   GET /api/FindSubscription
   Authorization: Function key + JWT
   Query: email (required), domain (required)
   Calls IFindSubscription.FindAsync(email, domain)
   Return 200 with Stripe Subscription or 404.

3. [Function("GetStripeCheckoutSession")]
   GET /api/GetStripeCheckoutSession/{sessionId}
   Authorization: JWT
   Calls IStripeCheckoutSessionService.GetAsync(sessionId)
   Return 200 with Stripe Checkout Session, or 404.

4. [Function("StripeHealth")]
   GET /api/StripeHealth
   Public
   Return 200 with { status: "ok", timestamp: "..." }
```

---

### 12.2 POST Stripe Endpoints (enqueue pattern)

**Prompt:**

```
Implement the Stripe mutation endpoints using the enqueue pattern.

FILE: `InkStainedWretchFunctions/Http/Stripe/StripeFunctions.cs`
(add to same file as read-only functions)

All require Bearer JWT unless noted.

1. [Function("CreateStripeCustomer")]
   POST /api/CreateStripeCustomer
   Enqueue pattern:
   - Validate JWT
   - Deserialize { Email, Name, Description }
   - Enqueue StripeCreateCustomerMessage onto "stripe-commands"
   - Return 202 Accepted with { "message": "Customer creation queued." }

2. [Function("CreateStripeCheckoutSession")]
   POST /api/CreateStripeCheckoutSession
   Enqueue pattern:
   - Validate JWT + User Profile
   - Deserialize { PriceId, SuccessUrl, CancelUrl, Quantity }
   - Enqueue StripeCheckoutSessionMessage onto "stripe-commands"
   - Return 202 Accepted

3. [Function("CreateSubscription")]
   POST /api/CreateSubscription
   Enqueue pattern:
   - Validate JWT + User Profile
   - Deserialize { PriceId, CustomerId, DomainName }
   - Enqueue StripeCreateSubscriptionMessage onto "stripe-commands"
   - Return 202 Accepted

4. [Function("UpdateSubscription")]
   POST /api/UpdateSubscription/{subscriptionId}
   Enqueue pattern:
   - Validate JWT + [Authorize]
   - Deserialize UpdateSubscriptionRequest
   - Enqueue StripeUpdateSubscriptionMessage onto "stripe-commands"
   - Return 202 Accepted

5. [Function("CancelSubscription")]
   POST /api/CancelSubscription/{subscriptionId}
   Enqueue pattern:
   - Validate JWT + User Profile
   - Deserialize optional { InvoiceNow, Prorate }
   - Enqueue StripeCancelSubscriptionMessage onto "stripe-commands"
   - Return 202 Accepted

6. [Function("GetStripePriceInformation")]
   POST /api/GetStripePriceInformation
   NOTE: Despite POST, this is a read-only query operation. Implement as
   direct response (no enqueue):
   - Validate JWT
   - Deserialize { PriceId }
   - Call IStripePriceServiceWrapper.GetPriceAsync(priceId)
   - Return 200 with Stripe Price object

7. [Function("InvoicePreview")]
   POST /api/InvoicePreview
   NOTE: Read-only preview. Implement as direct response (no enqueue):
   - Validate JWT
   - Deserialize { CustomerId, SubscriptionId, NewPriceId }
   - Call IInvoicePreviewService.PreviewAsync(request)
   - Return 200 with invoice preview

FILE: `InkStainedWretchFunctions/Queue/StripeQueueProcessor.cs`
[Function("ProcessStripeCommand")]
Service Bus trigger on "stripe-commands"

Route by CommandType field:
- "CreateCustomer": call IEnsureCustomerForUser.EnsureAsync(message)
                    Notify: "StripeCustomerCreated"
- "CreateCheckoutSession": call IStripeCheckoutSessionService.CreateAsync(message)
                           Notify: "StripeCheckoutSessionCreated" with { SessionId, Url }
- "CreateSubscription": call ICreateSubscription.CreateAsync(message)
                        Notify: "StripeSubscriptionCreated" with { SubscriptionId }
- "UpdateSubscription": call IUpdateSubscription.UpdateAsync(message)
                        Notify: "StripeSubscriptionUpdated"
- "CancelSubscription": call ICancelSubscription.CancelAsync(message)
                        Notify: "StripeSubscriptionCancelled"

All notifications sent via INotificationHubService.SendAsync(userId, eventName, payload).
```

---

## 13. Stripe Webhook

**Prompt:**

```
Move the Stripe WebHook function to the consolidated project unchanged.
The webhook must NOT use the enqueue pattern — it must validate the Stripe
signature synchronously and return a 200 immediately.

FILE: `InkStainedWretchFunctions/Http/Stripe/StripeFunctions.cs`

[Function("WebHook")]
POST /api/WebHook
Authorization: Stripe HMAC signature validation (Stripe-Signature header)

Steps:
1. Read raw request body as string.
2. Read Stripe-Signature header.
3. Call IStripeWebhookHandler.HandleAsync(rawBody, signature).
   The handler validates the HMAC signature and processes the event.
4. Return 200 { ok: true } on success.
5. Return 400 on signature validation failure.
6. Return 500 on unhandled processing error.

Do NOT enqueue Stripe webhook events — the synchronous 200 response is required
by Stripe's webhook retry logic.
```

---

## 14. Cosmos DB Change-Feed Triggers

**Prompt:**

```
Move the Cosmos DB change-feed trigger functions to the consolidated project.

FILE: `InkStainedWretchFunctions/Triggers/DomainRegistrationTriggers.cs`

1. [Function("CreateDnsZone")]
   CosmosDBTrigger on DomainRegistrations container, lease prefix "dnszone"
   - Call IDnsZoneService.CreateZoneAsync(domainRegistration) for each new document
   - Reference existing CreateDnsZone.cs implementation
   - Preserve unit tests in CreateDnsZoneFunctionTests.cs

2. [Function("DomainRegistrationTrigger")]
   CosmosDBTrigger on DomainRegistrations container, lease prefix "domainregistration"
   - For new/updated documents, enqueue to IWhmcsQueueService
   - Reference existing DomainRegistrationTriggerFunction.cs
   - Preserve unit tests in DomainRegistrationTriggerFunctionTests.cs

No behavior changes — only namespace updates.
```

---

## 15. Notification Hub Integration

**Prompt:**

```
Add Azure Notification Hub support to OnePageAuthorLib.

FILE: `OnePageAuthorLib/interfaces/INotificationHubService.cs`

public interface INotificationHubService
{
    /// <summary>
    /// Sends a named event notification to a specific authenticated user.
    /// </summary>
    Task SendAsync(
        string userId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);
}

FILE: `OnePageAuthorLib/services/NotificationHubService.cs`

Implement INotificationHubService using
Azure.Messaging.NotificationHubs.NotificationHubClient:

1. Constructor injects IConfiguration (for NotificationHub connection string
   and hub name) and ILogger<NotificationHubService>.
2. Configuration keys:
   - NOTIFICATION_HUB_CONNECTION_STRING
   - NOTIFICATION_HUB_NAME
3. SendAsync serializes payload as JSON and sends a template notification
   with the tag "userId:<userId>" so only that user's registered devices
   receive the notification.
4. The notification template payload:
   { "eventName": "<eventName>", "payload": <serialized payload> }
5. If the userId is null or empty, skip the notification (log a warning).
6. Catch and log exceptions without re-throwing (notifications are
   best-effort and must not fail the queue processor).

FILE: `OnePageAuthorLib/ServiceFactory.cs`

Add extension method:
public static IServiceCollection AddNotificationHubService(
    this IServiceCollection services)
{
    services.AddScoped<INotificationHubService, NotificationHubService>();
    return services;
}

Call this from InkStainedWretchFunctions/Program.cs.

Add NOTIFICATION_HUB_CONNECTION_STRING and NOTIFICATION_HUB_NAME to
the required environment variables section of docs/API_ENDPOINT_INVENTORY.md.

Unit tests:
FILE: `OnePageAuthor.Test/Services/NotificationHubServiceTests.cs`

Write unit tests for INotificationHubService:
1. SendAsync_ValidUserId_SendsNotification — verifies NotificationHubClient
   is called with correct tag and payload (mock INotificationHubClient)
2. SendAsync_EmptyUserId_SkipsNotification — verifies no client call
3. SendAsync_ClientThrows_LogsAndDoesNotRethrow — verifies exception handling
```

---

## 16. Service Registration Updates

**Prompt:**

```
Update `InkStainedWretchFunctions/Program.cs` to register all services
required by the consolidated function app.

The Program.cs should call (in order):
1. services.AddJwtAuthentication(configuration)        // from ServiceFactory
2. services.AddAuthorDataServices()                    // from ServiceFactory
3. services.AddDomainRegistrationServices()            // from ServiceFactory
4. services.AddAuthorInvitationServices()              // from ServiceFactory
5. services.AddTestimonialServices()                   // from ServiceFactory (or add if missing)
6. services.AddLeadServices()                          // from ServiceFactory
7. services.AddReferralServices()                      // from ServiceFactory
8. services.AddWhmcsServices()                         // from ServiceFactory
9. services.AddExternalIntegrationServices()           // from ServiceFactory
10. services.AddReferenceDataServices()                // from ServiceFactory
11. services.AddImageServices()                        // from ServiceFactory (migrated from ImageAPI)
12. services.AddStripeServices()                       // from ServiceFactory (migrated from InkStainedWretchStripe)
13. services.AddServiceBusPublisher()                  // new (created in prompt 3.1)
14. services.AddNotificationHubService()               // new (created in prompt 15)
15. Add authorization policy:
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("RequireScope.Read", policy =>
            policy.RequireClaim("scp", "User.Read", "Author.Read"));

Also update `local.settings.json.example` (or the documented secrets reference)
to include:
- SERVICE_BUS_CONNECTION_STRING
- SERVICE_BUS_DOMAIN_REGISTRATION_QUEUE (default: "domain-registration-commands")
- SERVICE_BUS_AUTHOR_INVITATION_QUEUE (default: "author-invitation-commands")
- SERVICE_BUS_TESTIMONIAL_QUEUE (default: "testimonial-commands")
- SERVICE_BUS_LEAD_QUEUE (default: "lead-commands")
- SERVICE_BUS_REFERRAL_QUEUE (default: "referral-commands")
- SERVICE_BUS_IMAGE_QUEUE (default: "image-commands")
- SERVICE_BUS_STRIPE_QUEUE (default: "stripe-commands")
- NOTIFICATION_HUB_CONNECTION_STRING
- NOTIFICATION_HUB_NAME
```

---

*See [WHMCS_WORKER_REBUILD_PROMPT.md](./WHMCS_WORKER_REBUILD_PROMPT.md) for the WHMCS worker service rebuild prompt.*  
*See [GITHUB_ACTIONS_SINGLE_APP_PROMPT.md](./GITHUB_ACTIONS_SINGLE_APP_PROMPT.md) for the GitHub Actions rebuild prompt.*
