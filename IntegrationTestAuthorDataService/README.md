# IntegrationTestAuthorDataService

Integration tests for the AuthorDataService and Azure Cosmos DB integration.

## Quickstart
```pwsh
dotnet build IntegrationTestAuthorDataService.csproj
dotnet test IntegrationTestAuthorDataService.csproj
```

## Notes
- Ensure emulator/connection settings are configured for tests that hit Cosmos DB.
