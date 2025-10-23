# SeedInkStainedWretchesLocale

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)

Comprehensive, idempotent console application for seeding all InkStainedWretch localization data from JSON files into Cosmos DB containers.

## Overview
- **Comprehensive**: Seeds data for ALL containers in the LocalizationText model (25+ containers)
- **Idempotent**: Safe to run multiple times - automatically detects and skips existing data
- **Multi-language**: Supports North American countries (US, CA, MX) in 6 languages: English, Spanish, French, Arabic, Simplified Chinese, Traditional Chinese
- **Flexible**: Supports both standard locale codes (en-us) and extended codes (zh-cn-us, zh-tw-us)
- Discovers files under `data/` at runtime (copied to output by the project).
- For each file, infers culture (e.g., `en-us`, `zh-cn-us`) from the filename and writes records into the corresponding containers.
- Uses reflection to construct POCOs under `InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement` and inserts via a generic repository.

## Quickstart
```pwsh
dotnet build SeedInkStainedWretchesLocale.csproj
dotnet run --project SeedInkStainedWretchesLocale.csproj
```

## Configuration
Read from user secrets or environment variables (see Program.cs):
- EndpointUri
- PrimaryKey
- DatabaseId

Example user-secrets (dotnet user-secrets set):
- EndpointUri = https://<account>.documents.azure.com:443/
- PrimaryKey = <secret>
- DatabaseId = <db-name>

## Data Files
Place JSON files in `data/` with this pattern:
- **Standard format**: `inkstainedwretch.<language>-<country>.json` (e.g., `inkstainedwretch.en-us.json`)
- **Extended format**: `inkstainedwretch.<language-variant>-<country>.json` (e.g., `inkstainedwretch.zh-cn-us.json`, `inkstainedwretch.zh-tw-ca.json`)

Each top-level property name should match a POCO/container name (e.g., `Navbar`, `ThankYou`, `ArticleList`). Values can be an object or array.

### North America Locales Included
The seeder includes comprehensive localization for all North American countries:
- **United States (US)**: en-us, es-us, fr-us, ar-us, zh-cn-us, zh-tw-us
- **Canada (CA)**: en-ca, fr-ca, es-ca, ar-ca, zh-cn-ca, zh-tw-ca
- **Mexico (MX)**: es-mx, en-mx, fr-mx, ar-mx, zh-cn-mx, zh-tw-mx

## Notes
- Container managers and repository are provided by `OnePageAuthorLib` via `.AddInkStainedWretchServices()`.
- Partition key convention is usually `/Culture` for localization containers.