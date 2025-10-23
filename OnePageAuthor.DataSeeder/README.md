# OnePageAuthor Data Seeder

Console application for seeding StateProvince data into Azure Cosmos DB with comprehensive geographical data for North American countries.

## Overview

This application provides an **idempotent** data seeding operation that can be run multiple times safely without creating duplicate entries. It seeds state and province data for all North American countries (United States, Canada, and Mexico) in six languages:

- **English** (en-US, en-CA, en-MX)
- **French** (fr-US, fr-CA, fr-MX)
- **Spanish** (es-US, es-CA, es-MX)
- **Arabic** (ar-US, ar-CA, ar-MX)
- **Simplified Chinese** (zh-CN)
- **Traditional Chinese** (zh-TW)

## Geographical Coverage

- **United States**: All 50 states, DC, and territories (54 locations)
- **Canada**: All provinces and territories (13 locations)
- **Mexico**: All 32 states

Each location is provided in all six supported languages, resulting in **594 total StateProvince records**:
- US: 54 locations × 6 languages = 324 records
- Canada: 13 locations × 6 languages = 78 records
- Mexico: 32 locations × 6 languages = 192 records

## Key Features

### Idempotent Operation
The seeder implements idempotent behavior, meaning it can be run multiple times safely:
- Checks for existing entries before attempting to create them
- Skips entries that already exist in the database
- Only creates new entries that don't exist
- Provides detailed logging of created, skipped, and error counts
- No data deletion occurs - existing data is preserved

### Azure Cosmos DB Integration
The seeder integrates with the existing OnePageAuthorLib infrastructure:
- Uses established dependency injection patterns
- Leverages existing StateProvince services and repositories
- Respects partition key strategy (`/Culture`)
- Follows established error handling and logging patterns
- Enhanced data model with separate Country field for efficient querying

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

**Expected Output (First Run):**
```
Starting StateProvince Data Seeding...
Cosmos DB Endpoint configured: https://***:8081
Cosmos DB Database ID configured: OnePageAuthorDB
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Starting idempotent StateProvince data seeding...
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Preparing to seed 594 StateProvince entries...
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Idempotent data seeding completed. Created: 594, Skipped: 0, Errors: 0
Data seeding completed successfully!
```

**Expected Output (Subsequent Runs - Idempotent):**
```
Starting StateProvince Data Seeding...
Cosmos DB Endpoint configured: https://***:8081
Cosmos DB Database ID configured: OnePageAuthorDB
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Starting idempotent StateProvince data seeding...
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Preparing to seed 594 StateProvince entries...
info: OnePageAuthorAPI.DataSeeder.StateProvinceSeeder[0]
      Idempotent data seeding completed. Created: 0, Skipped: 594, Errors: 0
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
// United States Examples (All 6 Languages)
{ "Code": "CA", "Name": "California", "Country": "US", "Culture": "en-US" }
{ "Code": "CA", "Name": "Californie", "Country": "US", "Culture": "fr-US" }
{ "Code": "CA", "Name": "California", "Country": "US", "Culture": "es-US" }
{ "Code": "CA", "Name": "كاليفورنيا", "Country": "US", "Culture": "ar-US" }
{ "Code": "CA", "Name": "加利福尼亚州", "Country": "US", "Culture": "zh-CN" }
{ "Code": "CA", "Name": "加利福尼亞州", "Country": "US", "Culture": "zh-TW" }

// Canada Examples (All 6 Languages)
{ "Code": "QC", "Name": "Quebec", "Country": "CA", "Culture": "en-CA" }
{ "Code": "QC", "Name": "Québec", "Country": "CA", "Culture": "fr-CA" }
{ "Code": "QC", "Name": "Quebec", "Country": "CA", "Culture": "es-CA" }
{ "Code": "QC", "Name": "كيبك", "Country": "CA", "Culture": "ar-CA" }
{ "Code": "QC", "Name": "魁北克省", "Country": "CA", "Culture": "zh-CN" }
{ "Code": "QC", "Name": "魁北克省", "Country": "CA", "Culture": "zh-TW" }

// Mexico Examples (All 6 Languages)
{ "Code": "CMX", "Name": "Mexico City", "Country": "MX", "Culture": "en-MX" }
{ "Code": "CMX", "Name": "Mexico", "Country": "MX", "Culture": "fr-MX" }
{ "Code": "CMX", "Name": "Ciudad de México", "Country": "MX", "Culture": "es-MX" }
{ "Code": "CMX", "Name": "مدينة مكسيكو", "Country": "MX", "Culture": "ar-MX" }
{ "Code": "CMX", "Name": "墨西哥城", "Country": "MX", "Culture": "zh-CN" }
{ "Code": "CMX", "Name": "墨西哥城", "Country": "MX", "Culture": "zh-TW" }
```

## Total Records
- **US States**: 54 locations × 6 languages = 324 records
- **Canadian Provinces**: 13 locations × 6 languages = 78 records  
- **Mexican States**: 32 locations × 6 languages = 192 records
- **Total**: 594 StateProvince records

## Features

- **Idempotent Operations**: Safe to run multiple times without creating duplicates
- **Comprehensive Logging**: Detailed logging of success/skip/error operations
- **Error Handling**: Continues processing even if individual records fail
- **Multi-language Support**: Proper culture-specific naming conventions in 6 languages
- **Data Preservation**: Existing entries are not deleted or modified