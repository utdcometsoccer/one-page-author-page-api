# SeedCountries Console Application

This console application seeds country data to Cosmos DB for the InkStainedWretch API.

## Purpose

Seeds country name data in multiple languages to support the `GetCountriesByLanguage` API endpoint.

## Supported Languages

- English (`en`)
- Spanish (`es`)
- French (`fr`)
- Arabic (`ar`)
- Simplified Chinese (`zh-cn`)
- Traditional Chinese - Taiwan (`zh-tw`)

## Features

- **Idempotent**: Can be run multiple times without creating duplicates
- **Language Support**: Seeds country names in all supported languages
- **ISO Standards**: Uses ISO 3166-1 alpha-2 country codes
- **Comprehensive Coverage**: Includes major countries from all continents

## ⚙️ Configuration

### Required Settings

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB account endpoint | Azure Portal → Cosmos DB → Keys → URI | Establishes database connection for seeding |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary access key | Azure Portal → Cosmos DB → Keys → Primary Key | Authenticates write operations |
| `COSMOSDB_DATABASE_ID` | Database name | Your database name (e.g., "OnePageAuthorDb") | Identifies target database for country data |

### Setting Up User Secrets (Recommended)

```bash
cd SeedCountries
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

## Running the Seeder

```bash
cd SeedCountries
dotnet run

```

Or run from the solution directory:

```bash
dotnet run --project SeedCountries

```

## Data Files

Country data is stored in JSON files in the `data` directory:

- `countries-en.json` - English country names
- `countries-es.json` - Spanish country names
- `countries-fr.json` - French country names
- `countries-ar.json` - Arabic country names
- `countries-zh-cn.json` - Simplified Chinese country names
- `countries-zh-tw.json` - Traditional Chinese country names

### Data Format

Each JSON file contains an array of country objects:

```json
[
  { "code": "US", "name": "United States" },
  { "code": "CA", "name": "Canada" }
]

```

## Cosmos DB Structure

Countries are stored in the `Countries` container with:

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

## Exit Codes

- `0` - Success
- `1` - Error occurred during seeding

## Output

The application provides detailed console output:

- Files being processed
- Languages detected
- Countries created/skipped
- Summary statistics
- Any errors encountered

## Idempotent Behavior

The seeder checks if each country already exists before creating it. This allows you to:

- Re-run the seeder safely after failures
- Add new languages without affecting existing data
- Update the seeder and re-run without duplicates

## Adding New Languages

To add a new language:

1. Create a new JSON file: `data/countries-{language-code}.json`
2. Add country data in the target language
3. Run the seeder - it will automatically detect and process the new file

## Maintenance

To update country data:

1. Modify the JSON files in the `data` directory
2. Run the seeder to add new countries
3. Existing countries will be skipped automatically
