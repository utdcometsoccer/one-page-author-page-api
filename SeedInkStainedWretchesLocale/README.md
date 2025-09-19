# SeedInkStainedWretchesLocale

Console tool to seed Cosmos DB containers with localized UI content from JSON files named `inkstainedwretch.<language>-<country>.json`.

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