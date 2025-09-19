# InkStainedWretchFunctions

Azure Functions (Isolated Worker, .NET 9) exposing localized UI text aggregated from Cosmos DB.

## Overview
Provides a single endpoint to retrieve all localized fragments for a given culture with robust fallback.

- GET /api/localizedtext/{culture}
  - Returns a JSON object aggregating multiple localization containers (e.g., Navbar, ThankYou, etc.).
  - Fallback order: exact culture -> first language-prefixed variant -> neutral language -> empty object.

## Quickstart
```pwsh
dotnet build InkStainedWretchFunctions.csproj
func start
```

## Configuration
Environment or local.settings.json values consumed in Program.cs:
- COSMOSDB_ENDPOINT_URI
- COSMOSDB_PRIMARY_KEY
- COSMOSDB_DATABASE_ID

Example local.settings.json:
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "COSMOSDB_ENDPOINT_URI": "https://<account>.documents.azure.com:443/",
    "COSMOSDB_PRIMARY_KEY": "<secret>",
    "COSMOSDB_DATABASE_ID": "<db-name>"
  }
}

## Deployment
- Deploy as an Azure Functions app (v4, dotnet-isolated). Configure Cosmos settings as app settings.

## Notes
- Services are provided via `OnePageAuthorLib` and wired through `.AddInkStainedWretchServices()`.
- See `LocalizationREADME.md` at the repo root for data and fallback details.