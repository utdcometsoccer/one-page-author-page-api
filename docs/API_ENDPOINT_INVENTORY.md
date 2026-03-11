# API Endpoint Inventory

*Generated: 2026-03-04 | Reflects current codebase state*

This document provides an exhaustive listing of every API endpoint in the OnePageAuthor platform, with references to source files, request/response schemas, authorization requirements, service dependencies, and unit test coverage.

---

## Table of Contents

- [InkStainedWretchFunctions](#inkstainedwretchfunctions)
  - [Author Endpoints](#author-endpoints)
  - [Domain Registration Endpoints](#domain-registration-endpoints)
  - [Admin / Management Endpoints](#admin--management-endpoints)
  - [Author Invitation Endpoints](#author-invitation-endpoints)
  - [Testimonial Endpoints](#testimonial-endpoints)
  - [Lead Capture Endpoint](#lead-capture-endpoint)
  - [Referral Endpoints](#referral-endpoints)
  - [External Integration Endpoints](#external-integration-endpoints)
  - [Reference Data Endpoints](#reference-data-endpoints)
  - [Non-HTTP Triggers](#non-http-triggers)
- [InkStainedWretchStripe](#inkstainedwretchstripe)
  - [Subscription Management](#subscription-management)
  - [Checkout and Pricing](#checkout-and-pricing)
  - [Webhook](#webhook)
- [ImageAPI](#imageapi)
- [function-app](#function-app)
- [WhmcsWorkerService (Background)](#whmcsworkerservice-background)
- [Authorization Summary](#authorization-summary)
- [Service Interface Index](#service-interface-index)
- [Test Coverage Summary](#test-coverage-summary)

---

## InkStainedWretchFunctions

**Project path:** `InkStainedWretchFunctions/`  
**Base route prefix:** `/api` (implicit for Azure Functions)

---

### Author Endpoints

#### `GetAuthors`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetAuthors.cs` |
| **HTTP method** | GET |
| **Route** | `/api/authors/{secondLevelDomain?}/{topLevelDomain?}` |
| **Authorization** | Bearer JWT required; scope `Author.Read` validated via `IScopeValidationService` |
| **Query params** | None |
| **Route params** | `secondLevelDomain` (optional), `topLevelDomain` (optional) |

**Response (200 OK):**
```json
[
  {
    "id": "guid",
    "AuthorName": "string",
    "EmailAddress": "string",
    "TopLevelDomain": "string",
    "SecondLevelDomain": "string"
  }
]
```

**Status codes:** 200, 401, 403, 404, 500  
**Services called:** `IAuthorDataService`, `IScopeValidationService`, `IJwtValidationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/GetAuthorsTests.cs` (9 tests)

---

### Domain Registration Endpoints

#### `CreateDomainRegistration`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/DomainRegistrationFunction.cs` |
| **HTTP method** | POST |
| **Route** | `/api/domain-registrations` |
| **Authorization** | Bearer JWT required |

**Request body:**
```json
{
  "DomainName": "example.com",
  "RegistrationPeriodYears": 1,
  "AutoRenew": true,
  "PrivacyProtection": true,
  "ContactInfo": {
    "FirstName": "string",
    "LastName": "string",
    "Email": "user@example.com",
    "Phone": "+1-555-000-0000",
    "Address": {
      "Street": "string",
      "City": "string",
      "State": "string",
      "PostalCode": "string",
      "Country": "US"
    }
  }
}
```

**Response (201 Created):**
```json
{
  "Id": "guid",
  "DomainName": "example.com",
  "Status": "Pending",
  "CreatedAt": "2024-01-01T00:00:00Z"
}
```

**Status codes:** 201, 400, 401, 409, 500  
**Services called:** `IDomainRegistrationService`, `IJwtValidationService`, `IUserProfileService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/DomainRegistrationFunctionTests.cs`

---

#### `GetDomainRegistrations`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/DomainRegistrationFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/domain-registrations` |
| **Authorization** | Bearer JWT required |

**Response (200 OK):**
```json
[
  {
    "Id": "guid",
    "DomainName": "example.com",
    "Status": "Active | Pending | Failed",
    "CreatedAt": "2024-01-01T00:00:00Z",
    "LastUpdatedAt": "2024-01-02T00:00:00Z"
  }
]
```

**Status codes:** 200, 401, 500  
**Services called:** `IDomainRegistrationRepository`, `IJwtValidationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/DomainRegistrationFunctionTests.cs`

---

#### `GetDomainRegistrationById`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/DomainRegistrationFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/domain-registrations/{registrationId}` |
| **Authorization** | Bearer JWT required |

**Response (200 OK):** Single `DomainRegistrationResponse` object (same schema as list item above)  
**Status codes:** 200, 401, 404, 500  
**Services called:** `IDomainRegistrationRepository`, `IJwtValidationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/DomainRegistrationFunctionTests.cs`

---

#### `UpdateDomainRegistration`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/DomainRegistrationFunction.cs` |
| **HTTP method** | PUT |
| **Route** | `/api/domain-registrations/{registrationId}` |
| **Authorization** | Bearer JWT required |

**Request body:** Partial `DomainRegistrationUpdateRequest` (fields to update)  
**Response (200 OK):** Updated `DomainRegistrationResponse`  
**Status codes:** 200, 400, 401, 404, 500  
**Services called:** `IDomainRegistrationRepository`, `IJwtValidationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/DomainRegistrationFunctionTests.cs`

---

### Admin / Management Endpoints

#### `AdminGetIncompleteDomainRegistrations`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AdminDomainRegistrationFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/management/domain-registrations` |
| **Authorization** | Bearer JWT + `Admin` role required (`IRoleChecker`) |
| **Query params** | `maxResults` (optional, integer) |

**Response (200 OK):**
```json
[
  {
    "Id": "guid",
    "DomainName": "example.com",
    "Status": "Pending",
    "CreatedAt": "2024-01-01T00:00:00Z"
  }
]
```

**Status codes:** 200, 401, 403, 500  
**Services called:** `IDomainRegistrationRepository`, `IJwtValidationService`, `IRoleChecker`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AdminDomainRegistrationFunctionTests.cs`

---

#### `AdminGetAllDomainRegistrationsPaged`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AdminDomainRegistrationFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/management/domain-registrations/all` |
| **Authorization** | Bearer JWT + `Admin` role required (`IRoleChecker`) |
| **Query params** | `page` (optional, integer, default 1), `pageSize` (optional, integer, default 20) |

**Response (200 OK):**
```json
[
  {
    "id": "guid",
    "domain": { "topLevelDomain": "com", "secondLevelDomain": "example" },
    "contactInformation": null,
    "status": 2,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastUpdatedAt": "2024-01-02T00:00:00Z"
  }
]
```

Returns all statuses (Pending, InProgress, Completed, Failed, Cancelled), ordered by `createdAt` descending. Contact information is redacted (`null`).

**Status codes:** 200, 401, 403, 500  
**Services called:** `IDomainRegistrationRepository`, `IJwtValidationService`, `IRoleChecker`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AdminDomainRegistrationFunctionTests.cs`

---

#### `AdminUpdateDomainRegistrationStatus`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AdminDomainRegistrationFunction.cs` |
| **HTTP method** | PATCH |
| **Route** | `/api/management/domain-registrations/{registrationId}/status` |
| **Authorization** | Bearer JWT + `Admin` role required (`IRoleChecker`) |
| **Path params** | `registrationId` — Cosmos DB document ID |

**Request body:**
```json
{ "status": 2 }
```

Where `status` is a `DomainRegistrationStatus` integer: 0=Pending, 1=InProgress, 2=Completed, 3=Failed, 4=Cancelled.

**Response (200 OK):** Updated `DomainRegistrationResponse` with the new status.

**Status codes:** 200, 400, 401, 403, 404, 500  
**Services called:** `IDomainRegistrationRepository`, `IJwtValidationService`, `IRoleChecker`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AdminDomainRegistrationFunctionTests.cs`

---

#### `AdminCompleteDomainRegistration`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AdminDomainRegistrationFunction.cs` |
| **HTTP method** | POST |
| **Route** | `/api/management/domain-registrations/{registrationId}/complete` |
| **Authorization** | Bearer JWT + `Admin` role required |

**Request body:** Empty  
**Response (200 OK):** Updated `DomainRegistrationResponse`

**Processing steps:**
1. Register domain via WHMCS API (`IWhmcsService.RegisterDomainAsync`)
2. Create Azure DNS zone (`IDnsZoneService`)
3. Update name servers in WHMCS (`IWhmcsService.UpdateNameServersAsync`)
4. Add domain to Azure Front Door (`IFrontDoorService`)

**Status codes:** 200, 400, 401, 403, 404, 409, 500  
**Services called:** `IDomainRegistrationRepository`, `IWhmcsService`, `IDnsZoneService`, `IFrontDoorService`, `IRoleChecker`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AdminDomainRegistrationFunctionTests.cs`

---

### Author Invitation Endpoints

#### `CreateAuthorInvitation`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AuthorInvitationFunction.cs` |
| **HTTP method** | POST |
| **Route** | `/api/author-invitations` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Request body:**
```json
{
  "EmailAddress": "author@example.com",
  "DomainName": "example.com",
  "DomainNames": ["domain1.com", "domain2.com"],
  "Notes": "Optional notes"
}
```

**Response (201 Created):**
```json
{
  "Id": "string",
  "EmailAddress": "string",
  "DomainName": "string",
  "DomainNames": ["string"],
  "Status": "Pending",
  "CreatedAt": "2024-01-01T00:00:00Z",
  "ExpiresAt": "2024-01-31T00:00:00Z",
  "EmailSent": true,
  "LastEmailSentAt": "2024-01-01T00:00:00Z"
}
```

**Status codes:** 201, 400, 401, 409, 500  
**Services called:** `IAuthorInvitationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AuthorInvitationFunctionTests.cs`

---

#### `ListAuthorInvitations`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AuthorInvitationFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/author-invitations` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Response (200 OK):** Array of invitation objects (same schema as create response)  
**Status codes:** 200, 401, 500  
**Services called:** `IAuthorInvitationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AuthorInvitationFunctionTests.cs`

---

#### `GetAuthorInvitationById`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AuthorInvitationFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/author-invitations/{id}` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Response (200 OK):** Single invitation object  
**Status codes:** 200, 401, 404, 500  
**Services called:** `IAuthorInvitationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AuthorInvitationFunctionTests.cs`

---

#### `UpdateAuthorInvitation`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AuthorInvitationFunction.cs` |
| **HTTP method** | PUT |
| **Route** | `/api/author-invitations/{id}` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Request body:**
```json
{
  "DomainNames": ["updated-domain.com"],
  "Notes": "Updated notes",
  "ExpiresAt": "2024-02-28T00:00:00Z"
}
```

**Response (200 OK):** Updated invitation object  
**Status codes:** 200, 400, 401, 404, 500  
**Services called:** `IAuthorInvitationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AuthorInvitationFunctionTests.cs`

---

#### `ResendAuthorInvitation`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AuthorInvitationFunction.cs` |
| **HTTP method** | POST |
| **Route** | `/api/author-invitations/{id}/resend` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Request body:** Empty  
**Response (200 OK):** Updated invitation object with `LastEmailSentAt` refreshed  
**Status codes:** 200, 401, 404, 500  
**Services called:** `IAuthorInvitationService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/AuthorInvitationFunctionTests.cs`

---

### Testimonial Endpoints

#### `GetTestimonials`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetTestimonials.cs` |
| **HTTP method** | GET |
| **Route** | `/api/testimonials` |
| **Authorization** | None (public) |
| **Query params** | `limit` (integer, optional), `featured` (boolean, optional), `locale` (string, optional) |
| **Cache** | `Cache-Control: public, max-age=900` (15 minutes) |

**Response (200 OK):**
```json
{
  "testimonials": [
    {
      "id": "string",
      "AuthorName": "string",
      "Quote": "string",
      "Rating": 5,
      "Featured": true,
      "Locale": "en-US",
      "CreatedAt": "2024-01-01T00:00:00Z"
    }
  ],
  "total": 42
}
```

**Status codes:** 200, 500  
**Services called:** `ITestimonialRepository`  
**Unit tests:** None (repository pattern, direct calls)

---

#### `CreateTestimonial`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/CreateTestimonial.cs` |
| **HTTP method** | POST |
| **Route** | `/api/testimonials` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Request body:**
```json
{
  "AuthorName": "string",
  "Quote": "string",
  "Rating": 5,
  "Featured": false,
  "Locale": "en-US"
}
```

**Response (201 Created):** Created testimonial object  
**Status codes:** 201, 400, 401, 500  
**Services called:** `ITestimonialRepository`  
**Unit tests:** None

---

#### `UpdateTestimonial`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/UpdateTestimonial.cs` |
| **HTTP method** | PUT |
| **Route** | `/api/testimonials/{id}` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Request body:** Partial testimonial fields to update  
**Response (200 OK):** Updated testimonial object  
**Status codes:** 200, 400, 401, 404, 500  
**Services called:** `ITestimonialRepository`  
**Unit tests:** None

---

#### `DeleteTestimonial`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/DeleteTestimonial.cs` |
| **HTTP method** | DELETE |
| **Route** | `/api/testimonials/{id}` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Response (200 OK):** `{ "message": "Testimonial deleted" }`  
**Status codes:** 200, 401, 404, 500  
**Services called:** `ITestimonialRepository`  
**Unit tests:** None

---

### Lead Capture Endpoint

#### `CreateLead`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/LeadCaptureFunction.cs` |
| **HTTP method** | POST |
| **Route** | `/api/leads` |
| **Authorization** | None (public endpoint) |
| **Rate limit** | 10 requests / IP / minute (`IRateLimitService`) |

**Request body:**
```json
{
  "Email": "lead@example.com",
  "Source": "landing | blog | contact_form | social_media",
  "Name": "string (optional)",
  "Phone": "string (optional)",
  "Message": "string (optional)"
}
```

**Response (201 Created):**
```json
{
  "Id": "string",
  "Email": "string",
  "Source": "string",
  "CreatedAt": "2024-01-01T00:00:00Z"
}
```

**Status codes:** 201, 200 (duplicate), 400, 429, 500  
**Services called:** `ILeadService`, `IRateLimitService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/LeadCaptureFunctionTests.cs`

---

### Referral Endpoints

#### `CreateReferral`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/ReferralFunction.cs` |
| **HTTP method** | POST |
| **Route** | `/api/referrals` |
| **Authorization** | None |

**Request body:**
```json
{
  "UserId": "string",
  "Source": "string (optional)",
  "ReferredEmail": "string (optional)"
}
```

**Response (201 Created):**
```json
{
  "Id": "string",
  "ReferralCode": "string",
  "ReferralUrl": "https://...",
  "CreatedAt": "2024-01-01T00:00:00Z"
}
```

**Status codes:** 201, 400, 500  
**Services called:** `IReferralService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/ReferralFunctionTests.cs`

---

#### `GetReferralStats`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/ReferralFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/referrals/{userId}` |
| **Authorization** | None |

**Response (200 OK):**
```json
{
  "UserId": "string",
  "TotalReferrals": 10,
  "ConvertedReferrals": 3,
  "ReferralCode": "string",
  "ReferralUrl": "https://..."
}
```

**Status codes:** 200, 404, 500  
**Services called:** `IReferralService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/ReferralFunctionTests.cs`

---

### External Integration Endpoints

#### `GetTLDPricing`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetTLDPricingFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/whmcs/tld-pricing` |
| **Authorization** | Bearer JWT required |
| **Query params** | `clientId` (optional), `currencyId` (optional) |

**Response (200 OK):** WHMCS JSON (unmodified):
```json
{
  "result": "success",
  "pricing": {
    "com": {
      "registration": { "1": 8.95, "2": 17.90 },
      "renewal": { "1": 8.95 },
      "transfer": { "1": 8.95 }
    }
  }
}
```

**Status codes:** 200, 401, 500, 502  
**Services called:** `IWhmcsService`, `IUserProfileService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/GetTLDPricingFunctionTests.cs`

---

#### `SearchAmazonBooksByAuthor`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/AmazonProductFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/amazon/books/author/{authorName}` |
| **Authorization** | Bearer JWT required |
| **Query params** | `page` (optional, integer) |

**Response (200 OK):** Amazon Product Advertising API response (unmodified)  
**Status codes:** 200, 400, 401, 404, 500, 502  
**Services called:** `IAmazonProductService`, `IUserProfileService`  
**Unit tests:** None

---

#### `SearchPenguinAuthors`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/PenguinRandomHouseFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/penguin/authors/{authorName}` |
| **Authorization** | Bearer JWT required |

**Response (200 OK):** Penguin Random House API response (unmodified)  
**Status codes:** 200, 401, 404, 500, 502  
**Services called:** `IPenguinRandomHouseService`, `IUserProfileService`  
**Unit tests:** None

---

#### `GetPenguinTitlesByAuthor`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/PenguinRandomHouseFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/penguin/authors/{authorKey}/titles` |
| **Authorization** | Bearer JWT required |

**Response (200 OK):** Penguin Random House API response (unmodified)  
**Status codes:** 200, 401, 404, 500, 502  
**Services called:** `IPenguinRandomHouseService`, `IUserProfileService`  
**Unit tests:** None

---

#### `GetPersonFacts`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetPersonFacts.cs` |
| **HTTP method** | GET |
| **Route** | `/api/wikipedia/{language}/{personName}` |
| **Authorization** | None |

**Response (200 OK):** Wikipedia extract / facts JSON  
**Status codes:** 200, 404, 500, 502  
**Services called:** Wikipedia HTTP client (via `IWikipediaService` or similar)  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/GetPersonFactsTests.cs`

---

#### `LocalizedText`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/LocalizedTextFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/localizedtext/{culture}` |
| **Authorization** | None |

**Response (200 OK):** Key/value map of UI text in the requested culture  
**Status codes:** 200, 404, 500  
**Services called:** `ILocaleDataService` or `ILocaleRepository`  
**Unit tests:** None

---

### Reference Data Endpoints

#### `GetPlatformStats`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetPlatformStats.cs` |
| **HTTP method** | GET |
| **Route** | `/api/stats/platform` |
| **Authorization** | None (public) |
| **Cache** | `Cache-Control: public, max-age=3600` (1 hour) |

**Response (200 OK):**
```json
{
  "activeAuthors": 123,
  "booksPublished": 4567,
  "totalRevenue": 89012.34,
  "averageRating": 4.8,
  "countriesServed": 45,
  "lastUpdated": "2024-01-01T00:00:00Z"
}
```

**Status codes:** 200, 500  
**Services called:** `IPlatformStatsService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/GetPlatformStatsTests.cs`

---

#### `GetExperiments`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetExperiments.cs` |
| **HTTP method** | GET |
| **Route** | `/api/experiments` |
| **Authorization** | None |
| **Query params** | `page` (required: e.g. `landing`, `pricing`), `userId` (optional) |

**Response (200 OK):**
```json
{
  "SessionId": "string",
  "UserId": "string",
  "Page": "string",
  "Experiments": [
    {
      "ExperimentName": "string",
      "VariantName": "string",
      "Weight": 0.5
    }
  ]
}
```

**Status codes:** 200, 400, 500  
**Services called:** `IExperimentService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/GetExperimentsTests.cs`

---

#### `GetLanguages`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetLanguages.cs` |
| **HTTP method** | GET |
| **Route** | `/api/languages/{language}` |
| **Authorization** | None |

**Response (200 OK):** Array of `{ code, name }` language objects  
**Status codes:** 200, 500  
**Services called:** `ILanguageService`  
**Unit tests:** None

---

#### `GetCountriesByLanguage`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetCountriesByLanguage.cs` |
| **HTTP method** | GET |
| **Route** | `/api/countries/{language}` |
| **Authorization** | None |

**Response (200 OK):**
```json
{
  "Language": "en",
  "Count": 195,
  "Countries": [
    { "code": "US", "name": "United States" }
  ]
}
```

**Status codes:** 200, 500  
**Services called:** `ICountryService`  
**Unit tests:** None

---

#### `GetStateProvinces`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetStateProvincesByCountry.cs` |
| **HTTP method** | GET |
| **Route** | `/api/stateprovinces/{culture}` |
| **Authorization** | Bearer JWT required |

**Response (200 OK):** Array of state/province objects  
**Status codes:** 200, 401, 500  
**Services called:** `IStateProvinceService`  
**Unit tests:** None

---

#### `GetStateProvincesByCountry`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/GetStateProvincesByCountry.cs` |
| **HTTP method** | GET |
| **Route** | `/api/stateprovinces/{countryCode}/{culture}` |
| **Authorization** | Bearer JWT required |

**Response (200 OK):**
```json
{
  "Country": "US",
  "Culture": "en-US",
  "Count": 50,
  "StateProvinces": [
    { "Code": "CA", "Name": "California", "Country": "US", "Culture": "en-US" }
  ]
}
```

**Status codes:** 200, 401, 500  
**Services called:** `IStateProvinceService`  
**Unit tests:** None

---

### Non-HTTP Triggers

#### `CreateDnsZone`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/CreateDnsZone.cs` |
| **Trigger type** | Cosmos DB Change Feed |
| **Container** | `DomainRegistrations` |
| **Lease prefix** | `dnszone` |
| **Purpose** | Automatically creates an Azure DNS zone when a new domain registration document is inserted |

**Services called:** `IDnsZoneService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/CreateDnsZoneFunctionTests.cs`

---

#### `DomainRegistrationTrigger`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchFunctions/DomainRegistrationTriggerFunction.cs` |
| **Trigger type** | Cosmos DB Change Feed |
| **Container** | `DomainRegistrations` |
| **Lease prefix** | `domainregistration` |
| **Purpose** | Enqueues WHMCS domain registration operations onto the Azure Service Bus queue (`IWhmcsQueueService`). The `WhmcsWorkerService` on the static-IP VM dequeues and calls WHMCS. |

**Services called:** `IWhmcsQueueService`  
**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/DomainRegistrationTriggerFunctionTests.cs`

---

## InkStainedWretchStripe

**Project path:** `InkStainedWretchStripe/`

---

### Subscription Management

#### `CreateSubscription`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/CreateSubscription.cs` |
| **HTTP method** | POST |
| **Route** | `/api/CreateSubscription` |
| **Authorization** | Bearer JWT + User Profile |

**Request body:**
```json
{
  "PriceId": "price_abc123",
  "CustomerId": "cus_abc123",
  "DomainName": "example.com"
}
```

**Response (200 OK):**
```json
{
  "SubscriptionId": "sub_abc123",
  "ClientSecret": "seti_abc123"
}
```

**Status codes:** 200, 400, 401, 500  
**Services called:** `ICreateSubscription`  
**Unit tests:** None (integration tests in `IntegrationTestAuthorDataService/`)

---

#### `UpdateSubscription`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/UpdateSubscription.cs` |
| **HTTP method** | POST |
| **Route** | `/api/UpdateSubscription/{subscriptionId}` |
| **Authorization** | `[Authorize]` + Bearer JWT |

**Request body:** `UpdateSubscriptionRequest` (new price ID, quantity, etc.)  
**Response (200 OK):** Updated subscription details  
**Status codes:** 200, 400, 401, 403, 404, 500  
**Services called:** `IUpdateSubscription`  
**Unit tests:** None

---

#### `CancelSubscription`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/CancelSubscription.cs` |
| **HTTP method** | POST |
| **Route** | `/api/CancelSubscription/{subscriptionId}` |
| **Authorization** | Bearer JWT + User Profile |

**Request body (optional):**
```json
{
  "InvoiceNow": false,
  "Prorate": true
}
```

**Response (200 OK):** Cancelled subscription status  
**Status codes:** 200, 400, 401, 403, 404, 500  
**Services called:** `ICancelSubscription`  
**Unit tests:** None

---

#### `ListSubscription`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/ListSubscription.cs` |
| **HTTP method** | GET |
| **Route** | `/api/ListSubscription/{customerId}` |
| **Authorization** | Function-level key + `[Authorize]` + Bearer JWT |

**Response (200 OK):** Array of Stripe Subscription objects  
**Status codes:** 200, 401, 403, 404, 500  
**Services called:** `IListSubscriptions`  
**Unit tests:** None

---

#### `FindSubscription`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/FindSubscription.cs` |
| **HTTP method** | GET |
| **Route** | `/api/FindSubscription` |
| **Authorization** | Function-level key + Bearer JWT |
| **Query params** | `email` (required), `domain` (required) |

**Response (200 OK):** Stripe Subscription object or null  
**Status codes:** 200, 400, 401, 404, 500  
**Services called:** `IFindSubscription`  
**Unit tests:** None

---

### Checkout and Pricing

#### `CreateStripeCheckoutSession`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/CreateStripeCheckoutSession.cs` |
| **HTTP method** | POST |
| **Route** | `/api/CreateStripeCheckoutSession` |
| **Authorization** | Bearer JWT + User Profile |

**Request body:**
```json
{
  "PriceId": "price_abc123",
  "SuccessUrl": "https://example.com/success",
  "CancelUrl": "https://example.com/cancel",
  "Quantity": 1
}
```

**Response (200 OK):**
```json
{
  "SessionId": "cs_abc123",
  "Url": "https://checkout.stripe.com/..."
}
```

**Status codes:** 200, 400, 401, 500  
**Services called:** `IStripeCheckoutSessionService`  
**Unit tests:** None

---

#### `GetStripeCheckoutSession`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/GetStripeCheckoutSession.cs` |
| **HTTP method** | GET |
| **Route** | `/api/GetStripeCheckoutSession/{sessionId}` |
| **Authorization** | Bearer JWT |

**Response (200 OK):** Stripe Checkout Session details  
**Status codes:** 200, 401, 404, 500  
**Services called:** `IStripeCheckoutSessionService`  
**Unit tests:** None

---

#### `CreateStripeCustomer`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/CreateStripeCustomer.cs` |
| **HTTP method** | POST |
| **Route** | `/api/CreateStripeCustomer` |
| **Authorization** | Bearer JWT |

**Request body:**
```json
{
  "Email": "user@example.com",
  "Name": "string (optional)",
  "Description": "string (optional)"
}
```

**Response (200 OK):** Stripe Customer object  
**Status codes:** 200, 400, 401, 500  
**Services called:** `IEnsureCustomerForUser`  
**Unit tests:** None

---

#### `GetStripePriceInformation`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/GetStripePriceInformation.cs` |
| **HTTP method** | POST |
| **Route** | `/api/GetStripePriceInformation` |
| **Authorization** | Bearer JWT |

**Request body:** `{ "PriceId": "price_abc123" }`  
**Response (200 OK):** Stripe Price object with metadata  
**Status codes:** 200, 400, 401, 404, 500  
**Services called:** `IStripePriceServiceWrapper`  
**Unit tests:** None

---

#### `InvoicePreview`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/InvoicePreview.cs` |
| **HTTP method** | POST |
| **Route** | `/api/InvoicePreview` |
| **Authorization** | Bearer JWT |

**Request body:**
```json
{
  "CustomerId": "cus_abc123",
  "SubscriptionId": "sub_abc123",
  "NewPriceId": "price_abc123"
}
```

**Response (200 OK):** Upcoming invoice preview  
**Status codes:** 200, 400, 401, 404, 500  
**Services called:** `IInvoicePreviewService`  
**Unit tests:** None

---

#### `StripeHealth`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/StripeHealthFunction.cs` |
| **HTTP method** | GET |
| **Route** | `/api/StripeHealth` |
| **Authorization** | None |

**Response (200 OK):** `{ "status": "ok", "timestamp": "..." }`  
**Status codes:** 200  
**Services called:** Configuration validation only  
**Unit tests:** None

---

### Webhook

#### `WebHook`

| Property | Value |
|----------|-------|
| **Source file** | `InkStainedWretchStripe/WebHook.cs` |
| **HTTP method** | POST |
| **Route** | `/api/WebHook` |
| **Authorization** | Stripe webhook signature validation (HMAC) |

**Handled Stripe events:**
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_succeeded`
- `invoice.payment_failed`
- `charge.failed`

**Response (200 OK):** `{ "ok": true }`  
**Status codes:** 200, 400 (invalid signature), 500  
**Services called:** `IStripeWebhookHandler`  
**Unit tests:** None

---

## ImageAPI

**Project path:** `ImageAPI/`

---

#### `Upload`

| Property | Value |
|----------|-------|
| **Source file** | `ImageAPI/Upload.cs` |
| **HTTP method** | POST |
| **Route** | `/api/images/upload` |
| **Authorization** | `[Authorize(Policy = "RequireScope.Read")]` + Bearer JWT |
| **Content-Type** | `multipart/form-data` |

**Storage tier limits:**

| Tier | Storage | Bandwidth | Max File | Max Files |
|------|---------|-----------|----------|-----------|
| Starter (Free) | 5 GB | 25 GB | 5 MB | 20 |
| Pro ($9.99/mo) | 250 GB | 1 TB | 10 MB | 500 |
| Elite ($19.99/mo) | 2 TB | 10 TB | 25 MB | 2,000 |

**Response (201 Created):**
```json
{
  "id": "string",
  "url": "https://...",
  "name": "string",
  "size": 102400,
  "uploadedAt": "2024-01-01T00:00:00Z"
}
```

**Status codes:** 201, 400, 401, 402 (bandwidth exceeded), 403 (max files exceeded), 500, 507 (storage full)  
**Services called:** `IImageUploadService`  
**Unit tests:** None

---

#### `Delete`

| Property | Value |
|----------|-------|
| **Source file** | `ImageAPI/Delete.cs` |
| **HTTP method** | DELETE |
| **Route** | `/api/Delete` |
| **Authorization** | `[Authorize(Policy = "RequireScope.Read")]` + Bearer JWT |
| **Query params** | `id` (image ID) |

**Response (200 OK):** `{ "message": "Image deleted successfully" }`  
**Status codes:** 200, 400, 401, 404, 500  
**Services called:** `IImageDeleteService`  
**Unit tests:** None

---

#### `WhoAmI`

| Property | Value |
|----------|-------|
| **Source file** | `ImageAPI/WhoAmI.cs` |
| **HTTP method** | GET |
| **Route** | `/api/whoami` |
| **Authorization** | `[Authorize(Policy = "RequireScope.Read")]` + Bearer JWT |

**Response (200 OK):** Authenticated user's claims/profile  
**Status codes:** 200, 401, 500  
**Services called:** JWT claims extraction  
**Unit tests:** None

---

#### `User`

| Property | Value |
|----------|-------|
| **Source file** | `ImageAPI/User.cs` |
| **HTTP method** | GET / PUT |
| **Route** | `/api/user` |
| **Authorization** | Bearer JWT required |

**Response (200 OK):** User profile object  
**Status codes:** 200, 401, 404, 500  
**Services called:** `IUserProfileService`  
**Unit tests:** None

---

## function-app

**Project path:** `function-app/`

---

#### `GetAuthorData`

| Property | Value |
|----------|-------|
| **Source file** | `function-app/GetAuthorData.cs` |
| **HTTP method** | GET |
| **Route** | `/GetAuthorData/{tld}/{sld}/{language}/{region?}` |
| **Authorization** | None |

**Response (200 OK):** Author profile with localized content  
**Status codes:** 200, 404, 500  
**Services called:** `IAuthorDataService`  
**Unit tests:** None

---

#### `GetLocaleData`

| Property | Value |
|----------|-------|
| **Source file** | `function-app/GetLocaleData.cs` |
| **HTTP method** | GET |
| **Route** | `/GetLocaleData/{language}/{region?}` |
| **Authorization** | None |

**Response (200 OK):** Locale metadata (languages, currencies, formatting)  
**Status codes:** 200, 500  
**Services called:** `ILocaleDataService`  
**Unit tests:** None

---

#### `GetSitemap`

| Property | Value |
|----------|-------|
| **Source file** | `function-app/GetSitemap.cs` |
| **HTTP method** | GET |
| **Route** | `/sitemap.xml/{tld}/{sld}` |
| **Authorization** | None |
| **Content-Type (response)** | `application/xml` |

**Response (200 OK):** XML sitemap for the given domain  
**Status codes:** 200, 404, 500  
**Services called:** `IDomainRegistrationRepository`  
**Unit tests:** None

---

## WhmcsWorkerService (Background)

**Project path:** `WhmcsWorkerService/`  
**Type:** .NET Worker Service (systemd daemon on Azure Linux VM)  
**Source files:** `WhmcsWorkerService/Worker.cs`, `WhmcsWorkerService/Program.cs`

**Purpose:** Dequeues domain registration messages from the Azure Service Bus queue and calls the WHMCS REST API. Deployed to a VM with a static outbound IP address so WHMCS can allowlist it.

**Configuration environment variables:**

| Variable | Description |
|----------|-------------|
| `SERVICE_BUS_CONNECTION_STRING` | Azure Service Bus connection string (WhmcsListener policy) |
| `SERVICE_BUS_WHMCS_QUEUE_NAME` | Queue name (default: `whmcs-domain-registrations`) |
| `WHMCS_API_URL` | WHMCS REST API base URL |
| `WHMCS_API_IDENTIFIER` | WHMCS API credential identifier |
| `WHMCS_API_SECRET` | WHMCS API credential secret |

**Queue message schema (`WhmcsDomainRegistrationMessage`):**
```json
{
  "DomainRegistration": {
    "Domain": {
      "FullDomainName": "example.com"
    }
  },
  "NameServers": ["ns1.azure-dns.com", "ns2.azure-dns.net"]
}
```

**Message processing outcomes:**

| Outcome | Trigger | Service Bus Action |
|---------|---------|-------------------|
| `Complete` | Success | Remove from queue |
| `Abandon` | Transient failure (WHMCS unavailable) | Return to queue for retry |
| `DeadLetterInvalidJson` | Malformed JSON | Move to dead-letter sub-queue |
| `DeadLetterMissingData` | Missing domain data | Move to dead-letter sub-queue |

**Processing steps:**
1. Deserialize `WhmcsDomainRegistrationMessage` from Service Bus message body
2. Call `IWhmcsService.RegisterDomainAsync(domainRegistration)` — returns bool
3. If registration succeeds and 2–5 name servers are present, call `IWhmcsService.UpdateNameServersAsync(domainName, nameServers)`
4. Complete or abandon/dead-letter based on outcome

**Unit tests:** `OnePageAuthor.Test/InkStainedWretchFunctions/DomainRegistrationTriggerFunctionTests.cs`  
**Integration tests:** `WhmcsTestHarness/`

---

## Authorization Summary

| Pattern | Endpoints | Notes |
|---------|-----------|-------|
| Public (no auth) | `GetTestimonials`, `CreateLead`, `CreateReferral`, `GetReferralStats`, `GetCountriesByLanguage`, `GetLanguages`, `GetPersonFacts`, `GetPlatformStats`, `GetExperiments`, `LocalizedText`, `GetAuthorData`, `GetLocaleData`, `GetSitemap`, `StripeHealth` | 14 endpoints |
| Bearer JWT | `GetAuthors`, `CreateDomainRegistration`, `GetDomainRegistrations`, `GetDomainRegistrationById`, `UpdateDomainRegistration`, `GetTLDPricing`, `SearchAmazonBooksByAuthor`, `SearchPenguinAuthors`, `GetPenguinTitlesByAuthor`, `GetStateProvinces`, `GetStateProvincesByCountry`, `CreateStripeCustomer`, `CreateSubscription`, `CancelSubscription`, `GetStripeCheckoutSession`, `GetStripePriceInformation`, `InvoicePreview`, `CreateStripeCheckoutSession` | 18 endpoints |
| `[Authorize]` + JWT | `CreateAuthorInvitation`, `ListAuthorInvitations`, `GetAuthorInvitationById`, `UpdateAuthorInvitation`, `ResendAuthorInvitation`, `CreateTestimonial`, `UpdateTestimonial`, `DeleteTestimonial`, `ListSubscription`, `UpdateSubscription` | 10 endpoints |
| JWT + `Admin` role | `AdminGetIncompleteDomainRegistrations`, `AdminCompleteDomainRegistration` | 2 endpoints |
| JWT + Scope | `GetAuthors` (requires `Author.Read` scope via `IScopeValidationService`) | 1 endpoint |
| ImageAPI policy | `Upload`, `Delete`, `WhoAmI` | `RequireScope.Read` policy |
| Stripe signature | `WebHook` | HMAC signature verification |
| Function key | `FindSubscription`, `ListSubscription` | Azure Function key header |

---

## Service Interface Index

All interfaces are located in `OnePageAuthorLib/interfaces/`.

| Interface | Implementation location | Used by |
|-----------|------------------------|---------|
| `IAuthorDataService` | `OnePageAuthorLib/services/` | `GetAuthors`, `function-app` |
| `IAuthorInvitationService` | `OnePageAuthorLib/services/AuthorInvitationService.cs` | `AuthorInvitationFunction` |
| `IAuthorInvitationRepository` | `OnePageAuthorLib/nosql/` | `AuthorInvitationService` |
| `ICountryService` | `OnePageAuthorLib/services/` | `GetCountriesByLanguage` |
| `ICountryRepository` | `OnePageAuthorLib/nosql/` | `CountryService` |
| `IDnsZoneService` | `OnePageAuthorLib/api/` | `AdminDomainRegistrationFunction`, `CreateDnsZone` |
| `IDomainRegistrationRepository` | `OnePageAuthorLib/nosql/DomainRegistrationRepository.cs` | Domain functions |
| `IDomainRegistrationService` | `OnePageAuthorLib/services/` | `CreateDomainRegistration` |
| `IEmailService` | `OnePageAuthorLib/services/` | `AuthorInvitationService` |
| `IExperimentService` | `OnePageAuthorLib/services/` | `GetExperiments` |
| `IExperimentRepository` | `OnePageAuthorLib/nosql/` | `ExperimentService` |
| `IFrontDoorService` | `OnePageAuthorLib/api/` | `AdminDomainRegistrationFunction` |
| `IJwtValidationService` | `OnePageAuthorLib/Authentication/` | All authenticated functions |
| `ILanguageService` | `OnePageAuthorLib/services/` | `GetLanguages` |
| `ILanguageRepository` | `OnePageAuthorLib/nosql/` | `LanguageService` |
| `ILeadService` | `OnePageAuthorLib/services/` | `LeadCaptureFunction` |
| `ILeadRepository` | `OnePageAuthorLib/nosql/` | `LeadService` |
| `ILocaleDataService` | `OnePageAuthorLib/services/` | `function-app` |
| `ILocaleRepository` | `OnePageAuthorLib/nosql/` | `LocaleDataService` |
| `IPlatformStatsService` | `OnePageAuthorLib/services/` | `GetPlatformStats` |
| `IPlatformStatsRepository` | `OnePageAuthorLib/nosql/` | `PlatformStatsService` |
| `IRateLimitService` | `OnePageAuthorLib/services/` | `LeadCaptureFunction` |
| `IReferralService` | `OnePageAuthorLib/services/` | `ReferralFunction` |
| `IReferralRepository` | `OnePageAuthorLib/nosql/` | `ReferralService` |
| `IRoleChecker` | `OnePageAuthorLib/Authentication/RoleChecker.cs` | Admin functions |
| `IScopeValidationService` | `OnePageAuthorLib/api/ScopeValidationService.cs` | `GetAuthors` |
| `IStateProvinceService` | `OnePageAuthorLib/services/` | `GetStateProvinces*` |
| `IStateProvinceRepository` | `OnePageAuthorLib/nosql/` | `StateProvinceService` |
| `ITestimonialRepository` | `OnePageAuthorLib/nosql/` | Testimonial functions |
| `IUserProfileService` | `OnePageAuthorLib/services/` | Many authenticated functions |
| `IUserProfileRepository` | `OnePageAuthorLib/nosql/` | `UserProfileService` |
| `IWhmcsQueueService` | `OnePageAuthorLib/interfaces/IWhmcsQueueService.cs` | `DomainRegistrationTrigger` |
| `IWhmcsService` | `OnePageAuthorLib/api/` | Admin domain, `WhmcsWorkerService` |

---

## Test Coverage Summary

| Test file | Functions covered | Count |
|-----------|------------------|-------|
| `GetAuthorsTests.cs` | `GetAuthors` | 9 |
| `DomainRegistrationFunctionTests.cs` | `CreateDomainRegistration`, `GetDomainRegistrations`, `GetDomainRegistrationById`, `UpdateDomainRegistration` | ~12 |
| `AdminDomainRegistrationFunctionTests.cs` | `AdminGetIncompleteDomainRegistrations`, `AdminCompleteDomainRegistration` | ~10 |
| `AuthorInvitationFunctionTests.cs` | All 5 author invitation endpoints | ~15 |
| `CreateDnsZoneFunctionTests.cs` | `CreateDnsZone` | ~5 |
| `DomainRegistrationTriggerFunctionTests.cs` | `DomainRegistrationTrigger` | ~8 |
| `GetExperimentsTests.cs` | `GetExperiments` | ~6 |
| `GetPersonFactsTests.cs` | `GetPersonFacts` | ~4 |
| `GetPlatformStatsTests.cs` | `GetPlatformStats` | ~5 |
| `GetTLDPricingFunctionTests.cs` | `GetTLDPricing` | ~6 |
| `LeadCaptureFunctionTests.cs` | `CreateLead` | ~8 |
| `ReferralFunctionTests.cs` | `CreateReferral`, `GetReferralStats` | ~6 |
| `OnePageAuthor.Test/API/ScopeValidationServiceTests.cs` | `IScopeValidationService` | ~10 |

**Coverage gaps (no unit tests):** Testimonial functions, Stripe functions, ImageAPI functions, `function-app` functions, `GetCountriesByLanguage`, `GetLanguages`, `GetStateProvinces*`, `LocalizedText`, Amazon/Penguin functions.

---

*See [COPILOT_IMPLEMENTATION_PROMPTS.md](./COPILOT_IMPLEMENTATION_PROMPTS.md) for prompts to implement endpoints in the new consolidated architecture.*
