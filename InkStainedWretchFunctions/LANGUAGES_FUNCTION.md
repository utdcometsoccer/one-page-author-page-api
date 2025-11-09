# GetLanguages Function

## Overview

The `GetLanguages` Azure Function provides an API endpoint to retrieve all available languages with their names localized in a specific request language. This endpoint follows the established patterns in the InkStainedWretchFunctions project and supports JWT authentication.

## Endpoint

**Route:** `GET /api/languages/{language}`

**Parameters:**
- `language` (path parameter): The ISO 639-1 two-letter language code for which to return localized language names (e.g., "en", "es", "fr", "ar", "zh-cn", "zh-tw")

## Authentication

This endpoint requires JWT authentication. Include a valid JWT token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Response Format

The endpoint returns an array of language objects in JSON format:

```json
[
  {
    "code": "en",
    "name": "English"
  },
  {
    "code": "es", 
    "name": "Spanish"
  },
  {
    "code": "fr",
    "name": "French"
  }
]
```

### Response Structure

Each language object contains:
- `code`: ISO 639-1 two-letter language code (lowercase)
-
ame`: Localized name of the language in the requested language

## Supported Languages

The API supports the following languages:
- **English (en)**: English
- **Spanish (es)**: Español
- **French (fr)**: Français
- **Arabic (ar)**: العربية
- **Chinese Simplified (zh-cn)**: 中文（简体）- Mainland China
- **Chinese Traditional (zh-tw)**: 中文（繁體）- Taiwan

## Example Requests

### Get languages in English
```bash
curl -X GET "https://your-function-app.azurewebsites.net/api/languages/en" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
[
  { "code": "en", "name": "English" },
  { "code": "es", "name": "Spanish" },
  { "code": "fr", "name": "French" },
  { "code": "ar", "name": "Arabic" },
  { "code": "zh-cn", "name": "Chinese (Simplified)" },
  { "code": "zh-tw", "name": "Chinese (Traditional)" }
]
```

### Get languages in Spanish
```bash
curl -X GET "https://your-function-app.azurewebsites.net/api/languages/es" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
[
  { "code": "en", "name": "Inglés" },
  { "code": "es", "name": "Español" },
  { "code": "fr", "name": "Francés" },
  { "code": "ar", "name": "Árabe" },
  { "code": "zh-cn", "name": "Chino (Simplificado)" },
  { "code": "zh-tw", "name": "Chino (Tradicional)" }
]
```

### Get languages in Chinese (Simplified)
```bash
curl -X GET "https://your-function-app.azurewebsites.net/api/languages/zh-cn" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
[
  { "code": "en", "name": "英语" },
  { "code": "es", "name": "西班牙语" },
  { "code": "fr", "name": "法语" },
  { "code": "ar", "name": "阿拉伯语" },
  { "code": "zh-cn", "name": "中文（简体）" },
  { "code": "zh-tw", "name": "中文（繁体）" }
]
```

## Error Responses

### 400 Bad Request
Returned when the language parameter is missing or invalid.

```json
{
  "error": "Language parameter is required"
}
```

### 401 Unauthorized
Returned when JWT token is missing or invalid.

```json
{
  "error": "Unauthorized"
}
```

### 404 Not Found
Returned when no languages are found for the requested language.

```json
{
  "message": "No Languages found for language: xyz"
}
```

### 500 Internal Server Error
Returned when an unexpected error occurs.

```json
{
  "error": "Internal server error occurred while retrieving Languages"
}
```

## Data Seeding

Language data must be seeded into the Cosmos DB `Languages` container before using this endpoint. Use the `SeedLanguages` console application to populate the data:

```bash
cd SeedLanguages
dotnet run
```

See the [SeedLanguages README](../SeedLanguages/README.md) for more details on data seeding.

## Implementation Details

### Dependencies
- `ILanguageService`: Service layer for language operations
- `IJwtValidationService`: JWT token validation
- Cosmos DB `Languages` container with `/RequestLanguage` partition key

### Architecture Pattern
This function follows the same pattern as `GetStateProvinces` and other functions in the project:
1. Route parameter validation
2. JWT authentication
3. Service layer invocation
4. Structured error handling
5. Clean JSON response format

## Best Practices for API Consumers

1. **Caching**: Language data changes infrequently. Consider caching responses on the client side.
2. **Fallback**: Always have a fallback to a default language (typically "en") if the requested language is not available.
3. **Case Handling**: The API normalizes language codes to lowercase, but clients should also normalize before making requests.
4. **Error Handling**: Implement proper error handling for all response status codes.

## Related Documentation

- [StateProvinces Functions](STATEPROVINCES_FUNCTIONS.md)
- [SeedLanguages Application](../SeedLanguages/README.md)
- [JWT Authentication Guide](USER_SECRETS_GUIDE.md)
