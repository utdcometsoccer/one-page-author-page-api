# OnePageAuthor API Platform - Copilot Instructions

## Project Overview

This is a comprehensive .NET 9 platform providing APIs and utilities for author management, content publishing, and subscription services. The platform is built with:

- **Azure Functions** (isolated worker model) for serverless API endpoints
- **Azure Cosmos DB** for NoSQL document storage with repository patterns
- **Stripe** for subscription billing and payment processing
- **Microsoft Entra ID** for JWT-based authentication
- **External API integrations** (Penguin Random House, Amazon Product Advertising API)
- **Multi-language support** with comprehensive localization infrastructure
- **Domain management** with automated Azure Front Door integration

The platform serves as a backend for author profile management, content publishing, subscription services, and domain registration.

## Tech Stack

### Core Technologies
- **Language**: C# 12
- **Framework**: .NET 9.0
- **Runtime**: Azure Functions v4 with isolated worker model
- **Database**: Azure Cosmos DB (NoSQL)
- **Authentication**: Microsoft Entra ID with JWT Bearer tokens
- **Payment Processing**: Stripe API (v49.2.0)

### Key Dependencies
- `Microsoft.Azure.Cosmos` (3.54.1) - Cosmos DB client
- `Microsoft.EntityFrameworkCore.Cosmos` (9.0.10) - EF Core Cosmos provider
- `Stripe.net` (49.2.0) - Stripe payment processing
- `Azure.Storage.Blobs` (12.26.0) - Azure Blob Storage for images
- `Azure.ResourceManager.Cdn` (1.5.0) - Azure Front Door integration
- `Google.Cloud.Domains.V1` (2.5.0) - Google Domains registration
- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.10) - JWT authentication

### Azure Services
- Azure Functions (v4, isolated worker)
- Azure Cosmos DB (NoSQL)
- Azure Blob Storage
- Azure Front Door
- Azure DNS
- Application Insights (monitoring)

## Project Structure

