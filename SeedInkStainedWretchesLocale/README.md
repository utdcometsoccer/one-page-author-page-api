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

## ⚙️ Configuration

### Required Settings

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `EndpointUri` | Cosmos DB account endpoint | Azure Portal → Cosmos DB → Keys → URI | Establishes database connection for seeding localization data |
| `PrimaryKey` | Cosmos DB primary access key | Azure Portal → Cosmos DB → Keys → Primary Key | Authenticates write operations to create localization records |
| `DatabaseId` | Database name | Your database name (e.g., "OnePageAuthorDb") | Identifies target database for localization containers |

### Setting Up User Secrets (Recommended)

```bash
cd SeedInkStainedWretchesLocale
dotnet user-secrets init

# Set required configuration
dotnet user-secrets set "EndpointUri" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "PrimaryKey" "your-cosmos-primary-key"
dotnet user-secrets set "DatabaseId" "OnePageAuthorDb"

# Verify configuration
dotnet user-secrets list
```

### How to Obtain Configuration Values

1. **EndpointUri**:
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to your Cosmos DB account
   - Click "Keys" in the left sidebar
   - Copy the "URI" value (format: `https://your-account.documents.azure.com:443/`)

2. **PrimaryKey**:
   - In the same "Keys" section
   - Copy the "Primary Key" value
   - ⚠️ Keep this secret and never commit to source control

3. **DatabaseId**:
   - This is your database name (e.g., "OnePageAuthorDb")
   - Found in Cosmos DB → Data Explorer → Database name

### Alternative: Environment Variables

```bash
export EndpointUri="https://your-account.documents.azure.com:443/"
export PrimaryKey="your-cosmos-primary-key"
export DatabaseId="OnePageAuthorDb"
```

## Data Files

Place JSON files in `data/` with this pattern:

- **Standard format**: `inkstainedwretch.<language>-<country>.json` (e.g., `inkstainedwretch.en-us.json`)
- **Extended format**: `inkstainedwretch.<language-variant>-<country>.json` (e.g., `inkstainedwretch.zh-cn-us.json`, `inkstainedwretch.zh-tw-ca.json`)

Each top-level property name should match a POCO/container name (e.g., `Navbar`, `ThankYou`, `ArticleList`). Values can be an object or array.

### Nested JSON Support

The seeder supports nested JSON structures that are automatically flattened to match entity properties:

```json
{
  "Navbar": {
    "brand": "Ink Stained Wretches",
    "brandAriaLabel": "Navigate to home page",
    "navItems": {
      "login": {
        "label": "Login",
        "description": "Sign In / Sign Up",
        "ariaLabel": "Sign in or create an account"
      }
    }
  }
}
```

This nested structure is flattened to match entity properties like `navItems_login_label`, `navItems_login_description`, and `navItems_login_ariaLabel`.

### North America Locales Included

The seeder includes comprehensive localization for all North American countries:

- **United States (US)**: en-us, es-us, fr-us, ar-us, zh-cn-us, zh-tw-us
- **Canada (CA)**: en-ca, fr-ca, es-ca, ar-ca, zh-cn-ca, zh-tw-ca
- **Mexico (MX)**: es-mx, en-mx, fr-mx, ar-mx, zh-cn-mx, zh-tw-mx

## Recent Updates

### Accessibility Enhancements (December 2024)

All locale files now include comprehensive ARIA labels for improved accessibility:

- **Navbar.brandAriaLabel**: Screen reader label for the brand/logo link
- **navItems.*.ariaLabel**: Descriptive labels for each navigation item

These accessibility labels are properly translated across all 20 supported locales (EN, ES, FR, AR, ZH-CN, ZH-TW for US, CA, MX, and EG).

## Notes

- Container managers and repository are provided by `OnePageAuthorLib` via `.AddInkStainedWretchServices()`.
- Partition key convention is usually `/Culture` for localization containers.
- The seeder uses reflection and dynamic typing to handle various POCO types and supports both flat and nested JSON structures.
- Nested JSON objects are automatically flattened using underscore-separated property names (e.g., `navItems_login_ariaLabel`).
