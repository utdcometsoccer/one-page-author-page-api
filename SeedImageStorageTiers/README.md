# SeedImageStorageTiers

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)

Utility application for seeding image storage tier configurations into the OnePageAuthor system database.

## 🚀 Overview

SeedImageStorageTiers is a console application that initializes the database with predefined image storage tier configurations (Starter, Pro, Elite). This ensures the system has the necessary subscription tier data for proper image upload validation and user limit enforcement.

## 🏗️ Architecture

- **Runtime**: .NET 9 Console Application
- **Database**: Azure Cosmos DB
- **Dependencies**: OnePageAuthorLib for data access services

## 📋 Storage Tiers Configuration

The utility seeds the following storage tiers:

### Starter Tier (Free)


- **Storage Limit**: 5GB
- **Bandwidth**: 25GB/month
- **Max File Size**: 5MB
- **Max Files**: 20 files
- **Monthly Cost**: $0

### Pro Tier


- **Storage Limit**: 250GB
- **Bandwidth**: 1TB/month
- **Max File Size**: 10MB
- **Max Files**: 500 files
- **Monthly Cost**: $9.99

### Elite Tier


- **Storage Limit**: 2TB
- **Bandwidth**: 10TB/month
- **Max File Size**: 25MB
- **Max Files**: 2000 files
- **Monthly Cost**: $19.99

## 🚀 Quick Start

### Prerequisites


- .NET 9.0 SDK
- Access to OnePageAuthor Azure Cosmos DB
- Proper connection string configuration

### Running the Seeder


```bash
# Navigate to the project directory
cd SeedImageStorageTiers

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the seeder
dotnet run

```

### Configuration

The application uses user secrets for configuration. Set up your connection strings:

```bash
# Set Cosmos DB connection string
dotnet user-secrets set "CosmosDbConnectionString" "your-cosmos-connection-string"

# Set other required configuration
dotnet user-secrets set "DatabaseId" "your-database-id"
dotnet user-secrets set "ContainerId" "your-container-id"

```

## 🔧 Usage

The seeder will:

1. Connect to the configured Cosmos DB instance
2. Create or update storage tier records
3. Verify the seeding operation completed successfully
4. Display confirmation messages for each tier created/updated

### Sample Output


```
Starting Image Storage Tiers Seeding...
✅ Starter tier configured successfully
✅ Pro tier configured successfully  
✅ Elite tier configured successfully
Seeding completed! 3 storage tiers are now available.

```

## 🧪 Testing

The seeding operation can be run multiple times safely - it will update existing records rather than creating duplicates.

To verify the seeding worked correctly:

1. Check your Cosmos DB container for the storage tier records
2. Run the ImageAPI and verify tier validation works properly
3. Check application logs for any tier-related errors

## 📖 Documentation

- [Complete System Documentation](../Complete-System-Documentation.md)
- [OnePageAuthorLib Documentation](../OnePageAuthorLib/README.md)
- [ImageAPI Documentation](../ImageAPI/README.md)

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/storage-tiers-update`)
3. Commit your changes (`git commit -m 'Update storage tier configurations'`)
4. Push to the branch (`git push origin feature/storage-tiers-update`)
5. Open a Pull Request

## 📄 License

This project is part of the OnePageAuthor system. See the [main repository](../) for license information.

## 🔗 Related Projects

- [OnePageAuthorLib](../OnePageAuthorLib/) - Core data access library
- [ImageAPI](../ImageAPI/) - Image management functions that use these tiers
- [SeedAPIData](../SeedAPIData/) - General API data seeding utility
- [InkStainedWretchStripe](../InkStainedWretchStripe/) - Subscription management that references these tiers