### Azure Functions (API Layer)
- **ImageAPI/** - Image upload, management, and retrieval services
- **InkStainedWretchFunctions/** - Domain registration, localization, and external API integrations
- **InkStainedWretchStripe/** - Stripe payment processing and subscription management
- **function-app/** - Core author data and additional infrastructure functions

### Core Libraries (Business Logic)
- **OnePageAuthorLib/** - Shared library with:
  - `entities/` - Data models (Author, Book, Article, Social profiles)
  - `nosql/` - Cosmos DB repositories and containers
  - `api/` - External service integrations (Stripe, Penguin Random House, Amazon)
  - `services/` - Business logic services
  - `interfaces/` - Service contracts and abstractions
  - `Authentication/` - JWT authentication services
  - `ServiceFactory.cs` - Dependency injection configuration

### Data Management (Seeding & Utilities)
- **SeedAPIData/** - Author, book, and article data initialization
- **SeedInkStainedWretchesLocale/** - Multi-language localization (EN, ES, FR, AR, ZH-CN, ZH-TW)
- **SeedImageStorageTiers/** - Image storage tier configuration
- **OnePageAuthor.DataSeeder/** - StateProvince and geographical data
- **SeedCountries/** - Country data seeding
- **SeedLanguages/** - Language data seeding

### Testing & Quality
- **OnePageAuthor.Test/** - Unit and integration tests
- **IntegrationTestAuthorDataService/** - Service validation testing

### Utility Tools
- **EntraIdRoleManager/** - Entra ID role management
- **StripeProductManager/** - Stripe product/price management
- **AmazonProductTestConsole/** - Amazon API testing

## Coding Guidelines

### General Principles
- Follow **Conventional Commits** style (e.g., `feat:`, `fix:`, `docs:`, `refactor:`)
- Use **C# 12 / .NET 9 features** when they improve clarity and maintainability
- Enable **nullable reference types** (`<Nullable>enable</Nullable>`)
- Enable **implicit usings** (`<ImplicitUsings>enable</ImplicitUsings>`)

### Architecture Patterns
- **Keep Azure Functions thin** - Put orchestration and core logic in `OnePageAuthorLib`
- **Repository pattern** for data access - All Cosmos DB operations go through repositories
- **Dependency injection** - Use DI extensions in `ServiceFactory.cs` for consistent configuration
- **Orchestrator pattern** - Complex workflows use orchestrator classes (e.g., `CustomerOrchestrator`)

### Async/Await
- **Always use async** for I/O operations (database, HTTP calls, blob storage)
- Use `ConfigureAwait(false)` in library code when appropriate
- Avoid blocking calls like `.Result` or `.Wait()`

### Error Handling
- Keep **exception messages actionable** with contextual details
- Use structured logging with appropriate log levels
- Validate inputs at API boundaries with clear error responses
- Never expose internal error details or secrets to clients

### Naming Conventions
- **Classes**: PascalCase (e.g., `AuthorRepository`, `StripePaymentService`)
- **Methods**: PascalCase (e.g., `GetAuthorByIdAsync`, `CreateCheckoutSession`)
- **Variables/Parameters**: camelCase (e.g., `customerId`, `authorData`)
- **Private fields**: `_camelCase` with underscore prefix (e.g., `_logger`, `_repository`)
- **Async methods**: Always suffix with `Async` (e.g., `SaveAuthorAsync`)
- **Interfaces**: `I` prefix (e.g., `IAuthorRepository`, `IStripeService`)

### Testing
- Add **unit tests** for new functionality (happy path + edge cases)
- Place tests in `OnePageAuthor.Test/` project
- Use **xUnit** testing framework
- Mock external dependencies (Cosmos DB, Stripe API, external services)
- Test naming: `MethodName_Scenario_ExpectedBehavior` pattern

### Security
- **Never commit secrets** - Use environment variables or Azure Key Vault
- **Validate JWT tokens** on protected endpoints using `[Authorize]` attribute
- **Verify Stripe webhook signatures** in webhook handlers
- **Sanitize user inputs** to prevent injection attacks
- See `SECURITY.md` for vulnerability reporting

### Cosmos DB Best Practices
- **Partition keys** are critical - Understand partitioning strategy for each container
- **Repository pattern** - Don't create Cosmos clients/containers in function code
- Use **bulk operations** for seeding large datasets
- **Handle transient failures** with appropriate retry policies
- **Query optimization** - Use partition key in queries when possible

### Configuration Management
- **Environment variables** for configuration (see `local.settings.json` example)
- **Configuration validation** on startup
- **Masking sensitive values** in logs (see `ConfigurationMaskingStandardization.md`)

## Build & Test Commands

### Building
```bash
# Build entire solution
dotnet build OnePageAuthorAPI.sln -c Debug

# Build specific project
dotnet build OnePageAuthorLib/OnePageAuthorLib.csproj
dotnet build InkStainedWretchStripe/InkStainedWretchStripe.csproj
```

### Testing
```bash
# Run all tests
dotnet test OnePageAuthorAPI.sln -c Debug

# Run tests with detailed output
dotnet test OnePageAuthor.Test/OnePageAuthor.Test.csproj --logger "console;verbosity=detailed"

# Run tests with coverage
dotnet test OnePageAuthorAPI.sln --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

### Running Locally
```bash
# Run Azure Function app
cd InkStainedWretchStripe
func start
# or
dotnet run

# Run data seeders
cd SeedAPIData && dotnet run
cd SeedInkStainedWretchesLocale && dotnet run
cd OnePageAuthor.DataSeeder && dotnet run
```

### Data Seeding (Development)
```bash
# Seed author and content data
cd SeedAPIData && dotnet run

# Seed comprehensive localization (idempotent)
cd SeedInkStainedWretchesLocale && dotnet run

# Seed image storage tiers
cd SeedImageStorageTiers && dotnet run

# Seed geographic data (StateProvince)
cd OnePageAuthor.DataSeeder && dotnet run

# Seed countries
cd SeedCountries && dotnet run
```

## Common Workflows

### Adding a New Azure Function Endpoint
1. Create the function method in appropriate project (ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe, or function-app)
2. Add `[Function("FunctionName")]` attribute
3. Configure HTTP trigger with appropriate authorization level
4. Add `[Authorize]` attribute if authentication is required
5. Implement business logic in `OnePageAuthorLib` services
6. Add unit tests in `OnePageAuthor.Test`
7. Update API documentation if needed

### Adding a New Repository
1. Create interface in `OnePageAuthorLib/interfaces/`
2. Implement repository in `OnePageAuthorLib/nosql/`
3. Add DI extension in `ServiceFactory.cs` (e.g., `AddXRepository()`)
4. Use the repository in services/orchestrators
5. Add unit tests with mocked repository

### Adding a New Service
1. Create interface in `OnePageAuthorLib/interfaces/`
2. Implement service in `OnePageAuthorLib/services/`
3. Add DI extension in `ServiceFactory.cs` (e.g., `AddXServices()`)
4. Inject dependencies via constructor
5. Add comprehensive unit tests

### Adding Localization
1. Add translations to `SeedInkStainedWretchesLocale` project
2. Support languages: EN, ES, FR, AR, ZH-CN, ZH-TW
3. Use culture codes (e.g., "en-US", "es-MX", "fr-CA")
4. Run seeder to update Cosmos DB
5. Test fallback logic for missing translations

## API Endpoints (Key Examples)

### ImageAPI
- `POST /api/upload` - Upload user images
- `GET /api/images/{imageId}` - Retrieve image metadata
- `DELETE /api/images/{imageId}` - Delete images

### InkStainedWretchFunctions
- `GET /api/localizedtext/{culture}` - Get localized UI text
- `POST /api/domain-registrations` - Register domains
- `GET /api/domain-registrations` - List user domains

### InkStainedWretchStripe
- `POST /api/CreateStripeCheckoutSession` - Create checkout sessions
- `POST /api/CreateStripeCustomer` - Create Stripe customers
- `POST /api/CreateSubscription` - Create subscriptions
- `POST /api/WebHook` - Handle Stripe webhooks
- `GET /api/ListSubscription/{customerId}` - List subscriptions

### Authentication
All protected endpoints require JWT Bearer token:
```http
Authorization: Bearer <jwt-token-from-entra-id>
```

## Configuration

### Required Environment Variables
| Variable | Description | Example |
|----------|-------------|---------|
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB endpoint | `https://your-account.documents.azure.com:443/` |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary key | `your-primary-key==` |
| `COSMOSDB_DATABASE_ID` | Database name | `OnePageAuthor` |
| `STRIPE_API_KEY` | Stripe secret key | `sk_test_...` |
| `AAD_TENANT_ID` | Entra ID tenant GUID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AAD_AUDIENCE` | API client ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `STRIPE_WEBHOOK_SECRET` | Webhook signing secret | `whsec_...` |

### Optional External API Variables
- Penguin Random House: `PENGUIN_RANDOM_HOUSE_API_URL`, `PENGUIN_RANDOM_HOUSE_API_KEY`
- Amazon Product API: `AMAZON_PRODUCT_ACCESS_KEY`, `AMAZON_PRODUCT_SECRET_KEY`, `AMAZON_PRODUCT_PARTNER_TAG`
- Google Domains: Configuration via Google Cloud SDK

## Documentation Resources

### Core Documentation
- [`README.md`](../README.md) - Main project overview
- [`CONTRIBUTING.md`](../CONTRIBUTING.md) - Development guidelines and PR process
- [`SECURITY.md`](../SECURITY.md) - Security policies and vulnerability reporting
- [`CODE_OF_CONDUCT.md`](../CODE_OF_CONDUCT.md) - Community standards

### Technical Documentation
- [`Complete-System-Documentation.md`](../Complete-System-Documentation.md) - Comprehensive system overview
- [`API-Documentation.md`](../API-Documentation.md) - Detailed API reference
- [`LocalizationREADME.md`](../LocalizationREADME.md) - Internationalization guide
- [`DEVELOPMENT_SCRIPTS.md`](../DEVELOPMENT_SCRIPTS.md) - Development automation scripts

### Implementation Summaries
- [`IMPLEMENTATION_SUMMARY.md`](../IMPLEMENTATION_SUMMARY.md) - Feature implementation details
- [`IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md`](../IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) - Entra ID integration
- [`IMPLEMENTATION_SUMMARY_LANGUAGES.md`](../IMPLEMENTATION_SUMMARY_LANGUAGES.md) - Language support
- [`COUNTRIES_IMPLEMENTATION_SUMMARY.md`](../COUNTRIES_IMPLEMENTATION_SUMMARY.md) - Country data
- [`STATEPROVINCE_BOILERPLATE_SUMMARY.md`](../STATEPROVINCE_BOILERPLATE_SUMMARY.md) - Geographic data

### Enhancement Documentation
- [`CULTURE_SUPPORT_ENHANCEMENT.md`](../CULTURE_SUPPORT_ENHANCEMENT.md) - Culture/localization features
- [`ACTIVE_PRODUCTS_FILTER_ENHANCEMENT.md`](../ACTIVE_PRODUCTS_FILTER_ENHANCEMENT.md) - Product filtering
- [`LABEL_VALIDATION_ENHANCEMENT.md`](../LABEL_VALIDATION_ENHANCEMENT.md) - Validation improvements
- [`SUBSCRIPTION_PLAN_SERVICE_REFACTORING.md`](../SUBSCRIPTION_PLAN_SERVICE_REFACTORING.md) - Service refactoring

### Migration & Configuration
- [`MIGRATION_GUIDE_ENTRA_ID_ROLES.md`](../MIGRATION_GUIDE_ENTRA_ID_ROLES.md) - Entra ID migration guide
- [`ConfigurationValidation.md`](../ConfigurationValidation.md) - Configuration validation
- [`ConfigurationMaskingStandardization.md`](../ConfigurationMaskingStandardization.md) - Config masking

## Code Style Examples

### Azure Function with Authentication
```csharp
[Function("GetAuthor")]
[Authorize]
public async Task<HttpResponseData> GetAuthor(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{authorId}")] 
    HttpRequestData req,
    string authorId)
{
    _logger.LogInformation("Getting author with ID: {AuthorId}", authorId);
    
    var author = await _authorService.GetAuthorByIdAsync(authorId);
    
    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(author);
    return response;
}
```

### Repository Pattern
```csharp
public class AuthorRepository : IAuthorRepository
{
    private readonly Container _container;
    private readonly ILogger<AuthorRepository> _logger;

    public AuthorRepository(Container container, ILogger<AuthorRepository> logger)
    {
        _container = container;
        _logger = logger;
    }

    public async Task<Author> GetByIdAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _container.ReadItemAsync<Author>(
                id, 
                new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Author not found: {AuthorId}", id);
            return null;
        }
    }
}
```

### Dependency Injection Extension
```csharp
public static IServiceCollection AddAuthorServices(this IServiceCollection services)
{
    services.AddScoped<IAuthorService, AuthorService>();
    services.AddScoped<IAuthorRepository, AuthorRepository>();
    services.AddScoped<IAuthorOrchestrator, AuthorOrchestrator>();
    return services;
}
```

## Common Pitfalls to Avoid

1. **Don't create Cosmos clients in function code** - Always use injected repositories
2. **Don't use `.Result` or `.Wait()`** - Use async/await properly
3. **Don't forget partition keys** - Critical for Cosmos DB performance
4. **Don't commit secrets** - Use environment variables or Key Vault
5. **Don't skip authentication** - Use `[Authorize]` on protected endpoints
6. **Don't ignore webhook signature verification** - Validate Stripe webhooks
7. **Don't forget error handling** - Validate inputs and handle exceptions gracefully
8. **Don't skip tests** - Add tests for new functionality
9. **Don't duplicate logic** - Keep business logic in `OnePageAuthorLib`, not in functions
10. **Don't forget localization** - Support multiple languages where applicable

## Development Tips

- **Use PowerShell scripts** for automation (see `*.ps1` files in root)
- **Run seeders idempotently** - Safe to run multiple times
- **Check documentation files** for feature-specific implementation details
- **Follow existing patterns** - Look at similar code for consistency
- **Keep functions focused** - Single responsibility per function
- **Log actionable information** - Include context for debugging
- **Test locally first** - Use Cosmos DB emulator and Stripe test mode
- **Review CONTRIBUTING.md** before submitting PRs
