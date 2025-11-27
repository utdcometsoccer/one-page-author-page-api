# InkStainedWretchFunctions

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)

Azure Functions application providing domain registration management, external API integrations, and localized UI text services.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [HTTP API Endpoints](#http-api-endpoints)
- [Cosmos DB Triggered Functions](#cosmos-db-triggered-functions)
- [External API Integrations](#external-api-integrations)
- [Testing Strategy](#testing-strategy)
- [Security & User Secrets](#security--user-secrets)
- [Deployment](#deployment)

## Overview

The InkStainedWretchFunctions project provides a comprehensive Azure Functions application that handles:

- **Domain Registration Management**: Automated provisioning of domain registrations with Azure Front Door, DNS zones, and Google Domains integration
- **Localized Content**: Multi-language UI text services with fallback support
- **External API Integrations**: Amazon Product Advertising API and Penguin Random House API
- **Geographic Data**: Countries, languages, and state/province information in multiple languages
- **Automated Infrastructure**: Cosmos DB-triggered functions for automated Azure resource provisioning

### Functions Overview

#### HTTP Triggered Functions


- **GET /api/localizedtext/{culture}** - Returns localized UI text
- **POST /api/domain-registrations** - Creates new domain registrations
- **GET /api/domain-registrations** - Gets all domain registrations for user
- **GET /api/domain-registrations/{registrationId}** - Gets specific domain registration
- **GET /api/countries/{language}** - Gets countries by language
- **GET /api/languages/{language}** - Gets languages with localized names
- **GET /api/stateprovinces/{culture}** - Gets all states/provinces by culture
- **GET /api/stateprovinces/{countryCode}/{culture}** - Gets states/provinces by country
- **GET /api/amazon/books/author/{authorName}** - Searches Amazon books by author
- **GET /api/SearchPenguinAuthors** - Searches Penguin Random House authors
- **GET /api/GetPenguinTitlesByAuthor** - Gets Penguin titles by author

#### Cosmos DB Triggered Functions


- **DomainRegistrationTrigger** - Automatically adds domains to Azure Front Door
- **CreateDnsZoneFunction** - Automatically creates Azure DNS zones
- **GoogleDomainRegistrationFunction** - Automatically registers domains via Google Domains API

## Quick Start

```pwsh
# Build the project
dotnet build InkStainedWretchFunctions.csproj

# Run locally
func start

```

## ⚙️ Configuration (.NET 10)

### 🔐 Security Requirements

**CRITICAL**: This project uses sensitive credentials that must NOT be stored in source control.

### Development Setup (Required)

#### 1. Initialize User Secrets

```bash
cd InkStainedWretchFunctions
dotnet user-secrets init
```

#### 2. Core Configuration

```bash
# Azure Functions Core
dotnet user-secrets set "AzureWebJobsStorage" "your-connection-string"
dotnet user-secrets set "FUNCTIONS_WORKER_RUNTIME" "dotnet-isolated"

# Azure Cosmos DB (Required)
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-cosmos-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthorDb"
dotnet user-secrets set "CosmosDBConnection" "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key;"

# Azure AD Authentication (Optional)
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"
dotnet user-secrets set "AAD_AUDIENCE" "your-client-id"
```

#### 3. External API Integration (Optional)

```bash
# Amazon Product Advertising API
dotnet user-secrets set "AMAZON_PRODUCT_ACCESS_KEY" "your-aws-access-key"
dotnet user-secrets set "AMAZON_PRODUCT_SECRET_KEY" "your-aws-secret-key"
dotnet user-secrets set "AMAZON_PRODUCT_PARTNER_TAG" "yourtag-20"
dotnet user-secrets set "AMAZON_PRODUCT_REGION" "us-east-1"
dotnet user-secrets set "AMAZON_PRODUCT_MARKETPLACE" "www.amazon.com"

# Penguin Random House API
dotnet user-secrets set "PENGUIN_RANDOM_HOUSE_API_KEY" "your-api-key"
dotnet user-secrets set "PENGUIN_RANDOM_HOUSE_API_DOMAIN" "PRH.US"
```

#### 4. Azure Infrastructure (For Domain Registration Features)

```bash
# Azure Resource Management
dotnet user-secrets set "AZURE_SUBSCRIPTION_ID" "your-subscription-id"
dotnet user-secrets set "AZURE_DNS_RESOURCE_GROUP" "your-dns-resource-group"

# Google Domains (Optional)
dotnet user-secrets set "GOOGLE_CLOUD_PROJECT_ID" "your-google-project-id"
dotnet user-secrets set "GOOGLE_DOMAINS_LOCATION" "global"
```

#### 5. Testing Configuration (Optional)

```bash
# Testing Mode Settings
dotnet user-secrets set "TESTING_MODE" "true"
dotnet user-secrets set "MOCK_AZURE_INFRASTRUCTURE" "true"
dotnet user-secrets set "MOCK_GOOGLE_DOMAINS" "true"
dotnet user-secrets set "MAX_TEST_COST_LIMIT" "0.00"
```

### Environment Variables Reference

| Variable | Required | Description |
|----------|----------|-------------|
| `COSMOSDB_ENDPOINT_URI` | ✅ Yes | Azure Cosmos DB endpoint URL |
| `COSMOSDB_PRIMARY_KEY` | ✅ Yes | Cosmos DB primary access key |
| `COSMOSDB_DATABASE_ID` | ✅ Yes | Database name (typically "OnePageAuthorDb") |
| `CosmosDBConnection` | ✅ Yes | Full Cosmos DB connection string (for triggers) |
| `AZURE_SUBSCRIPTION_ID` | ⚪ Optional | Azure subscription for DNS/Front Door operations |
| `AZURE_DNS_RESOURCE_GROUP` | ⚪ Optional | Resource group for DNS zones |
| `GOOGLE_CLOUD_PROJECT_ID` | ⚪ Optional | Google Cloud project for domain registration |
| `AMAZON_PRODUCT_ACCESS_KEY` | ⚪ Optional | AWS access key for Amazon API |
| `AMAZON_PRODUCT_SECRET_KEY` | ⚪ Optional | AWS secret key for Amazon API |
| `AMAZON_PRODUCT_PARTNER_TAG` | ⚪ Optional | Amazon Associates partner tag |
| `PENGUIN_RANDOM_HOUSE_API_KEY` | ⚪ Optional | API key for Penguin Random House |
| `AAD_TENANT_ID` | ⚪ Optional | Azure AD tenant ID for authentication |
| `AAD_AUDIENCE` | ⚪ Optional | Azure AD client ID |

### Why These Settings Are Needed

<details>
<summary>🗄️ Cosmos DB Configuration</summary>

**Required for all functionality** - Cosmos DB stores all application data including domain registrations, localized text, countries, languages, and state/province data.

| Variable | Purpose | How to Obtain |
|----------|---------|---------------|
| `COSMOSDB_ENDPOINT_URI` | Database connection endpoint | Azure Portal → Cosmos DB → Keys → URI |
| `COSMOSDB_PRIMARY_KEY` | Authentication for database access | Azure Portal → Cosmos DB → Keys → Primary Key |
| `COSMOSDB_DATABASE_ID` | Identifies the application database | Your database name in Cosmos DB |
| `CosmosDBConnection` | Connection string for Cosmos DB triggers | Combine endpoint and key: `AccountEndpoint={URI};AccountKey={KEY};` |

**Note**: The `CosmosDBConnection` is specifically required for Cosmos DB-triggered functions (change feed triggers).

</details>

<details>
<summary>🌐 Azure Infrastructure (Domain Management)</summary>

**Required for domain registration features** - These enable automatic DNS zone creation and Azure Front Door integration.

| Variable | Purpose | How to Obtain |
|----------|---------|---------------|
| `AZURE_SUBSCRIPTION_ID` | Identifies your Azure subscription for resource management | Azure Portal → Subscriptions → Select subscription → Subscription ID |
| `AZURE_DNS_RESOURCE_GROUP` | Resource group where DNS zones will be created | Azure Portal → Resource Groups → Name of your DNS resource group |

**RBAC Permissions Required**:
- DNS Zone Contributor role on the DNS resource group
- CDN Profile Contributor on the Front Door profile (if using Front Door integration)

</details>

<details>
<summary>🌍 Google Domains Integration</summary>

**Required for automatic domain registration** - Enables registering domains via Google Domains API.

| Variable | Purpose | How to Obtain |
|----------|---------|---------------|
| `GOOGLE_CLOUD_PROJECT_ID` | Your Google Cloud project identifier | [Google Cloud Console](https://console.cloud.google.com) → Project selector → Project ID |
| `GOOGLE_DOMAINS_LOCATION` | Regional location for domain operations | Usually "global" |

**Setup Steps**:
1. Create a project in [Google Cloud Console](https://console.cloud.google.com)
2. Enable the Cloud Domains API
3. Set up authentication via Workload Identity Federation or service account
4. Grant Domain Registration Admin permissions

</details>

<details>
<summary>📚 Amazon Product Advertising API</summary>

**Required for Amazon book search functionality** - Enables searching Amazon's catalog for book information.

| Variable | Purpose | How to Obtain |
|----------|---------|---------------|
| `AMAZON_PRODUCT_ACCESS_KEY` | AWS Access Key ID | [AWS Console](https://console.aws.amazon.com) → Security Credentials → Access keys |
| `AMAZON_PRODUCT_SECRET_KEY` | AWS Secret Access Key | Created with Access Key (save immediately - shown only once) |
| `AMAZON_PRODUCT_PARTNER_TAG` | Your Associates tracking ID | [Amazon Associates](https://affiliate-program.amazon.com) → Your tracking IDs |
| `AMAZON_PRODUCT_REGION` | AWS region for API calls | Based on your marketplace (e.g., "us-east-1") |
| `AMAZON_PRODUCT_MARKETPLACE` | Target Amazon marketplace | E.g., "www.amazon.com", "www.amazon.co.uk" |

**Prerequisites**:
1. Join [Amazon Associates Program](https://affiliate-program.amazon.com)
2. Apply for Product Advertising API access (separate approval required)
3. Partner tag format varies by region: US `-20`, UK `-21`, DE `-03`

</details>

<details>
<summary>🐧 Penguin Random House API</summary>

**Required for Penguin Random House book search** - Enables searching their catalog for author and book information.

| Variable | Purpose | How to Obtain |
|----------|---------|---------------|
| `PENGUIN_RANDOM_HOUSE_API_KEY` | API authentication key | Request via [PRH Developer Portal](https://developer.penguinrandomhouse.com) |
| `PENGUIN_RANDOM_HOUSE_API_DOMAIN` | API domain identifier | Provided with API access (e.g., "PRH.US") |

</details>

<details>
<summary>🔐 Azure AD Authentication</summary>

**Required for JWT-protected endpoints** - Validates bearer tokens for secure API access.

| Variable | Purpose | How to Obtain |
|----------|---------|---------------|
| `AAD_TENANT_ID` | Your Azure AD tenant identifier | Azure Portal → Microsoft Entra ID → Overview → Tenant ID |
| `AAD_AUDIENCE` | API application client ID | Azure Portal → Microsoft Entra ID → App registrations → Your App → Application (client) ID |

</details>

### ⚠️ Migration from local.settings.json

**If you have an existing `local.settings.json` file with real credentials:**

1. **STOP** - Do not commit it to source control
2. **MIGRATE** - Copy values to user secrets using commands above
3. **SECURE** - Delete or rename the local.settings.json file
4. **VERIFY** - Ensure .gitignore includes `local.settings.json`

```bash
# Quick migration helper
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "$(jq -r '.Values.COSMOSDB_ENDPOINT_URI' local.settings.json)"
# Repeat for other sensitive values, then delete local.settings.json
```

### Production Deployment

For Azure deployment, configure these values in:
- Azure Portal → Function App → Configuration → Application Settings
- Or use Azure CLI: `az functionapp config appsettings set`

## HTTP API Endpoints

### Localized Text API

**Endpoint:** `GET /api/localizedtext/{culture}`

Returns aggregated localized UI text from multiple containers with fallback support.

**Fallback Order:** exact culture → language-prefixed variant → neutral language → empty object

**Example:**

```bash
curl -X GET "https://your-api.azurewebsites.net/api/localizedtext/en-US" \
  -H "Authorization: Bearer your-jwt-token"

```

### Countries API

**Endpoint:** `GET /api/countries/{language}`

Returns localized country names based on the requested language.

**Parameters:**

- `language` (required): Language code (`en`, `es`, `fr`, `ar`, `zh-cn`, `zh-tw`)

**Supported Countries:** 40 major countries from all continents

**Example Response:**

```json
{
  "language": "en",
  "count": 40,
  "countries": [
    { "code": "US", "name": "United States" },
    { "code": "CA", "name": "Canada" }
  ]
}

```

**Example:**

```bash
curl -X GET "https://your-api.azurewebsites.net/api/countries/en" \
  -H "Authorization: Bearer your-jwt-token"

```

### Languages API

**Endpoint:** `GET /api/languages/{language}`

Returns all available languages with names localized in the requested language.

**Supported Languages:** English, Spanish, French, Arabic, Chinese (Simplified), Chinese (Traditional)

**Example Response:**

```json
[
  { "code": "en", "name": "English" },
  { "code": "es", "name": "Spanish" },
  { "code": "fr", "name": "French" }
]

```

**Example:**

```bash
curl -X GET "https://your-api.azurewebsites.net/api/languages/en" \
  -H "Authorization: Bearer your-jwt-token"

```

### StateProvince APIs

#### Get All StateProvinces by Culture

**Endpoint:** `GET /api/stateprovinces/{culture}`

Returns all states and provinces for a specific culture across all countries.

**Parameters:**

- `culture` (required): Culture code (e.g., `en-US`, `fr-CA`, `es-MX`, `zh-CN`, `zh-TW`, `ar-EG`)

**Example Response:**

```json
{
  "Culture": "en-US",
  "TotalCount": 159,
  "Data": [
    {
      "Country": "US",
      "Culture": "en-US",
      "StateProvinces": [
        { "Code": "CA", "Name": "California", "Country": "US", "Culture": "en-US" }
      ]
    }
  ]
}

```

#### Get StateProvinces by Country and Culture

**Endpoint:** `GET /api/stateprovinces/{countryCode}/{culture}`

Returns states and provinces for a specific country and culture.

**Parameters:**

- `countryCode` (required): Two-letter ISO country code (`US`, `CA`, `MX`, `CN`, `TW`, `EG`)
- `culture` (required): Culture code

**Example Response:**

```json
{
  "Country": "US",
  "Culture": "en-US",
  "Count": 55,
  "StateProvinces": [
    { "Code": "CA", "Name": "California", "Country": "US", "Culture": "en-US" }
  ]
}

```

**Supported Geographic Data:**

- **United States**: 55 states and territories
- **Canada**: 13 provinces and territories
- **Mexico**: 32 states
- **China**: 33 provinces and regions
- **Taiwan**: 22 counties and cities
- **Egypt**: 28 governorates

### Domain Registration APIs

#### Create Domain Registration

**Endpoint:** `POST /api/domain-registrations`

Creates a new domain registration for the authenticated user.

**Request Body:**

```json
{
  "domain": "example.com",
  "email": "user@example.com"
}

```

#### Get All Domain Registrations

**Endpoint:** `GET /api/domain-registrations`

Gets all domain registrations for the authenticated user.

#### Get Specific Domain Registration

**Endpoint:** `GET /api/domain-registrations/{registrationId}`

Gets a specific domain registration by ID.

## Cosmos DB Triggered Functions

### DomainRegistrationTrigger

**Purpose:** Automatically adds custom domains to Azure Front Door when domain registrations are created.

**Trigger:** New or updated documents in the `DomainRegistrations` container

**Configuration:**

- **Lease Container**: `leases`
- **Lease Prefix**: `domainregistration`
- **Status Filter**: Only processes `Pending` or `InProgress` registrations

**Process Flow:**

1. Triggered by Cosmos DB change feed
2. Validates domain registration data
3. Checks if domain exists in Front Door
4. Adds domain with managed TLS certificate if it doesn't exist
5. Logs success or failure

**Required RBAC Permissions:**

- CDN Profile Contributor or CDN Endpoint Contributor role on the Front Door profile

**Assigning Permissions:**

```bash
az role assignment create \
  --assignee <function-app-managed-identity-object-id> \
  --role "CDN Profile Contributor" \
  --scope /subscriptions/<subscription-id>/resourceGroups/<rg-name>/providers/Microsoft.Cdn/profiles/<profile-name>

```

### CreateDnsZoneFunction

**Purpose:** Automatically creates Azure DNS zones for domain registrations.

**Trigger:** New or updated documents in the `DomainRegistrations` container

**Configuration:**

- **Lease Container**: `leases`
- **Lease Prefix**: `DnsZone`
- **Status Filter**: Only processes `Pending` or `InProgress` registrations

**Process Flow:**

1. Monitors DomainRegistrations container
2. Validates domain registration data
3. Checks if DNS zone already exists
4. Creates DNS zone if it doesn't exist
5. Uses Azure.ResourceManager.Dns SDK

**Authentication:**
Uses `DefaultAzureCredential` supporting:

1. Managed Identity (recommended for production)
2. Environment Variables (local development)
3. Azure CLI (local development)

**Required RBAC Permissions:**

- DNS Zone Contributor role on the resource group

**Assigning Permissions:**

```bash
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "DNS Zone Contributor" \
  --resource-group <dns-resource-group>

```

### GoogleDomainRegistrationFunction

**Purpose:** Automatically registers domains using the Google Domains API.

**Trigger:** New or updated documents in the `DomainRegistrations` container

**Configuration:**

- **Lease Container**: `leases`
- **Lease Prefix**: `googledomainregistration`
- **Status Filter**: Only processes `Pending` registrations

**Process Flow:**

1. Triggered by domain registration creation
2. Validates domain registration data
3. Calls Google Domains API to register the domain
4. Initiates long-running operation (returns immediately)
5. Logs success or failure

**Authentication:**
Uses Application Default Credentials (ADC):

- Managed Identity in Azure
- Workload Identity Federation recommended for production
- Requires Domain Registration Admin permissions in Google Cloud

**Cost Considerations:**

- Domain registration costs vary by TLD
- Long-running operations may have additional costs
- Consider implementing domain availability checks before registration

## External API Integrations

### Amazon Product Advertising API

**Purpose:** Search for books and products using Amazon's Product Advertising API (PAAPI 5.0).

#### SearchAmazonBooksByAuthor

**Endpoint:** `GET /api/amazon/books/author/{authorName}`

Searches for books by author name and returns unmodified JSON from Amazon.

**Parameters:**

- `authorName` (route parameter, required): Author name to search for
- `page` (query parameter, optional): Page number for pagination (default: 1)

**Example:**

```bash
curl "http://localhost:7072/api/amazon/books/author/Stephen%20King" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

```

**Features:**

- AWS Signature Version 4 authentication
- Unmodified JSON response from Amazon
- Pagination support
- JWT authentication required

#### Getting Amazon Credentials

1. Sign up for [Amazon Associates Program](https://affiliate-program.amazon.com/)
2. Locate your Associate Tag (format: `yourstore-20`)
3. Apply for [Product Advertising API](https://webservices.amazon.com/paapi5/documentation/) access (separate approval required)
4. Generate AWS Access Key and Secret Key after PA API approval
5. Configure credentials in your settings

**Partner Tag Format:**

- US: Ends with `-20`
- UK: Ends with `-21`
- Germany: Ends with `-03`
- France: Ends with `-21`
- Japan: Ends with `-22`
- Canada: Ends with `-20`

### Penguin Random House API

**Purpose:** Search for authors and titles from Penguin Random House.

#### SearchPenguinAuthors

**Endpoint:** `GET/POST /api/SearchPenguinAuthors`

Searches for authors by name and returns unmodified JSON.

**Parameters:**

- Query parameter: `?authorName=Stephen King`
- Or JSON body: `{"authorName": "Stephen King"}`
- Alternative parameter names: `author`, `name`

**Example:**

```bash
# GET request
curl "http://localhost:7072/api/SearchPenguinAuthors?authorName=Stephen%20King"

# POST request
curl -X POST "http://localhost:7072/api/SearchPenguinAuthors" \
  -H "Content-Type: application/json" \
  -d '{"authorName": "Stephen King"}'

```

#### GetPenguinTitlesByAuthor

**Endpoint:** `GET/POST /api/GetPenguinTitlesByAuthor`

Gets titles by author key with pagination support.

**Parameters:**

- Query parameters: `?authorKey=123456&rows=10&start=0`
- Or JSON body: `{"authorKey": "123456", "rows": 10, "start": 0}`

**Example:**

```bash
curl "http://localhost:7072/api/GetPenguinTitlesByAuthor?authorKey=123456&rows=10&start=0"

```

## Testing Strategy

The project includes a comprehensive testing framework with three distinct scenarios to minimize costs while ensuring thorough testing.

### Testing Scenarios

#### Scenario 1: Frontend UI Testing (Completely Safe)


- **Purpose**: Test client UI without creating actual resources
- **Cost**: $0.00 (all operations mocked)
- **Safety**: 100% safe - no real resources created
- **Use Case**: Daily development, UI testing, integration testing

**Configuration:** All mocking enabled, $0 cost limit

**Usage:**

```powershell
# Switch to safe testing
.\Testing\SwitchTestConfig.ps1 -Scenario 1

# Run tests
.\Testing\RunTests.ps1 -Scenario 1 -DomainName "test.example.com"

```

#### Scenario 2: Individual Function Testing (Minimal Cost)


- **Purpose**: Test each function with real Azure APIs but mock expensive operations
- **Cost**: ~$0.50-2.00 per test run
- **Safety**: Low cost - creates some Azure resources but no domain purchases
- **Use Case**: Function validation, Azure integration testing

**Configuration:** Azure real, domains mocked, $5 cost limit

**Usage:**

```powershell
.\Testing\SwitchTestConfig.ps1 -Scenario 2
.\Testing\RunTests.ps1 -Scenario 2 -DomainName "test.example.com"

```

#### Scenario 3: Full End-to-End with Real Money (Production Test)


- **Purpose**: Complete production test with real domains, DNS, and Front Door
- **Cost**: $12-50+ per test (domain registration costs vary by TLD)
- **Safety**: ⚠️ HIGH COST - uses real money and creates production resources
- **Use Case**: Final production validation before launch

**Configuration:** Everything real, $50 cost limit

**Usage:**

```powershell
.\Testing\SwitchTestConfig.ps1 -Scenario 3
.\Testing\RunTests.ps1 -Scenario 3 -DomainName "real-test-domain.com" -ConfirmRealMoney $true

```

### Testing Infrastructure

**Files Created:**

- `TestingConfiguration.cs` - Feature flag management
- `Testing/Mocks/` - Mock implementations for services
- `TestHarnessFunction.cs` - Individual function testing endpoints
- `EndToEndTestFunction.cs` - Complete workflow testing
- `scenario1.local.settings.json` - Safe testing config
- `scenario2.local.settings.json` - Individual testing config
- `scenario3.local.settings.json` - Production testing config

**Test Endpoints:**

- `POST /api/test/frontdoor` - Test Front Door operations
- `POST /api/test/dns` - Test DNS zone operations
- `POST /api/test/googledomains` - Test Google Domains operations
- `POST /api/test/scenario1` - Safe frontend testing
- `POST /api/test/scenario3` - Full production testing

### Cost Management

**Safety Features:**

1. **Cost Limits**: `MAX_TEST_COST_LIMIT` prevents expensive operations
2. **Domain Purchase Protection**: `SKIP_DOMAIN_PURCHASE` prevents accidental purchases
3. **Confirmation Required**: Scenario 3 requires explicit `confirmRealMoney: true`
4. **Detailed Logging**: `ENABLE_TEST_LOGGING` shows all operations
5. **Mock Overrides**: Feature flags override real services with mocks

## Security & User Secrets

### Why Use User Secrets?

- **Security**: Secrets stored outside source code
- **Team Safety**: No risk of committing sensitive data
- **Environment Isolation**: Development secrets separate from production
- **Azure Functions Compatibility**: Works seamlessly with local development

### Quick Setup

```powershell
# Run the migration script (preview)
.\MoveSecretsToUserSecrets.ps1 -WhatIf

# Run the actual migration
.\MoveSecretsToUserSecrets.ps1

# Verify
dotnet user-secrets list

```

### Secrets Moved to User Secrets

- `COSMOSDB_PRIMARY_KEY` - Cosmos DB access key
- `COSMOSDB_CONNECTION_STRING` - Cosmos DB connection string
- `PENGUIN_RANDOM_HOUSE_API_KEY` - Penguin API key
- `AMAZON_PRODUCT_ACCESS_KEY` - Amazon access key
- `AMAZON_PRODUCT_SECRET_KEY` - Amazon secret key
- `AAD_CLIENT_ID` - Azure AD client ID
- `AAD_TENANT_ID` - Azure AD tenant ID
- `GOOGLE_CLOUD_PROJECT_ID` - Google Cloud project ID

### Public Configuration (Remains in local.settings.json)

- API URLs and endpoints
- Region settings
- Feature flags
- Testing configuration
- Non-sensitive Azure resource names

### Managing User Secrets

```bash
# View all secrets
dotnet user-secrets list

# Add a secret
dotnet user-secrets set "KEY_NAME" "secret-value"

# Remove a secret
dotnet user-secrets remove "KEY_NAME"

# Clear all secrets
dotnet user-secrets clear

```

### File Locations

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\{user-secrets-id}\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/{user-secrets-id}/secrets.json`

## Deployment

### Prerequisites

1. Azure Functions App (v4, dotnet-isolated)
2. Azure Cosmos DB account
3. Azure Front Door profile (for domain management)
4. Azure DNS zone resource group (for DNS management)
5. Google Cloud project with Domains API enabled (optional)
6. Amazon Product Advertising API credentials (optional)
7. Penguin Random House API credentials (optional)

### Deployment Steps

#### 1. Enable Managed Identity

```bash
az functionapp identity assign \
  --name <function-app-name> \
  --resource-group <rg-name>

```

#### 2. Assign RBAC Roles

**For Front Door:**

```bash
az role assignment create \
  --assignee <managed-identity-object-id> \
  --role "CDN Profile Contributor" \
  --scope /subscriptions/<subscription-id>/resourceGroups/<rg-name>/providers/Microsoft.Cdn/profiles/<profile-name>

```

**For DNS Zones:**

```bash
az role assignment create \
  --assignee <managed-identity-object-id> \
  --role "DNS Zone Contributor" \
  --resource-group <dns-resource-group>

```

#### 3. Configure Application Settings

```bash
az functionapp config appsettings set \
  --name <function-app-name> \
  --resource-group <rg-name> \
  --settings \
  COSMOSDB_ENDPOINT_URI=<value> \
  COSMOSDB_PRIMARY_KEY=<value> \
  COSMOSDB_DATABASE_ID=<value> \
  AZURE_SUBSCRIPTION_ID=<value> \
  AZURE_RESOURCE_GROUP_NAME=<value> \
  AZURE_FRONTDOOR_PROFILE_NAME=<value> \
  AZURE_DNS_RESOURCE_GROUP=<value>

```

#### 4. Deploy the Function App

```bash
func azure functionapp publish <function-app-name>

```

### Production Best Practices

1. **Use Azure Key Vault** for sensitive configuration
2. **Enable Application Insights** for monitoring
3. **Configure Alerts** for function failures
4. **Implement Retry Logic** for transient failures
5. **Monitor Costs** for domain registrations and Azure resources
6. **Rotate Secrets Regularly** in production
7. **Use Different Credentials** for each environment

### Monitoring

**View Function Execution:**

- Azure Portal > Function App > Functions
- Application Insights logs
- Cosmos DB change feed metrics

**Key Log Messages:**

- "DomainRegistrationTrigger processing {Count} domain registration(s)"
- "Successfully processed domain {DomainName} for Front Door"
- "DNS zone created successfully for domain {DomainName}"
- "Failed to add domain {DomainName} to Front Door"

**Log Queries (KQL):**

```kql
// Find DNS zone creation events
traces
| where message contains "DNS zone"
| project timestamp, message, severityLevel
| order by timestamp desc

// Find errors in domain registration
traces
| where severityLevel == 3 // Error
| where operation_Name == "CreateDnsZone"
| project timestamp, message
| order by timestamp desc

```

## Notes

- Services are provided via `OnePageAuthorLib` and wired through `.AddInkStainedWretchServices()`
- See `LocalizationREADME.md` at the repo root for data and fallback details
- All HTTP endpoints require JWT authentication
- Cosmos DB triggers use unique lease prefixes to prevent conflicts
- External API integrations return unmodified JSON responses
- Testing framework provides cost-safe development workflow

## Related Documentation

- [LocalizationREADME.md](../LocalizationREADME.md) - Localization data and fallback details
- [SeedCountries](../SeedCountries/README.md) - Country data seeding
- [SeedLanguages](../SeedLanguages/README.md) - Language data seeding
- [OnePageAuthor.DataSeeder](../OnePageAuthor.DataSeeder/README.md) - StateProvince data seeding
- [OnePageAuthorLib](../OnePageAuthorLib/README.md) - Core business logic library

## Support

For issues or questions:

- File an issue in the repository
- Contact the development team
- Review Application Insights logs for errors
- Check Azure Function App logs for troubleshooting
