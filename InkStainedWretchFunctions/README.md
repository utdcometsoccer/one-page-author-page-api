# InkStainedWretchFunctions

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)

Azure Functions application providing domain registration management, external API integrations, and localized UI text services.

## Overview
Provides endpoints for domain registration management, external API integrations, localized UI text services, and automated Front Door domain provisioning.

### Functions

#### HTTP Triggered Functions
- **GET /api/localizedtext/{culture}**
  - Returns a JSON object aggregating multiple localization containers (e.g., Navbar, ThankYou, etc.).
  - Fallback order: exact culture -> first language-prefixed variant -> neutral language -> empty object.

- **POST /api/domain-registrations**
  - Creates a new domain registration for the authenticated user.
  
- **GET /api/domain-registrations**
  - Gets all domain registrations for the authenticated user.
  
- **GET /api/domain-registrations/{registrationId}**
  - Gets a specific domain registration by ID for the authenticated user.

#### Cosmos DB Triggered Functions
- **DomainRegistrationTrigger**
  - Automatically triggered when new domain registrations are created in the DomainRegistrations container.
  - Checks if the domain already exists in Azure Front Door.
  - Adds the domain to Front Door if it doesn't exist.
  - Uses a unique lease prefix ("domainregistration") to avoid conflicts with other triggers.
  - Requires Azure RBAC permissions for the Function App's Managed Identity to manage Front Door resources.

## Quickstart
```pwsh
dotnet build InkStainedWretchFunctions.csproj
func start
```

## Configuration
Environment or local.settings.json values consumed in Program.cs:
- COSMOSDB_ENDPOINT_URI
- COSMOSDB_PRIMARY_KEY
- COSMOSDB_DATABASE_ID
- COSMOSDB_CONNECTION_STRING (for Cosmos DB trigger)
- AZURE_SUBSCRIPTION_ID (for Front Door integration)
- AZURE_RESOURCE_GROUP_NAME (for Front Door integration)
- AZURE_FRONTDOOR_PROFILE_NAME (for Front Door integration)

Example local.settings.json:
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "COSMOSDB_ENDPOINT_URI": "https://<account>.documents.azure.com:443/",
    "COSMOSDB_PRIMARY_KEY": "<secret>",
    "COSMOSDB_DATABASE_ID": "<db-name>",
    "COSMOSDB_CONNECTION_STRING": "AccountEndpoint=https://<account>.documents.azure.com:443/;AccountKey=<key>;",
    "AZURE_SUBSCRIPTION_ID": "<subscription-id>",
    "AZURE_RESOURCE_GROUP_NAME": "<resource-group-name>",
    "AZURE_FRONTDOOR_PROFILE_NAME": "<frontdoor-profile-name>"
  }
}

## Deployment
- Deploy as an Azure Functions app (v4, dotnet-isolated). Configure Cosmos settings as app settings.
- **For Front Door integration**: Enable Managed Identity for the Function App and assign appropriate RBAC roles:
  - CDN Profile Contributor or CDN Endpoint Contributor role on the Front Door profile or resource group
  - Required for the DomainRegistrationTrigger function to add custom domains to Front Door

## Notes
- Services are provided via `OnePageAuthorLib` and wired through `.AddInkStainedWretchServices()`.
- See `LocalizationREADME.md` at the repo root for data and fallback details.