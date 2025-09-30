# OnePageAuthor Data Seeder

Console application for seeding StateProvince data into Azure Cosmos DB with comprehensive geographical data### Azure Cosmos DB Integration

The seeder integrates with the existing OnePageAuthorLib infrastructure:
- Uses established dependency injection patterns
- Leverages existing StateProvince services and repositories
- Respects partition key strategy (`/Culture`)
- Follows established error handling and logging patterns
- Enhanced data model with separate Country field for efficient querying
- **United States**: All 50 states, DC, and territories
- **Canada**: All provinces and territories  
- **Mexico**: All 32 states
- **China**: All provinces, autonomous regions, and special administrative regions
- **Taiwan**: All counties and cities
- **Egypt**: All governorates

Each location is provided in multiple languages:
- **English** (en-US, en-CA, en-MX, en-CN, en-TW, en-EG)
- **French** (fr-US, fr-CA, fr-MX)  
- **Spanish** (es-US, es-MX)
- **Simplified Chinese** (zh-CN)
- **Traditional Chinese** (zh-TW)
- **Arabic** (ar-EG)

## Configuration

### User Secrets (Recommended for Development)
The application uses user secrets for secure configuration storage in development:

```powershell
# Set up user secrets (already configured)
dotnet user-secrets set "CosmosDb:Endpoint" "https://localhost:8081" --project OnePageAuthor.DataSeeder
dotnet user-secrets set "CosmosDb:Key" "your-cosmos-key" --project OnePageAuthor.DataSeeder  
dotnet user-secrets set "CosmosDb:Database" "OnePageAuthorDB" --project OnePageAuthor.DataSeeder
```

### Environment Variables (Alternative)
Set these environment variables as an alternative to user secrets:

```bash
COSMOS_DB_ENDPOINT=https://your-cosmos-account.documents.azure.com:443/
COSMOS_DB_KEY=your-cosmos-primary-key
COSMOS_DB_DATABASE=OnePageAuthorDB
```

### Configuration Priority
1. User secrets (development environment)
2. Environment variables
3. appsettings.json defaults (Cosmos DB Emulator)

## Usage

### Prerequisites
1. .NET 9.0 SDK installed
2. **Azure Cosmos DB Emulator** (for local development) OR Azure Cosmos DB account

### Step 1: Start Cosmos DB Emulator (Local Development)

**Option A: Using the provided script**
```powershell
# Run the PowerShell script (recommended)
.\OnePageAuthor.DataSeeder\start-cosmos-emulator.ps1

# OR run the batch file
.\OnePageAuthor.DataSeeder\start-cosmos-emulator.cmd
```

**Option B: Manual startup**
1. Install [Azure Cosmos DB Emulator](https://aka.ms/cosmosdb-emulator)
2. Start the emulator (it will be available at https://localhost:8081)
3. Wait for it to fully start (1-2 minutes on first run)

### Step 2: Run the Data Seeder

```powershell
# Navigate to the solution root
cd c:\path\to\one-page-author-page-api

# Restore dependencies (if not already done)
dotnet restore

# Run the seeder
dotnet run --project OnePageAuthor.DataSeeder
```

**Expected Output:**
```
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Starting StateProvince data seeding...
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Seeding 450 StateProvince entries...
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Data seeding completed. Success: 450, Errors: 0
Data seeding completed successfully!
```

### Production Deployment

For production environments, set the environment variables:

```bash
# Set environment variables
export COSMOS_DB_ENDPOINT="https://your-account.documents.azure.com:443/"
export COSMOS_DB_KEY="your-primary-key"
export COSMOS_DB_DATABASE="OnePageAuthorDB"

# Run the seeder
dotnet run
```

## Data Structure

Each StateProvince entry contains:
- **Code**: State/Province code without country prefix (e.g., "CA", "ON", "JAL")
- **Name**: Localized name in the appropriate language
- **Country**: Two-letter country code (e.g., "US", "CA", "MX")
- **Culture**: Culture code (e.g., "en-US", "fr-CA", "es-MX")
- **id**: Unique identifier combining code and culture

### Sample Data Examples
```json
// United States Examples
{ "Code": "CA", "Name": "California", "Country": "US", "Culture": "en-US" }
{ "Code": "CA", "Name": "Californie", "Country": "US", "Culture": "fr-US" }
{ "Code": "CA", "Name": "California", "Country": "US", "Culture": "es-US" }

// Canada Examples  
{ "Code": "QC", "Name": "Quebec", "Country": "CA", "Culture": "en-CA" }
{ "Code": "QC", "Name": "Québec", "Country": "CA", "Culture": "fr-CA" }

// Mexico Examples
{ "Code": "CMX", "Name": "Ciudad de México", "Country": "MX", "Culture": "es-MX" }
{ "Code": "CMX", "Name": "Mexico City", "Country": "MX", "Culture": "en-MX" }
{ "Code": "CMX", "Name": "Mexico", "Country": "MX", "Culture": "fr-MX" }

// China Examples
{ "Code": "BJ", "Name": "北京市", "Country": "CN", "Culture": "zh-CN" }
{ "Code": "BJ", "Name": "Beijing", "Country": "CN", "Culture": "en-CN" }

// Taiwan Examples
{ "Code": "TPE", "Name": "臺北市", "Country": "TW", "Culture": "zh-TW" }
{ "Code": "TPE", "Name": "Taipei City", "Country": "TW", "Culture": "en-TW" }

// Egypt Examples
{ "Code": "CAI", "Name": "القاهرة", "Country": "EG", "Culture": "ar-EG" }
{ "Code": "CAI", "Name": "Cairo", "Country": "EG", "Culture": "en-EG" }
```

## Total Records
- **US States**: 54 locations × 3 languages = 162 records
- **Canadian Provinces**: 13 locations × 2 languages = 26 records  
- **Mexican States**: 32 locations × 3 languages = 96 records
- **Chinese Provinces**: 33 locations × 2 languages = 66 records
- **Taiwan Regions**: 22 locations × 2 languages = 44 records
- **Egyptian Governorates**: 28 locations × 2 languages = 56 records
- **Total**: 450 StateProvince records

## Features

- **Duplicate Prevention**: Checks for existing entries before insertion
- **Comprehensive Logging**: Detailed logging of success/error operations
- **Error Handling**: Continues processing even if individual records fail
- **Batch Processing**: Efficiently processes all records with progress tracking
- **Multi-language Support**: Proper culture-specific naming conventions

## Azure Cosmos DB Integration

The seeder integrates with the existing OnePageAuthorLib infrastructure:
- Uses established dependency injection patterns
- Leverages existing StateProvince services and repositories
- Respects partition key strategy (`/Culture`)
- Follows established error handling and logging patterns