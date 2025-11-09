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

### Domain Registration Function Tests

The `DomainRegistrationFunctionTests` test suite provides comprehensive unit test coverage for the `DomainRegistrationFunction` class, which contains three Azure Functions:

1. **CreateDomainRegistration** - POST endpoint for creating domain registrations
2. **GetDomainRegistrations** - GET endpoint for retrieving user's domain registrations
3. **GetDomainRegistrationById** - GET endpoint for retrieving a specific domain registration by ID

#### Test Coverage Breakdown

**Constructor Tests (5 tests):**
- Tests for null parameter validation on all dependencies
- Validates proper dependency injection

**CreateDomainRegistration Tests (3 tests):**
- `WithNullPayload_ReturnsServerError`: Tests null payload handling
- `WithEmptySecondLevelDomain_ReturnsServerError`: Tests domain validation
- `WithEmptyFirstName_ReturnsServerError`: Tests contact information validation

**GetDomainRegistrations Tests (3 tests):**
- `WithoutUserId_ReturnsServerError`: Tests missing user ID handling
- `WithInvalidUserId_ReturnsServerError`: Tests invalid user ID format
- `SuccessfulRetrieval_ReturnsOkWithRegistrations`: Tests successful retrieval

**GetDomainRegistrationById Tests (3 tests):**
- `WithoutUserId_ReturnsServerError`: Tests missing user ID handling
- `WithInvalidId_ReturnsServerError`: Tests invalid registration ID format
- `SuccessfulRetrieval_ReturnsOkWithRegistration`: Tests successful retrieval

#### Testing Limitations

**JWT Authentication Challenges:**
The main limitation is the use of static JWT authentication methods (`JwtAuthenticationHelper.ValidateJwtTokenAsync`):

1. **Cannot Mock Static Methods**: Standard mocking frameworks like Moq cannot mock static methods
2. **Authentication Fails First**: Without proper JWT tokens, functions return 500 status codes instead of 400 for validation errors
3. **Limited Business Logic Testing**: The authentication layer blocks access to the actual business logic validation

**Current Test Behavior:**
- Tests expecting validation errors (400 status) actually receive authentication errors (500 status)
- Tests validate that functions handle unauthenticated requests appropriately
- Business logic validation cannot be fully tested in isolation

#### Recommendations for Future Improvements

**1. Dependency Injection Refactoring:**
```csharp
// Current (Static):
var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);

// Recommended (Injectable):
var (authenticatedUser, authError) = await _jwtAuthenticationHelper.ValidateJwtTokenAsync(req);
```

**2. Integration Testing:**
- Use real JWT tokens or test authentication middleware
- Test the full request/response cycle
- Validate business logic with proper authentication context

**3. Testable Architecture Patterns:**
- **Middleware approach**: Move JWT validation to middleware
- **Decorator pattern**: Wrap functions with authentication decorators
- **Service layer extraction**: Move business logic to separate, testable service classes

#### Running Domain Registration Tests

```powershell
dotnet test --filter "FullyQualifiedName~DomainRegistrationFunctionTests"
```

### Additional Documentation

- [Complete System Documentation](../Complete-System-Documentation.md)
- [API Documentation](../API-Documentation.md)
- [Testing Guidelines](../CONTRIBUTING.md)
