# Complete System Documentation

This project includes an automated documentation generation system that creates comprehensive developer documentation from XML comments across **all projects** in the solution.

## Quick Start

### Generate Complete Documentation (Recommended)
Generate documentation for all 11 projects in the solution:

**PowerShell (Complete System):**
```powershell
.\Generate-Complete-Documentation.ps1
```

This will:
1. Automatically add XML documentation generation to any project that doesn't have it
2. Build all 11 projects in the solution
3. Parse XML documentation from all projects  
4. Generate `Complete-System-Documentation.md` with comprehensive coverage

### Generate API-Only Documentation (Legacy)
Generate documentation for just Azure Function projects:

**PowerShell (API Only):**
```powershell
.\Generate-ApiDocumentation.ps1
```

**Batch File:**
```cmd
.\Generate-Documentation.bat
```

**MSBuild Target:**
```cmd
dotnet build --target:BuildApiDocumentation
```

## Output

The system generates comprehensive documentation files:

### Complete-System-Documentation.md
**All 11 Projects** including:
- **Azure Functions**: All 4 function app projects with endpoints and methods
- **Core Library**: OnePageAuthorLib business logic and entities  
- **Utilities**: All 4 seeding and data management tools
- **Tests**: Unit tests and integration testing utilities
- **System Architecture**: Complete project relationships and dependencies
- **Build Statistics**: Project counts and documentation coverage metrics

### API-Documentation.md (Legacy)
**Azure Functions Only** containing:
- **Complete API Reference**: All Azure Function endpoints with detailed descriptions
- **TypeScript Examples**: Practical code samples for consuming the API
- **Authentication Guide**: JWT token handling and security requirements  
- **Error Handling**: HTTP status codes and error response formats
- **Rate Limiting**: Subscription tier limits and headers
- **Interactive Examples**: React components and async/await patterns

## Features

✅ **Complete Solution Coverage**: Documents all 11 projects in the solution automatically  
✅ **Automatic Project Discovery**: Finds and processes every project type (Functions, Libraries, Utilities, Tests)  
✅ **Auto-Configuration**: Adds XML documentation generation to projects that don't have it  
✅ **TypeScript Integration**: Full interface definitions and usage examples for APIs  
✅ **Authentication Details**: JWT claims, security notes, and token validation  
✅ **Practical Examples**: Real React components and API client patterns  
✅ **Error Documentation**: Complete HTTP status codes and error handling  
✅ **Build Integration**: Seamless MSBuild integration for automatic updates  
✅ **Smart Organization**: Groups projects by type (Functions, Libraries, Utilities, Tests)  

## Project Structure

```
├── Generate-Complete-Documentation.ps1  # Complete system documentation generator (NEW)
├── Generate-ApiDocumentation.ps1       # Legacy API-only documentation generator
├── Generate-Documentation.bat          # Batch file wrapper for easy execution  
├── Directory.Build.targets             # MSBuild targets for automatic generation
├── Complete-System-Documentation.md    # Generated comprehensive system documentation (NEW)
├── API-Documentation.md                # Legacy API-only documentation
├── README-Documentation.md             # This documentation guide
├── ImageAPI/                           # Image management Azure Functions
│   ├── Delete.cs, Upload.cs, User.cs, WhoAmI.cs
├── InkStainedWretchFunctions/          # Domain registration & external APIs  
│   ├── DomainRegistrationFunction.cs, PenguinRandomHouseFunction.cs
├── InkStainedWretchStripe/             # Stripe payment processing (NEW)
│   ├── CreateSubscription.cs, WebHook.cs, etc.
├── function-app/                       # Main application functions (NEW)
│   ├── GetAuthorData.cs, GetLocaleData.cs
├── OnePageAuthorLib/                   # Core business logic library (NEW)
│   ├── entities/, api/, nosql/, Authentication/
├── OnePageAuthor.Test/                 # Unit and integration tests (NEW)
├── SeedAPIData/                        # API data seeding utility (NEW)
├── SeedImageStorageTiers/              # Storage tier configuration (NEW)
├── SeedInkStainedWretchesLocale/       # Comprehensive localization seeding (NEW)
└── IntegrationTestAuthorDataService/   # Integration test utilities (NEW)
```

## XML Documentation Standards

All functions include comprehensive XML documentation with:

- `<summary>`: Clear description of the function's purpose
- `<remarks>`: Authentication requirements and important usage notes  
- `<param>`: Detailed parameter descriptions and validation rules
- `<returns>`: HTTP status codes and response format details
- `<example>`: Complete TypeScript usage examples with React components

## Authentication Requirements

All API endpoints require JWT Bearer token authentication:

```http
Authorization: Bearer <your-jwt-token>
```

The documentation includes detailed examples for:
- Token validation and error handling
- Claims extraction and user information  
- Authentication middleware integration
- Security best practices

## Subscription Tiers & Limits

The API enforces different limits based on subscription tiers:

| Tier | Rate Limit | Upload Limit | Storage Quota |
|------|------------|--------------|---------------|
| Starter | 100 req/min | 5MB | 100MB |  
| Pro | 1000 req/min | 50MB | 10GB |
| Elite | 10000 req/min | 500MB | 1TB |

## Development Workflow

1. **Write Code**: Add comprehensive XML documentation to all public functions
2. **Build**: Documentation automatically generates on Debug builds  
3. **Review**: Check `Complete-System-Documentation.md` for accuracy and completeness
4. **Deploy**: Updated documentation deploys with your application

### Current Documentation Statistics  
- **Total Projects**: 10 (complete solution coverage)
- **Azure Functions**: 4 projects (ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe, function-app)  
- **Libraries**: 1 project (OnePageAuthorLib)
- **Utilities**: 3 projects (SeedAPIData, SeedImageStorageTiers, SeedInkStainedWretchesLocale)
- **Test Projects**: 2 projects (OnePageAuthor.Test, IntegrationTestAuthorDataService)
- **Documented Members**: 298 classes and methods across all projects

## Troubleshooting

**XML Warnings**: Ensure all `&` characters in code examples are escaped as `&amp;`  
**Build Failures**: Check that PowerShell Core (pwsh.exe) is installed and accessible  
**Missing Documentation**: Verify XML comments are properly formatted and positioned before attributes  
**Generation Errors**: Run the PowerShell script manually to see detailed error messages  

## Requirements

- **.NET 9.0**: For building the Azure Function projects
- **PowerShell Core**: For running the documentation generation script  
- **MSBuild**: Integrated with Visual Studio or .NET CLI

---

*This documentation system automatically stays up-to-date with your code changes, ensuring developers always have accurate API information.*
