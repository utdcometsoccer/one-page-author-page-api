# One Page Author API

This repository contains the source code and supporting files for the One Page Author API solution. It provides backend services for managing author, book, article, and social data, with integration to Azure Cosmos DB.

## Projects
- `OnePageAuthorLib`: Core library containing entities, repositories, and services for author-related data.
- `SeedAPIData`: Utility project for seeding the database with initial data.
- `IntegrationTestAuthorDataService`: Project for integration testing the AuthorDataService and Cosmos DB integration.

## Build Instructions

1. Ensure you have [.NET 9 SDK](https://dotnet.microsoft.com/download) installed.
2. Open a terminal in the root directory.
3. Run:
   ```pwsh
   dotnet build OnePageAuthorAPI.sln
   ```

## Usage
- See individual project folders for more details and usage instructions.

## License
MIT
