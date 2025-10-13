# One Page Author API

A .NET 9 solution providing APIs and utilities for the One Page Author platform. It uses Azure Functions (isolated worker) with ASP.NET Core middleware, Azure Cosmos DB for persistence, and Stripe for billing. Authentication is powered by Microsoft Entra ID (Azure AD).

## Highlights

- .NET 9, Azure Functions isolated worker
- Cosmos DB repositories and DI extensions
- Stripe integration (customers, prices, subscriptions, invoices, webhooks)
- Entra ID JWT auth for protected functions
- External API integrations (Penguin Random House, Amazon Product Advertising API)
- Clean DI with reusable IServiceCollection extensions
- Organized seeders for author data and locales
- Thorough test coverage with xUnit

## Repository layout

- `function-app/` — Core Functions app
- `InkStainedWretchFunctions/` — Domain functions for author-management localization
- `InkStainedWretchStripe/` — Stripe Functions app (customer creation, etc.)
- `OnePageAuthorLib/` — Shared library: DI, repositories, services, orchestrators
- `SeedAPIData/`, `SeedLocales/`, `SeedInkStainedWretchesLocale/` — Seeding utilities
- `OnePageAuthor.Test/` — Unit tests

## Prerequisites

- .NET SDK 9.x
- Azure Functions Core Tools v4 (optional for local run)
- Azure Cosmos DB (or local emulator)
- Stripe account + API key
- Entra ID app registration (tenant, audience/client id)

## Configuration

Environment variables (or local.settings.json for Functions apps):

- `COSMOSDB_ENDPOINT_URI` — Cosmos DB endpoint URI
- `COSMOSDB_PRIMARY_KEY` — Cosmos DB primary key
- `COSMOSDB_DATABASE_ID` — Database name
- `STRIPE_API_KEY` — Stripe secret key
- `AAD_TENANT_ID` — Entra ID tenant GUID (optional if using AAD_AUTHORITY)
- `AAD_AUDIENCE` — API application (client) ID / audience
- `AAD_CLIENT_ID` — Alternative to AAD_AUDIENCE
- `AAD_AUTHORITY` — Optional, e.g. `https://login.microsoftonline.com/<tenantId>/v2.0`
- (If using webhooks) `STRIPE_WEBHOOK_SECRET` — Endpoint secret for signature verification

### External API Integration (Optional)

For Penguin Random House API integration:
- `PENGUIN_RANDOM_HOUSE_API_URL` — Base URL for Penguin Random House API
- `PENGUIN_RANDOM_HOUSE_API_KEY` — API key for authentication
- `PENGUIN_RANDOM_HOUSE_API_DOMAIN` — Domain for API requests (e.g., "PRH.US")
- `PENGUIN_RANDOM_HOUSE_SEARCH_API` — Search API endpoint template
- `PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API` — List titles endpoint template
- `PENGUIN_RANDOM_HOUSE_URL` — Base URL for Penguin Random House website

For Amazon Product Advertising API integration:
- `AMAZON_PRODUCT_ACCESS_KEY` — AWS Access Key ID
- `AMAZON_PRODUCT_SECRET_KEY` — AWS Secret Access Key
- `AMAZON_PRODUCT_PARTNER_TAG` — Amazon Associates Partner Tag (e.g., "yourtag-20")
- `AMAZON_PRODUCT_REGION` — AWS region (e.g., "us-east-1")
- `AMAZON_PRODUCT_MARKETPLACE` — Amazon marketplace domain (e.g., "www.amazon.com")
- `AMAZON_PRODUCT_API_ENDPOINT` — API endpoint URL

### Example local.settings.json (Functions apps)

Note: Do not commit secrets. This is for local development only.

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "COSMOSDB_ENDPOINT_URI": "https://localhost:8081/",
    "COSMOSDB_PRIMARY_KEY": "<your-emulator-or-account-key>",
    "COSMOSDB_DATABASE_ID": "OnePageAuthor",
    "STRIPE_API_KEY": "sk_test_xxx",
    "AAD_TENANT_ID": "<tenant-guid>",
    "AAD_AUDIENCE": "<app-client-id>",
    "STRIPE_WEBHOOK_SECRET": "whsec_xxx"
  }
}
```

## Dependency injection extensions

Available DI helpers (from `OnePageAuthorLib`):

- Cosmos + DB
  - `AddCosmosClient(endpointUri, primaryKey)`
  - `AddCosmosDatabase(databaseId)`
- Repositories
  - `AddAuthorRepositories()`
  - `AddLocaleRepository()`
  - `AddUserProfileRepository()`
- Services
  - `AddAuthorDataService()`
  - `AddLocaleDataService()`
  - `AddInkStainedWretchServices()`
  - `AddStripeServices()`
- Orchestrators
  - `AddStripeOrchestrators()` (e.g., `IEnsureCustomerForUser`)

## Build

```pwsh
# From repository root
dotnet build OnePageAuthorAPI.sln -c Debug
```

## Run locally

You can run a Functions app via `dotnet run` or Azure Functions Core Tools (`func start`):

```pwsh
# Example: run Stripe functions app
cd InkStainedWretchStripe
dotnet run
# or
func start
```

Make sure your environment variables (or `local.settings.json`) are set for the chosen app.

## Seeding data

- Author data: `SeedAPIData`
- Locales: `SeedLocales`, `SeedInkStainedWretchesLocale`

```pwsh
# Example: seed API data
cd SeedAPIData
dotnet run
```

## Tests

```pwsh
dotnet test OnePageAuthorAPI.sln -c Debug
```

## Notable functions and flows

- Create Stripe customer: `InkStainedWretchStripe/CreateStripeCustomer`
  - [Authorize] protected; requires valid JWT
  - Request body: `CreateCustomerRequest` (at minimum: `Email`)
  - Orchestration: `IEnsureCustomerForUser` ensures `UserProfile` exists and links/persists `StripeCustomerId` (safeguard returns existing if already linked)

## Security

- Entra ID JWT Bearer authentication is configured; most endpoints are protected with `[Authorize]`.
- Stripe webhooks verify signatures and enforce timestamp tolerance.
- See `SECURITY.md` for vulnerability reporting.

## Contributing and community

- Please read `CONTRIBUTING.md` for development workflow, branching, and testing guidance.
- See `CODE_OF_CONDUCT.md` for expected behavior.


## Project index
- OnePageAuthorLib — Core library of entities, repositories, and services
   - ./OnePageAuthorLib/README.md
- InkStainedWretchFunctions — Functions app exposing localized text API and external integrations
   - ./InkStainedWretchFunctions/README.md
   - ./InkStainedWretchFunctions/README_PenguinAPI.md — Penguin Random House API integration
   - ./InkStainedWretchFunctions/README_AmazonAPI.md — Amazon Product Advertising API integration
- InkStainedWretchStripe — Functions app integrating with Stripe
   - ./InkStainedWretchStripe/README.md
- function-app — Additional Functions host (infrastructure/experiments)
   - ./function-app/README.md
- SeedInkStainedWretchesLocale — Console seeder for localized UI data
   - ./SeedInkStainedWretchesLocale/README.md
- SeedLocales — Console seeder for locale data
   - ./SeedLocales/README.md
- SeedAPIData — Console seeder for core API data
   - ./SeedAPIData/README.md
- IntegrationTestAuthorDataService — Integration test project
   - ./IntegrationTestAuthorDataService/README.md
- OnePageAuthor.Test — Unit test project
   - ./OnePageAuthor.Test/README.md

## Quickstart
- Most project-specific run/test instructions are in their respective READMEs (see index above).

## License
MIT
