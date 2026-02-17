# OnePageAuthor API Platform

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A comprehensive .NET 10 platform providing APIs and utilities for author management, content publishing, and subscription services. Built with Azure Functions (isolated worker), Azure Cosmos DB for persistence, Stripe for billing, and Microsoft Entra ID for authentication.

## 🚀 North America Launch Status

**Platform Readiness:** 95% ✅ | **Target Launch:** Q1 2026 | **Time to Launch:** 1-2 weeks

The OnePageAuthor API platform is ready for North America launch and first customer sale. Core features are production-ready, with domain registration workflow requiring final validation testing.

### Launch Documentation (NEW - 2026-02-11)

- **[Launch Documentation Index](docs/LAUNCH_INDEX.md)** 📋 - Navigation guide to all launch docs
- **[Executive Summary](docs/NORTH_AMERICA_LAUNCH_EXECUTIVE_SUMMARY.md)** 🎯 - Decision brief for leadership
- **[Launch Readiness Plan](docs/LAUNCH_READINESS_PLAN.md)** 📊 - Comprehensive preparation guide
- **[Minimum Viable Launch](docs/MINIMUM_VIABLE_LAUNCH.md)** ✅ - Critical path checklist

### Ready for Launch ✅
- Payment processing via Stripe (subscriptions, checkout, webhooks)
- Authentication via Microsoft Entra ID (JWT, validated Dec 2025)
- Author profile and content management (full CRUD)
- Image storage with tiered quotas (5GB to 2TB)
- Multi-language support (EN, ES, FR for North America)

### Final Validation Required ⚠️
- Domain registration E2E testing with real domains
- Azure DNS zone creation validation
- Azure Front Door integration testing

**See [TODO: Human Intervention](docs/TODO_HUMAN_INTERVENTION.md) for launch blocker details.**

## 📋 Product Roadmap

For strategic planning, feature roadmap, technical debt tracking, and release planning, see the **[Product Roadmap](docs/PRODUCT_ROADMAP.md)**.

## 🚀 Platform Overview

### Key Features

- **.NET 10** Azure Functions with isolated worker runtime
- **Azure Cosmos DB** with NoSQL document storage and repository patterns
- **Stripe Integration** for subscription management, checkout, and webhook processing
- **Microsoft Entra ID** JWT authentication for secure API access
- **External API Integrations** (Penguin Random House, Amazon Product Advertising API)
- **Multi-language Support** with comprehensive localization infrastructure
- **Domain Management** with automated Azure Front Door integration
- **Image Management** with upload, storage, and retrieval capabilities
- **Comprehensive Testing** with unit and integration test coverage

### Architecture Components

#### 🏗️ Azure Functions (API Layer)

- **ImageAPI** — Image upload, management, and retrieval services
- **InkStainedWretchFunctions** — Domain registration, localization, and external API integrations
- **InkStainedWretchStripe** — Stripe payment processing and subscription management
- **function-app** — Core author data and additional infrastructure functions

#### 📚 Core Libraries (Business Logic Layer)

- **OnePageAuthorLib** — Shared library with entities, repositories, services, and orchestrators

#### 🛠️ Data Management (Seeding & Utilities)

- **SeedAPIData** — Author, book, and article data initialization
- **SeedInkStainedWretchesLocale** — Comprehensive multi-language localization and UI text (North America: EN, ES, FR, AR, ZH-CN, ZH-TW)
- **SeedImageStorageTiers** — Image storage tier configuration
- **OnePageAuthor.DataSeeder** — StateProvince and geographical data seeding
- **AuthorInvitationTool** — Command-line tool for inviting authors to create Microsoft accounts linked to their domains

#### 🧪 Testing & Quality Assurance

- **OnePageAuthor.Test** — Comprehensive unit and integration tests
- **IntegrationTestAuthorDataService** — Author data service validation testing

## 🛠️ Prerequisites & Setup

### System Requirements

- **.NET SDK 10.0** or later
- **Azure Functions Core Tools v4** (optional for local development)
- **Azure Cosmos DB** account or local emulator
- **Stripe Account** with API keys for payment processing
- **Microsoft Entra ID** app registration (tenant ID, client ID)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/utdcometsoccer/one-page-author-page-api.git
cd one-page-author-page-api

# Build the entire solution
dotnet build OnePageAuthorAPI.sln -c Debug

# Run tests
dotnet test OnePageAuthorAPI.sln -c Debug
```

### GitHub Secrets Configuration (for CI/CD)

Before deploying to Azure via GitHub Actions, configure repository secrets using GitHub CLI:

```powershell
# Install and authenticate GitHub CLI (required)
gh auth login

# Run the PowerShell script directly
.\Scripts\Initialize-GitHubSecrets.ps1 -Interactive

# Or use a configuration file
.\Scripts\Initialize-GitHubSecrets.ps1 -ConfigFile secrets.json

# Optional: Use NPM wrappers if preferred
npm run init-secrets:interactive
npm run init-secrets -- -ConfigFile secrets.json
```

**New in 2026**: Additional secrets management scripts:

```powershell
# Update existing secrets file with new variables from template
.\Scripts\Update-SecretsConfig.ps1

