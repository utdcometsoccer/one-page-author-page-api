# One Page Author API

This repository contains the source code and supporting files for the One Page Author API solution. It provides backend services for managing author, book, article, and social data, with integration to Azure Cosmos DB.

## Project index
- OnePageAuthorLib — Core library of entities, repositories, and services
   - ./OnePageAuthorLib/README.md
- InkStainedWretchFunctions — Functions app exposing localized text API
   - ./InkStainedWretchFunctions/README.md
- InkStainedWretchStripe — Functions app integrating with Stripe
   - ./InkStainedWretchStripe/README.md
- function-app — Additional Functions host (infrastructure/experiments)
   - ./function-app/README.md
- SeedInkStainedWretchesLocale — Console seeder for localized UI data
   - ./SeedInkStainedWretchesLocale/README.md
- SeedLocales — Console seeder for locale data
   - ./SeedLocales/README.md
- SeedAPIData — Console seeder for core API data
   - ./SeedAPIData/README.md
- IntegrationTestAuthorDataService — Integration test project
   - ./IntegrationTestAuthorDataService/README.md
- OnePageAuthor.Test — Unit test project
   - ./OnePageAuthor.Test/README.md

## Build

1. Ensure you have [.NET 9 SDK](https://dotnet.microsoft.com/download) installed.
2. Open a terminal in the root directory.
3. Build the full solution:
    ```pwsh
    dotnet build OnePageAuthorAPI.sln
    ```

## Quickstart
- Most project-specific run/test instructions are in their respective READMEs (see index above).

## License
MIT
