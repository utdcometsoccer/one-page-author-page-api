# Testimonials API

This module provides REST API endpoints for managing and retrieving testimonials for the landing page.

## Features

- **Public GET endpoint** - Retrieve testimonials with filtering and caching
- **Admin CRUD endpoints** - Create, update, and delete testimonials (authenticated)
- **Multi-language support** - Filter testimonials by locale
- **Featured testimonials** - Flag testimonials for special display
- **Rating system** - 1-5 star ratings
- **Caching** - Public endpoint cached for 15 minutes

## Endpoints

### Public Endpoint

#### GET /api/testimonials

Retrieves testimonials with optional filtering.

**Query Parameters:**
- `limit` (optional, default: 5, max: 20) - Maximum number of testimonials to return
- `featured` (optional) - Filter to only featured testimonials
- `locale` (optional) - Filter by locale (e.g., "en-US", "es-ES")

**Example Request:**
```bash
curl "https://api.example.com/api/testimonials?featured=true&locale=en-US&limit=3"
```

**Example Response:**
```json
{
  "testimonials": [
    {
      "id": "sarah-mitchell-en-us",
      "authorName": "Sarah Mitchell",
      "authorTitle": "Mystery Novelist",
      "quote": "This platform transformed how I connect with my readers.",
      "rating": 5,
      "photoUrl": null,
      "featured": true,
      "createdAt": "2025-01-15T10:30:00Z",
      "locale": "en-US"
    }
  ],
  "total": 7
}
```

**Cache-Control:** `public, max-age=900` (15 minutes)

### Admin Endpoints (Authenticated)

All admin endpoints require JWT Bearer token authentication.

#### POST /api/admin/testimonials

Creates a new testimonial.

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

#### PUT /api/admin/testimonials/{id}

Updates an existing testimonial.

**Response:** 200 OK

#### DELETE /api/admin/testimonials/{id}

Deletes a testimonial.

**Response:** 204 No Content

## Data Model

### Testimonial Entity

```csharp
public class Testimonial
{
    public string id { get; set; }              // Unique identifier
    public string AuthorName { get; set; }       // Required
    public string AuthorTitle { get; set; }      // e.g., "Mystery Novelist"
    public string Quote { get; set; }            // Required
    public int Rating { get; set; }              // 1-5, Required
    public string? PhotoUrl { get; set; }        // Optional
    public bool Featured { get; set; }           // Default: false
    public DateTime CreatedAt { get; set; }      // UTC timestamp
    public string Locale { get; set; }           // Default: "en-US"
}
```

## Storage

Testimonials are stored in Azure Cosmos DB in the `Testimonials` container with:
- **Partition Key:** `/Locale`
- **ID:** String-based unique identifier

## Seeding Data

Use the `SeedTestimonials` console application to populate initial testimonial data:

```bash
cd SeedTestimonials
dotnet run
```

The seeder includes sample testimonials in multiple languages (en-US, es-ES, fr-FR) and is idempotent - safe to run multiple times.

## Implementation Files

- **GetTestimonials.cs** - Public GET endpoint
- **CreateTestimonial.cs** - Admin POST endpoint
- **UpdateTestimonial.cs** - Admin PUT endpoint
- **DeleteTestimonial.cs** - Admin DELETE endpoint
- **OnePageAuthorLib/entities/Testimonial.cs** - Entity model
- **OnePageAuthorLib/interfaces/ITestimonialRepository.cs** - Repository interface
- **OnePageAuthorLib/nosql/TestimonialRepository.cs** - Repository implementation
- **OnePageAuthorLib/nosql/TestimonialsContainerManager.cs** - Container manager

## Testing

Run the unit tests:

```bash
dotnet test --filter "FullyQualifiedName~Testimonial"
```

Test coverage includes:
- Repository CRUD operations
- Filtering by featured and locale
- Limit enforcement
- Container manager initialization
- Edge cases and error handling

## Configuration

No additional configuration required beyond standard Cosmos DB settings:
- `COSMOSDB_ENDPOINT_URI`
- `COSMOSDB_PRIMARY_KEY`
- `COSMOSDB_DATABASE_ID`

## Authentication

Admin endpoints use JWT Bearer token authentication via the `[Authorize]` attribute. Ensure users have proper authentication configured through Microsoft Entra ID.

## Caching Strategy

The public GET endpoint includes a `Cache-Control: public, max-age=900` header, allowing CDNs and browsers to cache responses for 15 minutes. This reduces load on the API while ensuring testimonials remain reasonably fresh.

## Multi-Language Support

Testimonials support the same locales as the rest of the platform:
- English (en-US)
- Spanish (es-ES, es-MX)
- French (fr-FR, fr-CA)
- Arabic (ar)
- Chinese Simplified (zh-CN)
- Chinese Traditional (zh-TW)

Filter by locale to display appropriate testimonials for each market.

## Best Practices

1. **Featured Testimonials**: Mark 3-5 high-quality testimonials as featured for homepage display
2. **Diverse Testimonials**: Include testimonials from different author types and locales
3. **Regular Updates**: Refresh testimonials periodically to keep content fresh
4. **Photo URLs**: Use consistent image sizes and formats for professional appearance
5. **Quote Length**: Keep quotes concise (150-250 characters) for better readability
