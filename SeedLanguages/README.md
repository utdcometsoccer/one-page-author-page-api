# SeedLanguages Console Application

This console application seeds language data into the Cosmos DB Languages container. It is designed to be **idempotent**, meaning it can be run multiple times without creating duplicate entries.

## Purpose

Seeds language names in multiple languages to support the GetLanguages API endpoint, which returns language names localized for a specific request language.

## Supported Languages

The seeder supports the following languages as specified in the issue:

- **English (en)**: English
- **Spanish (es)**: Español  
- **French (fr)**: Français
- **Arabic (ar)**: العربية
- **Chinese Simplified (zh-cn)**: 中文（简体）- Mainland China
- **Chinese Traditional (zh-tw)**: 中文（繁體）- Taiwan

## Data Structure

Each language data file (`languages-{language}.json`) contains an array of language entries with:

- `Code`: ISO 639-1 two-letter language code (e.g., "en", "es", "fr")
- `Name`: Localized name of the language
- `RequestLanguage`: The language in which the name is provided (partition key)

## Configuration

The application requires the following configuration settings:

- `COSMOSDB_ENDPOINT_URI`: Cosmos DB account endpoint
- `COSMOSDB_PRIMARY_KEY`: Cosmos DB primary key
- `COSMOSDB_DATABASE_ID`: Cosmos DB database name

These can be provided via:
1. User Secrets (recommended for development)
2. Environment variables

### Setting up User Secrets

```bash
dotnet user-secrets init --project SeedLanguages
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "your-endpoint" --project SeedLanguages
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-key" --project SeedLanguages
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "your-database" --project SeedLanguages
```

## Running the Application

```bash
cd SeedLanguages
dotnet run
```

## Idempotency

The seeder checks if each language entry already exists before inserting it. If a language with the same `Code` and `RequestLanguage` already exists, it will be skipped. This ensures the seeder can be safely run multiple times.

## Container Setup

The application automatically creates the `Languages` container if it doesn't exist, with `/RequestLanguage` as the partition key.
