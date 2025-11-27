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

## ⚙️ Configuration

### Required Settings

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB account endpoint | Azure Portal → Cosmos DB → Keys → URI | Establishes database connection for seeding language data |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary access key | Azure Portal → Cosmos DB → Keys → Primary Key | Authenticates write operations to create language records |
| `COSMOSDB_DATABASE_ID` | Database name | Your database name (e.g., "OnePageAuthorDb") | Identifies target database for the Languages container |

### Setting Up User Secrets (Recommended)

```bash
cd SeedLanguages
dotnet user-secrets init

# Set required configuration
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-cosmos-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthorDb"

# Verify configuration
dotnet user-secrets list
```

### How to Obtain Configuration Values

1. **COSMOSDB_ENDPOINT_URI**:
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to your Cosmos DB account
   - Click "Keys" in the left sidebar
   - Copy the "URI" value

2. **COSMOSDB_PRIMARY_KEY**:
   - In the same "Keys" section
   - Copy the "Primary Key" value
   - ⚠️ Keep this secret and never commit to source control

3. **COSMOSDB_DATABASE_ID**:
   - This is your database name (e.g., "OnePageAuthorDb")
   - Found in Cosmos DB → Data Explorer → Database name

### Alternative: Environment Variables

```bash
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-cosmos-primary-key"
export COSMOSDB_DATABASE_ID="OnePageAuthorDb"
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
