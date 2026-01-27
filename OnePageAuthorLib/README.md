# OnePageAuthorLib

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
![NuGet](https://img.shields.io/badge/Library-Core-green.svg)

Core library for the OnePageAuthor API solution providing business logic, data access, and shared services.

## 🚀 Overview

OnePageAuthorLib is the foundational library for the OnePageAuthor system, providing:

- **Entity Models**: Author, book, article, and social profile data structures
- **Data Access**: Repository patterns and Azure Cosmos DB integration
- **Business Logic**: Core services for application functionality
- **Authentication**: JWT token validation and user management
- **API Services**: Stripe integration, external API clients
- **Utilities**: Common helpers and validation logic

## 📁 Project Structure

```text
OnePageAuthorLib/
├── entities/           # Data models and entity classes
│   ├── authormanagement/   # Author and user entities
│   ├── Article.cs         # Article entity
│   ├── Author.cs          # Author entity  
│   ├── Book.cs           # Book entity
│   └── Social.cs         # Social media profile entity
├── nosql/              # Cosmos DB data access layer
│   ├── ArticlesContainerManager.cs
│   ├── CosmosDatabaseManager.cs
│   └── LocalesContainerManager.cs
├── api/                # External API integrations
│   └── Stripe/         # Stripe payment processing
├── Authentication/     # JWT and auth services
└── interfaces/         # Service contracts and interfaces

```

## 🔧 Key Components

### Entity Models

- **Author**: Core author information and metadata
- **Article**: Blog posts and content management
- **Book**: Book metadata and publishing information
- **Social**: Social media profile integration

### Data Services

- **Cosmos DB Integration**: NoSQL document storage and querying
- **Repository Pattern**: Abstracted data access with interfaces
- **Connection Management**: Efficient database connection handling

### Business Services

- **Authentication Services**: JWT token validation and user context
- **Stripe Integration**: Payment processing and subscription management
- **Localization Services**: Multi-language content support
- **Image Management**: File upload and storage validation

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- Azure Cosmos DB account
- Visual Studio 2022 or VS Code

### Building the Library

```bash
# Restore dependencies
dotnet restore OnePageAuthorLib.csproj

# Build the project
dotnet build OnePageAuthorLib.csproj

# Run tests
dotnet test ../OnePageAuthor.Test/OnePageAuthor.Test.csproj

```

### Using in Your Project

Add a project reference to use OnePageAuthorLib:

```xml
<ItemGroup>
  <ProjectReference Include="../OnePageAuthorLib/OnePageAuthorLib.csproj" />
</ItemGroup>

```

### ⚙️ Configuration

The library expects configuration values to be provided by the consuming application (Azure Functions, console apps, etc.).

#### Required Configuration Values

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB account endpoint | Azure Portal → Cosmos DB → Keys → URI | Establishes database connection for all data operations |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary access key | Azure Portal → Cosmos DB → Keys → Primary Key | Authenticates database requests |
| `COSMOSDB_DATABASE_ID` | Database name | Your database name in Cosmos DB | Identifies the database containing application containers |

#### Optional Configuration Values

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `AZURE_STORAGE_CONNECTION_STRING` | Blob Storage connection | Azure Portal → Storage Account → Access keys | Required for image upload services |
| `STRIPE_API_KEY` | Stripe secret key | [Stripe Dashboard](https://dashboard.stripe.com) → Developers → API keys | Required for payment/subscription services |
| `AAD_TENANT_ID` | Azure AD tenant ID | Azure Portal → Microsoft Entra ID → Tenant ID | Required for JWT authentication services |
| `AAD_AUDIENCE` | Azure AD client ID | Azure Portal → App registrations → Application ID | Required for JWT token validation |

#### Configuration Setup in Consuming Applications

The library uses dependency injection. Configure services in your `Program.cs`:

```csharp
// Example Azure Functions configuration
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;
        
        // Add Cosmos DB client
        services.AddCosmosClient(
            config["COSMOSDB_ENDPOINT_URI"],
            config["COSMOSDB_PRIMARY_KEY"]);
        
        // Add database
        services.AddCosmosDatabase(config["COSMOSDB_DATABASE_ID"]);
        
        // Add repositories and services
        services.AddAuthorRepositories();
        services.AddAuthorDataService();
        services.AddStripeServices();  // If using Stripe
    })
    .Build();
```

#### Using User Secrets for Development

Projects consuming this library should use .NET User Secrets:

```bash
cd YourProjectDirectory
dotnet user-secrets init
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthorDb"
```

## 📖 Documentation

- [Complete System Documentation](../Complete-System-Documentation.md)
- [API Documentation](../API-Documentation.md)
- [Authentication Guide](../README-Documentation.md)

## 🧪 Testing

The library is thoroughly tested via the OnePageAuthor.Test project:

```bash
cd ../OnePageAuthor.Test
dotnet test --filter "Category=OnePageAuthorLib"

```

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/new-service`)
3. Commit your changes (`git commit -m 'Add new service'`)
4. Push to the branch (`git push origin feature/new-service`)
5. Open a Pull Request

## 📄 License

This project is part of the OnePageAuthor system. See the [main repository](../) for license information.
