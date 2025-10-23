# SeedLocalizationData

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)

Console application for seeding InkStainedWretch-specific localization data from JSON files into remote Cosmos DB containers.

## Overview
This application seeds localization data for specific InkStainedWretch author management containers to a remote Cosmos DB instance. It:
- Discovers `inkstainedwretch.<language>-<country>.json` files under `data/` at runtime (copied to output by the project)
- For each file, infers culture (e.g., `en-us`) from the filename
- Seeds records into the following containers, forcing initialization if they don't exist:
  - ArticleList
  - AuthorDocList
  - AuthorMainForm
  - ChooseCulture
  - ChooseSubscription
  - CountdownIndicator
  - DomainInput
  - DomainRegistrationsList
  - OpenLibraryAuthorForm
  - SocialForm
  - SocialList

## Key Features
- **Remote Cosmos DB Support**: Seeds to a remote Cosmos DB instance using user secrets for connection
- **Container Initialization**: Forces creation of containers if they don't exist
- **Duplicate Detection**: Checks for existing data to avoid duplicates
- **Selective Processing**: Only processes containers specified in the target list
- **Culture-based Data**: All records are tagged with the appropriate culture code

## Quickstart
```pwsh
# Option 1: Copy secrets from SeedInkStainedWretchesLocale (if already configured)
.\Copy-LocalizationSeederSecrets.ps1

# Option 2: Configure secrets manually (if starting fresh)
cd SeedLocalizationData
dotnet user-secrets set "EndpointUri" "https://<your-account>.documents.azure.com:443/"
dotnet user-secrets set "PrimaryKey" "<your-primary-key>"
dotnet user-secrets set "DatabaseId" "<your-database-id>"
cd ..

# Build and run
dotnet build SeedLocalizationData/SeedLocalizationData.csproj
dotnet run --project SeedLocalizationData/SeedLocalizationData.csproj
```

## Configuration
Read from user secrets (recommended) or environment variables:
- `EndpointUri`: The Cosmos DB account endpoint URI
- `PrimaryKey`: The Cosmos DB primary key
- `DatabaseId`: The Cosmos DB database ID

### Option 1: Copy secrets from SeedInkStainedWretchesLocale (Recommended)
If you already have secrets configured in `SeedInkStainedWretchesLocale`, use the PowerShell script to copy them:
```pwsh
# Preview what would be copied
.\Copy-LocalizationSeederSecrets.ps1 -WhatIf

# Copy the secrets
.\Copy-LocalizationSeederSecrets.ps1
```

### Option 2: Configure secrets manually
```pwsh
cd SeedLocalizationData
dotnet user-secrets set "EndpointUri" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "PrimaryKey" "your-primary-key-here"
dotnet user-secrets set "DatabaseId" "your-database-name"
```

## Data Files
Place JSON files in `data/` with this pattern:
- `inkstainedwretch.<language>-<country>.json` (e.g., `inkstainedwretch.en-us.json`)

Each top-level property name should match a POCO/container name from the target list. Values can be an object or array.

Example structure:
```json
{
    "ArticleList": {
        "title": "Articles",
        "date": "Date:",
        "publication": "Publication:",
        "url": "URL:",
        "edit": "Edit",
        "delete": "Delete",
        "addArticle": "Add Article"
    },
    "ChooseCulture": {
        "title": "Choose Your Language and Region",
        "subtitle": "Select your preferred language...",
        "legend": "Select Language and Country"
    }
}
```

## Pattern
This application follows the same pattern as `SeedInkStainedWretchesLocale`:
1. Reads JSON files from the `data/` directory
2. Parses culture information from filenames
3. Uses reflection to dynamically load POCO types
4. Registers container managers via `AddInkStainedWretchServices()`
5. Ensures containers exist before seeding
6. Checks for existing data to avoid duplicates
7. Seeds data using the generic repository pattern

## Notes
- Container managers and repository are provided by `OnePageAuthorLib` via `.AddInkStainedWretchServices()`
- Partition key convention is `/Culture` for localization containers
- All POCO types are under the `InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement` namespace
- The application will skip containers not in the target list
- Duplicate entries are detected and skipped to allow re-running the seeder safely
