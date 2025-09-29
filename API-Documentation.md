# OnePageAuthor API Documentation

*Generated on 2025-09-29 13:20:30 UTC*

This comprehensive API documentation covers all Azure Functions and endpoints available in the OnePageAuthor system.

## Table of Contents

- [Authentication](#authentication)
- [Image API](#image-api)
- [Domain Registration API](#domain-registration-api)
- [External Integration API](#external-integration-api)
- [Error Handling](#error-handling)
- [TypeScript Examples](#typescript-examples)

## Authentication

All API endpoints require authentication using JWT Bearer tokens. Include the token in the Authorization header:

```http
Authorization: Bearer <your-jwt-token>
```

### TypeScript Authentication Helper

```typescript
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
```


## ImageAPI

Image management API for uploading, retrieving, and deleting user images

### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

#### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

### FunctionExecutorAutoStartup

System.Xml.XmlElement

#### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**
- `hostBuilder`: The instance to use for service registration.

---

### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

#### String)

---

#### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

#### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

#### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**
- `hostBuilder`: The instance to use for service registration.

---



## InkStainedWretchFunctions

Core application functions for domain registration and external API integration

### DomainRegistrationFunction

HTTP endpoint to create and manage domain registrations.

System.Xml.XmlElement

#### CreateDomainRegistrationRequest)

**Description:** Creates a new domain registration for the authenticated user.

**Parameters:**
- `req`: HTTP request containing the domain registration data
- `payload`: Domain registration request payload with domain details

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

#### HttpRequest)

**Description:** Gets all domain registrations for the authenticated user.

**Parameters:**
- `req`: HTTP request (no additional parameters required)

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

#### String)

**Description:** Gets a specific domain registration by ID for the authenticated user.

**Parameters:**
- `req`: HTTP request
- `registrationId`: The registration ID from the route

**Returns:** Domain registration or error response

---

### PenguinRandomHouseFunction

Azure Function for calling Penguin Random House API

#### String)

**Description:** Searches for authors by name and returns the unmodified JSON response from Penguin Random House API.

**Parameters:**
- `req`: HTTP request with authentication
- `authorName`: Author name from route parameter to search for

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

#### String)

**Description:** Gets titles by author key and returns the unmodified JSON response from Penguin Random House API.

**Parameters:**
- `req`: HTTP request with authentication
- `authorKey`: Author key from route parameter (obtained from search results)

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

### LocalizedText

System.Xml.XmlElement

#### ILocalizationTextProvider)

**Description:** System.Xml.XmlElement

**Parameters:**
- `logger`: Logger instance.
- `provider`: Localization text provider service.

---

#### String)

**Description:** Handles HTTP GET requests for localized text.

**Parameters:**
- `req`: The incoming HTTP request.
- `culture`: Route parameter representing the culture (e.g. en-US).

**Returns:** 200 with JSON payload of localized text; 400 if culture is invalid or retrieval fails.

---

### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

#### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

### FunctionExecutorAutoStartup

System.Xml.XmlElement

#### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**
- `hostBuilder`: The instance to use for service registration.

---

### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

#### String)

---

#### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

#### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

#### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**
- `hostBuilder`: The instance to use for service registration.

---


## Error Handling

All API endpoints return consistent error responses:

```json
{
  "error": "Descriptive error message"
}
```

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

```typescript
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
```

## Rate Limiting

API endpoints may be rate-limited based on subscription tier:

- **Starter**: 100 requests/minute
- **Pro**: 1000 requests/minute  
- **Elite**: 10000 requests/minute

Rate limit headers are included in responses:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1640995200
```

---

*This documentation is automatically generated from source code XML comments. Last updated: 2025-09-29 13:20:30 UTC*
