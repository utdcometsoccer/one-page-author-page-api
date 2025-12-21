# OnePageAuthor API Documentation

*Generated on 2025-10-13 14:59:21 UTC*

This comprehensive API documentation covers all Azure Functions and endpoints available in the OnePageAuthor system.

## Table of Contents

- [Authentication](#authentication)
- [Image API](#image-api)
- [Domain Registration API](#domain-registration-api)
- [External Integration API](#external-integration-api)
- [Testimonials API](#testimonials-api)
- [Error Handling](#error-handling)
- [TypeScript Examples](#typescript-examples)

## Authentication

All API endpoints require authentication using JWT Bearer tokens. Include the token in the Authorization header:

`http
Authorization: Bearer <your-jwt-token>
`

### TypeScript Authentication Helper

`	ypescript
class ApiClient {
  private baseUrl: string;
  private token: string;

  constructor(baseUrl: string, token: string) {
    this.baseUrl = baseUrl.replace(/\/$/, '');
    this.token = token;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = ${'$'}{this.baseUrl}{endpoint};

    const response = await fetch(url, {
      ...options,
      headers: {
        'Authorization': Bearer {this.token},
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    if (response.status === 401) {
      throw new Error('Authentication failed - token may be expired');
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Unknown error' }));
      throw new Error(error.error || HTTP {response.status}: {response.statusText});
    }

    return response.json();
  }

  public get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' });
  }

  public post<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  public delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }
}
`


## Azure Functions API

The following Azure Functions provide the core API endpoints for the OnePageAuthor system:

### function-app

Main application functions for author data and localization services

#### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

##### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

#### FunctionExecutorAutoStartup

System.Xml.XmlElement

##### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

#### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

##### String)

---

##### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

#### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

##### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

#### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

##### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

### ImageAPI

Image management API for uploading, retrieving, and deleting user images

#### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

##### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

#### FunctionExecutorAutoStartup

System.Xml.XmlElement

##### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

#### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

##### String)

---

##### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

#### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

##### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

#### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

##### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

### InkStainedWretchFunctions

Core application functions for domain registration and external API integration

#### CreateDnsZoneFunction

Cosmos DB trigger function that creates DNS zones when domain registrations are added or modified. Uses a unique lease collection to avoid conflicts with other triggers on the same container.

##### DomainRegistration})

**Description:** Triggered when documents are inserted or updated in the DomainRegistrations container. Creates Azure DNS zones for newly registered domains.

**Parameters:**

- `input`: List of domain registrations that were added or modified

---

#### DomainRegistrationFunction

HTTP endpoint to create and manage domain registrations.

##### CreateDomainRegistrationRequest)

**Description:** Creates a new domain registration for the authenticated user.

**Parameters:**

- `req`: HTTP request containing the domain registration data
- `payload`: Domain registration request payload with domain details

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

##### HttpRequest)

**Description:** Gets all domain registrations for the authenticated user.

**Parameters:**

- `req`: HTTP request (no additional parameters required)

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

##### String)

**Description:** Gets a specific domain registration by ID for the authenticated user.

**Parameters:**

- `req`: HTTP request
- `registrationId`: The registration ID from the route

**Returns:** Domain registration or error response

---

##### DomainRegistration})

**Description:** Processes changes to domain registrations and registers domains via Google Domains API.

**Parameters:**

- `input`: List of changed domain registrations from Cosmos DB

---

#### DomainRegistrationTriggerFunction

Azure Function triggered by changes to the DomainRegistrations Cosmos DB container. Processes new domain registrations and adds them to Azure Front Door if they don't already exist.

##### DomainRegistration})

**Description:** Processes changes to domain registrations and adds new domains to Azure Front Door.

**Parameters:**

- `input`: List of changed domain registrations from Cosmos DB

---

#### GoogleDomainRegistrationFunction

Azure Function triggered by changes to the DomainRegistrations Cosmos DB container. Registers domains using the Google Domains API when new registrations are created.

##### DomainRegistration})

**Description:** Processes changes to domain registrations and registers domains via Google Domains API.

**Parameters:**

- `input`: List of changed domain registrations from Cosmos DB

---

#### PenguinRandomHouseFunction

Azure Function for calling Penguin Random House API

##### String)

**Description:** Searches for authors by name and returns the unmodified JSON response from Penguin Random House API.

**Parameters:**

- `req`: HTTP request with authentication
- `authorName`: Author name from route parameter to search for

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

##### String)

**Description:** Gets titles by author key and returns the unmodified JSON response from Penguin Random House API.

**Parameters:**

- `req`: HTTP request with authentication
- `authorKey`: Author key from route parameter (obtained from search results)

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

#### GetStateProvinces

Azure Function for retrieving StateProvince data by culture.

##### String)

**Description:** Gets states and provinces by culture code.

**Parameters:**

- `req`: The HTTP request.
- `culture`: The culture code (e.g., "en-US", "fr-CA", "es-MX").

**Returns:** List of StateProvince entities for the specified culture.

---

##### String)

**Description:** Gets states and provinces by country code and culture.

**Parameters:**

- `req`: The HTTP request.
- `countryCode`: The two-letter country code (e.g., "US", "CA", "MX").
- `culture`: The culture code (e.g., "en-US", "fr-CA", "es-MX").

**Returns:** List of StateProvince entities for the specified country and culture.

---

#### GetStateProvincesByCountry

Azure Function for retrieving StateProvince data by country and culture.

##### String)

**Description:** Gets states and provinces by country code and culture.

**Parameters:**

- `req`: The HTTP request.
- `countryCode`: The two-letter country code (e.g., "US", "CA", "MX").
- `culture`: The culture code (e.g., "en-US", "fr-CA", "es-MX").

**Returns:** List of StateProvince entities for the specified country and culture.

---

#### LocalizedText

System.Xml.XmlElement

##### ILocalizationTextProvider)

**Description:** System.Xml.XmlElement

**Parameters:**

- `logger`: Logger instance.
- `provider`: Localization text provider service.

---

##### String)

**Description:** Handles HTTP GET requests for localized text.

**Parameters:**

- `req`: The incoming HTTP request.
- `culture`: Route parameter representing the culture (e.g. en-US).

**Returns:** 200 with JSON payload of localized text; 400 if culture is invalid or retrieval fails.

---

#### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

##### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

#### FunctionExecutorAutoStartup

System.Xml.XmlElement

##### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

#### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

##### String)

---

##### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

#### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

##### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

#### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

##### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

### InkStainedWretchStripe

Stripe payment processing functions for subscription management and billing

#### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

##### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

#### FunctionExecutorAutoStartup

System.Xml.XmlElement

##### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

#### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

##### String)

---

##### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

#### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

##### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

#### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

##### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---



## Testing & Validation

The following projects provide comprehensive testing coverage:

### OnePageAuthor.Test

Unit and integration tests for the OnePageAuthor application


## Error Handling

All API endpoints return consistent error responses:

`json
{
  "error": "Descriptive error message"
}
`

### Common HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Invalid or missing token |
| 402 | Payment Required - Subscription limit exceeded |
| 403 | Forbidden - Access denied |
| 404 | Not Found - Resource not found |
| 409 | Conflict - Resource already exists |
| 500 | Internal Server Error |
| 507 | Insufficient Storage - Storage quota exceeded |

### TypeScript Error Handling

`	ypescript
interface ApiError {
  error: string;
  details?: any;
}

class ApiException extends Error {
  public statusCode: number;
  public apiError: ApiError;

  constructor(statusCode: number, apiError: ApiError) {
    super(apiError.error);
    this.statusCode = statusCode;
    this.apiError = apiError;
  }
}

// Usage in async functions
try {
  const result = await apiClient.get('/api/images/user');
} catch (error) {
  if (error instanceof ApiException) {
    switch (error.statusCode) {
      case 401:
        // Redirect to login
        window.location.href = '/login';
        break;
      case 403:
        // Show upgrade prompt
        showUpgradePrompt();
        break;
      default:
        // Show general error
        showErrorMessage(error.message);
    }
  }
}
`

## Rate Limiting

API endpoints may be rate-limited based on subscription tier:

- **Starter**: 100 requests/minute
- **Pro**: 1000 requests/minute
- **Elite**: 10000 requests/minute

Rate limit headers are included in responses:

`
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1640995200
`

---

## Testimonials API

The Testimonials API provides endpoints for managing and retrieving testimonials for the landing page.

### GET /api/testimonials

**Public endpoint** - Retrieves testimonials with optional filtering and caching.

**Query Parameters:**
- `limit` (optional, number): Maximum number of testimonials to return. Default: 5, Max: 20
- `featured` (optional, boolean): Filter to only featured testimonials
- `locale` (optional, string): Filter by locale (e.g., "en-US", "es-ES", "fr-FR")

**Response:**
```json
{
  "testimonials": [
    {
      "id": "string",
      "authorName": "string",
      "authorTitle": "string",
      "quote": "string",
      "rating": 5,
      "photoUrl": "string | null",
      "featured": true,
      "createdAt": "2025-01-15T10:30:00Z",
      "locale": "en-US"
    }
  ],
  "total": 10
}
```

**Cache Control:** 15 minutes (public, max-age=900)

**TypeScript Example:**
```typescript
// Get featured testimonials in English
const response = await fetch('/api/testimonials?featured=true&locale=en-US&limit=3');
const data = await response.json();

// Interface
interface GetTestimonialsResponse {
  testimonials: Testimonial[];
  total: number;
}

interface Testimonial {
  id: string;
  authorName: string;
  authorTitle: string;
  quote: string;
  rating: number;
  photoUrl?: string;
  featured: boolean;
  createdAt: string;
  locale: string;
}
```

### POST /api/admin/testimonials

**Protected endpoint** - Creates a new testimonial. Requires authentication.

**Request Body:**
```json
{
  "authorName": "Sarah Mitchell",
  "authorTitle": "Mystery Novelist",
  "quote": "This platform transformed how I connect with my readers.",
  "rating": 5,
  "photoUrl": null,
  "featured": true,
  "locale": "en-US"
}
```

**Response:** 201 Created
```json
{
  "id": "generated-id",
  "authorName": "Sarah Mitchell",
  "authorTitle": "Mystery Novelist",
  "quote": "This platform transformed how I connect with my readers.",
  "rating": 5,
  "photoUrl": null,
  "featured": true,
  "createdAt": "2025-01-15T10:30:00Z",
  "locale": "en-US"
}
```

**Validation Rules:**
- `authorName` is required
- `quote` is required
- `rating` must be between 1-5

**TypeScript Example:**
```typescript
const apiClient = new ApiClient(baseUrl, token);
const newTestimonial = {
  authorName: "Sarah Mitchell",
  authorTitle: "Mystery Novelist",
  quote: "This platform transformed how I connect with my readers.",
  rating: 5,
  featured: true,
  locale: "en-US"
};

const created = await apiClient.post<Testimonial>('/api/admin/testimonials', newTestimonial);
console.log('Created testimonial:', created.id);
```

### PUT /api/admin/testimonials/{id}

**Protected endpoint** - Updates an existing testimonial. Requires authentication.

**Request Body:**
```json
{
  "authorName": "Sarah Mitchell",
  "authorTitle": "Mystery Novelist - Updated",
  "quote": "Updated testimonial text.",
  "rating": 5,
  "photoUrl": "https://example.com/photo.jpg",
  "featured": false,
  "locale": "en-US"
}
```

**Response:** 200 OK (returns updated testimonial)

**Error Responses:**
- 404 Not Found - Testimonial with specified ID does not exist
- 400 Bad Request - Invalid testimonial data or validation failure

**TypeScript Example:**
```typescript
const apiClient = new ApiClient(baseUrl, token);
const updates = {
  authorName: "Sarah Mitchell",
  authorTitle: "Mystery Novelist - Updated",
  quote: "Updated testimonial text.",
  rating: 5,
  photoUrl: "https://example.com/photo.jpg",
  featured: false,
  locale: "en-US"
};

const updated = await apiClient.put<Testimonial>('/api/admin/testimonials/sarah-mitchell-en-us', updates);
```

### DELETE /api/admin/testimonials/{id}

**Protected endpoint** - Deletes a testimonial. Requires authentication.

**Response:** 204 No Content

**Error Responses:**
- 404 Not Found - Testimonial with specified ID does not exist

**TypeScript Example:**
```typescript
const apiClient = new ApiClient(baseUrl, token);
await apiClient.delete('/api/admin/testimonials/sarah-mitchell-en-us');
console.log('Testimonial deleted successfully');
```

---

*This documentation is automatically generated from source code XML comments. Last updated: 2025-10-13 14:59:21 UTC*
