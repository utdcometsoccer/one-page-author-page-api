# Get Countries By Language API

## Overview

The Get Countries By Language API endpoint provides localized country names based on the requested language. This endpoint is part of the InkStainedWretch API and follows the established patterns for multilingual data access.

## Endpoint

```
GET /api/countries/{language}
```

### Parameters

- `language` (required): The language code for country names
  - Supported values: `en`, `es`, `fr`, `ar`, `zh-cn`, `zh-tw`
  - Case-insensitive

### Authentication

Requires JWT authentication via Bearer token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Response Format

### Success Response (200 OK)

Returns a JSON object with language, count, and array of countries:

```json
{
  "language": "en",
  "count": 40,
  "countries": [
    {
      "code": "AR",
      "name": "Argentina"
    },
    {
      "code": "AU",
      "name": "Australia"
    },
    {
      "code": "AT",
      "name": "Austria"
    }
  ]
}
```

### Country Object

Each country object contains:

- `code` (string): ISO 3166-1 alpha-2 country code (2 uppercase letters)
- `name` (string): Localized country name in the requested language

Countries are returned sorted alphabetically by name in the requested language.

## Error Responses

### 400 Bad Request

Returned when the language parameter is missing or invalid:

```json
{
  "error": "Language parameter is required"
}
```

Or:

```json
{
  "error": "Invalid language format: xyz"
}
```

### 401 Unauthorized

Returned when JWT token is missing or invalid:

```json
{
  "error": "Unauthorized: No token provided"
}
```

Or:

```json
{
  "error": "Unauthorized: Invalid token"
}
```

### 404 Not Found

Returned when no countries are found for the specified language:

```json
{
  "message": "No Countries found for language: xyz"
}
```

### 500 Internal Server Error

Returned when an unexpected error occurs:

```json
{
  "error": "Internal server error occurred while retrieving Countries"
}
```

## Supported Languages

| Language Code | Language Name | Country Count |
|--------------|---------------|---------------|
| `en` | English | 40 |
| `es` | Spanish (Español) | 40 |
| `fr` | French (Français) | 40 |
| `ar` | Arabic (العربية) | 40 |
| `zh-cn` | Simplified Chinese (简体中文) | 40 |
| `zh-tw` | Traditional Chinese (繁體中文) | 40 |

## Country Coverage

The API includes 40 major countries from all continents:

- **North America**: United States, Canada, Mexico
- **South America**: Brazil, Argentina, Chile, Colombia, Peru, Venezuela
- **Europe**: United Kingdom, France, Germany, Italy, Spain, Portugal, Netherlands, Belgium, Switzerland, Austria, Sweden, Norway, Denmark, Finland, Poland, Greece
- **Asia**: China, Taiwan, Japan, South Korea, India, Saudi Arabia, United Arab Emirates, Turkey, Israel
- **Africa**: South Africa, Egypt
- **Oceania**: Australia, New Zealand

## Example Requests

### cURL

```bash
# Get countries in English
curl -X GET "https://your-api.azurewebsites.net/api/countries/en" \
  -H "Authorization: Bearer your-jwt-token"

# Get countries in Spanish
curl -X GET "https://your-api.azurewebsites.net/api/countries/es" \
  -H "Authorization: Bearer your-jwt-token"

# Get countries in Simplified Chinese
curl -X GET "https://your-api.azurewebsites.net/api/countries/zh-cn" \
  -H "Authorization: Bearer your-jwt-token"
```

### JavaScript/TypeScript

```typescript
async function getCountriesByLanguage(language: string, token: string) {
  const response = await fetch(
    `https://your-api.azurewebsites.net/api/countries/${language}`,
    {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );
  
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  
  return await response.json();
}

// Usage
const countries = await getCountriesByLanguage('en', 'your-jwt-token');
console.log(countries.countries); // Array of country objects
```

### C#

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

public class CountryResponse
{
    public string Language { get; set; }
    public int Count { get; set; }
    public List<CountryDto> Countries { get; set; }
}

public class CountryDto
{
    public string Code { get; set; }
    public string Name { get; set; }
}

public async Task<CountryResponse> GetCountriesByLanguage(string language, string token)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var response = await client.GetAsync(
        $"https://your-api.azurewebsites.net/api/countries/{language}");
    
    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<CountryResponse>(json);
}

// Usage
var countries = await GetCountriesByLanguage("en", "your-jwt-token");
foreach (var country in countries.Countries)
{
    Console.WriteLine($"{country.Code}: {country.Name}");
}
```

## Best Practices

1. **Caching**: The country list is relatively static. Consider caching the response client-side for better performance.

2. **Language Fallback**: If a requested language is not available, fall back to English (`en`) as the default.

3. **Token Management**: Store JWT tokens securely and refresh them before expiration.

4. **Error Handling**: Always implement proper error handling for network failures and API errors.

5. **Localization**: Use the language code that matches your application's current locale.

## Data Seeding

To populate or update country data in the database, use the `SeedCountries` console application:

```bash
cd SeedCountries
dotnet run
```

The seeder is idempotent and can be run multiple times without creating duplicates. See [SeedCountries README](../SeedCountries/README.md) for more information.

## Implementation Details

### Architecture

- **Entity**: `Country` (in `OnePageAuthorLib/entities/`)
- **Service**: `CountryService` implements `ICountryService`
- **Repository**: `CountryRepository` implements `ICountryRepository`
- **Container Manager**: `CountriesContainerManager` manages Cosmos DB container
- **Function**: `GetCountriesByLanguage` Azure Function HTTP trigger

### Cosmos DB Schema

- **Container Name**: `Countries`
- **Partition Key**: `/Language`
- **Document Structure**:
  ```json
  {
    "id": "unique-guid",
    "Code": "US",
    "Name": "United States",
    "Language": "en"
  }
  ```

### Service Registration

Country services are registered in the DI container via:
- `AddCountryRepository()` - Registers repository and container manager
- `AddCountryServices()` - Registers the country service

## Related Endpoints

- `GET /api/stateprovinces/{culture}` - Get states/provinces by culture
- `GET /api/stateprovinces/{countryCode}/{culture}` - Get states/provinces by country and culture

## Versioning

Current API version: v1
This endpoint follows the same versioning strategy as other InkStainedWretch API endpoints.

## Rate Limiting

Rate limiting applies per the standard Azure Function consumption plan limits. Consider implementing application-level rate limiting if needed.

## Support

For issues or questions, please contact the development team or file an issue in the repository.
