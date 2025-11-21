# Wikipedia Person Facts API

## Overview

The `GetPersonFacts` API endpoint retrieves structured facts about a person from Wikipedia. It combines data from two Wikipedia APIs:
- **REST API v1** - For summary information, descriptions, thumbnails, and canonical URLs
- **MediaWiki API** - For detailed lead paragraph text

## Endpoint

```
GET /api/wikipedia/{language}/{personName}
```

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `language` | string | Yes | Wikipedia language code (e.g., "en", "es", "fr", "ar", "zh", "de") |
| `personName` | string | Yes | Name of the person to search for (URL encoded if contains spaces) |

### Response

Returns a JSON object with the following structure:

```typescript
interface WikipediaPersonFacts {
  title: string;              // Title of the Wikipedia page
  description: string;        // Short description of the person
  extract: string;            // Introduction text about the person
  leadParagraph: string;      // Lead paragraph from MediaWiki API (plain text)
  thumbnail?: {               // Thumbnail image (may be null)
    source: string;           // URL to the thumbnail image
    width: number;            // Width in pixels
    height: number;           // Height in pixels
  };
  canonicalUrl: string;       // Full URL to the Wikipedia page
  language: string;           // Language code used for the request
}
```

### HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 OK | Successfully retrieved person facts |
| 400 Bad Request | Invalid or missing parameters |
| 404 Not Found | No Wikipedia page found for the specified person |
| 502 Bad Gateway | Error calling Wikipedia APIs |
| 500 Internal Server Error | Unexpected server error |

## Examples

### cURL Examples

#### Get facts about Albert Einstein in English
```bash
curl "http://localhost:7071/api/wikipedia/en/Albert_Einstein"
```

#### Get facts about Marie Curie in French
```bash
curl "http://localhost:7071/api/wikipedia/fr/Marie_Curie"
```

#### Get facts with spaces in name (URL encoded)
```bash
curl "http://localhost:7071/api/wikipedia/en/Stephen%20Hawking"
```

### TypeScript/JavaScript Examples

#### Basic Usage
```typescript
interface WikipediaPersonFacts {
  title: string;
  description: string;
  extract: string;
  leadParagraph: string;
  thumbnail?: {
    source: string;
    width: number;
    height: number;
  };
  canonicalUrl: string;
  language: string;
}

async function getPersonFacts(
  personName: string, 
  language: string = 'en'
): Promise<WikipediaPersonFacts> {
  const encodedName = encodeURIComponent(personName.trim());
  
  const response = await fetch(
    `/api/wikipedia/${language}/${encodedName}`,
    {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    }
  );

  if (response.ok) {
    return await response.json();
  } else if (response.status === 404) {
    throw new Error('Person not found on Wikipedia');
  }

  throw new Error('Failed to fetch person facts');
}
```

### Response Example

Request:
```
GET /api/wikipedia/en/Albert_Einstein
```

Response (200 OK):
```json
{
  "title": "Albert Einstein",
  "description": "German-born scientist (1879–1955)",
  "extract": "Albert Einstein was a German-born theoretical physicist...",
  "leadParagraph": "Albert Einstein (14 March 1879 – 18 April 1955)...",
  "thumbnail": {
    "source": "https://upload.wikimedia.org/.../Einstein.jpg",
    "width": 320,
    "height": 396
  },
  "canonicalUrl": "https://en.wikipedia.org/wiki/Albert_Einstein",
  "language": "en"
}
```

## Supported Languages

The API supports all Wikipedia language codes, including:

- **en** - English
- **es** - Spanish
- **fr** - French
- **de** - German
- **zh** - Chinese
- **ar** - Arabic
- And many more...

For a complete list, see: https://meta.wikimedia.org/wiki/List_of_Wikipedias

## Testing

The implementation includes:

1. **Unit Tests** - 14 tests for service logic
2. **Function Tests** - 17 tests for Azure Function
3. **Integration Tests** - Optional tests for live Wikipedia API

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~Wikipedia|FullyQualifiedName~GetPersonFacts"
```
