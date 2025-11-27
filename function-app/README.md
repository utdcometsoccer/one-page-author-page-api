# function-app

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)

Main Azure Functions application providing core author data services and localization support for the OnePageAuthor platform.

## 🚀 Overview

The function-app is the primary Azure Functions application that provides:

- **Author Data Services**: Core author information retrieval and management
- **Localization Support**: Multi-language content delivery
- **Cosmos DB Integration**: Direct database access for author and locale data
- **RESTful API**: HTTP-triggered functions for client applications

## 🔌 API Endpoints

| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| `GET` | `/api/authors/{authorId}` | Get author information | Optional |
| `GET` | `/api/locales/{culture}` | Get localized text | Anonymous |

## 🏗️ Architecture

- **Runtime**: Azure Functions v4 (.NET 10)
- **Database**: Azure Cosmos DB
- **Authentication**: JWT Bearer tokens (where required)
- **Deployment**: Automated via GitHub Actions
- Application Insights telemetry

## Quickstart


```pwsh
dotnet build function-app.csproj
func start

```

## Configuration

See `Program.cs` for required settings and bindings.
