# SeedAPIData

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)

Console application for seeding the OnePageAuthor database with initial author, book, and article data for development and testing purposes.

## 🚀 Overview

SeedAPIData provides a reliable way to initialize your OnePageAuthor system with sample data including:

- **Author Profiles**: Sample author information with biographies and metadata
- **Book Catalog**: Demo book entries with covers, descriptions, and publication details
- **Article Content**: Sample blog posts and article content
- **Social Media Links**: Author social media profiles and connections
- **Relationships**: Proper linking between authors, books, and articles

## 🏗️ Architecture

- **Runtime**: .NET 9 Console Application
- **Database**: Azure Cosmos DB integration
- **Dependencies**: OnePageAuthorLib for data access services
- **Configuration**: User Secrets for secure connection strings

## 🚀 Quick Start

### Prerequisites


- .NET 9.0 SDK
- Access to OnePageAuthor Azure Cosmos DB
- Proper connection string configuration

### Running the Seeder


```bash
# Navigate to project directory
cd SeedAPIData

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the seeder
dotnet run

```

## ⚙️ Configuration

### Required Settings

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB account endpoint | Azure Portal → Cosmos DB → Keys → URI | Establishes database connection for seeding operations |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary access key | Azure Portal → Cosmos DB → Keys → Primary Key | Authenticates write operations to the database |
| `COSMOSDB_DATABASE_ID` | Database name | Your database name (e.g., "OnePageAuthorDb") | Identifies target database for author, book, and article data |

### Setting Up User Secrets (Recommended)

```bash
cd SeedAPIData
dotnet user-secrets init

# Set required configuration
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-cosmos-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthorDb"

# Verify configuration
dotnet user-secrets list
```

### Alternative: Environment Variables

```bash
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-cosmos-primary-key"
export COSMOSDB_DATABASE_ID="OnePageAuthorDb"
```

## 📋 Sample Data

The seeder creates the following sample entities:

### Authors


- Multiple author profiles with biographical information
- Professional headshots and cover images
- Social media profiles and website links
- Geographic and demographic diversity

### Books


- Fiction and non-fiction titles
- Complete metadata (ISBN, publication dates, descriptions)
- Cover images and promotional materials
- Author-book relationships

### Articles


- Blog posts and articles
- Various content categories and tags
- Publication dates and reading time estimates
- Author attribution and cross-references

## 🔧 Usage

### Safe Operation


- The seeder can be run multiple times safely
- Existing records will be updated rather than duplicated
- Includes verification and rollback capabilities
- Provides detailed logging of all operations

### Verification

After running the seeder:

1. Check Cosmos DB containers for new records
2. Verify data integrity and relationships
3. Test API endpoints with seeded data
4. Review logs for any errors or warnings

### Sample Output


```
Starting API Data Seeding...
📚 Seeding authors... (3 authors created)
📖 Seeding books... (8 books created)  
📰 Seeding articles... (12 articles created)
🔗 Creating relationships...
✅ Seeding completed successfully!
Total records: 23 entities created

```

## 📖 Documentation

- [Complete System Documentation](../Complete-System-Documentation.md)
- [OnePageAuthorLib Documentation](../OnePageAuthorLib/README.md)
- [Database Schema Guide](../README-Documentation.md)

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/new-seed-data`)
3. Add your sample data or modify existing entries
4. Commit your changes (`git commit -m 'Add new sample data'`)
5. Push to the branch (`git push origin feature/new-seed-data`)
6. Open a Pull Request
