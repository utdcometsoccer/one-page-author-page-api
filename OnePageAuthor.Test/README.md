# OnePageAuthor.Test

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![xUnit](https://img.shields.io/badge/Tests-xUnit-green.svg)](https://xunit.net/)
[![Coverage](https://img.shields.io/badge/Coverage-154%20Tests-brightgreen.svg)](#)

Comprehensive unit and integration test suite for the OnePageAuthor API system and core library components.

## 🚀 Overview

Comprehensive test suite providing quality assurance for the OnePageAuthor system:

- **Unit Tests**: Core library functionality and business logic
- **Integration Tests**: Database operations and external API integrations  
- **API Tests**: Azure Functions endpoint validation
- **Security Tests**: Authentication and authorization verification
- **Performance Tests**: Load and stress testing scenarios

## 🧪 Test Coverage

### Test Categories
- **ImageAPI Tests**: Image upload, validation, and management
- **Authentication Tests**: JWT token validation and user context
- **Data Access Tests**: Cosmos DB operations and repository patterns
- **Service Tests**: Business logic and external API integrations
- **Validation Tests**: Input sanitization and error handling

### Testing Framework
- **xUnit**: Primary testing framework
- **Moq**: Mocking and test doubles
- **Microsoft.NET.Test.Sdk**: Test execution and reporting
- **Coverlet**: Code coverage analysis

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- Azure Cosmos DB Emulator (for integration tests)

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific category
dotnet test --filter "Category=ImageAPI"
dotnet test --filter "Category=Authentication"
dotnet test --filter "Category=DataAccess"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run in verbose mode
dotnet test --logger "console;verbosity=detailed"
```

### Test Configuration
For integration tests, configure connection strings in user secrets:
```bash
dotnet user-secrets set "CosmosDbConnectionString" "your-test-cosmos-connection"
dotnet user-secrets set "BlobStorageConnectionString" "your-test-storage-connection"
```

## 📊 Test Results

Current test statistics:
- **Total Tests**: 154 tests
- **Pass Rate**: 100% (154/154 passing)
- **Code Coverage**: ~85% line coverage
- **Performance**: Average execution time <30 seconds

## 🤝 Contributing

### Adding New Tests
1. Create test files following naming convention: `[ComponentName]Tests.cs`
2. Use appropriate test categories with `[Trait("Category", "ComponentName")]`
3. Include both positive and negative test cases
4. Add integration tests for database operations
5. Ensure tests are deterministic and can run in parallel

### Test Guidelines  
- **Arrange-Act-Assert**: Follow AAA pattern for test structure
- **Single Responsibility**: Each test should verify one behavior
- **Meaningful Names**: Test method names should describe the scenario
- **Mock External Dependencies**: Use Moq for external service calls
- **Clean Up**: Dispose of resources properly in test teardown

## 📖 Documentation

- [Complete System Documentation](../Complete-System-Documentation.md)
- [API Documentation](../API-Documentation.md)
- [Testing Guidelines](../CONTRIBUTING.md)
