# Google Domain Registration Function

## Overview

This document describes the **GoogleDomainRegistrationFunction** - an Azure Function that automatically registers domains using the Google Domains API when new domain registrations are added to the Cosmos DB `DomainRegistrations` container.

## Architecture

### Components

1. **GoogleDomainRegistrationFunction** (InkStainedWretchFunctions)

   - Azure Function with Cosmos DB trigger
   - Orchestrates domain registration flow
   - Minimal logic - delegates to service layer

2. **IGoogleDomainsService** (OnePageAuthorLib/interfaces)

   - Interface defining Google Domains API operations
   - Follows dependency injection pattern

3. **GoogleDomainsService** (OnePageAuthorLib/api)

   - Implements business logic for domain registration
   - Integrates with Google Cloud Domains API
   - Handles error scenarios and logging

4. **Unit Tests** (OnePageAuthor.Test)

   - GoogleDomainsServiceTests
   - Validates service behavior and error handling

## Implementation Details

### Cosmos DB Trigger Configuration

The function uses a unique lease prefix to avoid conflicts with other triggers on the same container:

```csharp
[CosmosDBTrigger(
    databaseName: "%COSMOSDB_DATABASE_ID%",
    containerName: "DomainRegistrations",
    Connection = "COSMOSDB_CONNECTION_STRING",
    LeaseContainerName = "leases",
    LeaseContainerPrefix = "googledomainregistration",
    CreateLeaseContainerIfNotExists = true)]

```

### Processing Flow

1. Function triggered when documents are added/updated in DomainRegistrations container
2. Validates domain registration data
3. Checks if status is `Pending`
4. Calls Google Domains API to register the domain
5. Logs success or failure (does not update status - requires ClaimsPrincipal)

### Google Domains API Integration

The service uses the official Google.Cloud.Domains.V1 NuGet package (v2.4.0) which provides:

- Domain registration capabilities
- Domain availability checking
- Contact information management
- Long-running operation support

### Configuration Requirements

The following environment variables/app settings are required:

- `GOOGLE_CLOUD_PROJECT_ID` - Google Cloud project ID
- `GOOGLE_DOMAINS_LOCATION` - Location for domain operations (default: "global")
- `COSMOSDB_DATABASE_ID` - Cosmos DB database name
- `COSMOSDB_CONNECTION_STRING` - Cosmos DB connection string

### Authentication

The service uses Application Default Credentials (ADC) for Google Cloud authentication:

- In Azure, configure Managed Identity
- Grant the identity appropriate permissions in Google Cloud (Domain Registration Admin)
- Workload Identity Federation recommended for production

## Service Methods

### RegisterDomainAsync

Registers a domain using the Google Domains API.

**Parameters:**

- `domainRegistration` - Domain registration information from Cosmos DB

**Returns:**

- `true` if registration initiated successfully
- `false` if validation fails or API call errors

**Behavior:**

- Validates input parameters
- Creates Google Domains client
- Builds registration request with contact information
- Initiates domain registration (long-running operation)
- Returns immediately (does not wait for completion)

### IsDomainAvailableAsync

Checks if a domain is available for registration.

**Parameters:**

- `domainName` - Full domain name to check

**Returns:**

- `true` if domain is available
- `false` if unavailable or error occurs

## Error Handling

The function is designed to be resilient:

- Validates all inputs before API calls
- Catches and logs exceptions per registration
- Continues processing remaining registrations on error
- Does not throw exceptions that would stop the function

## Dependency Injection

Services are registered in `ServiceFactory.cs`:

```csharp
public static IServiceCollection AddGoogleDomainsService(this IServiceCollection services)
{
    services.AddScoped<Interfaces.IGoogleDomainsService, API.GoogleDomainsService>();
    return services;
}

```

Registered in `Program.cs`:

```csharp
builder.Services
    .AddCosmosClient(endpointUri!, primaryKey!)
    .AddCosmosDatabase(databaseId!)
    // ... other services ...
    .AddGoogleDomainsService(); // Add Google Domains service

```

## Testing

Unit tests cover:

- Constructor validation (null parameters, missing configuration)
- RegisterDomainAsync validation (null/empty inputs)
- IsDomainAvailableAsync validation
- Default configuration behavior

Tests use Moq for mocking dependencies.

## NuGet Dependencies

### OnePageAuthorLib


- **Google.Cloud.Domains.V1** (v2.4.0) - Google Cloud Domains API client

### InkStainedWretchFunctions  


- No additional packages required (references OnePageAuthorLib)

## Deployment Checklist

- [ ] Configure Google Cloud project and enable Cloud Domains API
- [ ] Set up authentication (Managed Identity or Service Account)
- [ ] Grant necessary permissions for domain registration
- [ ] Set environment variables in Azure Function configuration
- [ ] Deploy OnePageAuthorLib with new dependencies
- [ ] Deploy InkStainedWretchFunctions
- [ ] Verify function appears in Function App
- [ ] Test with a sample domain registration

## Monitoring and Observability

The function provides detailed logging:

- Information level: Processing progress, operation status
- Warning level: Validation failures, skipped registrations
- Error level: API failures, unexpected exceptions

Monitor using:

- Application Insights
- Azure Functions logs
- Cosmos DB monitoring (lease container activity)

## Comparison with Existing Functions

This function follows the same pattern as:

- **DomainRegistrationTriggerFunction** - Adds domains to Azure Front Door
- **CreateDnsZoneFunction** - Creates Azure DNS zones

Key similarities:

- Uses Cosmos DB trigger on DomainRegistrations
- Unique lease prefix prevents conflicts
- Minimal function logic with service delegation
- Dependency injection for testability
- Comprehensive error handling

## Cost Considerations

- Google Domains API charges apply per registration
- Domain registration costs vary by TLD
- Long-running operations may have additional costs
- Consider implementing domain availability checks before registration

## Security Considerations

- Contact information is transmitted to Google Domains
- Ensure HTTPS is used for all API calls
- Protect configuration values (use Key Vault)
- Audit domain registration activity
- Implement approval workflows for sensitive domains

## Future Enhancements

Potential improvements:

- Track long-running operation status
- Update DomainRegistration status when complete
- Implement retry logic for failed registrations
- Add domain availability pre-check
- Support for multiple domain registrars
- Webhook integration for registration status updates
- Cost estimation before registration

## Support and Troubleshooting

Common issues:

1. **"GOOGLE_CLOUD_PROJECT_ID configuration is required"**
   - Ensure environment variable is set in Function App configuration

2. **Authentication errors**

   - Verify Managed Identity is configured
   - Check Google Cloud IAM permissions

3. **Domain registration fails**

   - Verify domain is available
   - Check contact information is complete and valid
   - Review Google Cloud Domains API quotas

4. **Function not triggering**

   - Verify Cosmos DB connection string
   - Check lease container exists
   - Review Application Insights for errors

## References

- [Google Cloud Domains API Documentation](https://cloud.google.com/domains/docs)
- [Azure Functions Cosmos DB Trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-cosmosdb-v2-trigger)
- [Workload Identity Federation](https://cloud.google.com/iam/docs/workload-identity-federation)
