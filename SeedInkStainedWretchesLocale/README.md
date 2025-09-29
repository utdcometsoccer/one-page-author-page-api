# SeedInkStainedWretchesLocale

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)

Specialized console application for seeding InkStainedWretch-specific localization data from JSON files into Cosmos DB containers.

## Overview
- Discovers files under `data/` at runtime (copied to output by the project).
- For each file, infers culture (e.g., `en-GB`) from the filename and writes records into the corresponding containers.
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
- inkstainedwretch.<language>-<country>.json (e.g., inkstainedwretch.en-gb.json)

Each top-level property name should match a POCO/container name (e.g., `Navbar`, `ThankYou`). Values can be an object or array.

## Notes
- Container managers and repository are provided by `OnePageAuthorLib` via `.AddInkStainedWretchServices()`.
- Partition key convention is usually `/Culture` for localization containers.