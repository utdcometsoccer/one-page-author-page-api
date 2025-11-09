# StateProvince Azure Functions

This document describes the new Azure Functions added to the InkStainedWretchFunctions project for retrieving StateProvince data from Cosmos DB.

## Functions Overview

### 1. GetStateProvinces Function

**Route:** `GET /api/stateprovinces/{culture}`

**Purpose:** Retrieves all states and provinces for a specific culture across all countries.

**Parameters:**
- `culture` (route parameter): The culture code (e.g., "en-US", "fr-CA", "es-MX", "zh-CN", "zh-TW", "ar-EG")

**Authentication:** Required - JWT token with appropriate scope

**Response Format:**
```json
{
  "Culture": "en-US",
  "TotalCount": 159,
  "Data": [
    {
      "Country": "CA",
      "Culture": "en-US",
      "StateProvinces": [
        {
          "Code": "AB",
          "Name": "Alberta",
          "Country": "CA",
          "Culture": "en-CA"
        }
      ]
    },
    {
      "Country": "US",
      "Culture": "en-US", 
      "StateProvinces": [
        {
          "Code": "AL",
          "Name": "Alabama",
          "Country": "US",
          "Culture": "en-US"
        }
      ]
    }
  ]
}
```

**Example Usage:**
- `GET /api/stateprovinces/en-US` - Get all English (US) state/province names
- `GET /api/stateprovinces/fr-CA` - Get all French (Canadian) state/province names
- `GET /api/stateprovinces/es-MX` - Get all Spanish (Mexican) state/province names
- `GET /api/stateprovinces/zh-CN` - Get all Simplified Chinese state/province names
- `GET /api/stateprovinces/zh-TW` - Get all Traditional Chinese state/province names
- `GET /api/stateprovinces/ar-EG` - Get all Arabic state/province names

### 2. GetStateProvincesByCountry Function

**Route:** `GET /api/stateprovinces/{countryCode}/{culture}`

**Purpose:** Retrieves states and provinces for a specific country and culture combination.

**Parameters:**
- `countryCode` (route parameter): The two-letter ISO country code (e.g., "US", "CA", "MX", "CN", "TW", "EG")
- `culture` (route parameter): The culture code (e.g., "en-US", "fr-CA", "es-MX")

**Authentication:** Required - JWT token with appropriate scope

**Response Format:**
```json
{
  "Country": "US",
  "Culture": "en-US",
  "Count": 55,
  "StateProvinces": [
    {
      "Code": "AL",
      "Name": "Alabama",
      "Country": "US",
      "Culture": "en-US"
    },
    {
      "Code": "AK", 
      "Name": "Alaska",
      "Country": "US",
      "Culture": "en-US"
    }
  ]
}
```

**Example Usage:**
- `GET /api/stateprovinces/US/en-US` - Get US states in English
- `GET /api/stateprovinces/CA/fr-CA` - Get Canadian provinces in French
- `GET /api/stateprovinces/MX/es-MX` - Get Mexican states in Spanish
- `GET /api/stateprovinces/CN/zh-CN` - Get Chinese provinces in Simplified Chinese
- `GET /api/stateprovinces/TW/zh-TW` - Get Taiwan regions in Traditional Chinese
- `GET /api/stateprovinces/EG/ar-EG` - Get Egyptian governorates in Arabic

## Security

Both functions require JWT authentication but do not enforce specific scopes, allowing any authenticated user to access StateProvince data.

## Supported Data

The functions can retrieve data for the following countries and cultures:

### United States (US)
- English: `en-US`
- French: `fr-US`
- Spanish: `es-US`
- 55 states and territories total

### Canada (CA)  
- English: `en-CA`
- French: `fr-CA`
- 13 provinces and territories total

### Mexico (MX)
- Spanish: `es-MX`
- English: `en-MX`
- French: `fr-MX`
- 32 states total

### China (CN)
- Simplified Chinese: `zh-CN`
- English: `en-CN`
- 33 provinces, regions, and special administrative regions total

### Taiwan (TW)
- Traditional Chinese: `zh-TW`
- English: `en-TW`
- 22 counties and cities total

### Egypt (EG)
- Arabic: `ar-EG`
- English: `en-EG`
- 28 governorates total

## Error Handling

Both functions provide comprehensive error handling for:
- Missing or invalid parameters
- Authentication failures
- Authorization (insufficient permissions)
- No data found scenarios
- Internal server errors

## Dependencies

The functions depend on:
- `IStateProvinceService` - Business logic for StateProvince operations
- `IJwtValidationService` - JWT token validation
- `ILogger` - Structured logging

## Registration

The required services are registered in `Program.cs`:
```csharp
.AddStateProvinceRepository() // Add StateProvince repository
.AddStateProvinceServices() // Add StateProvince services
```
