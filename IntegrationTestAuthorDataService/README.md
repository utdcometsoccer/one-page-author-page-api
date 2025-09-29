# IntegrationTestAuthorDataService

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![Tests](https://img.shields.io/badge/Test%20Type-Integration-blue.svg)](#)

Specialized integration test suite for validating AuthorDataService functionality and Azure Cosmos DB data access operations.

## 🚀 Overview

This project provides comprehensive integration testing specifically for:

- **AuthorDataService Operations**: CRUD operations and business logic validation
- **Cosmos DB Integration**: Database connection, query performance, and data consistency
- **Data Persistence**: Entity mapping, serialization, and repository patterns
- **Error Handling**: Connection failures, timeout scenarios, and data validation
- **Performance Testing**: Query optimization and response time validation

## 🧪 Test Scenarios

### Data Access Tests
- Author creation, retrieval, update, and deletion
- Complex query scenarios and filtering operations
- Bulk operations and batch processing
- Transaction handling and rollback scenarios

### Integration Validation
- Database connectivity and authentication
- Container and partition key validation  
- Index utilization and query optimization
- Cross-container relationship integrity

### Error Handling Tests
- Network connectivity issues simulation
- Invalid data validation and error responses
- Concurrent access and conflict resolution
- Resource throttling and retry logic

## 🏗️ Architecture

- **Runtime**: .NET 9 Console Application
- **Testing Framework**: Custom integration test harness
- **Database**: Azure Cosmos DB (test instance)
- **Dependencies**: OnePageAuthorLib, AuthorDataService

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Azure Cosmos DB Emulator OR test instance
- Valid connection strings and configuration

### Running Integration Tests
```bash
# Navigate to project directory
cd IntegrationTestAuthorDataService

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run integration tests
dotnet run

# Alternative: Run as test project
dotnet test
```

### Configuration
Set up test database connection:
```bash
# For Cosmos DB Emulator
dotnet user-secrets set "CosmosDbConnectionString" "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

# For test instance
dotnet user-secrets set "CosmosDbConnectionString" "your-test-cosmos-connection-string"
dotnet user-secrets set "DatabaseId" "OnePageAuthorTest"
```

## 🔧 Test Features

### Comprehensive Coverage
- **Author Entity Tests**: Full CRUD lifecycle validation
- **Query Performance**: Response time benchmarking
- **Data Integrity**: Consistency and validation checks
- **Concurrency Tests**: Multi-user scenario simulation

### Automated Validation
- Database schema verification
- Index effectiveness analysis  
- Query cost optimization checks
- Memory usage and resource monitoring

### Sample Test Output
```
Starting Author Data Service Integration Tests...

📊 Database Connectivity Tests
  ✅ Connection establishment: 85ms
  ✅ Authentication validation: 42ms
  ✅ Container access verification: 28ms

👤 Author CRUD Operations  
  ✅ Create author: 156ms
  ✅ Retrieve by ID: 23ms
  ✅ Update author: 89ms  
  ✅ Delete author: 45ms

🔍 Query Performance Tests
  ✅ Simple queries: avg 34ms (10 tests)
  ✅ Complex filters: avg 127ms (5 tests)
  ✅ Pagination: avg 78ms (8 tests)

✅ All integration tests passed! (23/23)
Total execution time: 2.3 seconds
```

## 📖 Documentation

- [Complete System Documentation](../Complete-System-Documentation.md)
- [OnePageAuthorLib Documentation](../OnePageAuthorLib/README.md)
- [Main Test Suite](../OnePageAuthor.Test/README.md)

## 🤝 Contributing

### Adding Integration Tests
1. Create test scenarios in the appropriate test category
2. Use proper setup and teardown for database operations
3. Include performance benchmarks where applicable
4. Add comprehensive error scenario coverage
5. Document test expectations and success criteria

### Test Guidelines
- **Isolated Tests**: Each test should be independent and repeatable
- **Real Data**: Use realistic test data that mirrors production scenarios
- **Performance Baselines**: Include response time expectations
- **Cleanup**: Properly dispose of test resources and data
- **Documentation**: Comment complex test scenarios thoroughly
