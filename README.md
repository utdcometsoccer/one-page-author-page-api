# OnePageAuthor API Platform

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A comprehensive .NET 9 platform providing APIs and utilities for author management, content publishing, and subscription services. Built with Azure Functions (isolated worker), Azure Cosmos DB for persistence, Stripe for billing, and Microsoft Entra ID for authentication.

## 🚀 Platform Overview

### Key Features
- **.NET 9** Azure Functions with isolated worker runtime
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
- **SeedLocales** — Multi-language localization data seeding
- **SeedInkStainedWretchesLocale** — Application-specific UI text localization
- **SeedImageStorageTiers** — Image storage tier configuration
- **OnePageAuthor.DataSeeder** — StateProvince and geographical data seeding

#### 🧪 Testing & Quality Assurance
- **OnePageAuthor.Test** — Comprehensive unit and integration tests
- **IntegrationTestAuthorDataService** — Author data service validation testing

## 🛠️ Prerequisites & Setup

### System Requirements
- **.NET SDK 9.0** or later
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

## ⚙️ Configuration

### Core Environment Variables
| Variable | Description | Required |
|----------|-------------|----------|
| `COSMOSDB_ENDPOINT_URI` | Azure Cosmos DB endpoint URI | ✅ Yes |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary access key | ✅ Yes |
| `COSMOSDB_DATABASE_ID` | Database name (e.g., "OnePageAuthor") | ✅ Yes |
| `STRIPE_API_KEY` | Stripe secret API key | ✅ Yes |
| `AAD_TENANT_ID` | Microsoft Entra ID tenant GUID | ✅ Yes |
| `AAD_AUDIENCE` | API application/client ID | ✅ Yes |
| `STRIPE_WEBHOOK_SECRET` | Webhook endpoint secret for verification | For webhooks |

### External API Integration (Optional)
<details>
<summary>🐧 Penguin Random House API Configuration</summary>

| Variable | Description |
|----------|-------------|
| `PENGUIN_RANDOM_HOUSE_API_URL` | Base API URL |
| `PENGUIN_RANDOM_HOUSE_API_KEY` | Authentication key |
| `PENGUIN_RANDOM_HOUSE_API_DOMAIN` | API domain (e.g., "PRH.US") |
| `PENGUIN_RANDOM_HOUSE_SEARCH_API` | Search endpoint template |
| `PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API` | Author titles endpoint |
</details>

<details>
<summary>📚 Amazon Product Advertising API Configuration</summary>

| Variable | Description |
|----------|-------------|
| `AMAZON_PRODUCT_ACCESS_KEY` | AWS Access Key ID |
| `AMAZON_PRODUCT_SECRET_KEY` | AWS Secret Access Key |
| `AMAZON_PRODUCT_PARTNER_TAG` | Associates Partner Tag |
| `AMAZON_PRODUCT_REGION` | AWS region (e.g., "us-east-1") |
| `AMAZON_PRODUCT_MARKETPLACE` | Marketplace domain |
</details>

### Example Configuration (local.settings.json)
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "COSMOSDB_ENDPOINT_URI": "https://localhost:8081/",
    "COSMOSDB_PRIMARY_KEY": "<your-cosmos-db-key>",
    "COSMOSDB_DATABASE_ID": "OnePageAuthor",
    "STRIPE_API_KEY": "<your-stripe-secret-key>",
    "AAD_TENANT_ID": "<your-tenant-id>",
    "AAD_AUDIENCE": "<your-client-id>",
    "STRIPE_WEBHOOK_SECRET": "<your-webhook-secret>"
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
- See [`SECURITY.md`](SECURITY.md) for vulnerability reporting

## 📊 Data Management & Seeding

### Available Seeders
- **SeedAPIData** — Author profiles, books, articles, and relationships
- **SeedLocales** — Multi-language localization data (EN, ES, FR, DE, PT, IT)
- **SeedInkStainedWretchesLocale** — Application-specific UI text
- **SeedImageStorageTiers** — Image storage configuration
- **OnePageAuthor.DataSeeder** — StateProvince geographic data

### Running Seeders
```bash
# Seed author and content data
cd SeedAPIData && dotnet run

# Initialize localization data
cd SeedLocales && dotnet run

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
- **Features**: Azure Front Door integration, multi-language support, external API proxying

#### 💳 InkStainedWretchStripe
**Purpose**: Stripe payment processing and subscription management
- `POST /api/CreateStripeCheckoutSession` — Create secure checkout sessions
- `POST /api/CreateStripeCustomer` — Customer creation and management
- `POST /api/CreateSubscription` — Subscription lifecycle management
- `POST /api/WebHook` — Stripe webhook event processing
- `GET /api/ListSubscription/{customerId}` — Subscription queries with filtering

#### 📚 function-app
**Purpose**: Core author data and additional infrastructure functions
- Author profile management
- Content publishing workflows
- System health monitoring

### Authentication
All endpoints require JWT Bearer authentication:
```http
Authorization: Bearer <your-jwt-token>
```

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
| **SeedLocales** | Localization data | Multi-language UI text, cultural settings |
| **SeedInkStainedWretchesLocale** | App-specific text | Custom UI components, messages |
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

### Azure Resources Required
- **Azure Functions Apps** (v4, .NET 9 isolated)
- **Azure Cosmos DB** account with containers
- **Azure Storage** account for function app storage
- **Microsoft Entra ID** app registration
- **Azure Front Door** profile for domain management
- **Application Insights** for monitoring and logging

### Deployment Checklist
- [ ] Configure all required environment variables
- [ ] Set up managed identity for Azure resource access
- [ ] Configure CORS policies for frontend integration
- [ ] Set up Application Insights monitoring
- [ ] Configure Stripe webhook endpoints
- [ ] Validate JWT token configuration
- [ ] Run smoke tests on deployed endpoints

## 🤝 Contributing & Community

### Development Workflow
1. Read [`CONTRIBUTING.md`](CONTRIBUTING.md) for guidelines
2. Follow [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md) for community standards
3. Review [`SECURITY.md`](SECURITY.md) for security considerations
4. Check existing issues and PRs before creating new ones

### Getting Help
- **Documentation**: Check project-specific README files for detailed information
- **Issues**: Use GitHub Issues for bug reports and feature requests
- **Security**: Follow responsible disclosure via [`SECURITY.md`](SECURITY.md)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 📚 Additional Documentation

- [`Complete-System-Documentation.md`](Complete-System-Documentation.md) — Comprehensive system overview
- [`API-Documentation.md`](API-Documentation.md) — Detailed API reference and examples  
- [`STATEPROVINCE_BOILERPLATE_SUMMARY.md`](STATEPROVINCE_BOILERPLATE_SUMMARY.md) — Geographic data implementation
- [`LocalizationREADME.md`](LocalizationREADME.md) — Internationalization guide
- [`DEVELOPMENT_SCRIPTS.md`](DEVELOPMENT_SCRIPTS.md) — Development automation scripts

*For project-specific documentation, see individual README files in each project directory.*
