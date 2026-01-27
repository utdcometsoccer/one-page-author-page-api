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

## ⚙️ Configuration

### Environment Variables

| Variable | Required | Description | Where to Find |
|----------|----------|-------------|---------------|
| `COSMOSDB_ENDPOINT_URI` | ✅ Yes | Azure Cosmos DB endpoint URL | Azure Portal → Cosmos DB → Keys → URI |
| `COSMOSDB_PRIMARY_KEY` | ✅ Yes | Cosmos DB primary access key | Azure Portal → Cosmos DB → Keys → Primary Key |
| `COSMOSDB_DATABASE_ID` | ✅ Yes | Database name | Your database name (e.g., "OnePageAuthorDb") |
| `AAD_TENANT_ID` | ⚪ Optional | Azure AD tenant ID | Azure Portal → Microsoft Entra ID → Overview → Tenant ID |
| `AAD_AUDIENCE` | ⚪ Optional | Azure AD client ID | Azure Portal → Microsoft Entra ID → App registrations → Your App |

### Why These Settings Are Needed

<details>
<summary>🗄️ Cosmos DB Configuration</summary>

**Purpose**: The function-app uses Cosmos DB to store and retrieve author profiles, books, articles, and localized content.

| Variable | Why It's Needed |
|----------|-----------------|
| `COSMOSDB_ENDPOINT_URI` | Establishes connection to your Cosmos DB account for author data retrieval |
| `COSMOSDB_PRIMARY_KEY` | Authenticates database operations - required for reading author and locale data |
| `COSMOSDB_DATABASE_ID` | Identifies which database contains the Authors, Articles, Books, and Locales containers |

</details>

<details>
<summary>🔐 Azure AD Authentication</summary>

**Purpose**: Optional JWT validation for protected endpoints.

| Variable | Why It's Needed |
|----------|-----------------|
| `AAD_TENANT_ID` | Validates that JWT tokens are issued by your Azure AD tenant |
| `AAD_AUDIENCE` | Ensures tokens are intended for your API application |

</details>

### Setting Up User Secrets (Development)

```bash
cd function-app
dotnet user-secrets init

# Required settings
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-cosmos-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthorDb"

# Optional authentication settings
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"
dotnet user-secrets set "AAD_AUDIENCE" "your-client-id"

# Verify
dotnet user-secrets list
```

### Production Deployment

Configure settings in Azure Portal → Function App → Configuration → Application Settings

See `Program.cs` for required settings and bindings.