# Set dotnet user-secrets for local development
.\Scripts\Set-DotnetUserSecrets.ps1 -ConfigFile secrets.config.json
```

📖 **See [docs/GITHUB_SECRETS_CONFIGURATION.md](docs/GITHUB_SECRETS_CONFIGURATION.md) for comprehensive documentation**

**Note:** The script uses **GitHub CLI (`gh secret set`)** to configure secrets. NPM is optional.

## ⚙️ Configuration

### Core Environment Variables

| Variable | Description | Required | Where to Find |
|----------|-------------|----------|---------------|
| `COSMOSDB_ENDPOINT_URI` | Azure Cosmos DB endpoint URI | ✅ Yes | Azure Portal → Cosmos DB → Keys → URI |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary access key | ✅ Yes | Azure Portal → Cosmos DB → Keys → Primary Key |
| `COSMOSDB_DATABASE_ID` | Database name (e.g., "OnePageAuthor") | ✅ Yes | Your database name in Cosmos DB |
| `STRIPE_API_KEY` | Stripe secret API key | ✅ Yes | [Stripe Dashboard](https://dashboard.stripe.com) → Developers → API Keys → Secret key |
| `AAD_TENANT_ID` | Microsoft Entra ID tenant GUID | ✅ Yes | Azure Portal → Microsoft Entra ID → Overview → Tenant ID |
| `AAD_AUDIENCE` | API application/client ID | ✅ Yes | Azure Portal → Microsoft Entra ID → App Registrations → Your App → Application (client) ID |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights connection string for telemetry (OpenTelemetry Azure Monitor exporter) | Optional | Azure Portal → Application Insights → Overview → Connection String |
| `AAD_VALID_ISSUERS` | Comma-separated v2.0 issuer URLs (multi-issuer JWT support) | Optional | For Entra External ID/CIAM, use issuers like `https://{your-ciam-domain}.ciamlogin.com/{your-tenant}/B2C_1_signup_signin/v2.0/` (and other policies as needed); for standard Entra ID tenants you can also include `https://login.microsoftonline.com/{tenant}/v2.0` |
| `STRIPE_WEBHOOK_SECRET` | Webhook endpoint secret for verification | For webhooks | [Stripe Dashboard](https://dashboard.stripe.com) → Developers → Webhooks → Select endpoint → Signing secret |

**Note (CI/CD overrides):** The GitHub Actions workflow supports per-app overrides via GitHub secrets `APPLICATIONINSIGHTS_CONNECTION_STRING_FUNCTION_APP` (for `function-app`) and `APPLICATIONINSIGHTS_CONNECTION_STRING_ISW` (for `ImageAPI`, `InkStainedWretchFunctions`, `InkStainedWretchStripe`, `InkStainedWretchesConfig`). If not set, it falls back to `APPLICATIONINSIGHTS_CONNECTION_STRING`.

### Why These Settings Are Needed

<details>
<summary>🗄️ Azure Cosmos DB Configuration</summary>

**Purpose**: Cosmos DB serves as the primary NoSQL database for storing all application data including author profiles, books, articles, user profiles, and localization content.

| Variable | Why It's Needed |
|----------|-----------------|
| `COSMOSDB_ENDPOINT_URI` | Required to establish a connection to your Cosmos DB account. This is the HTTPS endpoint that the application uses to communicate with the database. |
| `COSMOSDB_PRIMARY_KEY` | Authentication key that grants read/write access to your database. Keep this secret and never commit it to source control. |
| `COSMOSDB_DATABASE_ID` | Identifies which database within your Cosmos DB account contains the application containers (Authors, Books, Articles, Locales, etc.). |

**How to Obtain**:

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to your Cosmos DB account
3. Click on "Keys" in the left sidebar
4. Copy the URI and Primary Key values

</details>

<details>
<summary>💳 Stripe Configuration</summary>

**Purpose**: Stripe handles all subscription billing, payment processing, and customer management for the platform.

| Variable | Why It's Needed |
|----------|-----------------|
| `STRIPE_API_KEY` | Authenticates API calls to Stripe for creating customers, subscriptions, and processing payments. Use test keys (starting with `sk_test_`) for development. |
| `STRIPE_WEBHOOK_SECRET` | Validates that webhook events are genuinely from Stripe. Used to verify the signature of incoming webhook payloads to prevent spoofing attacks. |

**How to Obtain**:

1. Sign up at [Stripe Dashboard](https://dashboard.stripe.com)
2. Navigate to Developers → API keys
3. Copy the Secret key (use Test mode key for development)
4. For webhooks: Developers → Webhooks → Add endpoint → Copy the Signing secret

**Important**: Never use production keys (`sk_live_`) in development environments.

</details>

<details>
<summary>🔐 Microsoft Entra ID (Azure AD) Configuration</summary>

**Purpose**: Provides JWT-based authentication and authorization for securing API endpoints and managing user identity.

| Variable | Why It's Needed |
|----------|-----------------|
| `AAD_TENANT_ID` | Identifies your Azure AD tenant for token validation. This ensures tokens are issued by your organization. |
| `AAD_AUDIENCE` | Specifies which application the tokens are intended for. Tokens without this audience claim will be rejected. |
| `AAD_VALID_ISSUERS` | Enables accepting tokens from multiple issuers (v2.0 URLs). If not set, a single issuer derived from tenant/authority is used. |

**How to Obtain**:

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to Microsoft Entra ID (formerly Azure Active Directory)
3. **Tenant ID**: Found on the Overview page
4. **Client ID**: Go to App registrations → Select your app → Application (client) ID

**Additional Setup**:

- For personal Microsoft accounts, configure the app registration with `signInAudience: "PersonalMicrosoftAccount"`
- For organizational accounts, use `signInAudience: "AzureADMyOrg"`
- Audience tip: Use the raw Application (client) ID for `AAD_AUDIENCE` (not an `api://` scope) to match token `aud`.
- Issuer tip: Provide v2.0 issuer URLs in `AAD_VALID_ISSUERS`, for example CIAM issuers like `https://{your-ciam-domain}.ciamlogin.com/{your-tenant}/B2C_1_signup_signin/v2.0/` (and other policies as needed), and optionally standard Entra ID issuers such as `https://login.microsoftonline.com/{tenant}/v2.0`.

</details>

### External API Integration (Optional)

<details>
<summary>🐧 Penguin Random House API Configuration</summary>

**Purpose**: Enables searching for book information and author details from the Penguin Random House catalog.

| Variable | Description | How to Obtain |
|----------|-------------|---------------|
| `PENGUIN_RANDOM_HOUSE_API_URL` | Base API URL | Contact Penguin Random House Developer Relations |
| `PENGUIN_RANDOM_HOUSE_API_KEY` | Authentication key | Request via [PRH Developer Portal](https://developer.penguinrandomhouse.com) |
| `PENGUIN_RANDOM_HOUSE_API_DOMAIN` | API domain (e.g., "PRH.US") | Provided with API access approval |
| `PENGUIN_RANDOM_HOUSE_SEARCH_API` | Search endpoint template | API documentation |
| `PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API` | Author titles endpoint | API documentation |

**Why It's Needed**: Allows the platform to search and display book information from Penguin Random House's extensive catalog, enriching author pages with published works.

</details>

<details>
<summary>📚 Amazon Product Advertising API Configuration</summary>

**Purpose**: Enables searching for books on Amazon and displaying product information with affiliate links.

| Variable | Description | How to Obtain |
|----------|-------------|---------------|
| `AMAZON_PRODUCT_ACCESS_KEY` | AWS Access Key ID | [AWS Console](https://console.aws.amazon.com) → Security Credentials → Access keys |
| `AMAZON_PRODUCT_SECRET_KEY` | AWS Secret Access Key | Created with the Access Key ID (save immediately, shown only once) |
| `AMAZON_PRODUCT_PARTNER_TAG` | Associates Partner Tag | [Amazon Associates](https://affiliate-program.amazon.com) → Your tracking IDs |
| `AMAZON_PRODUCT_REGION` | AWS region (e.g., "us-east-1") | Based on your marketplace location |
| `AMAZON_PRODUCT_MARKETPLACE` | Marketplace domain | Your target Amazon marketplace (e.g., <www.amazon.com>) |

**Why It's Needed**: Allows the platform to search Amazon's catalog for book information and generate affiliate links for author pages.

**Setup Steps**:

1. Sign up for [Amazon Associates Program](https://affiliate-program.amazon.com)
2. Apply for [Product Advertising API](https://webservices.amazon.com/paapi5/documentation/) access (separate approval required)
3. After approval, create AWS credentials with PA API permissions
4. Note: Partner Tag format varies by region (US: `-20`, UK: `-21`, DE: `-03`)

</details>

### Setting Up User Secrets (Recommended for Development)

**Important**: Never store sensitive values in source control. Use .NET User Secrets for local development.

```bash
# Initialize user secrets for the project
cd [project-directory]
dotnet user-secrets init

# Set required configuration
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-cosmos-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthorDb"
dotnet user-secrets set "STRIPE_API_KEY" "sk_test_your-stripe-key"
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"
dotnet user-secrets set "AAD_AUDIENCE" "your-client-id"

# Verify configuration
dotnet user-secrets list
```

### Example Configuration (local.settings.json)

⚠️ **Warning**: Only use for non-sensitive development settings. Store secrets in User Secrets instead.

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "COSMOSDB_DATABASE_ID": "OnePageAuthorDb"
  }
}
```

## 🏗️ Development & Deployment

### Building the Solution

```bash
# Build entire solution
dotnet build OnePageAuthorAPI.sln -c Debug

# Build specific project
dotnet build OnePageAuthorLib/OnePageAuthorLib.csproj

```

### Running Locally

```bash
# Run a specific Azure Function app
cd InkStainedWretchStripe
dotnet run
# or use Azure Functions Core Tools
func start

# Run data seeders
cd SeedAPIData
dotnet run

```

### Startup Configuration Diagnostics

- All Functions apps now emit masked configuration summaries at startup and a concise line indicating whether sanitization was applied to Cosmos settings (`COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`).
- Sanitization trims surrounding quotes and whitespace so validation and clients receive clean values. Example log:

```
Cosmos DB Endpoint configured: https:****com/
Cosmos DB Primary Key configured: C2y6****Jw==
Cosmos DB Database ID configured: OnePageAuthorDB
Config sanitization applied: yes
```

### Stripe Orchestrator Tests

- Added unit tests for the Stripe client secret extraction orchestrator covering hydrated PaymentIntent success and missing secret error paths.
- Tests live in `OnePageAuthor.Test/StripeExtractorTests.cs`.
- When a `PaymentIntent` is present but the `client_secret` is missing, the orchestrator now throws a clear exception:

```
PaymentIntent found but missing client_secret. Ensure the intent is in a state that exposes client_secret or retrieve via supported methods.
```

This behavior helps surface misconfiguration or unsupported retrieval states early during subscription checkout flows.

### Testing

```bash
# Run all tests
dotnet test OnePageAuthorAPI.sln -c Debug

# Run specific test project
dotnet test OnePageAuthor.Test/OnePageAuthor.Test.csproj --logger "console;verbosity=detailed"

```

## 🔧 Dependency Injection Extensions

The platform provides comprehensive DI extensions through `OnePageAuthorLib`:

### Database & Storage

- `AddCosmosClient(endpointUri, primaryKey)` — Azure Cosmos DB client
- `AddCosmosDatabase(databaseId)` — Database configuration

### Repositories & Data Access

- `AddAuthorRepositories()` — Author data repositories
- `AddLocaleRepository()` — Localization data access
- `AddUserProfileRepository()` — User profile management
- `AddStateProvinceRepository()` — Geographic data access

### Business Services

- `AddAuthorDataService()` — Author management services
- `AddLocaleDataService()` — Localization services
- `AddInkStainedWretchServices()` — Core platform services
- `AddStripeServices()` — Payment processing services
- `AddStateProvinceServices()` — Geographic services

### Authentication & Security

- `AddJwtAuthentication()` — JWT token validation
- `AddUserProfileServices()` — User authentication services

### External Integrations

- `AddPenguinRandomHouseServices()` — Book catalog integration
- `AddDomainRegistrationServices()` — Domain management
- `AddFrontDoorServices()` — Azure Front Door integration

## 🔐 Security & Authentication

- **JWT Bearer Authentication** via Microsoft Entra ID
- **API Protection** with `[Authorize]` attributes on sensitive endpoints
- **Webhook Security** with Stripe signature verification
- **RBAC Integration** for Azure resource management
- See [`SECURITY.md`](docs/SECURITY.md) for vulnerability reporting

## 📊 Data Management & Seeding

### Available Seeders

- **SeedAPIData** — Author profiles, books, articles, and relationships
- **SeedInkStainedWretchesLocale** — Comprehensive multi-language localization for all UI components (North America: EN, ES, FR, AR, ZH-CN, ZH-TW for US, CA, MX)
- **SeedImageStorageTiers** — Image storage configuration
- **OnePageAuthor.DataSeeder** — StateProvince geographic data

### Running Seeders

```bash
# Seed author and content data
cd SeedAPIData && dotnet run

# Initialize comprehensive localization data (idempotent)
cd SeedInkStainedWretchesLocale && dotnet run

# Setup geographic data
cd OnePageAuthor.DataSeeder && dotnet run

```

## 🌐 Internationalization & Localization

The platform supports comprehensive multi-language functionality:

- **Supported Languages**: English, Spanish, French, German, Portuguese, Italian
- **Fallback Logic**: Graceful degradation when translations are missing
- **Cultural Adaptation**: Regional date, number, and currency formatting
- **Dynamic Loading**: Runtime language switching with caching

## 📡 API Documentation & Endpoints

### Azure Functions Applications

#### 🖼️ ImageAPI

**Purpose**: Image upload, management, and retrieval services

- `POST /api/upload` — Upload user images with size and format validation
- `GET /api/images/{imageId}` — Retrieve image metadata and URLs
- `DELETE /api/images/{imageId}` — Delete user images
- **Features**: Automatic resizing, format conversion, storage tier management

#### 🌐 InkStainedWretchFunctions

**Purpose**: Domain registration, localization, and external API integrations

- `GET /api/localizedtext/{culture}` — Retrieve localized UI text with fallback logic
- `POST /api/domain-registrations` — Create domain registrations with auto-provisioning
- `GET /api/domain-registrations` — List user domain registrations
- `GET /api/testimonials` — Public: list testimonials (`limit`, `featured`, `locale`)
- `POST /api/testimonials` — Create testimonial (authenticated)
- `PUT /api/testimonials/{id}` — Update testimonial (authenticated)
- `DELETE /api/testimonials/{id}` — Delete testimonial (authenticated)
- **Features**: Azure Front Door integration, multi-language support, external API proxying

#### 💳 InkStainedWretchStripe

**Purpose**: Stripe payment processing and subscription management

- `POST /api/CreateStripeCheckoutSession` — Create secure checkout sessions
- `POST /api/CreateStripeCustomer` — Customer creation and management
- `POST /api/CreateSubscription` — Subscription lifecycle management
- `POST /api/WebHook` — Stripe webhook event processing
- `GET /api/ListSubscription/{customerId}` — Subscription queries with filtering
- `GET /api/stripe/health` — Public: configuration health check (mode and connectivity)

#### 📚 function-app

**Purpose**: Core author data and additional infrastructure functions

- Author profile management
- Content publishing workflows
- System health monitoring

### Authentication

- Protected endpoints require JWT Bearer authentication. Include:

```http
Authorization: Bearer <your-jwt-token>

```

- Public endpoints (no token required):
  - `GET /api/testimonials`
  - `GET /api/stripe/health`

## 🧪 Testing & Quality Assurance

### Test Coverage

- **Unit Tests**: Business logic validation with 90%+ coverage
- **Integration Tests**: End-to-end API workflow validation
- **Service Tests**: External API integration verification
- **Performance Tests**: Load testing for critical endpoints

### Running Tests

```bash
# Run all tests with coverage
dotnet test OnePageAuthorAPI.sln --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Unit"

```

## 📋 Project Structure & Components

<details>
<summary><strong>🏗️ Azure Functions (API Layer)</strong></summary>

| Project | Purpose | Key Features |
|---------|---------|--------------|
| **ImageAPI** | Image management services | Upload, resize, storage tiering |
| **InkStainedWretchFunctions** | Domain & localization APIs | Multi-language, domain provisioning |
| **InkStainedWretchStripe** | Payment processing | Stripe integration, webhooks |
| **function-app** | Core platform APIs | Author data, content management |

</details>

<details>
<summary><strong>📚 Core Libraries (Business Logic)</strong></summary>

| Component | Purpose | Key Features |
|-----------|---------|--------------|
| **OnePageAuthorLib** | Shared business logic | Entities, repositories, services |
| **entities/** | Data models | Author, Book, Article, Social profiles |
| **nosql/** | Data access layer | Cosmos DB repositories, containers |
| **api/** | External integrations | Stripe, Penguin Random House, Amazon |
| **interfaces/** | Service contracts | Dependency injection interfaces |

</details>

<details>
<summary><strong>🛠️ Data Management (Utilities)</strong></summary>

| Project | Purpose | Data Types |
|---------|---------|------------|
| **SeedAPIData** | Core data initialization | Authors, books, articles, relationships |
| **SeedInkStainedWretchesLocale** | Comprehensive localization | All UI components, messages, navigation for North American countries (US, CA, MX) in EN, ES, FR, AR, ZH-CN, ZH-TW |
| **SeedImageStorageTiers** | Storage configuration | Image processing settings, storage tiers |
| **OnePageAuthor.DataSeeder** | Geographic data | StateProvince, country codes, regions |

</details>

<details>
<summary><strong>🧪 Testing & Quality (QA Layer)</strong></summary>

| Project | Purpose | Test Types |
|---------|---------|------------|
| **OnePageAuthor.Test** | Comprehensive testing | Unit, integration, service tests |
| **IntegrationTestAuthorDataService** | Service validation | Author data workflows, API contracts |

</details>

## 🚀 Production Deployment

### Automated CI/CD Deployment

The platform uses GitHub Actions for automated deployment to Azure with built-in quality gates:

- **Unit Tests** - All unit tests must pass before deployment proceeds
- **Build Validation** - All Azure Functions are compiled and validated
- **Infrastructure Deployment** - Automated Bicep template deployments

See the comprehensive guides:

- **[Deployment Guide](docs/DEPLOYMENT_GUIDE.md)** - Complete deployment workflow documentation
- **[GitHub Secrets Reference](docs/GITHUB_SECRETS_REFERENCE.md)** - Quick reference for all required secrets

**Note:** The CI/CD pipeline automatically runs unit tests before any deployment. If tests fail, the deployment is blocked to prevent broken code from reaching production.

### Azure Resources Required

- **Azure Functions Apps** (v4, .NET 10 isolated)
- **Azure Cosmos DB** account with containers
- **Azure Storage** account for function app storage and blobs
- **Azure Key Vault** for secure secrets management
- **Microsoft Entra ID** app registration for authentication
- **Azure Front Door** profile for domain management (optional)
- **Application Insights** for monitoring and logging
- **DNS Zone** for custom domains (optional)
- **Static Web App** for frontend hosting (optional)

### Quick Deployment Setup

1. **Configure GitHub Secrets** (see [GitHub Secrets Reference](docs/GITHUB_SECRETS_REFERENCE.md))
   - Azure credentials (`AZURE_CREDENTIALS`)
   - Infrastructure settings (`ISW_RESOURCE_GROUP`, `ISW_LOCATION`, `ISW_BASE_NAME`)
   - Application configuration (`COSMOSDB_CONNECTION_STRING`, `STRIPE_API_KEY`, etc.)

2. **Push to main branch** - Deployment runs automatically

3. **Monitor deployment** - Check GitHub Actions tab for progress

### Deployment Checklist

- [ ] Configure all required environment variables
- [ ] Set up Azure Service Principal with appropriate permissions
- [ ] Configure CORS policies for frontend integration
- [ ] Set up Application Insights monitoring
- [ ] Configure Stripe webhook endpoints
- [ ] Validate JWT token configuration
- [ ] Run smoke tests on deployed endpoints

### ⚠️ Common Deployment Issues

**Service Principal Permissions Error**

If you encounter this error during deployment:

```
ERROR: "The client '***' does not have permission to perform action 
'Microsoft.Authorization/roleAssignments/write'"
```

**Quick Fix**: Run the permission script **once** before deploying:

```bash
cd infra
./Grant-ServicePrincipalPermissions.ps1  # Windows PowerShell
# OR
./Grant-ServicePrincipalPermissions.sh   # Linux/macOS
```

This grants your GitHub Actions service principal the required permissions to assign roles during deployment. See [PERMISSIONS_QUICK_FIX.md](PERMISSIONS_QUICK_FIX.md) for detailed instructions.

### Manual Deployment (Alternative)

For manual deployment using Azure CLI:

```bash
# Deploy infrastructure
az deployment group create \
  --resource-group InkStainedWretches-RG \
  --template-file infra/inkstainedwretches.bicep \
  --parameters baseName=yourbasename location="West US 2"

# Deploy function apps
az functionapp deployment source config-zip \
  --name yourbasename-imageapi \
  --resource-group InkStainedWretches-RG \
  --src ImageAPI/imageapi.zip
```

## 🤝 Contributing & Community

### Development Workflow

1. Read [`CONTRIBUTING.md`](docs/CONTRIBUTING.md) for guidelines
2. Follow [`CODE_OF_CONDUCT.md`](docs/CODE_OF_CONDUCT.md) for community standards
3. Review [`SECURITY.md`](docs/SECURITY.md) for security considerations
4. Check existing issues and PRs before creating new ones

### Getting Help

- **Documentation**: Check project-specific README files for detailed information
- **Issues**: Use GitHub Issues for bug reports and feature requests
- **Security**: Follow responsible disclosure via [`SECURITY.md`](docs/SECURITY.md)

## 📖 Enhancement & Implementation Documentation

For detailed documentation on specific features, enhancements, and implementations, see the [`docs/`](docs/) directory:

### Feature Enhancements

- [Active Products Filter Enhancement](docs/ACTIVE_PRODUCTS_FILTER_ENHANCEMENT.md) - Stripe product filtering
- [Culture Support Enhancement](docs/CULTURE_SUPPORT_ENHANCEMENT.md) - Multi-language subscription plans
- [Label Validation Enhancement](docs/LABEL_VALIDATION_ENHANCEMENT.md) - Input validation improvements
- [Subscription Plan Service Refactoring](docs/SUBSCRIPTION_PLAN_SERVICE_REFACTORING.md) - Service architecture improvements

### Implementation Summaries

- [DNS Zone Implementation](docs/IMPLEMENTATION_SUMMARY.md) - Azure DNS zone automation
- [Entra ID Roles](docs/IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) - Role-based access control
- [Language Support](docs/IMPLEMENTATION_SUMMARY_LANGUAGES.md) - Multi-language implementation
- [Country Data](docs/COUNTRIES_IMPLEMENTATION_SUMMARY.md) - Geographic data implementation
- [StateProvince Boilerplate](docs/STATEPROVINCE_BOILERPLATE_SUMMARY.md) - Geographic entities
- [Multi-Function Deployment](docs/IMPLEMENTATION_SUMMARY_MULTI_FUNCTION_DEPLOYMENT.md) - Deployment architecture
- [Cosmos DB & App Insights](docs/IMPLEMENTATION_SUMMARY_COSMOS_APPINSIGHTS.md) - Monitoring setup
- [Conditional Environment Variables](docs/IMPLEMENTATION_SUMMARY_CONDITIONAL_ENV_VARS.md) - Configuration patterns
- [Permissions Fix](docs/IMPLEMENTATION_SUMMARY_PERMISSIONS_FIX.md) - Access control fixes
- [Author Invitation System](docs/AUTHOR_INVITATION_IMPLEMENTATION_SUMMARY.md) - User onboarding
- [Key Vault Implementation](docs/KEY_VAULT_IMPLEMENTATION_SUMMARY.md) - Secrets management

## 📖 Configuration & Setup

### Configurationvalidation

### Overview

All applications in the OnePageAuthor API repository have been updated to implement consistent configuration validation patterns. Required configuration values now throw `InvalidOperationException` with clear error messages when missing, while optional values use appropriate fallbacks.

### Standardized Configuration Keys

#### Required Settings

All applications now use standardized environment variable names:

**Cosmos DB (Required for all apps)**

- `COSMOSDB_ENDPOINT_URI` - Cosmos DB endpoint URL
- `COSMOSDB_PRIMARY_KEY` - Cosmos DB primary access key
- `COSMOSDB_DATABASE_ID` - Cosmos DB database name

**Azure Storage (Required for ImageAPI only)**

- `AZURE_STORAGE_CONNECTION_STRING` - Azure Blob Storage connection string

**Stripe (Required for InkStainedWretchStripe only)**

- `STRIPE_API_KEY` - Stripe API key for payment processing

**Azure AD/Entra ID (Required for EntraIdRoleManager only)**

- `AAD_TENANT_ID` - Azure AD tenant ID
- `AAD_MANAGEMENT_CLIENT_ID` - Management app client ID
- `AAD_MANAGEMENT_CLIENT_SECRET` - Management app client secret
- `AAD_TARGET_CLIENT_ID` - Target app client ID

#### Optional Settings

These settings have reasonable defaults or fallback behavior:

**Azure AD/Entra ID (Optional for ImageAPI, InkStainedWretchStripe)**

- `AAD_TENANT_ID` - Optional for authentication
- `AAD_AUDIENCE` / `AAD_CLIENT_ID` - Optional for JWT validation
- `AAD_AUTHORITY` - Optional, auto-constructed from tenant ID

### Applications Updated

#### ✅ Production Applications

1. **ImageAPI** (`ImageAPI/Program.cs`)

   - ✅ Added validation for `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
   - ✅ Added validation for `AZURE_STORAGE_CONNECTION_STRING`
   - ✅ Removed null-forgiving operators (!)
   - ✅ Optional Azure AD settings remain optional with proper fallbacks

2. **InkStainedWretchFunctions** (`InkStainedWretchFunctions/Program.cs`)

   - ✅ Added validation for `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
   - ✅ Removed null-forgiving operators (!)

3. **InkStainedWretchStripe** (`InkStainedWretchStripe/Program.cs`)

   - ✅ Added validation for `COSMOSDB_ENDPOINT_URI`, `COSMOSDB_PRIMARY_KEY`, `COSMOSDB_DATABASE_ID`
   - ✅ Existing `STRIPE_API_KEY` validation maintained
   - ✅ Removed null-forgiving operators (!)
   - ✅ Optional Azure AD settings remain optional with proper masking in logs

4. **function-app** (`function-app/Program.cs`)

   - ✅ Already had proper validation - verified consistency

#### ✅ Management/Utility Applications

1. **EntraIdRoleManager** (`EntraIdRoleManager/Program.cs`)

   - ✅ Already had proper validation - verified consistency

2. **SeedImageStorageTiers** (`SeedImageStorageTiers/Program.cs`)

   - ✅ Already had proper validation - verified consistency

#### ✅ Data Seeder Applications

All seeder applications updated to use standardized keys with backward compatibility:

1. **SeedLocalizationData** (`SeedLocalizationData/Program.cs`)

   - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy `EndpointUri`, `PrimaryKey`, `DatabaseId`
   - ✅ Added environment variable support

2. **OnePageAuthor.DataSeeder** (`OnePageAuthor.DataSeeder/Program.cs`)

   - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
   - ✅ Removed unsafe emulator defaults
   - ✅ Added helpful error messages with emulator information

3. **SeedLocales** (`SeedLocales/Program.cs`)

   - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
   - ✅ Added environment variable support

4. **SeedInkStainedWretchesLocale** (`SeedInkStainedWretchesLocale/Program.cs`)

    - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
    - ✅ Added environment variable support

5. **SeedAPIData** (`SeedAPIData/Program.cs`)

    - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
    - ✅ Added environment variable support

6. **IntegrationTestAuthorDataService** (`IntegrationTestAuthorDataService/Program.cs`)

    - ✅ Standardized to `COSMOSDB_*` keys with fallback to legacy keys
    - ✅ Added proper validation (was using empty strings as defaults)
    - ✅ Added environment variable support

### Validation Pattern

All applications now use the consistent pattern:

```csharp
// For required settings
var requiredValue = config["REQUIRED_KEY"] ?? throw new InvalidOperationException("REQUIRED_KEY is required");

// For optional settings with fallback
var optionalValue = config["OPTIONAL_KEY"] ?? "default_value";

// For backward compatibility in seeder apps
var value = config["NEW_KEY"] ?? config["LEGACY_KEY"] ?? throw new InvalidOperationException("NEW_KEY is required");

```

### Error Messages

All error messages are clear and actionable:

- Specify the exact environment variable name needed
- For development tools, include helpful information about emulator settings
- No sensitive information is logged

### Benefits

1. **Production Safety** - No more silent failures due to missing configuration
2. **Consistency** - Standardized configuration keys across all applications
3. **Developer Experience** - Clear error messages with actionable guidance
4. **Backward Compatibility** - Seeder apps support both new and legacy configuration keys
5. **Security** - No hardcoded secrets or unsafe defaults in production applications

### Testing

All applications should be tested to ensure:

1. They fail fast with clear error messages when required configuration is missing
2. They work correctly when proper configuration is provided
3. Optional settings behave appropriately with and without values
4. Seeder applications work with both new and legacy configuration key formats

---

### Configurationmaskingstandardization

### Overview

All applications in the OnePageAuthor API repository have been updated with standardized configuration masking and logging. This ensures sensitive values are properly masked in logs while providing clear visibility into what configuration values are loaded.

### Standardized Masking Functions

#### Helper Functions Added to All Applications

```csharp
// Helper function to mask sensitive configuration values
static string MaskSensitiveValue(string? value, string notSetText = "(not set)")
{
    if (string.IsNullOrWhiteSpace(value)) return notSetText;
    if (value.Length < 8) return "(set)";
    return $"{value[..4]}****{value[^4..]}";
}

// Helper function to mask URLs (show more of the beginning for readability)
static string MaskUrl(string? value, string notSetText = "(not set)")
{
    if (string.IsNullOrWhiteSpace(value)) return notSetText;
    if (value.Length < 12) return "(set)";
    return $"{value[..8]}****{value[^4..]}";
}

```

#### Masking Rules

- **Short values (< 8 chars)**: Show "(set)" to avoid revealing too much
- **Sensitive values (8+ chars)**: Show first 4 chars + "****" + last 4 chars
- **URLs (12+ chars)**: Show first 8 chars + "****" + last 4 chars (more context for debugging)
- **Missing values**: Show "(not set)" for optional values
- **Database IDs**: Not masked (not sensitive, useful for debugging)
- **Domain names**: Not masked (not sensitive, useful for debugging)

### Applications Updated

#### ✅ Production Azure Functions

1. **ImageAPI** (`ImageAPI/Program.cs`)

   ```

   Azure AD Tenant ID configured: (not set)
   Azure AD Audience configured: (not set)
   Azure AD Authority configured: (not set)
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Primary Key configured: C2y6****Jw==
   Cosmos DB Database ID configured: OnePageAuthorDB
   Azure Storage Connection String configured: Defa****key1

   ```

2. **InkStainedWretchFunctions** (`InkStainedWretchFunctions/Program.cs`)

   ```

   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Primary Key configured: C2y6****Jw==
   Cosmos DB Database ID configured: OnePageAuthorDB

   ```

3. **InkStainedWretchStripe** (`InkStainedWretchStripe/Program.cs`)

   ```

   Stripe API key configured: sk_t****_1N2
   Azure AD Tenant ID configured: (not set)
   Azure AD Audience configured: (not set)
   Azure AD Authority configured: (not set)
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Primary Key configured: C2y6****Jw==
   Cosmos DB Database ID configured: OnePageAuthorDB

   ```

4. **function-app** (`function-app/Program.cs`)

   ```

   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Primary Key configured: C2y6****Jw==
   Cosmos DB Database ID configured: OnePageAuthorDB

   ```

#### ✅ Management/Utility Applications

1. **EntraIdRoleManager** (`EntraIdRoleManager/Program.cs`)

   ```

   Configuration (masked for security):
     Management App ID: 0816****333a
     Target App ID: f2b0****db73
     Tenant ID: 5c6d****461e
     Cosmos DB Endpoint: https:****com/
     Cosmos DB Database ID: OnePageAuthorDB

   ```

2. **SeedImageStorageTiers** (`SeedImageStorageTiers/Program.cs`)

   ```

   Configuration (masked for security):
     Cosmos DB Endpoint: https:****com/
     Cosmos DB Database ID: OnePageAuthorDB

   ```

#### ✅ Data Seeder Applications

1. **SeedAPIData** (`SeedAPIData/Program.cs`)

   ```

   Starting API Data Seeding...
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Database ID configured: OnePageAuthorDB

   ```

2. **SeedInkStainedWretchesLocale** (`SeedInkStainedWretchesLocale/Program.cs`)

   ```

   Starting InkStainedWretches Locale Seeding...
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Database ID configured: OnePageAuthorDB

   ```

3. **OnePageAuthor.DataSeeder** (`OnePageAuthor.DataSeeder/Program.cs`)

   ```

   Starting StateProvince Data Seeding...
   Cosmos DB Endpoint configured: https:****com/
   Cosmos DB Database ID configured: OnePageAuthorDB

   ```

4. **IntegrationTestAuthorDataService** (`IntegrationTestAuthorDataService/Program.cs`)

    ```

    Starting Integration Test for Author Data Service...
    Cosmos DB Endpoint configured: https:****com/
    Cosmos DB Database ID configured: OnePageAuthorDB
    Author Domain configured: example.com

    ```

### Configuration Values Logged

#### Always Masked

- **Cosmos DB Primary Keys**: `MaskSensitiveValue()` - Shows `C2y6****Jw==`
- **Azure Storage Connection Strings**: `MaskSensitiveValue()` - Shows connection string secrets masked
- **Stripe API Keys**: `MaskSensitiveValue()` - Shows `sk_t****_1N2`
- **Azure AD Client IDs**: `MaskSensitiveValue()` - Shows `0816****333a`
- **Azure AD Client Secrets**: `MaskSensitiveValue()` - Shows first/last 4 chars
- **Azure AD Tenant IDs**: `MaskSensitiveValue()` - Shows `5c6d****461e`

#### URL Masked (More Context)

- **Cosmos DB Endpoints**: `MaskUrl()` - Shows `https:****com/`
- **Azure AD Authority URLs**: `MaskUrl()` - Shows `https:****v2.0`

#### Never Masked (Not Sensitive)

- **Database IDs**: Shown in full (useful for debugging, not sensitive)
- **Domain Names**: Shown in full (not sensitive)
- **Application Names**: Shown in full

#### Optional Values

- **Azure AD Settings**: Show "(not set)" when not configured
- All optional authentication parameters handled gracefully

### Security Benefits

1. **No Secrets in Logs**: All sensitive values are properly masked
2. **Debugging Friendly**: Enough context to verify configuration without exposing secrets
3. **Consistent Format**: Same masking pattern across all applications
4. **Clear Status**: Easy to see what's configured vs. missing
5. **Production Safe**: Safe to enable in production environments

### Implementation Pattern

All applications now follow this consistent pattern:

1. Load configuration with proper validation (from previous update)
2. Apply standardized masking functions
3. Log configuration status with appropriate masking
4. Continue with application startup

This provides excellent visibility into application configuration while maintaining security best practices.

---

### Azure Storage Emulator Setup

### Overview

Added a new VS Code launch configuration to easily start the Azure Storage Emulator (Azurite) for local development.

### Prerequisites

#### Install Azurite

You need to have Azurite installed globally:

```bash
npm install -g azurite

```

Or if you prefer using it locally in the project:

```bash
npm install --save-dev azurite

```

### Launch Configurations Added

#### 1. Launch Azure Storage Emulator

**Name**: `Launch Azure Storage Emulator`

- **Purpose**: Starts Azurite (Azure Storage Emulator) for local development
- **Location**: Stores data in `${workspaceFolder}/.azurite`
- **Debug Log**: Saves debug information to `${workspaceFolder}/.azurite/debug.log`
- **Mode**: Silent mode (reduced console output)

#### 2. Launch All Services (with Storage)

**Name**: `Launch All Services (with Storage)`

- **Purpose**: Compound configuration that starts:

  1. Azure Storage Emulator
  2. ImageAPI Functions
  3. InkStainedWretchFunctions
  4. InkStainedWretchStripe Functions

### How to Use

#### Option 1: Launch Storage Emulator Only

1. Open VS Code
2. Go to Run and Debug (Ctrl+Shift+D)
3. Select "Launch Azure Storage Emulator"
4. Click the green play button

#### Option 2: Launch All Services Including Storage

1. Open VS Code
2. Go to Run and Debug (Ctrl+Shift+D)
3. Select "Launch All Services (with Storage)"
4. Click the green play button

### Storage Emulator Details

#### Default Endpoints

When Azurite starts, it provides these endpoints:

- **Blob Service**: `http://127.0.0.1:10000/{account}`
- **Queue Service**: `http://127.0.0.1:10001/{account}`
- **Table Service**: `http://127.0.0.1:10002/{account}`

#### Connection String

Use this connection string in your applications:

```text
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;

```

#### Data Persistence

- **Storage Location**: `${workspaceFolder}/.azurite/`
- **Debug Logs**: `${workspaceFolder}/.azurite/debug.log`
- **Data Files**: Azurite creates `.blob`, `.queue`, and `.table` files

### Configuration Details

```json
{
    "name": "Launch Azure Storage Emulator",
    "type": "node",
    "request": "launch",
    "program": "azurite",
    "args": [
        "--silent",
        "--location", "${workspaceFolder}/.azurite",
        "--debug", "${workspaceFolder}/.azurite/debug.log"
    ],
    "cwd": "${workspaceFolder}",
    "console": "integratedTerminal"
}

```

#### Arguments Explained

- `--silent`: Reduces console output for cleaner logs
- `--location`: Specifies where to store emulator data
- `--debug`: Enables debug logging to specified file

### Troubleshooting

#### Port Already in Use

If you get port conflicts, you can customize the ports:

1. Edit the launch configuration
2. Add port arguments:

   ```json

   "args": [
       "--blobPort", "10000",
       "--queuePort", "10001",
       "--tablePort", "10002",
       "--silent",
       "--location", "${workspaceFolder}/.azurite",
       "--debug", "${workspaceFolder}/.azurite/debug.log"
   ]

   ```

#### Azurite Not Found

If you get "azurite command not found":

1. Install globally:

pm install -g azurite`

1. Or update the program path to local installation:

   ```json

   "program": "${workspaceFolder}/node_modules/.bin/azurite"

   ```

#### Clear Storage Data

To reset the emulator data:

1. Stop the emulator
2. Delete the `.azurite` folder
3. Restart the emulator

### Integration with Functions

Your Azure Functions can connect to the local storage emulator using:

#### In local.settings.json

```json
{
  "Values": {
    "AzureWebJobsStorage": "your-connection-string",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}

```

#### Or with explicit connection string

```json
{
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"
  }
}

```

### Benefits

1. **Easy Development**: One-click storage emulator startup
2. **Integrated Workflow**: Combined with Function Apps debugging
3. **Persistent Data**: Data survives emulator restarts
4. **Debug Logs**: Easy troubleshooting with debug output
5. **Team Consistency**: Standardized development environment

This configuration makes local Azure Storage development much more convenient and integrated with your VS Code workflow!

---

### Find Partner Tag

### 🎯 Quick Steps to Find Your Partner Tag

#### 1. **Access Amazon Associates Central**

```
URL: https://affiliate-program.amazon.com/

```

- Sign in with your Amazon Associates account
- If you don't have one, apply first (approval required)

#### 2. **Locate Your Associate ID**

##### Option A: Dashboard Method

1. After login, look at your main dashboard
2. Find "Associate ID" or "Tracking ID"
3. Copy the value (format: `yourstore-20`)

##### Option B: Account Settings Method

1. Go to **"Account & Login Info"**
2. Click **"Manage Your Tracking IDs"**
3. All your Associate IDs will be listed
4. Copy the one you want to use

##### Option C: Link Generator Method

1. Go to **"Product Linking"** → **"Link to Any Page"**
2. Generate any affiliate link
3. Your Associate ID appears as `tag=yourstore-20` in the URL

#### 3. **Verify Tag Format**

✅ **Correct Format**: `storename-20`, `mybooks-21`, `techstore-20`
❌ **Wrong Format**: `storename`, `20-storename`, `storename_20`

#### 4. **Regional Suffixes**

- 🇺🇸 **US**: `-20` (amazon.com)
- 🇬🇧 **UK**: `-21` (amazon.co.uk)
- 🇩🇪 **Germany**: `-03` (amazon.de)
- 🇫🇷 **France**: `-21` (amazon.fr)
- 🇯🇵 **Japan**: `-22` (amazon.co.jp)
- 🇨🇦 **Canada**: `-20` (amazon.ca)

### ⚠️ IMPORTANT: Product Advertising API Access

**Having an Amazon Associates account is NOT enough!**

You need **separate approval** for Product Advertising API:

#### 1. Apply for PA API Access

```
URL: https://developer.amazon.com/

```

1. Sign in to Amazon Developer Portal
2. Navigate to **"Product Advertising API"**
3. Click **"Request Access"**
4. Fill out application form
5. Wait for approval (can take days/weeks)

#### 2. Generate AWS Credentials

After PA API approval:

1. Go to **"Manage Your Apps"** in Developer Portal
2. Create or select your PA API application
3. Generate **Access Key** and **Secret Key**
4. These are your `AMAZON_PRODUCT_ACCESS_KEY` and `AMAZON_PRODUCT_SECRET_KEY`

### 🔧 Update Your Configuration

Once you have your real Partner Tag:

```bash
## Update user secrets
dotnet user-secrets set "AMAZON_PRODUCT_PARTNER_TAG" "your-real-tag-20" --project InkStainedWretchFunctions\InkStainedWretchFunctions.csproj

## Test the configuration
dotnet run --project AmazonProductTestConsole\AmazonProductTestConsole.csproj -- --config

```

### 🚨 Common Issues

#### Issue: 404 Error with Valid Partner Tag

**Cause**: Associates account exists but no PA API approval
**Solution**: Apply for Product Advertising API access separately

#### Issue: Can't Find Partner Tag

**Cause**: No Amazon Associates account
**Solution**:

1. Apply at <https://affiliate-program.amazon.com/>
2. Complete profile and tax information
3. Wait for approval
4. Then apply for PA API access

#### Issue: 403 Forbidden Error

**Cause**: Invalid AWS credentials or wrong region
**Solution**:

1. Verify Access Key and Secret Key from Developer Portal
2. Ensure region matches your PA API setup
3. Check that credentials have PA API permissions

### 📞 Support Resources

- **Amazon Associates Help**: <https://affiliate-program.amazon.com/help/>
- **PA API Documentation**: <https://webservices.amazon.com/paapi5/documentation/>
- **Developer Portal**: <https://developer.amazon.com/apps-and-games/services/paapi>

### ✅ Verification Steps

1. **Check Associate Account**: Login to affiliate-program.amazon.com
2. **Find Partner Tag**: Look for Associate ID/Tracking ID
3. **Verify PA API Access**: Check developer.amazon.com for approved applications
4. **Test Configuration**: Use the console app to verify API calls
5. **Monitor Logs**: Check debug output for signature validation

Your Partner Tag should work once both your Associates account and Product Advertising API access are approved!

---

## 📖 Development & Maintenance

### Refactoring Summary

### Overview

Successfully refactored the `GetUserUpn` method in `DomainRegistrationService` to make it testable by extracting the user identity logic into a separate, injectable service.

### Changes Made

#### 1. Created IUserIdentityService Interface

**File**: `OnePageAuthorLib\interfaces\IUserIdentityService.cs`

- Defines contract for extracting user identity information from claims
- Single method: `GetUserUpn(ClaimsPrincipal user)` returning string

#### 2. Implemented UserIdentityService

**File**: `OnePageAuthorLib\api\UserIdentityService.cs`

- Concrete implementation of `IUserIdentityService`
- Extracts user UPN with fallback to email claim
- Proper error handling for unauthenticated users and missing claims
- Handles empty/whitespace values correctly

#### 3. Refactored DomainRegistrationService

**File**: `OnePageAuthorLib\api\DomainRegistrationService.cs`

**Changes**:

- Added `IUserIdentityService` dependency to constructor
- Updated all method calls to use `_userIdentityService.GetUserUpn(user)` instead of private static method
- Removed private static `GetUserUpn` method
- Maintained all existing functionality and error handling

#### 4. Updated Unit Tests

**File**: `OnePageAuthor.Test\DomainRegistration\DomainRegistrationServiceTests.cs`

**Changes**:

- Added `Mock<IUserIdentityService>` to test setup
- Updated constructor to inject mocked service
- Added default mock behavior for successful scenarios
- Fixed existing tests that expected user validation errors
- Added comprehensive integration tests for user identity scenarios

#### 5. Created Comprehensive UserIdentityService Tests

**File**: `OnePageAuthor.Test\API\UserIdentityServiceTests.cs`

**Test Coverage**:

- ✅ Success with UPN claim
- ✅ Success with email claim fallback
- ✅ UPN preference over email when both present
- ✅ Fallback to email when UPN is empty
- ✅ Error handling for null user
- ✅ Error handling for unauthenticated user
- ✅ Error handling for missing claims
- ✅ Error handling for empty/whitespace claims

### Benefits Achieved

#### 1. **Testability** ✅

- User identity extraction is now fully testable in isolation
- Mock-able dependency allows comprehensive testing of error scenarios
- Clean separation of concerns between business logic and identity extraction

#### 2. **Maintainability** ✅

- Single responsibility: `UserIdentityService` handles only user identity extraction
- Easy to modify claim extraction logic without touching business logic
- Clearer error messages and handling

#### 3. **Reusability** ✅

- `IUserIdentityService` can be injected into other services that need user identity
- Consistent user identity handling across the application
- Centralized claim extraction logic

#### 4. **Dependency Injection** ✅

- Follows IoC principles with proper dependency injection
- Easy to substitute implementations for different authentication providers
- Better integration with ASP.NET Core DI container

### Test Results

- **UserIdentityServiceTests**: 9/9 tests passing ✅
- **DomainRegistrationServiceTests**: 20/20 tests passing ✅
- **DependencyInjectionTests**: 3/3 tests passing ✅
- **Total**: 32/32 tests passing ✅

### Migration Notes

When deploying this refactor, ensure that:

1. **Dependency Injection Setup**: ✅ **COMPLETED** - `IUserIdentityService` is now automatically registered in your DI container via:

   ```csharp

   services.AddDomainRegistrationServices(); // Now includes IUserIdentityService registration

   ```

2. **No Breaking Changes**: ✅ The public interface of `DomainRegistrationService` remains unchanged
3. **Error Handling**: ✅ Same error scenarios and messages are preserved for backward compatibility
4. **Automatic Registration**: ✅ All existing applications using `.AddDomainRegistrationServices()` will automatically get the new `IUserIdentityService`

### Future Enhancements

The refactored architecture now supports:

- Easy addition of other identity providers (Azure AD, Auth0, etc.)
- Caching of user identity information
- Advanced claim transformation logic
- User identity auditing and logging

---

### Security Audit Report

**Date:** October 18, 2025
**Repository:** one-page-author-page-api
**Status:** ✅ SECURED (Issues Fixed)

### 📊 Summary

| Category | Status | Count |
|----------|--------|-------|
| 🚨 Critical Issues | ✅ Fixed | 1 |
| ⚠️ Medium Issues | ✅ None Found | 0 |
| 📋 Best Practices | ✅ Implemented | 5 |

### 🚨 Issues Found & Fixed

#### 1. ✅ FIXED: InkStainedWretchStripe Exposed Secrets

**Issue:** `InkStainedWretchStripe/local.settings.json` contained real secrets in plain text
**Impact:** High - Exposed Stripe API keys, Cosmos DB keys, and Azure AD credentials
**Resolution:**

- Replaced all secret values with placeholder text
- Created `USER_SECRETS_SETUP.md` with setup instructions
- Secrets are now properly ignored by git

**Files Fixed:**

- `InkStainedWretchStripe/local.settings.json` - Secrets removed
- `InkStainedWretchStripe/USER_SECRETS_SETUP.md` - Setup guide created

### ✅ Security Best Practices Found

1. **local.settings.json files properly ignored** - ✅ All projects have proper .gitignore
2. **No hardcoded secrets in C# code** - ✅ All code reads from configuration
3. **Testing configurations use templates** - ✅ No actual secrets in test files
4. **Proper configuration patterns** - ✅ Uses IConfiguration and dependency injection
5. **Separate test configurations** - ✅ Testing scenarios properly configured

### 📋 Files Scanned

#### Configuration Files ✅

- `InkStainedWretchFunctions/local.settings.json` - ✅ Properly ignored
- `InkStainedWretchStripe/local.settings.json` - ✅ Fixed (secrets removed)
- `ImageAPI/local.settings.json` - ✅ Properly ignored
- `function-app/local.settings.json` - ✅ Properly ignored
- Testing scenario files - ✅ Only contain templates

#### Source Code ✅

- All C# files scanned - ✅ No hardcoded secrets found
- Configuration classes - ✅ Proper abstraction patterns
- Service classes - ✅ Use dependency injection for config

#### Documentation ✅

- README files - ✅ No sensitive information
- Setup guides - ✅ Proper security instructions

### 🔧 Setup Required for Development

#### For InkStainedWretchStripe

```bash
cd InkStainedWretchStripe
dotnet user-secrets init
dotnet user-secrets set "STRIPE_API_KEY" "your-stripe-key"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-cosmos-key"
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"
dotnet user-secrets set "AAD_CLIENT_ID" "your-client-id"
dotnet user-secrets set "AAD_AUDIENCE" "your-client-id"

```

#### For InkStainedWretchFunctions

Use the testing scenarios or set up user secrets as documented.

### 🏭 Production Security

- ✅ Use Azure App Settings for production secrets
- ✅ Use Azure Key Vault for sensitive data
- ✅ Enable managed identity for Azure resources
- ✅ Use Azure AD authentication where applicable
- ✅ Rotate keys regularly

### 📚 Security Documentation Created

1. `InkStainedWretchStripe/USER_SECRETS_SETUP.md` - Complete setup guide
2. `TESTING_SCENARIOS_GUIDE.md` - Secure testing configurations
3. This security audit report

### 🎯 Recommendations

#### Immediate Actions ✅ COMPLETED

- [x] Remove exposed secrets from local.settings.json
- [x] Create user secrets setup documentation
- [x] Verify all secrets are properly ignored by git

#### Ongoing Best Practices

- [ ] Regular security audits (quarterly)
- [ ] Key rotation schedule (every 6 months)
- [ ] Security training for development team
- [ ] Automated secret scanning in CI/CD pipeline

### 🔍 Monitoring & Detection

Consider implementing:

- Azure Key Vault monitoring
- GitHub secret scanning alerts
- Automated security scanning in CI/CD
- Regular dependency vulnerability scans

### ✅ CONCLUSION

**The repository is now SECURE.** All exposed secrets have been removed and proper security practices are in place. Development teams can safely work with the repository using user secrets for local development and proper Azure configuration for production deployments.

**Next Steps:**

1. Team members should set up user secrets using the provided guides
2. Implement regular security audits
3. Consider additional automated security tooling

---
*This audit was performed using comprehensive file scanning and git history analysis.*

---

### Update Stripe Price Nickname Examples

### Option 1: Stripe Dashboard (Recommended for manual updates)

1. Go to [Stripe Dashboard](https://dashboard.stripe.com/)
2. Navigate to **Products** → **Prices**
3. Find your price and click on it
4. Edit the **Nickname** field
5. Save changes

### Option 2: Stripe API (Programmatic updates)

#### Using Stripe .NET SDK

```csharp
using Stripe;

public async Task<Price> UpdatePriceNicknameAsync(string priceId, string newNickname)
{
    var service = new PriceService();
    var options = new PriceUpdateOptions
    {
        Nickname = newNickname
    };
    
    return await service.UpdateAsync(priceId, options);
}

```

#### Example Usage

```csharp
// Update a price nickname
var updatedPrice = await UpdatePriceNicknameAsync("price_1234567890", "Pro Monthly Plan");

```

### Option 3: Add to Your Service

You could add a method to your existing `SubscriptionPlanService` or create a new service:

```csharp
public interface IPriceManagementService
{
    Task<bool> UpdatePriceNicknameAsync(string priceId, string newNickname);
}

public class PriceManagementService : IPriceManagementService
{
    private readonly ILogger<PriceManagementService> _logger;

    public PriceManagementService(ILogger<PriceManagementService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> UpdatePriceNicknameAsync(string priceId, string newNickname)
    {
        try
        {
            _logger.LogInformation("Updating nickname for price {PriceId} to {NewNickname}", priceId, newNickname);

            var service = new PriceService();
            var options = new PriceUpdateOptions
            {
                Nickname = newNickname
            };

            var updatedPrice = await service.UpdateAsync(priceId, options);
            
            _logger.LogInformation("Successfully updated price nickname. Price ID: {PriceId}, New Nickname: {Nickname}", 
                updatedPrice.Id, updatedPrice.Nickname);
                
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error while updating price nickname for {PriceId}: {Message}", 
                priceId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price nickname for {PriceId}", priceId);
            return false;
        }
    }
}

```

### Option 4: Azure Function Endpoint

Create an Azure Function to update price nicknames:

```csharp
[Function("UpdatePriceNickname")]
public async Task<IActionResult> UpdatePriceNickname(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = "prices/{priceId}/nickname")] HttpRequest req,
    string priceId,
    ILogger log)
{
    try
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updateRequest = JsonSerializer.Deserialize<UpdateNicknameRequest>(requestBody);

        if (string.IsNullOrWhiteSpace(updateRequest?.Nickname))
        {
            return new BadRequestObjectResult("Nickname is required");
        }

        var service = new PriceService();
        var options = new PriceUpdateOptions
        {
            Nickname = updateRequest.Nickname
        };

        var updatedPrice = await service.UpdateAsync(priceId, options);

        return new OkObjectResult(new 
        { 
            PriceId = updatedPrice.Id, 
            Nickname = updatedPrice.Nickname,
            Message = "Price nickname updated successfully" 
        });
    }
    catch (StripeException ex)
    {
        log.LogError(ex, "Stripe error updating price {PriceId}", priceId);
        return new BadRequestObjectResult($"Stripe error: {ex.Message}");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error updating price {PriceId}", priceId);
        return new StatusCodeResult(500);
    }
}

public class UpdateNicknameRequest
{
    public string Nickname { get; set; } = string.Empty;
}

```

### Important Notes

1. **Price IDs are immutable** - You can only update the nickname, not create a new price with a different ID
2. **Nicknames are optional** - Prices can exist without nicknames
3. **Your SubscriptionPlanService will automatically use updated nicknames** - The next time it fetches price data, it will get the updated nickname
4. **Caching considerations** - If you're caching price data, you may need to invalidate the cache after updating nicknames

### Testing the Update

After updating a price nickname, you can test that your `SubscriptionPlanService` picks up the change:

```csharp
[Fact]
public async Task MapToSubscriptionPlanAsync_UsesUpdatedNickname()
{
    // Arrange - simulate a PriceDto with an updated nickname
    var priceDto = new PriceDto
    {
        Id = "price_test",
        ProductId = "prod_test",
        ProductName = "Professional Plan",
        Nickname = "Updated Pro Plan", // This would be the new nickname from Stripe
        UnitAmount = 1999,
        Currency = "usd",
        Active = true,
        CreatedDate = DateTime.UtcNow
    };

    // Act
    var result = await _service.MapToSubscriptionPlanAsync(priceDto);

    // Assert
    Assert.Equal("Updated Pro Plan", result.Label); // Should use the updated nickname
}

```

The recommended approach depends on your use case:

- **Manual updates**: Use Stripe Dashboard
- **Programmatic updates**: Use Stripe API with proper error handling
- **Bulk updates**: Create a management service or Azure Function

---

### Ink Stained Wretch User Features

**Generated on November 8, 2025**

### 🎯 System Overview

The **Ink Stained Wretch Application** is a comprehensive author management and content publishing platform built on Azure cloud infrastructure. It provides authors with a complete toolkit for managing their online presence, content, and business operations through a secure, scalable, and multilingual platform.

---

### 👤 User Authentication & Identity Management

#### **Secure Authentication**

- **Microsoft Entra ID Integration**: Enterprise-grade authentication using JWT Bearer tokens
- **Single Sign-On (SSO)**: Seamless login experience across all platform features
- **User Profile Management**: Automatic user profile creation and management
- **Identity Claims**: Secure access to user identity information and claims
- **Session Management**: Persistent authentication across platform services

#### **User Identity Services**

- **WhoAmI Functionality**: View current user identity and authentication status
- **Profile Validation**: Automatic validation and synchronization of user profiles
- **Secure Token Handling**: JWT token validation and refresh capabilities

---

### 🖼️ Image Management System

#### **Image Upload & Storage**

- **Multi-Format Support**: Upload images in various formats (JPEG, PNG, WebP, etc.)
- **Azure Blob Storage Integration**: Secure, scalable cloud storage for all images
- **Tier-Based Storage Plans**: Multiple storage tiers with different limits and pricing
- **Subscription Validation**: Automatic verification of storage tier permissions
- **File Size Management**: Intelligent handling of file size limits based on subscription

#### **Image Organization & Retrieval**

- **Personal Image Library**: View all uploaded images in a centralized gallery
- **Image Metadata**: Automatic capture and storage of image properties
- **Search & Filter**: Find images quickly using various criteria
- **Bulk Operations**: Manage multiple images simultaneously

#### **Image Management Operations**

- **Secure Upload**: Protected image upload with authentication
- **Image Deletion**: Safe removal of images with proper cleanup
- **Access Control**: User-specific image access and permissions
- **Storage Analytics**: Track storage usage and tier limits

---

### 🌐 Domain Registration & Management

#### **Custom Domain Services**

- **Domain Registration**: Register custom domains for author websites
- **Domain Validation**: Comprehensive validation of domain availability and format
- **Registration Status Tracking**: Monitor domain registration progress and status
- **Contact Information Management**: Secure storage of registration contact details

#### **DNS & Infrastructure Management**

- **Automated DNS Zone Creation**: Automatic DNS configuration for registered domains
- **Azure Front Door Integration**: Global content delivery and performance optimization
- **SSL Certificate Management**: Automatic SSL certificate provisioning and renewal
- **Domain Health Monitoring**: Continuous monitoring of domain status and performance

#### **Domain Portfolio Management**

- **Multi-Domain Support**: Manage multiple domains from a single account
- **Domain History**: Track registration history and status changes
- **Renewal Management**: Automated renewal notifications and management
- **Transfer Services**: Domain transfer and migration capabilities

---

### 💳 Subscription & Payment Management

#### **Stripe Payment Integration**

- **Secure Payment Processing**: PCI-compliant payment handling through Stripe
- **Multiple Payment Methods**: Support for credit cards, bank transfers, and digital wallets
- **Recurring Billing**: Automated subscription billing and management
- **Payment Security**: Advanced fraud protection and secure tokenization

#### **Subscription Management**

- **Flexible Subscription Plans**: Multiple tiers with different features and limits
- **Plan Upgrades & Downgrades**: Seamless subscription tier changes
- **Billing History**: Complete transaction and billing history
- **Invoice Management**: Automated invoice generation and delivery
- **Payment Analytics**: Detailed payment and subscription analytics

#### **Customer Management**

- **Stripe Customer Profiles**: Integrated customer management through Stripe
- **Payment Method Management**: Add, remove, and update payment methods
- **Billing Address Management**: Secure storage and management of billing information
- **Subscription Status Tracking**: Real-time subscription status monitoring

#### **Webhook & Event Processing**

- **Real-Time Updates**: Immediate processing of payment and subscription events
- **Event Logging**: Comprehensive logging of all payment-related activities
- **Automated Responses**: Automatic handling of payment success, failure, and disputes
- **Notification System**: Automated notifications for payment and billing events

---

### 📚 Content Discovery & Integration

#### **Book Search & Discovery**

- **Penguin Random House Integration**: Search and discover books from PRH catalog
- **Author Search**: Find authors and their published works
- **Book Metadata**: Detailed book information including titles, descriptions, and publication data
- **Multi-Language Support**: Search across different language editions

#### **Amazon Product Integration**

- **Amazon Product API**: Search and retrieve product information from Amazon
- **Affiliate Marketing**: Built-in support for Amazon affiliate links
- **Product Recommendations**: Intelligent product suggestions based on author content
- **Price Tracking**: Monitor product prices and availability

#### **External Content Aggregation**

- **API Integration**: Seamless integration with external content providers
- **Data Synchronization**: Automatic synchronization of external content data
- **Content Validation**: Quality assurance for imported content
- **Metadata Management**: Structured metadata for all external content

---

### 🌍 Internationalization & Localization

#### **Multi-Language Support**

- **North American Focus**: Comprehensive support for US, Canada, and Mexico markets
- **Language Options**:

  - **English (EN)**: Primary language support
  - **Spanish (ES)**: Full Spanish localization
  - **French (FR)**: Complete French language support
  - **Arabic (AR)**: Right-to-left language support
  - **Chinese Simplified (ZH-CN)**: Simplified Chinese characters
  - **Chinese Traditional (ZH-TW)**: Traditional Chinese characters

#### **Regional Customization**

- **Country-Specific Features**: Tailored functionality for different regions
- **Currency Support**: Multi-currency support for international users
- **Date & Time Formats**: Localized date, time, and number formatting
- **Cultural Adaptations**: Region-appropriate content and interface elements

#### **Dynamic Localization**

- **Real-Time Language Switching**: Switch languages without losing session data
- **Contextual Translation**: Context-aware translations for technical terms
- **Localization API**: RESTful API for retrieving localized content
- **Translation Management**: Dynamic loading of translation resources

---

### 📍 Geographic Data Services

#### **Location Intelligence**

- **Country Data**: Comprehensive country information and metadata
- **State/Province Support**: Detailed state and province data for supported countries
- **Language-Country Associations**: Smart mapping of languages to geographic regions
- **Regional Preferences**: Location-based feature customization

#### **Address & Contact Management**

- **Address Validation**: Real-time address validation and standardization
- **International Formats**: Support for various international address formats
- **Contact Information**: Secure storage and validation of contact details
- **Geographic Search**: Location-based search and filtering capabilities

---

### 🔧 Developer & Administrative Features

#### **API Management**

- **RESTful API Design**: Well-structured REST APIs for all platform features
- **API Documentation**: Comprehensive API documentation and examples
- **Rate Limiting**: Intelligent rate limiting and throttling
- **API Versioning**: Backward-compatible API versioning strategy

#### **Data Management**

- **Data Seeding**: Automated data population for development and testing
- **Migration Tools**: Database migration and schema management
- **Backup & Recovery**: Automated backup and disaster recovery capabilities
- **Data Validation**: Comprehensive data validation and integrity checks

#### **Monitoring & Analytics**

- **Application Insights**: Real-time application performance monitoring
- **Usage Analytics**: Detailed usage statistics and user behavior analytics
- **Error Tracking**: Comprehensive error logging and tracking
- **Performance Metrics**: System performance monitoring and optimization

#### **Testing & Quality Assurance**

- **Comprehensive Test Suite**: Unit, integration, and end-to-end testing
- **Test Automation**: Automated testing pipelines and continuous integration
- **Test Data Management**: Sophisticated test data generation and management
- **Quality Metrics**: Code coverage and quality assurance metrics

---

### 🚀 Technical Infrastructure

#### **Cloud-Native Architecture**

- **Azure Functions**: Serverless compute for scalable API endpoints
- **Azure Cosmos DB**: Global-scale NoSQL database for data persistence
- **Azure Blob Storage**: Secure, scalable file storage for images and documents
- **Azure Front Door**: Global content delivery network and security

#### **Security & Compliance**

- **Enterprise Security**: Multi-layer security architecture
- **Data Encryption**: End-to-end encryption for data at rest and in transit
- **Access Control**: Role-based access control and permissions
- **Audit Logging**: Comprehensive audit trails for all user activities

#### **Performance & Scalability**

- **Auto-Scaling**: Automatic scaling based on demand
- **Global Distribution**: Multi-region deployment for optimal performance
- **Caching Strategy**: Intelligent caching for improved response times
- **Load Balancing**: Advanced load balancing and traffic distribution

#### **Integration Capabilities**

- **Third-Party APIs**: Extensive third-party service integrations
- **Webhook Support**: Real-time event processing and notifications
- **Data Export**: Comprehensive data export and migration tools
- **Custom Integrations**: Flexible architecture for custom integrations

---

### 🎯 Business Features

#### **Subscription Tiers**

- **Flexible Pricing**: Multiple subscription tiers to meet different needs
- **Feature Gating**: Tier-based feature access and limitations
- **Usage Monitoring**: Real-time tracking of resource usage
- **Billing Transparency**: Clear, detailed billing and usage reports

#### **Content Management**

- **Author Profiles**: Comprehensive author profile management
- **Content Organization**: Structured content management and organization
- **Publishing Tools**: Tools for content creation and publishing
- **Social Media Integration**: Connect and manage social media presence

#### **Business Intelligence**

- **Usage Analytics**: Detailed analytics on platform usage and engagement
- **Revenue Tracking**: Comprehensive revenue and subscription analytics
- **Performance Insights**: Insights into content performance and user engagement
- **Reporting Tools**: Customizable reports and data visualization

---

### 🔄 Integration Ecosystem

#### **External Service Integrations**

- **Stripe**: Payment processing and subscription management
- **Penguin Random House**: Book catalog and author information
- **Amazon Product API**: Product search and affiliate marketing
- **Microsoft Entra ID**: Enterprise authentication and identity management

#### **API Ecosystem**

- **RESTful APIs**: Comprehensive REST API coverage for all features
- **Webhook Support**: Real-time event processing and notifications
- **Data Synchronization**: Bi-directional data sync with external systems
- **Custom Connectors**: Framework for building custom integrations

---

### 📱 Platform Accessibility

#### **Cross-Platform Support**

- **Web-Based Platform**: Accessible through modern web browsers
- **Responsive Design**: Optimized for desktop, tablet, and mobile devices
- **API-First Architecture**: Enables development of native mobile applications
- **Progressive Web App**: PWA capabilities for app-like experience

#### **Accessibility Features**

- **WCAG Compliance**: Web Content Accessibility Guidelines compliance
- **Screen Reader Support**: Full support for assistive technologies
- **Keyboard Navigation**: Complete keyboard navigation support
- **High Contrast Mode**: Visual accessibility options

---

*This comprehensive feature list represents the full capabilities of the Ink Stained Wretch Application platform, designed to empower authors with professional-grade tools for managing their digital presence and business operations.*

---

## 📖 Migration & Guides

### Migration Guide Entra Id Roles

This guide explains how to migrate from Cosmos DB-based ImageStorageTierMembership to Microsoft Entra ID App Roles for image storage tier management.

### Overview

The image storage tier system has been refactored to use Microsoft Entra ID App Roles instead of Cosmos DB memberships. This provides several benefits:

- **Centralized Identity Management**: Tier assignments are managed in Azure AD alongside user identities
- **JWT Token Integration**: Tier information is included directly in JWT tokens, eliminating database lookups
- **Better Security**: Role-based access control (RBAC) through standard Azure AD mechanisms
- **Simplified Architecture**: Reduced database complexity by separating tier assignment from usage tracking

### Architecture Changes

#### Before

```
User Authentication → Cosmos DB Lookup (ImageStorageTierMembership) → Tier Determination
                                    ↓
                            Usage Tracking (Same Entity)

```

#### After

```
User Authentication → JWT Roles Claim → Tier Determination
                                    ↓
                    Cosmos DB Lookup (ImageStorageUsage) → Usage Tracking Only

```

### Migration Steps

#### Step 1: Prerequisites

Ensure you have:

- Azure AD application with appropriate permissions:

  - `Application.ReadWrite.All` (to create/update app roles)
  - `AppRoleAssignment.ReadWrite.All` (to assign users to roles)

- Service Principal credentials (Client ID, Client Secret, Tenant ID)
- Cosmos DB access credentials
- Backup of existing ImageStorageTierMembership data

#### Step 2: Run the EntraIdRoleManager

The EntraIdRoleManager console application performs the migration:

1. Configure environment variables or user secrets:

   ```bash

   dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "<your-endpoint>"
   dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "<your-key>"
   dotnet user-secrets set "COSMOSDB_DATABASE_ID" "<your-database>"
   dotnet user-secrets set "AAD_TENANT_ID" "<your-tenant-id>"
   dotnet user-secrets set "AAD_CLIENT_ID" "<your-client-id>"
   dotnet user-secrets set "AAD_CLIENT_SECRET" "<your-client-secret>"

   ```

2. Run the migration:

   ```bash

   cd EntraIdRoleManager
   dotnet run

   ```

3. The tool will:

   - Read all ImageStorageTiers from Cosmos DB
   - Create app roles in Azure AD (e.g., `ImageStorageTier.Starter`, `ImageStorageTier.Pro`)
   - Read all ImageStorageTierMemberships
   - Assign users to corresponding app roles
   - Skip existing assignments (idempotent)

#### Step 3: Verify App Roles in Azure Portal

1. Navigate to Azure Portal → Azure Active Directory → App registrations
2. Find your application
3. Go to "App roles" section
4. Verify roles are created with format: `ImageStorageTier.{TierName}`
5. Check "Enterprise applications" → your app → "Users and groups" to verify assignments

#### Step 4: Update Token Configuration (if needed)

Ensure your Azure AD app registration includes roles in the token:

1. Navigate to Token configuration
2. Add optional claim "roles" if not already present
3. Ensure "roles" claim is included in both ID and Access tokens

#### Step 5: Deploy Updated ImageAPI

The ImageAPI has been updated to:

- Accept user ClaimsPrincipal to read roles from JWT
- Use ImageStorageTierService to determine tier from roles
- Automatically assign Starter (or lowest cost) tier to users without roles
- Track usage separately in ImageStorageUsage entity

Deploy the updated ImageAPI code to your environment.

#### Step 6: Migrate Usage Data

If you need to migrate existing usage data from ImageStorageTierMembership to ImageStorageUsage:

Create a simple migration script:

```csharp
var memberships = await membershipRepository.GetAllAsync(); // You'll need to implement GetAllAsync
foreach (var membership in memberships)
{
    var usage = new ImageStorageUsage
    {
        id = membership.UserProfileId,
        UserProfileId = membership.UserProfileId,
        StorageUsedInBytes = membership.StorageUsedInBytes,
        BandwidthUsedInBytes = membership.BandwidthUsedInBytes,
        LastUpdated = DateTime.UtcNow
    };
    await usageRepository.AddAsync(usage);
}

```

#### Step 7: Test the Migration

1. **Test Role Assignment**:

   - Authenticate as a migrated user
   - Check JWT token contains `roles` claim with `ImageStorageTier.{TierName}`
   - Verify image upload works with role-based tier

2. **Test Default Tier Assignment**:

   - Authenticate as a new user (no role assigned)
   - Attempt image upload
   - Verify user is automatically assigned to Starter tier
   - Check logs for tier assignment

3. **Test Usage Tracking**:

   - Upload an image
   - Verify ImageStorageUsage is created/updated
   - Check storage and bandwidth values are tracked correctly

#### Step 8: Monitoring and Verification

Monitor the following:

- Application logs for tier determination
- Usage tracking updates in Cosmos DB
- Any errors related to missing roles or tier configuration

#### Step 9: Cleanup (Optional)

After confirming everything works:

1. Keep ImageStorageTierMembership data for historical reference
2. Consider archiving old membership records
3. Update documentation to reflect new architecture

### Rollback Procedure

If you need to rollback:

1. Redeploy previous version of ImageAPI
2. App roles in Azure AD are harmless and can remain
3. Remove assignments if needed via Azure Portal or PowerShell

### Automatic Tier Assignment Logic

When a user doesn't have an `ImageStorageTier.*` role:

1. System checks for "Starter" tier by name
2. If found, user is automatically assigned this tier for the request
3. If not found, system assigns lowest cost tier
4. No database write occurs - assignment is request-scoped only

**Note**: Automatic tier assignment does NOT update Azure AD. It only affects the current request. You should create a separate process to assign actual roles to new users.

### New User Onboarding

For new users, you have two options:

#### Option 1: Automatic Default Assignment (Recommended)

Configure a process to automatically assign new users to the Starter role when they register:

```csharp
// In your user registration flow
var graphClient = new GraphServiceClient(credential);
var roleAssignment = new AppRoleAssignment
{
    PrincipalId = Guid.Parse(userId),
    ResourceId = Guid.Parse(servicePrincipalId),
    AppRoleId = starterRoleId
};
await graphClient.ServicePrincipals[servicePrincipalId].AppRoleAssignedTo.PostAsync(roleAssignment);

```

#### Option 2: Request-Scoped Default

Let the ImageStorageTierService handle defaults automatically. Users without roles will use Starter tier but won't have persistent role assignment.

### Troubleshooting

#### User has no tier

**Symptom**: Log shows "User has no tier role, assigning default tier"
**Solution**: This is expected for new users. Ensure Starter tier exists in Cosmos DB.

#### Role not found in database

**Symptom**: "User has role ImageStorageTier.X but tier not found in database"
**Solution**: Ensure tier names in Azure AD match exactly with Cosmos DB tier names.

#### Usage not tracked

**Symptom**: ImageStorageUsage not created or updated
**Solution**: Check Cosmos DB container exists. Verify ImageStorageUsagesContainerManager is registered.

#### Migration fails with permission errors

**Symptom**: "Insufficient privileges to complete the operation"
**Solution**: Verify service principal has required permissions in Azure AD.

### Benefits of New Architecture

1. **Performance**: No database lookup for tier determination - read from JWT
2. **Scalability**: Reduced Cosmos DB queries
3. **Security**: Centralized role management through Azure AD
4. **Auditability**: Role assignments are tracked in Azure AD audit logs
5. **Consistency**: Single source of truth for user roles

### Support

For issues or questions:

1. Check application logs for detailed error messages
2. Verify Azure AD app role configuration
3. Ensure JWT tokens include roles claim
4. Review Cosmos DB for usage tracking data

---

### Testing Scenarios Guide

### Your 3 Testing Scenarios

#### Scenario 1: Frontend-Safe Testing (Cost: $0.00)

**Purpose**: End-to-end UI testing without creating domains or modifying infrastructure

**To activate this scenario, set these values in `local.settings.json`:**

```json
{
  "TESTING_MODE": "true",
  "MOCK_AZURE_INFRASTRUCTURE": "true",
  "MOCK_GOOGLE_DOMAINS": "true", 
  "MOCK_STRIPE_PAYMENTS": "true",
  "MOCK_EXTERNAL_APIS": "true",
  "SKIP_DOMAIN_PURCHASE": "true",
  "STRIPE_TEST_MODE": "true",
  "MAX_TEST_COST_LIMIT": "0.00",
  "TEST_SCENARIO": "frontend-safe",
  "TEST_DOMAIN_SUFFIX": "test-frontend.local"
}

```

#### Scenario 2: Individual Function Testing (Cost: $0.50-$5.00)

**Purpose**: Test individual Azure Functions that modify Azure infrastructure

**To activate this scenario, set these values in `local.settings.json`:**

```json
{
  "TESTING_MODE": "true",
  "MOCK_AZURE_INFRASTRUCTURE": "false",
  "MOCK_GOOGLE_DOMAINS": "true",
  "MOCK_STRIPE_PAYMENTS": "true", 
  "MOCK_EXTERNAL_APIS": "false",
  "SKIP_DOMAIN_PURCHASE": "true",
  "STRIPE_TEST_MODE": "true",
  "MAX_TEST_COST_LIMIT": "5.00",
  "TEST_SCENARIO": "individual-testing",
  "TEST_DOMAIN_SUFFIX": "test-individual.local"
}

```

#### Scenario 3: Production Testing (Cost: $12-$50+)

**Purpose**: Full end-to-end test with real money, real domains, real infrastructure

**To activate this scenario, set these values in `local.settings.json`:**

```json
{
  "TESTING_MODE": "true",
  "MOCK_AZURE_INFRASTRUCTURE": "false",
  "MOCK_GOOGLE_DOMAINS": "false",
  "MOCK_STRIPE_PAYMENTS": "false",
  "MOCK_EXTERNAL_APIS": "false", 
  "SKIP_DOMAIN_PURCHASE": "false",
  "STRIPE_TEST_MODE": "false",
  "MAX_TEST_COST_LIMIT": "50.00",
  "TEST_SCENARIO": "production-test",
  "TEST_DOMAIN_SUFFIX": "test-production.com"
}

```

### 🚀 How to Run Each Scenario

1. **Update local.settings.json** with the scenario configuration above
2. **Set up your secrets** using dotnet user-secrets (for sensitive values)
3. **Start the Functions app**:

   ```bash

   cd InkStainedWretchFunctions
   func start

   ```

4. **Test your endpoints** according to the scenario

### 📊 What Each Scenario Tests

#### Scenario 1 (Frontend-Safe)

- ✅ UI/UX flows
- ✅ API response structures
- ✅ Error handling
- ❌ No real external API calls
- ❌ No infrastructure changes
- ❌ No costs incurred

#### Scenario 2 (Individual Functions)

- ✅ Real Azure operations (DNS zones, Front Door)
- ✅ External API integrations (Amazon, Penguin Random House)
- ✅ Infrastructure modifications
- ❌ No domain purchases
- ❌ No real Stripe charges
- 💰 Minimal Azure costs ($0.50-$5.00)

#### Scenario 3 (Production)

- ✅ Everything real
- ✅ Domain purchases
- ✅ Real Stripe transactions
- ✅ Complete end-to-end flow
- 💰 Significant costs ($12-$50+)

### ⚠️ Important Notes

- **Always verify** `MAX_TEST_COST_LIMIT` before running
- **Scenario 3** requires live Stripe keys and real payment methods
- **Monitor costs** during testing
- **Use descriptive** `TEST_DOMAIN_SUFFIX` values to identify test domains

### 🔧 Quick Configuration Script

Save this as `switch-scenario.ps1` to quickly switch between scenarios:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("1", "2", "3", "frontend", "individual", "production")]
    [string]$Scenario
)

$configPath = "local.settings.json"
$config = Get-Content $configPath | ConvertFrom-Json

switch ($Scenario) {
    {$_ -in "1", "frontend"} {
        $config.Values.TESTING_MODE = "true"
        $config.Values.MOCK_AZURE_INFRASTRUCTURE = "true"
        $config.Values.MOCK_GOOGLE_DOMAINS = "true"
        $config.Values.MOCK_STRIPE_PAYMENTS = "true"
        $config.Values.MOCK_EXTERNAL_APIS = "true"
        $config.Values.SKIP_DOMAIN_PURCHASE = "true"
        $config.Values.STRIPE_TEST_MODE = "true"
        $config.Values.MAX_TEST_COST_LIMIT = "0.00"
        $config.Values.TEST_SCENARIO = "frontend-safe"
        $config.Values.TEST_DOMAIN_SUFFIX = "test-frontend.local"
        Write-Host "✅ Configured for Scenario 1: Frontend-Safe Testing" -ForegroundColor Green
    }
    {$_ -in "2", "individual"} {
        $config.Values.TESTING_MODE = "true"
        $config.Values.MOCK_AZURE_INFRASTRUCTURE = "false"
        $config.Values.MOCK_GOOGLE_DOMAINS = "true"
        $config.Values.MOCK_STRIPE_PAYMENTS = "true"
        $config.Values.MOCK_EXTERNAL_APIS = "false"
        $config.Values.SKIP_DOMAIN_PURCHASE = "true"
        $config.Values.STRIPE_TEST_MODE = "true"
        $config.Values.MAX_TEST_COST_LIMIT = "5.00"
        $config.Values.TEST_SCENARIO = "individual-testing"
        $config.Values.TEST_DOMAIN_SUFFIX = "test-individual.local"
        Write-Host "✅ Configured for Scenario 2: Individual Function Testing" -ForegroundColor Yellow
    }
    {$_ -in "3", "production"} {
        $config.Values.TESTING_MODE = "true"
        $config.Values.MOCK_AZURE_INFRASTRUCTURE = "false"
        $config.Values.MOCK_GOOGLE_DOMAINS = "false"
        $config.Values.MOCK_STRIPE_PAYMENTS = "false"
        $config.Values.MOCK_EXTERNAL_APIS = "false"
        $config.Values.SKIP_DOMAIN_PURCHASE = "false"
        $config.Values.STRIPE_TEST_MODE = "false"
        $config.Values.MAX_TEST_COST_LIMIT = "50.00"
        $config.Values.TEST_SCENARIO = "production-test"
        $config.Values.TEST_DOMAIN_SUFFIX = "test-production.com"
        Write-Host "⚠️  Configured for Scenario 3: Production Testing (REAL MONEY)" -ForegroundColor Red
    }
}

$config | ConvertTo-Json -Depth 10 | Out-File $configPath -Encoding UTF8
Write-Host "Configuration updated in $configPath" -ForegroundColor Cyan

```

### Usage

```powershell
## Switch to Scenario 1
.\switch-scenario.ps1 -Scenario 1

## Switch to Scenario 2  
.\switch-scenario.ps1 -Scenario individual

## Switch to Scenario 3
.\switch-scenario.ps1 -Scenario production

```

---

### Step By Step Cleanup

### ⚠️ CRITICAL: Complete this process ASAP to remove exposed secrets

#### Step 1: Prepare for History Cleanup

1. **Create a backup** (in case anything goes wrong):

   ```powershell

   git clone --mirror . ../backup-$(Get-Date -Format 'yyyy-MM-dd-HHmmss')

   ```

2. **Commit any pending changes**:

   ```powershell

   git add .
   git commit -m "Prepare for secret cleanup"

   ```

#### Step 2: Use BFG Repo-Cleaner (RECOMMENDED)

**Option A: Download BFG**

- Download `bfg.jar` from: <https://rtyley.github.io/bfg-repo-cleaner/>
- Requires Java (install if needed)

**Option B: Use Package Manager**

```powershell
## Windows (Scoop)
scoop install bfg

## macOS (Homebrew)  
brew install bfg

```

#### Step 3: Create Secret Replacements File

Create `replacements.txt` in your repository root:

```
your-cosmos-primary-key==>***REMOVED***
your-cosmos-secondary-key==>***REMOVED***
your-amazon-access-key==>***REMOVED***
your-amazon-secret-key==>***REMOVED***
your-azure-tenant-id==>***REMOVED***
your-app-client-id==>***REMOVED***
your-google-project-id==>***REMOVED***

```

#### Step 4: Clean Repository History

```powershell
## Create a fresh mirror clone for cleaning
git clone --mirror https://github.com/utdcometsoccer/one-page-author-page-api.git temp-clean.git

## Run BFG to clean secrets
java -jar bfg.jar --replace-text replacements.txt temp-clean.git

## Clean up Git objects  
cd temp-clean.git
git reflog expire --expire=now --all
git gc --prune=now --aggressive

## Force push cleaned history
git push --force

```

#### Step 5: Update Your Local Repository

```powershell
## Delete your current local copy
cd ..
Remove-Item -Recurse -Force one-page-author-page-api

## Re-clone the cleaned repository
git clone https://github.com/utdcometsoccer/one-page-author-page-api.git
cd one-page-author-page-api

```

#### Step 6: Set Up User Secrets for Development

```powershell
cd InkStainedWretchFunctions
dotnet user-secrets init

## Add your actual secrets (get from team lead or Azure portal):
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-actual-cosmos-key"
dotnet user-secrets set "COSMOSDB_CONNECTION_STRING" "your-actual-cosmos-connection"  
dotnet user-secrets set "CosmosDBConnection" "your-actual-cosmos-connection"
dotnet user-secrets set "PENGUIN_RANDOM_HOUSE_API_KEY" "your-actual-prh-key"
dotnet user-secrets set "AMAZON_PRODUCT_ACCESS_KEY" "your-actual-amazon-key"
dotnet user-secrets set "AMAZON_PRODUCT_SECRET_KEY" "your-actual-amazon-secret"
dotnet user-secrets set "AAD_TENANT_ID" "your-actual-tenant-id"
dotnet user-secrets set "AAD_CLIENT_ID" "your-actual-client-id" 
dotnet user-secrets set "GOOGLE_CLOUD_PROJECT_ID" "your-actual-project-id"

```

#### Step 7: Test Your Setup

```powershell
## Build and test the application
dotnet build
dotnet run  # or func start

```

#### Step 8: Notify Team Members

Send this message to all team members:

> **🚨 URGENT: Repository History Cleaned**
>
> The Git history has been cleaned to remove exposed secrets.
>
> **YOU MUST:**
>
> 1. Delete your local clone
> 2. Re-clone from GitHub
> 3. Set up user secrets (see instructions in repository)
> 4. DO NOT push old branches or commits
>
> **DO NOT commit secrets to the repository again!**

#### Step 9: Clean Up Temporary Files

```powershell
## Remove temporary files
Remove-Item replacements.txt -Force
Remove-Item -Recurse -Force temp-clean.git

```

### ✅ Verification

After completing these steps:

1. **Check history is clean**: `git log --oneline --all | grep -i secret` (should return nothing)
2. **Verify app works**: Test all functions locally
3. **Confirm secrets are in user-secrets**: `dotnet user-secrets list`

### 🚨 If Something Goes Wrong

1. Restore from backup: Copy from the backup directory you created
2. Contact team lead for help
3. Review Azure portal for production secret values

### Production Deployment

- Use Azure App Settings for production secrets
- Configure in Azure Portal > Function App > Configuration
- Never store production secrets in code or user-secrets

---

**EXECUTE THIS PROCESS IMMEDIATELY to secure your repository!**

---

## 📖 API & System Documentation

### Api Documentation

### Overview

This REST API provides access to a list of author objects for a given domain. Authentication is handled via Microsoft Entra ID (Azure AD) using OAuth 2.0. Only authenticated users with the appropriate permissions can access the endpoints.

### Authentication

- **Protocol:** OAuth 2.0 (OpenID Connect)
- **Provider:** Microsoft Entra ID (Azure AD)
- **Flow:** Authorization Code or Client Credentials
- **Scopes:** `api://<your-api-client-id>/Author.Read`

### Endpoints

#### GET /api/authors/{secondLevelDomain}/{topLevelDomain}

Returns a list of author objects for the specified domain.

##### Request

```http
GET /api/authors/{secondLevelDomain}/{topLevelDomain}
Authorization: Bearer <access_token>

```

##### Parameters

- `secondLevelDomain` (path): The second-level domain name (e.g., "example" from "example.com")
- `topLevelDomain` (path): The top-level domain (e.g., "com" from "example.com")

##### Response

**Success (200 OK)**

```json
[
  {
    "id": "string",
    "AuthorName": "string",
    "LanguageName": "string", 
    "RegionName": "string",
    "EmailAddress": "string",
    "WelcomeText": "string",
    "AboutText": "string", 
    "HeadShotURL": "string",
    "CopyrightText": "string",
    "TopLevelDomain": "string",
    "SecondLevelDomain": "string",
    "Articles": [
      {
        "Title": "string",
        "Date": "yyyy-MM-dd",
        "Publication": "string",
        "Url": "string"
      }
    ],
    "Books": [
      {
        "Title": "string", 
        "Description": "string",
        "Url": "string",
        "Cover": "string"
      }
    ],
    "Socials": [
      {
        "Name": "string",
        "Url": "string"
      }
    ]
  }
]

```

### Error Codes

- `401 Unauthorized`: Invalid or missing access token
- `403 Forbidden`: Insufficient permissions (missing Author.Read scope)
- `404 Not Found`: Domain not found
- `500 Internal Server Error`: Unexpected error

### Example Usage

#### Using TypeScript/JavaScript

See `src/services/fetchAuthorsByDomain.ts` for a complete example including:

- Microsoft Authentication Library (MSAL) setup
- Token acquisition for server-to-server calls
- Error handling
- TypeScript interfaces

#### Using cURL

```bash
curl -X GET "https://your-function-app.azurewebsites.net/api/authors/example/com" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json"

```

#### Using PowerShell

```powershell
$headers = @{
    "Authorization" = "Bearer YOUR_ACCESS_TOKEN"
    "Content-Type" = "application/json"
}

$response = Invoke-RestMethod -Uri "https://your-function-app.azurewebsites.net/api/authors/example/com" -Method Get -Headers $headers
$response | ConvertTo-Json -Depth 10

```

### Security Requirements

- All requests must use HTTPS
- Access tokens must be obtained from Microsoft Entra ID
- Do not expose client secrets in client-side code
- Tokens should be stored securely and refreshed as needed

### Configuration

The API requires the following Azure AD configuration:

- **Tenant ID**: Your Azure AD tenant identifier
- **Client ID**: Application ID registered in Azure AD
- **API Scope**: `api://your-api-client-id/Author.Read`
- **Authority**: `<https://login.microsoftonline.com/{tenant-id}/v2.0`>

### Rate Limiting

Standard Azure Functions rate limiting applies. Consider implementing client-side retry logic with exponential backoff.

### Support

For questions or support, contact the API administrator.

---

### Api Documentation

*Generated on 2025-10-13 14:59:21 UTC*

This comprehensive API documentation covers all Azure Functions and endpoints available in the OnePageAuthor system.

### Table of Contents

- [Authentication](#authentication)

### Authentication

All API endpoints require authentication using JWT Bearer tokens. Include the token in the Authorization header:

```http
Authorization: Bearer <your-jwt-token>
```

#### TypeScript Authentication Helper

```typescript
class ApiClient {
  private baseUrl: string;
  private token: string;

  constructor(baseUrl: string, token: string) {
    this.baseUrl = baseUrl.replace(/\/$/, '');
    this.token = token;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = ${'$'}{this.baseUrl}{endpoint};

    const response = await fetch(url, {
      ...options,
      headers: {
        'Authorization': Bearer {this.token},
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    if (response.status === 401) {
      throw new Error('Authentication failed - token may be expired');
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Unknown error' }));
      throw new Error(error.error || HTTP {response.status}: {response.statusText});
    }

    return response.json();
  }

  public get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' });
  }

  public post<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  public delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }
}
`

### Azure Functions API

The following Azure Functions provide the core API endpoints for the OnePageAuthor system:

#### function-app

Main application functions for author data and localization services

##### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

###### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

##### FunctionExecutorAutoStartup

System.Xml.XmlElement

###### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

##### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

###### String)

---

###### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

##### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

###### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

##### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

###### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

#### ImageAPI

Image management API for uploading, retrieving, and deleting user images

##### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

###### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

##### FunctionExecutorAutoStartup

System.Xml.XmlElement

###### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

##### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

###### String)

---

###### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

##### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

###### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

##### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

###### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

#### InkStainedWretchFunctions

Core application functions for domain registration and external API integration

##### CreateDnsZoneFunction

Cosmos DB trigger function that creates DNS zones when domain registrations are added or modified. Uses a unique lease collection to avoid conflicts with other triggers on the same container.

###### DomainRegistration})

**Description:** Triggered when documents are inserted or updated in the DomainRegistrations container. Creates Azure DNS zones for newly registered domains.

**Parameters:**

- `input`: List of domain registrations that were added or modified

---

##### DomainRegistrationFunction

HTTP endpoint to create and manage domain registrations.

###### CreateDomainRegistrationRequest)

**Description:** Creates a new domain registration for the authenticated user.

**Parameters:**

- `req`: HTTP request containing the domain registration data
- `payload`: Domain registration request payload with domain details

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

###### HttpRequest)

**Description:** Gets all domain registrations for the authenticated user.

**Parameters:**

- `req`: HTTP request (no additional parameters required)

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

###### String)

**Description:** Gets a specific domain registration by ID for the authenticated user.

**Parameters:**

- `req`: HTTP request
- `registrationId`: The registration ID from the route

**Returns:** Domain registration or error response

---

###### DomainRegistration})

**Description:** Processes changes to domain registrations and triggers infrastructure provisioning.

**Parameters:**

- `input`: List of changed domain registrations from Cosmos DB

---

##### DomainRegistrationTriggerFunction

Azure Function triggered by changes to the DomainRegistrations Cosmos DB container. Processes new domain registrations and adds them to Azure Front Door if they don't already exist.

###### DomainRegistration})

**Description:** Processes changes to domain registrations and adds new domains to Azure Front Door.

**Parameters:**

- `input`: List of changed domain registrations from Cosmos DB

---

##### PenguinRandomHouseFunction

Azure Function for calling Penguin Random House API

###### String)

**Description:** Searches for authors by name and returns the unmodified JSON response from Penguin Random House API.

**Parameters:**

- `req`: HTTP request with authentication
- `authorName`: Author name from route parameter to search for

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

###### String)

**Description:** Gets titles by author key and returns the unmodified JSON response from Penguin Random House API.

**Parameters:**

- `req`: HTTP request with authentication
- `authorKey`: Author key from route parameter (obtained from search results)

**Returns:** System.Xml.XmlElement

System.Xml.XmlElement

---

##### GetStateProvinces

Azure Function for retrieving StateProvince data by culture.

###### String)

**Description:** Gets states and provinces by culture code.

**Parameters:**

- `req`: The HTTP request.
- `culture`: The culture code (e.g., "en-US", "fr-CA", "es-MX").

**Returns:** List of StateProvince entities for the specified culture.

---

###### String)

**Description:** Gets states and provinces by country code and culture.

**Parameters:**

- `req`: The HTTP request.
- `countryCode`: The two-letter country code (e.g., "US", "CA", "MX").
- `culture`: The culture code (e.g., "en-US", "fr-CA", "es-MX").

**Returns:** List of StateProvince entities for the specified country and culture.

---

##### GetStateProvincesByCountry

Azure Function for retrieving StateProvince data by country and culture.

###### String)

**Description:** Gets states and provinces by country code and culture.

**Parameters:**

- `req`: The HTTP request.
- `countryCode`: The two-letter country code (e.g., "US", "CA", "MX").
- `culture`: The culture code (e.g., "en-US", "fr-CA", "es-MX").

**Returns:** List of StateProvince entities for the specified country and culture.

---

##### LocalizedText

System.Xml.XmlElement

###### ILocalizationTextProvider)

**Description:** System.Xml.XmlElement

**Parameters:**

- `logger`: Logger instance.
- `provider`: Localization text provider service.

---

###### String)

**Description:** Handles HTTP GET requests for localized text.

**Parameters:**

- `req`: The incoming HTTP request.
- `culture`: Route parameter representing the culture (e.g. en-US).

**Returns:** 200 with JSON payload of localized text; 400 if culture is invalid or retrieval fails.

---

##### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

###### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

##### FunctionExecutorAutoStartup

System.Xml.XmlElement

###### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

##### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

###### String)

---

###### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

##### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

###### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

##### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

###### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

#### InkStainedWretchStripe

Stripe payment processing functions for subscription management and billing

##### FunctionExecutorHostBuilderExtensions

System.Xml.XmlElement

###### IHostBuilder)

**Description:** Configures an optimized function executor to the invocation pipeline.

---

##### FunctionExecutorAutoStartup

System.Xml.XmlElement

###### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

##### GeneratedFunctionMetadataProvider

System.Xml.XmlElement

###### String)

---

###### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

##### WorkerHostBuilderFunctionMetadataProviderExtension

System.Xml.XmlElement

###### IHostBuilder)

**Description:** Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

---

##### FunctionMetadataProviderAutoStartup

System.Xml.XmlElement

###### IHostBuilder)

**Description:** System.Xml.XmlElement

**Parameters:**

- `hostBuilder`: The instance to use for service registration.

---

### Testing & Validation

The following projects provide comprehensive testing coverage:

#### OnePageAuthor.Test

Unit and integration tests for the OnePageAuthor application

### Error Handling

All API endpoints return consistent error responses:

`json
{
  "error": "Descriptive error message"
}
`

#### Common HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Invalid or missing token |
| 402 | Payment Required - Subscription limit exceeded |
| 403 | Forbidden - Access denied |
| 404 | Not Found - Resource not found |
| 409 | Conflict - Resource already exists |
| 500 | Internal Server Error |
| 507 | Insufficient Storage - Storage quota exceeded |

#### TypeScript Error Handling

```typescript
interface ApiError {
  error: string;
  details?: any;
}

class ApiException extends Error {
  public statusCode: number;
  public apiError: ApiError;

  constructor(statusCode: number, apiError: ApiError) {
    super(apiError.error);
    this.statusCode = statusCode;
    this.apiError = apiError;
  }
}

// Usage in async functions
try {
  const result = await apiClient.get('/api/images/user');
} catch (error) {
  if (error instanceof ApiException) {
    switch (error.statusCode) {
      case 401:
        // Redirect to login
        window.location.href = '/login';
        break;
      case 403:
        // Show upgrade prompt
        showUpgradePrompt();
        break;
      default:
        // Show general error
        showErrorMessage(error.message);
    }
  }
}
`

### Rate Limiting

API endpoints may be rate-limited based on subscription tier:

- **Starter**: 100 requests/minute
- **Pro**: 1000 requests/minute
- **Elite**: 10000 requests/minute

Rate limit headers are included in responses:

`
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1640995200
`

---

*This documentation is automatically generated from source code XML comments. Last updated: 2025-10-13 14:59:21 UTC*

---

### Complete System Documentation

*Generated on 2025-09-29 14:04:52 UTC*

This comprehensive documentation covers all components, APIs, and utilities in the OnePageAuthor system.

### System Overview

The OnePageAuthor system consists of multiple Azure Functions, core libraries, and utility applications that work together to provide a complete author management and content publishing platform.

### Architecture Components

#### Azure Functions (API Layer)

- **ImageAPI**: Image upload, management and retrieval
- **InkStainedWretchFunctions**: Domain registration and external integrations
- **InkStainedWretchStripe**: Payment processing and subscription management
- **function-app**: Core author data and localization services

#### Libraries (Business Logic Layer)

- **OnePageAuthorLib**: Core business logic, entities, and data access services

#### Utilities (Data Management Layer)

- **SeedAPIData**: API data initialization
- **SeedImageStorageTiers**: Storage tier configuration
- **SeedInkStainedWretchesLocale**: Comprehensive localization for all containers (North America: US, CA, MX in EN, ES, FR, AR, ZH-CN, ZH-TW)

#### Testing (Quality Assurance Layer)

- **OnePageAuthor.Test**: Unit and integration tests
- **IntegrationTestAuthorDataService**: Author data service validation

### Authentication

All API endpoints require JWT Bearer token authentication:

`
Authorization: Bearer <your-jwt-token>
`

### Project Details

#### Azure Functions

##### ImageAPI

Image management API for uploading, retrieving, and deleting user images

**Functions:**

- `Delete`: Azure Function for deleting an image by its ID. Uses the ImageDeleteService for business logic.

  - `HttpRequest)`: Deletes a user's image by ID.

- `Upload`: Azure Function for uploading image files to Azure Blob Storage. Uses the ImageUploadService for business logic and validation.

  - `HttpRequest)`: Uploads an image file to Azure Blob Storage with subscription tier validation.

- `User`: Azure Function for retrieving all images uploaded by the authenticated user. Uses the UserImageService for business logic.

  - `HttpRequest)`: Retrieves all images uploaded by the authenticated user.

- `WhoAmI`: Azure Function for retrieving information about the authenticated user. Returns user identity and claims information from JWT token.

  - `HttpRequest)`: Returns information about the authenticated user from JWT token claims.

- `FunctionExecutorHostBuilderExtensions`: System.Xml.XmlElement

  - `IHostBuilder)`: Configures an optimized function executor to the invocation pipeline.

- `FunctionExecutorAutoStartup`: System.Xml.XmlElement

  - `IHostBuilder)`: System.Xml.XmlElement

- `GeneratedFunctionMetadataProvider`: System.Xml.XmlElement

  - `String)`
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

- `WorkerHostBuilderFunctionMetadataProviderExtension`: System.Xml.XmlElement

  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

- `FunctionMetadataProviderAutoStartup`: System.Xml.XmlElement

  - `IHostBuilder)`: System.Xml.XmlElement

##### InkStainedWretchFunctions

Core application functions for domain registration and external API integration

**Functions:**

- `DomainRegistrationFunction`: HTTP endpoint to create and manage domain registrations.

  - `CreateDomainRegistrationRequest)`: Creates a new domain registration for the authenticated user.
  - `HttpRequest)`: Gets all domain registrations for the authenticated user.
  - `String)`: Gets a specific domain registration by ID for the authenticated user.

- `PenguinRandomHouseFunction`: Azure Function for calling Penguin Random House API

  - `String)`: Searches for authors by name and returns the unmodified JSON response from Penguin Random House API.
  - `String)`: Gets titles by author key and returns the unmodified JSON response from Penguin Random House API.

- `LocalizedText`: System.Xml.XmlElement

  - `ILocalizationTextProvider)`: System.Xml.XmlElement
  - `String)`: Handles HTTP GET requests for localized text.

- `FunctionExecutorHostBuilderExtensions`: System.Xml.XmlElement

  - `IHostBuilder)`: Configures an optimized function executor to the invocation pipeline.

- `FunctionExecutorAutoStartup`: System.Xml.XmlElement

  - `IHostBuilder)`: System.Xml.XmlElement

- `GeneratedFunctionMetadataProvider`: System.Xml.XmlElement

  - `String)`
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

- `WorkerHostBuilderFunctionMetadataProviderExtension`: System.Xml.XmlElement

  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

- `FunctionMetadataProviderAutoStartup`: System.Xml.XmlElement

  - `IHostBuilder)`: System.Xml.XmlElement

##### InkStainedWretchStripe

Stripe payment processing functions for subscription management and billing

**Functions:**

- `CreateStripeCustomer`: Azure Function for creating a Stripe customer. Handles HTTP POST requests, validates the incoming payload, and delegates customer creation logic.
- `FunctionExecutorHostBuilderExtensions`: System.Xml.XmlElement

  - `IHostBuilder)`: Configures an optimized function executor to the invocation pipeline.

- `FunctionExecutorAutoStartup`: System.Xml.XmlElement

  - `IHostBuilder)`: System.Xml.XmlElement

- `GeneratedFunctionMetadataProvider`: System.Xml.XmlElement

  - `String)`
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

- `WorkerHostBuilderFunctionMetadataProviderExtension`: System.Xml.XmlElement

  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

- `FunctionMetadataProviderAutoStartup`: System.Xml.XmlElement

  - `IHostBuilder)`: System.Xml.XmlElement

##### function-app

Main application functions for author data and localization services

**Functions:**

- `FunctionExecutorHostBuilderExtensions`: System.Xml.XmlElement

  - `IHostBuilder)`: Configures an optimized function executor to the invocation pipeline.

- `FunctionExecutorAutoStartup`: System.Xml.XmlElement

  - `IHostBuilder)`: System.Xml.XmlElement

- `GeneratedFunctionMetadataProvider`: System.Xml.XmlElement

  - `String)`
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

- `WorkerHostBuilderFunctionMetadataProviderExtension`: System.Xml.XmlElement

  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.

- `FunctionMetadataProviderAutoStartup`: System.Xml.XmlElement

  - `IHostBuilder)`: System.Xml.XmlElement

#### Libraries

##### OnePageAuthorLib

Core library containing business logic, entities, and data services

**Key Components:**

- `Article`: Represents an article authored by the author.
- `AuthorResponse`: Represents the response containing author data for the One Page Author API.
- `Book`: Represents a book authored by the author.
- `DomainRegistrationService`: Service for managing domain registration operations for authenticated users.
- `IImageDeleteService`: Service interface for handling image deletion operations.
- `IImageUploadService`: Service interface for handling image upload operations.
- `ImageDeleteService`: Service for handling image deletion operations.
- `ImageUploadService`: Service for handling image upload operations with tier-based validation.
- `IUserImageService`: Service interface for handling user image operations.
- `ImageUploadResult`: Result of an image upload operation.
- `UserImagesResult`: Result of retrieving user images.
- `ImageDeleteResult`: Result of an image deletion operation.
- `ServiceResult`: Base class for service operation results.
- `UploadImageResponse`: Response model for successful image upload.
- `UserImageResponse`: Response model for user image list item.
- `UserImageService`: Service for handling user image retrieval operations.
- `LocaleResponse`: Represents localized strings for the One Page Author API UI.
- `SocialLink`: Represents a social media link for the author.
- `UserProfileService`: Service for ensuring user profiles exist for authenticated users.
- `IAuthorRepository`: Interface for AuthorRepository, supports querying by domain and locale properties.
- `IContainerManager`1`: Generic interface for Cosmos DB container managers.
- `ICosmosDatabaseManager`: Public interface for CosmosDatabaseManager.
- `ICreateStripeCustomer`: Defines a contract for creating Stripe customers from a request payload.
- `IGenericRepository`1`: Generic repository interface for entities with Guid-based id and authorId.
- `ILocaleDataService`: Provides methods for retrieving locale data from the data source.
- `ILocaleRepository`: Interface for LocaleRepository, supports querying by id and other properties.
- `IUserProfileService`: Interface for user profile management services.
- `JwtAuthenticationHelper`: Helper class for JWT authentication in Azure Functions
- `JwtDebugHelper`: Utility class for debugging JWT tokens
- `TokenValidationTest`: Simple test class to verify token validation logic without requiring a full test framework
- `Article`: Represents an article and its associated metadata for the One Page Author API.
- `AuthGuard`: Represents authentication guard information for UI display when user authentication is required
- `LocalizationText`: Represents all localized text for the author management UI, matching the structure of inkstainedwretch.language-country.json.
- `LocalizationTextProvider`: System.Xml.XmlElement
- `PenguinRandomHouseAuthorDetail`: Represents localized text strings for the Penguin Random House author detail interface. This class contains UI text and field labels that can be localized for different cultures/languages. Used when displaying detailed information about a specific author from Penguin Random House. Inherits from AuthorManagementBase to include Culture and id properties for localization support.
- `PenguinRandomHouseAuthorList`: Represents localized text strings for the Penguin Random House author list interface. This class contains UI text that can be localized for different cultures/languages. Inherits from AuthorManagementBase to include Culture and id properties for localization support.
- `Book`: Represents a book and its associated metadata for the One Page Author API.
- `DomainRegistration`: Represents a domain registration request entity stored in Cosmos DB.
- `Domain`: Represents domain information for registration.
- `ContactInformation`: Represents contact information for domain registration.
- `DomainRegistrationStatus`: Status of a domain registration request.
- `CreateDomainRegistrationRequest`: Data transfer object for creating a domain registration request.
- `DomainDto`: Data transfer object for domain information.
- `ContactInformationDto`: Data transfer object for contact information.
- `DomainRegistrationResponse`: Data transfer object for domain registration response.
- `CreateDomainRegistrationRequest`: Data transfer object for creating a domain registration request.
- `DomainDto`: Data transfer object for domain information.
- `ContactInformationDto`: Data transfer object for contact information.
- `DomainRegistrationResponse`: Data transfer object for domain registration response.
- `Image`: Represents an uploaded image file stored in Azure Blob Storage.
- `ImageStorageTier`: Represents an image storage plan/tier with a friendly name and a monthly cost.
- `ImageStorageTierMembership`: Represents a user's membership/assignment to an ImageStorageTier.
- `Locale`: Entity representing a locale, inherits from LocaleResponse and adds an id property.
- `Social`: Represents a social profile or link associated with an author for the One Page Author API.
- `UserProfile`: Represents the authenticated user's identity details and linkage to Stripe.
- `ILocalizationTextProvider`: Abstraction for retrieving localized author management UI text objects aggregated across multiple Cosmos DB containers for a given culture.
- `IDomainRegistrationRepository`: Interface for domain registration repository operations.
- `IDomainRegistrationService`: Interface for domain registration service operations.
- `ArticlesContainerManager`: Manages the Cosmos DB container for Article entities.
- `AuthorRepository`: Repository for Author entities, supports querying by domain and locale properties.
- `AuthorsContainerManager`: Manages the Cosmos DB container for Author entities.
- `BooksContainerManager`: Manages the Cosmos DB container for Book entities.
- `CosmosDatabaseManager`: Manages creation and access to an Azure Cosmos NoSQL database.
- `DomainRegistrationRepository`: Repository for DomainRegistration with partition key Upn.
- `DomainRegistrationsContainerManager`: Manages the Cosmos DB container for DomainRegistration entities.
- `GenericRepository`1`: Generic repository for entities with Guid-based id and authorId, converting to string as necessary.
- `ImagesContainerManager`: Manages the Cosmos DB container for Image entities.
- `ImageStorageTierMembershipsContainerManager`: Manages the Cosmos DB container for ImageStorageTierMembership entities.
- `ImageStorageTiersContainerManager`: Manages the Cosmos DB container for ImageStorageTier entities.
- `LocaleRepository`: Repository for Locale entities, supports querying by id.
- `LocalesContainerManager`: Manages the Cosmos DB container for Locale entities.
- `SocialsContainerManager`: Manages the Cosmos DB container for Social entities.
- `UserProfileRepository`: Repository for UserProfile with partition key Upn.
- `UserProfilesContainerManager`: Manages the Cosmos DB container for UserProfile entities.
- `ServiceFactory`: Provides factory methods for dependency injection and repository/service creation for the OnePageAuthorAPI.
- `IPenguinRandomHouseConfig`: Configuration interface for Penguin Random House API settings
- `IPenguinRandomHouseService`: Service interface for interacting with Penguin Random House API
- `PenguinRandomHouseConfig`: Configuration implementation for Penguin Random House API settings Reads settings from local configuration (appsettings.json, local.settings.json, environment variables)
- `PenguinRandomHouseService`: Service implementation for calling Penguin Random House API
- `CreateCustomer`: Basic implementation that builds an initialized response object from the request.
- `ListSubscriptions`: Service for listing Stripe subscriptions following Stripe's sample patterns.
- `SubscriptionMappers`: Mapping helpers from Stripe entities to our DTOs. Kept simple for unit testing.
- `IStripeCheckoutSessionService`: Abstraction for creating Stripe Checkout Sessions.
- `IStripePriceServiceWrapper`: System.Xml.XmlElement
- `IListSubscriptions`: Abstraction for listing Stripe subscriptions.
- `CancelSubscriptionRequest`: Optional settings when cancelling a subscription.
- `CancelSubscriptionResponse`: Result of cancelling a subscription.
- `CreateCheckoutSessionRequest`: Represents the payload for creating a Stripe Checkout Session.
- `CreateCheckoutSessionResponse`: Represents the response payload after creating a Stripe Checkout Session.
- `CreateCustomerRequest`: Represents the payload for creating a Stripe customer.
- `CreateCustomerResponse`: Represents the result of a request to create a Stripe customer.
- `CreateSubscriptionRequest`: Represents the payload for creating a subscription from a Stripe price.
- `GetCheckoutSessionResponse`: Represents details of a Stripe Checkout Session for retrieval endpoints.
- `SubscriptionCreateResponse`: Response for creating a subscription, returning identifiers needed by the client.
- `SubscriptionListResponse`: Lightweight DTO wrapper for Stripe subscriptions that is easier for clients to consume and includes pagination helpers.
- `SubscriptionDto`: Simplified subscription model exposing commonly used fields.
- `SubscriptionItemDto`: Simplified representation of a subscription item (price/product + quantity).
- `SubscriptionPlan`: Represents a subscription plan that serializes to the structure in sample-subscription-plan.json.
- `SubscriptionPlanListResponse`: A response model analogous to StripePriceListResponse but containing SubscriptionPlan items.
- `SubscriptionsResponse`: Represents the result of a request to list Stripe subscriptions.

#### Utilities & Tools

##### SeedAPIData

Data seeding utility for populating API with initial data

##### SeedImageStorageTiers

Utility for seeding image storage tier configurations

##### SeedInkStainedWretchesLocale

Comprehensive, idempotent localization seeding utility for all UI components and containers. Supports North American countries (US, CA, MX) in multiple languages: English (EN), Spanish (ES), French (FR), Arabic (AR), Simplified Chinese (ZH-CN), and Traditional Chinese (ZH-TW). Features automatic container creation, duplicate detection, and support for both standard (en-us) and extended (zh-cn-us) locale codes.

#### Testing Projects

##### OnePageAuthor.Test

Unit and integration tests for the OnePageAuthor application

##### IntegrationTestAuthorDataService

Integration testing utility for author data service validation

### Development Information

#### Build Configuration

All projects are configured to automatically generate XML documentation during Debug builds.

#### Documentation Generation

This documentation is automatically generated from source code XML comments and can be regenerated using:
`
.\Scripts\Generate-ApiDocumentation.ps1
`

#### Project Statistics

- **Total Projects**: 11
- **Azure Functions**: 4
- **Libraries**: 4
- **Utilities**: 4
- **Test Projects**: 2
- **Documented Members**: 298

---

*Last updated: 2025-09-29 14:04:52 UTC*
*Generated from: OnePageAuthor API Documentation System*

---

### Localizationreadme

### Overview

This document explains how localized UI text for the Ink Stained Wretch author management experience is structured, seeded, retrieved, and served through the Azure Function endpoint.

### Data Shape

Each culture-specific JSON file (e.g. `inkstainedwretch.en-us.json`) contains top-level objects that map one-to-one with Cosmos DB containers and C# POCOs located in `OnePageAuthorLib/entities/authormanagement`.

Example excerpt:

```json
{
  "AuthorRegistration": { "authorListTitle": "Author Information", ... },
  "LoginRegister": { "loginHeader": { "title": "Login" } },
  "ThankYou": { "title": "Thank You", "message": "..." },
  "Navbar": { "brand": "Ink Stained Wretches", ... }
}

```

### Containers & Partitioning

Each top-level section is stored in its own Cosmos DB container. The partition key for all author-management containers is `/Culture` to allow fast retrieval of the full localized set by culture.

| Container | POCO | Partition Key |
|-----------|------|---------------|
| AuthorRegistration | `AuthorRegistration` | `/Culture` |
| LoginRegister | `LoginRegister` | `/Culture` |
| ThankYou | `ThankYou` | `/Culture` |
| Navbar | `Navbar` | `/Culture` |
| DomainRegistration | `DomainRegistration` | `/Culture` |
| ErrorPage | `ErrorPage` | `/Culture` |
| ImageManager | `ImageManager` | `/Culture` |
| Checkout | `Checkout` | `/Culture` |
| BookList | `BookList` | `/Culture` |
| BookForm | `BookForm` | `/Culture` |
| ArticleForm | `ArticleForm` | `/Culture` |

(Extend this table if additional POCOs are added.)

### Seeding Process

1. The seeding project enumerates `data/` and matches files with pattern: `inkstainedwretch.{language}-{country}.json`.
2. For each file:

   - Extract culture (e.g. `en-us` becomes `en-US` canonical form when needed).
   - Ensure each container exists (if missing create with `/Culture`).
   - Insert or upsert each object with its `Culture` property populated.

3. Result: All containers now contain exactly one document per culture (or more if versioning is introduced later).

### Aggregation Model

`LocalizationText` aggregates all author-management POCOs into a single object for convenience. This allows API consumers to fetch an entire culture’s localized text in one call.

### Provider Abstraction

`ILocalizationTextProvider` defines:

```csharp
Task<LocalizationText> GetLocalizationTextAsync(string culture);

```

Implementation: `LocalizationTextProvider`

- Validates culture via `CultureInfo.GetCultureInfo`.
- Queries each container by partition key.
- Fallback chain: exact culture -> first culture with matching language prefix (e.g. request `en-GB`, use `en-US` if present) -> neutral language (`en`) -> empty placeholder.
- Always returns a non-null object; placeholder carries requested culture if no data found.

#### Fallback Logic

Resolution order for a request like `en-GB`:

1. Exact match: `en-GB`
2. Any other `en-XX` variant (first one encountered, deterministic by Cosmos query paging)
3. Neutral language: `en`
4. Empty typed object (all default strings) with `Culture` = `en-GB`

The returned object’s `Culture` property is normalized to the originally requested specific culture even when data is sourced from a different language-region variant.

### Azure Function Endpoint

`LocalizedText` function (HTTP GET):

```
GET /api/localizedtext/{culture}

```

Response: `200 OK` with `LocalizationText` JSON or `400 Bad Request` if invalid culture.

#### Sample Request

```
GET https://localhost:7071/api/localizedtext/en-US

```

#### Sample (truncated) Response

```json
{
  "authorRegistration": { "authorListTitle": "Author Information", ... },
  "loginRegister": { "loginHeader_title": "Login", ... },
  "thankYou": { "title": "Thank You", "message": "Thank you for your purchase!" },
  "navbar": { "brand": "Ink Stained Wretches", ... }
}

```

### Dependency Injection

Register services via `AddInkStainedWretchServices` (after registering a `CosmosClient` + `Database`):

```csharp
services.AddInkStainedWretchServices();

```

This registers:

- `ILocalizationTextProvider` -> `LocalizationTextProvider`
- All `IContainerManager<T>` for each POCO

### Extending Localization

1. Add new JSON section to culture files.
2. Create corresponding POCO inheriting `AuthorManagementBase`.
3. Register new container manager + DI mapping.
4. Add property to `LocalizationText` and retrieval line in `LocalizationTextProvider`.

### Error Handling

- Invalid culture -> `ArgumentException` surfaced as 400.
- Missing container item -> returns empty object (never null) for resilience.

### Future Enhancements

- Caching layer (Memory / Distributed) per culture.
- Versioning or last-modified metadata.
- Batch query optimization using transactional batch (if writes grouped).
- Additional hierarchical fallback (e.g. regional -> neutral -> default) if global default is introduced.

---
Maintainer Notes: Keep JSON schema changes synchronized across POCOs, seeding logic, and provider aggregation.

---

### Development Scripts

This directory contains PowerShell scripts to streamline development workflow for the One Page Author API solution.

### Scripts Overview

#### `UpdateAndRun.ps1` - Main Development Script

Comprehensive script that updates packages, builds the solution, and runs Azure Functions projects.

**Usage:**

```powershell
.\Scripts\UpdateAndRun.ps1 [-SkipUpdate] [-SkipBuild] [-Help]

```

**Parameters:**

- `-SkipUpdate` - Skip the dotnet-update step
- `-SkipBuild` - Skip the build step (restore will still run)
- `-Help` - Show detailed help information

**What it does:**

1. ✅ **Updates NuGet packages** using `dotnet-update -u` across the entire solution
2. ✅ **Restores packages** using `dotnet restore`
3. ✅ **Builds solution** using `dotnet build --no-restore`
4. ✅ **Starts Azure Functions** as background jobs on dedicated ports:

   - **ImageAPI** → `http://localhost:7000`
   - **InkStainedWretchFunctions** → `http://localhost:7001`
   - **InkStainedWretchStripe** → `http://localhost:7002`

#### `StopFunctions.ps1` - Cleanup Script

Stops all running Azure Functions background jobs and cleans up completed jobs.

**Usage:**

```powershell
.\Scripts\StopFunctions.ps1 [-Help]

```

**What it does:**

1. ✅ **Identifies running functions** - finds background jobs for ImageAPI, InkStainedWretchFunctions, and InkStainedWretchStripe
2. ✅ **Stops all function jobs** gracefully
3. ✅ **Removes completed jobs** to clean up the job queue
4. ✅ **Reports status** of remaining background jobs

### Prerequisites

#### Required Tools

- **PowerShell 5.1+** or **PowerShell Core 7+**
- **.NET SDK 10.0** or later
- **Azure Functions Core Tools v4** (`func`)
- **dotnet-update tool** (automatically installed if missing)

#### Install Prerequisites

```powershell
## Install Azure Functions Core Tools (if needed)
npm install -g azure-functions-core-tools@4 --unsafe-perm true

## dotnet-update tool will be auto-installed by the script
## Or install manually:
dotnet tool install --global dotnet-update

```

### Typical Development Workflow

#### 🚀 Start Development Session

```powershell
## Full update and run (recommended daily)
.\Scripts\UpdateAndRun.ps1

## Quick start (skip package updates)
.\Scripts\UpdateAndRun.ps1 -SkipUpdate

## Build and run only (skip updates and initial build)
.\Scripts\UpdateAndRun.ps1 -SkipUpdate -SkipBuild

```

#### 🔧 During Development

```powershell
## Check function status
Get-Job

## View function logs
Receive-Job -Name "ImageAPI" -Keep
Receive-Job -Name "InkStainedWretchFunctions" -Keep
Receive-Job -Name "InkStainedWretchStripe" -Keep

## Test endpoints
Invoke-RestMethod http://localhost:7000/api/health
Invoke-RestMethod http://localhost:7001/api/health  
Invoke-RestMethod http://localhost:7002/api/health

```

#### 🛑 End Development Session

```powershell
## Stop all functions cleanly
.\Scripts\StopFunctions.ps1

## Or manually stop specific functions
Stop-Job -Name "ImageAPI"
Stop-Job -Name "InkStainedWretchFunctions" 
Stop-Job -Name "InkStainedWretchStripe"

```

### Job Management Commands

#### View Jobs

```powershell
## List all background jobs
Get-Job

## View detailed job information
Get-Job | Format-List

## Check job output (keep output for multiple views)
Receive-Job -Id <JobId> -Keep

## View live output (removes output after viewing)
Receive-Job -Id <JobId>

```

#### Control Jobs

```powershell
## Stop specific job
Stop-Job -Id <JobId>
Stop-Job -Name "ImageAPI"

## Stop all function jobs at once
Get-Job | Where-Object { @('ImageAPI', 'InkStainedWretchFunctions', 'InkStainedWretchStripe') -contains $_.Name } | Stop-Job

## Remove completed jobs
Get-Job | Remove-Job

## Force remove all jobs (stopped and running)
Get-Job | Remove-Job -Force

```

### Function Endpoints

Once started, the Azure Functions will be available at:

| Function | URL | Purpose |
|----------|-----|---------|
| **ImageAPI** | `http://localhost:7000` | Image upload, processing, and management |
| **InkStainedWretchFunctions** | `http://localhost:7001` | Author data, StateProvinces, domain registration |
| **InkStainedWretchStripe** | `http://localhost:7002` | Stripe payment processing |

#### Common API Endpoints

```powershell
## StateProvince endpoints (InkStainedWretchFunctions)
GET http://localhost:7001/api/stateprovinces/en-US
GET http://localhost:7001/api/stateprovinces/US/en-US

## Author endpoints (InkStainedWretchFunctions)  
GET http://localhost:7001/api/authors/example/com

## Image endpoints (ImageAPI)
POST http://localhost:7000/api/upload
GET http://localhost:7000/api/images/{id}

## Stripe endpoints (InkStainedWretchStripe)
POST http://localhost:7002/api/CreateStripeCustomer
POST http://localhost:7002/api/CreateSubscription

```

### Troubleshooting

#### Common Issues

**❌ "dotnet-update command not found"**

```powershell
## Install the tool manually
dotnet tool install --global dotnet-update

```

**❌ "func command not found"**

```powershell
## Install Azure Functions Core Tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true

```

**❌ Functions fail to start**

- Check that ports 7000-7002 are not in use
- Verify Azure Functions Core Tools is installed
- Check function app configuration and dependencies

**❌ Build failures**

- Run `dotnet clean` before trying again
- Check for package compatibility issues
- Verify .NET SDK version compatibility

#### Port Conflicts

If ports 7000-7002 are in use, you can manually start functions on different ports:

```powershell
## Start on custom ports
func start --port 8000  # In ImageAPI directory
func start --port 8001  # In InkStainedWretchFunctions directory  
func start --port 8002  # In InkStainedWretchStripe directory

```

#### Performance Tips

- Use `-SkipUpdate` flag when packages don't need updating (faster startup)
- Use `-SkipBuild` when only restarting functions after minor changes
- Monitor job output with `Receive-Job -Keep` to preserve logs for debugging

### Script Customization

The scripts can be modified to suit your development preferences:

- **Change default ports** by modifying the `$Port = 7000 + $PortCounter` line
- **Add more projects** by updating the `$Projects` array
- **Customize build options** by modifying the `dotnet build` command
- **Add pre/post build steps** by inserting commands in the appropriate sections

---

## 📖 Documentation

All detailed documentation has been organized in the [`docs/`](docs/) folder. For a complete navigation guide, see the **[Documentation Index](docs/INDEX.md)**.

### Quick Links

- [**Documentation Index**](docs/INDEX.md) - Complete navigation guide to all documentation
- [**API Reference**](docs/API-Documentation.md) - Comprehensive API documentation
- [**Deployment Guide**](docs/DEPLOYMENT_GUIDE.md) - Complete deployment workflow
- [**GitHub Secrets Reference**](docs/GITHUB_SECRETS_REFERENCE.md) - Secrets configuration guide
- [**Development Scripts**](docs/DEVELOPMENT_SCRIPTS.md) - Development automation

### Core Documentation

| Document | Description |
|----------|-------------|
| [CONTRIBUTING.md](docs/CONTRIBUTING.md) | Guidelines for contributing to the project |
| [CODE_OF_CONDUCT.md](docs/CODE_OF_CONDUCT.md) | Community standards and expectations |
| [SECURITY.md](docs/SECURITY.md) | Security policies and vulnerability reporting |

### API Documentation

| Document | Description |
|----------|-------------|
| [API-Documentation.md](docs/API-Documentation.md) | Comprehensive API reference |
| [Complete-System-Documentation.md](docs/Complete-System-Documentation.md) | Full system overview |
| [LocalizationREADME.md](docs/LocalizationREADME.md) | Internationalization guide |
| [WIKIPEDIA_API.md](docs/WIKIPEDIA_API.md) | Wikipedia API integration |

### Implementation Guides

| Document | Description |
|----------|-------------|
| [IMPLEMENTATION_SUMMARY.md](docs/IMPLEMENTATION_SUMMARY.md) | DNS zone automation |
| [IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md](docs/IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) | Entra ID integration |
| [IMPLEMENTATION_SUMMARY_LANGUAGES.md](docs/IMPLEMENTATION_SUMMARY_LANGUAGES.md) | Language support |
| [IMPLEMENTATION_SUMMARY_MULTI_FUNCTION_DEPLOYMENT.md](docs/IMPLEMENTATION_SUMMARY_MULTI_FUNCTION_DEPLOYMENT.md) | Multi-function deployment |
| [IMPLEMENTATION_SUMMARY_COSMOS_APPINSIGHTS.md](docs/IMPLEMENTATION_SUMMARY_COSMOS_APPINSIGHTS.md) | Monitoring implementation |
| [IMPLEMENTATION_SUMMARY_CONDITIONAL_ENV_VARS.md](docs/IMPLEMENTATION_SUMMARY_CONDITIONAL_ENV_VARS.md) | Conditional configuration |
| [IMPLEMENTATION_SUMMARY_PERMISSIONS_FIX.md](docs/IMPLEMENTATION_SUMMARY_PERMISSIONS_FIX.md) | Permissions fixes |
| [COUNTRIES_IMPLEMENTATION_SUMMARY.md](docs/COUNTRIES_IMPLEMENTATION_SUMMARY.md) | Country data implementation |
| [STATEPROVINCE_BOILERPLATE_SUMMARY.md](docs/STATEPROVINCE_BOILERPLATE_SUMMARY.md) | Geographic data |
| [KEY_VAULT_IMPLEMENTATION_SUMMARY.md](docs/KEY_VAULT_IMPLEMENTATION_SUMMARY.md) | Key Vault implementation |
| [KEY_VAULT_ROLE_ASSIGNMENT_IMPLEMENTATION.md](docs/KEY_VAULT_ROLE_ASSIGNMENT_IMPLEMENTATION.md) | Key Vault RBAC |
| [AUTHOR_INVITATION_IMPLEMENTATION_SUMMARY.md](docs/AUTHOR_INVITATION_IMPLEMENTATION_SUMMARY.md) | Author invitation system |
| [AUTHOR_INVITATION_SYSTEM.md](docs/AUTHOR_INVITATION_SYSTEM.md) | Invitation system overview |
| [INK_STAINED_WRETCH_USER_FEATURES.md](docs/INK_STAINED_WRETCH_USER_FEATURES.md) | User features documentation |

### Enhancement Documentation

| Document | Description |
|----------|-------------|
| [CULTURE_SUPPORT_ENHANCEMENT.md](docs/CULTURE_SUPPORT_ENHANCEMENT.md) | Culture/localization features |
| [ACTIVE_PRODUCTS_FILTER_ENHANCEMENT.md](docs/ACTIVE_PRODUCTS_FILTER_ENHANCEMENT.md) | Product filtering |
| [LABEL_VALIDATION_ENHANCEMENT.md](docs/LABEL_VALIDATION_ENHANCEMENT.md) | Validation improvements |
| [SUBSCRIPTION_PLAN_SERVICE_REFACTORING.md](docs/SUBSCRIPTION_PLAN_SERVICE_REFACTORING.md) | Service refactoring |
| [REFACTORING_SUMMARY.md](docs/REFACTORING_SUMMARY.md) | Refactoring documentation |

### Deployment & Infrastructure

| Document | Description |
|----------|-------------|
| [DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md) | Complete deployment workflow |
| [DEPLOYMENT_ARCHITECTURE.md](docs/DEPLOYMENT_ARCHITECTURE.md) | Infrastructure architecture |
| [GITHUB_SECRETS_REFERENCE.md](docs/GITHUB_SECRETS_REFERENCE.md) | GitHub Secrets configuration guide |
| [GITHUB_ACTIONS_UPDATE.md](docs/GITHUB_ACTIONS_UPDATE.md) | CI/CD pipeline updates |
| [COSMOS_APPINSIGHTS_DEPLOYMENT.md](docs/COSMOS_APPINSIGHTS_DEPLOYMENT.md) | Monitoring deployment |
| [QUICKSTART_COSMOS_APPINSIGHTS.md](docs/QUICKSTART_COSMOS_APPINSIGHTS.md) | Quick start guide |

### Configuration & Setup

| Document | Description |
|----------|-------------|
| [ConfigurationValidation.md](docs/ConfigurationValidation.md) | Configuration validation guide |
| [ConfigurationMaskingStandardization.md](docs/ConfigurationMaskingStandardization.md) | Config masking standards |
| [LOCAL_SETTINGS_SETUP.md](docs/LOCAL_SETTINGS_SETUP.md) | Local development setup |
| [AZURE_STORAGE_EMULATOR_SETUP.md](docs/AZURE_STORAGE_EMULATOR_SETUP.md) | Azure Storage emulator setup |
| [AZURE_COMMUNICATION_SERVICES_SETUP.md](docs/AZURE_COMMUNICATION_SERVICES_SETUP.md) | Email services setup |
| [DOTNET_10_UPGRADE.md](docs/DOTNET_10_UPGRADE.md) | .NET 10 upgrade guide |

### Migration & Testing

| Document | Description |
|----------|-------------|
| [MIGRATION_GUIDE_ENTRA_ID_ROLES.md](docs/MIGRATION_GUIDE_ENTRA_ID_ROLES.md) | Entra ID migration guide |
| [KEY_VAULT_MIGRATION_GUIDE.md](docs/KEY_VAULT_MIGRATION_GUIDE.md) | Key Vault migration |
| [TESTING_SCENARIOS_GUIDE.md](docs/TESTING_SCENARIOS_GUIDE.md) | Testing scenarios |
| [SECURITY_AUDIT_REPORT.md](docs/SECURITY_AUDIT_REPORT.md) | Security audit findings |

### Development & Utilities

| Document | Description |
|----------|-------------|
| [DEVELOPMENT_SCRIPTS.md](docs/DEVELOPMENT_SCRIPTS.md) | Development automation scripts |
| [README-Documentation.md](docs/README-Documentation.md) | Documentation overview |
| [FIND_PARTNER_TAG.md](docs/FIND_PARTNER_TAG.md) | Amazon Partner Tag guide |
| [UPDATE_STRIPE_PRICE_NICKNAME_EXAMPLES.md](docs/UPDATE_STRIPE_PRICE_NICKNAME_EXAMPLES.md) | Stripe price configuration |
| [STRIPE_PAYMENT_DASHBOARD.md](docs/STRIPE_PAYMENT_DASHBOARD.md) | Payment dashboard guide |
| [QUICK_START_INVITATION_TOOL.md](docs/QUICK_START_INVITATION_TOOL.md) | Invitation tool quick start |
| [SERVICE_PRINCIPAL_PERMISSIONS_FIX.md](docs/SERVICE_PRINCIPAL_PERMISSIONS_FIX.md) | Service principal permissions |
| [STEP_BY_STEP_CLEANUP.md](docs/STEP_BY_STEP_CLEANUP.md) | Cleanup procedures |

---

## 🔗 External References

The following external resources are referenced throughout this project:

### Azure Services

| Resource | URL | Description |
|----------|-----|-------------|
| Azure Portal | <https://portal.azure.com> | Azure management portal |
| Azure Functions Docs | <https://docs.microsoft.com/en-us/azure/azure-functions/> | Azure Functions documentation |
| Azure Cosmos DB Trigger | <https://learn.microsoft.com/azure/azure-functions/functions-bindings-cosmosdb-v2-trigger> | Cosmos DB trigger bindings |
| Azure Managed Identity | <https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview> | Managed identity documentation |

### Stripe

| Resource | URL | Description |
|----------|-----|-------------|
| Stripe Dashboard | <https://dashboard.stripe.com> | Stripe management dashboard |

### Amazon Services

| Resource | URL | Description |
|----------|-----|-------------|
| Amazon Associates | <https://affiliate-program.amazon.com> | Amazon affiliate program |
| Amazon Associates Help | <https://affiliate-program.amazon.com/help/> | Affiliate program documentation |
| PA API Documentation | <https://webservices.amazon.com/paapi5/documentation/> | Product Advertising API docs |
| Amazon Developer Portal | <https://developer.amazon.com/apps-and-games/services/paapi> | Developer services |
| AWS Console | <https://console.aws.amazon.com> | AWS management console |

### Development Tools

| Resource | URL | Description |
|----------|-----|-------------|
| .NET Downloads | <https://dotnet.microsoft.com/download> | .NET SDK downloads |
| Penguin Random House API | <https://developer.penguinrandomhouse.com> | PRH developer portal |
| BFG Repo Cleaner | <https://rtyley.github.io/bfg-repo-cleaner/> | Git history cleaning tool |

### Project Links

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Repository | <https://github.com/utdcometsoccer/one-page-author-page-api> | Source code repository |
| CI/CD Pipeline | <https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml> | Build status |

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
