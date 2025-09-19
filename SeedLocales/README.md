# SeedLocales

## Build Status
![Build Status](https://img.shields.io/badge/build-passing-brightgreen)

Console tool to seed locale data for the One Page Author API.

## Overview
- Seeds locale data to Azure Cosmos DB (or configured data source)
- .NET 9 console application

## Quickstart
```pwsh
dotnet build SeedLocales.csproj
dotnet run --project SeedLocales.csproj
```

## Configuration
- Provide required environment variables or app settings as expected by the code (see Program.cs if present).
