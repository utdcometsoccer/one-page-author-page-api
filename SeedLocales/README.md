# SeedLocales

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)

Console application for seeding localization data into the OnePageAuthor system to support multi-language functionality.

## 🚀 Overview

SeedLocales initializes the OnePageAuthor system with comprehensive localization data to support multiple languages and cultures:

- **Multi-Language Support**: Seeds text for English, Spanish, French, German, and more
- **UI Components**: Navigation, forms, buttons, and interface elements
- **Content Localization**: Article titles, descriptions, and metadata
- **Cultural Adaptation**: Date formats, number formats, and regional preferences
- **Fallback Logic**: Ensures graceful degradation when translations are missing

## 🌍 Supported Languages

The seeder includes localization data for:

- **English (en-US)**: Primary language and fallback
- **Spanish (es-ES)**: Full UI and content translations
- **French (fr-FR)**: Complete localization package
- **German (de-DE)**: UI and navigation translations
- **Portuguese (pt-BR)**: Brazilian Portuguese support
- **Italian (it-IT)**: Italian language pack

## 🏗️ Architecture

- **Runtime**: .NET 9 Console Application
- **Database**: Azure Cosmos DB
- **Format**: JSON-based locale files
- **Dependencies**: OnePageAuthorLib for data persistence

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Azure Cosmos DB access
- Localization JSON files (included)

### Running the Seeder
```bash
# Navigate to project directory
cd SeedLocales

# Restore dependencies  
dotnet restore

# Build the project
dotnet build

# Run the seeder
dotnet run
```

### Configuration
Configure your connection using user secrets:
```bash
# Set database connection
dotnet user-secrets set "CosmosDbConnectionString" "your-cosmos-connection-string"

# Set locale container configuration
dotnet user-secrets set "LocaleContainerId" "locales"
```

## 📋 Locale Structure

Each locale file contains structured translations:

```json
{
  "culture": "en-US",
  "navigation": {
    "home": "Home",
    "about": "About", 
    "books": "Books",
    "articles": "Articles"
  },
  "forms": {
    "submit": "Submit",
    "cancel": "Cancel",
    "save": "Save"
  },
  "messages": {
    "welcome": "Welcome to OnePageAuthor",
    "loading": "Loading...",
    "error": "An error occurred"
  }
}
```

## 🔧 Features

### Intelligent Seeding
- Validates JSON structure before insertion
- Updates existing translations without duplication
- Maintains translation versioning and history
- Provides detailed logging and error reporting

### Quality Assurance
- Validates translation completeness
- Checks for missing keys across locales  
- Ensures consistent terminology usage
- Reports translation coverage statistics

### Sample Output
```
Starting Localization Data Seeding...
🌍 Processing en-US (English)... ✅ 247 keys loaded
🌍 Processing es-ES (Spanish)... ✅ 242 keys loaded  
🌍 Processing fr-FR (French)... ✅ 238 keys loaded
🌍 Processing de-DE (German)... ✅ 235 keys loaded
🔍 Validation complete: 96% translation coverage
✅ Seeding completed! 4 locales with 962 total translations
```

## 📖 Documentation

- [Complete System Documentation](../Complete-System-Documentation.md)
- [Localization Guide](../LocalizationREADME.md)
- [InkStainedWretchFunctions](../InkStainedWretchFunctions/README.md) - Uses this data

## Configuration
- Provide required environment variables or app settings as expected by the code (see Program.cs if present).
