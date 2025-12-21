# Testimonial Seeder

This console application seeds the Cosmos DB Testimonials container with sample testimonial data.

## Prerequisites

- Azure Cosmos DB account configured
- User secrets or environment variables set with Cosmos DB credentials

## Configuration

The application uses the following configuration keys (via user secrets or environment variables):

- `COSMOSDB_ENDPOINT_URI` - Cosmos DB endpoint URI
- `COSMOSDB_PRIMARY_KEY` - Cosmos DB primary key
- `COSMOSDB_DATABASE_ID` - Cosmos DB database ID

## Data Format

Testimonial data is stored in `data/testimonials.json` with the following structure:

```json
[
  {
    "AuthorName": "Sarah Mitchell",
    "AuthorTitle": "Mystery Novelist",
    "Quote": "This platform transformed how I connect with my readers...",
    "Rating": 5,
    "PhotoUrl": null,
    "Featured": true,
    "Locale": "en-US"
  }
]
```

## Running the Seeder

```bash
cd SeedTestimonials
dotnet run
```

## Idempotency

The seeder generates deterministic IDs based on the author name and locale, making it safe to run multiple times. Existing testimonials will be skipped.

## Features

- Multi-language support (en-US, es-ES, fr-FR)
- Featured testimonial flagging
- Rating system (1-5 stars)
- Idempotent seeding (safe to run multiple times)
