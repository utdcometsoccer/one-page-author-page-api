# InkStainedWretchFunctions

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)

Azure Functions application providing domain registration management, external API integrations, and localized UI text services.

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