# Domain Registration Trigger Function

## Overview

This document describes the implementation of an Azure Function that automatically adds domains to Azure Front Door when new domain registrations are created in Cosmos DB.

## Architecture

### Components

1. **DomainRegistrationTriggerFunction** (InkStainedWretchFunctions)
   - Azure Function with Cosmos DB trigger
   - Monitors the `DomainRegistrations` container for changes
   - Triggers when new domain registrations are created

2. **FrontDoorService** (OnePageAuthorLib)
   - Business logic for Azure Front Door operations
   - Checks if domains already exist in Front Door
   - Adds new domains with managed certificates

3. **IFrontDoorService** (OnePageAuthorLib)
   - Interface for dependency injection
   - Defines contract for Front Door operations

## How It Works

1. User creates a domain registration via the HTTP API (POST /api/domain-registrations)
2. Domain registration is stored in Cosmos DB with status "Pending"
3. DomainRegistrationTriggerFunction is automatically triggered by the Cosmos DB change feed
4. Function validates the registration and checks if domain exists in Front Door
5. If domain doesn't exist, it's added to Front Door with:
   - Managed TLS certificate
   - TLS 1.2 minimum version
   - Automatic domain validation setup

## Configuration

### Required Environment Variables

```json
{
  "COSMOSDB_CONNECTION_STRING": "AccountEndpoint=https://...;AccountKey=...;",
  "COSMOSDB_ENDPOINT_URI": "https://<account>.documents.azure.com:443/",
  "COSMOSDB_PRIMARY_KEY": "<secret>",
  "COSMOSDB_DATABASE_ID": "<database-name>",
  "AZURE_SUBSCRIPTION_ID": "<subscription-id>",
  "AZURE_RESOURCE_GROUP_NAME": "<resource-group-name>",
  "AZURE_FRONTDOOR_PROFILE_NAME": "<frontdoor-profile-name>"
}
```

### Azure RBAC Permissions

The Function App's Managed Identity requires one of the following roles:
- CDN Profile Contributor (on the Front Door profile)
- CDN Endpoint Contributor (on the Front Door profile or resource group)

To assign the role:
```bash
az role assignment create \
  --assignee <function-app-managed-identity-object-id> \
  --role "CDN Profile Contributor" \
  --scope /subscriptions/<subscription-id>/resourceGroups/<rg-name>/providers/Microsoft.Cdn/profiles/<profile-name>
```

## Lease Management

The trigger uses a unique lease configuration to allow multiple functions to monitor the same container:

- **Lease Container Name**: `leases`
- **Lease Prefix**: `domainregistration`
- **Auto-create**: Yes (container is created automatically if it doesn't exist)

This prevents conflicts with other triggers that might be monitoring the same container.

## Error Handling

- Errors in processing one domain registration don't block processing of others
- All errors are logged with Application Insights integration
- Failed operations can be retried by updating the domain registration status back to "Pending"

## Testing

Unit tests are located in `OnePageAuthor.Test/FrontDoor/FrontDoorServiceTests.cs`

Run tests:
```bash
dotnet test OnePageAuthor.Test/OnePageAuthor.Test.csproj --filter "FullyQualifiedName~FrontDoorServiceTests"
```

Test coverage includes:
- Constructor validation
- Configuration validation
- Input parameter validation
- Domain name formatting

## Deployment

1. Enable Managed Identity for the Function App:
   ```bash
   az functionapp identity assign --name <function-app-name> --resource-group <rg-name>
   ```

2. Assign RBAC role (see Azure RBAC Permissions section above)

3. Configure environment variables in Function App settings:
   ```bash
   az functionapp config appsettings set \
     --name <function-app-name> \
     --resource-group <rg-name> \
     --settings \
     AZURE_SUBSCRIPTION_ID=<value> \
     AZURE_RESOURCE_GROUP_NAME=<value> \
     AZURE_FRONTDOOR_PROFILE_NAME=<value>
   ```

4. Deploy the function app

## Monitoring

Monitor function execution:
- Azure Portal > Function App > Functions > DomainRegistrationTrigger
- Application Insights logs
- Cosmos DB change feed metrics

Key log messages to watch for:
- "DomainRegistrationTrigger processing {Count} domain registration(s)"
- "Successfully processed domain {DomainName} for Front Door"
- "Failed to add domain {DomainName} to Front Door"

## Limitations

- Status updates require ClaimsPrincipal which isn't available in trigger functions
- Consider implementing a separate status update mechanism if needed
- Front Door domain addition is asynchronous (operation initiated, not completed)
- DNS validation is required after domain is added to Front Door

## Future Enhancements

Potential improvements:
1. Add status update mechanism for domain registrations
2. Implement domain validation status checking
3. Add retry logic with exponential backoff
4. Create a durable orchestrator for complex workflows
5. Add webhook notifications for domain provisioning completion
