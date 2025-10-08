# DNS Zone Creation Trigger Function

This document describes the `DomainRegistrationTriggerFunction`, a Cosmos DB triggered Azure Function that automatically creates Azure DNS zones when domain registrations are added to the DomainRegistrations container.

## Overview

The function uses the Cosmos DB change feed to monitor the `DomainRegistrations` container and automatically provisions Azure DNS zones for new domain registrations.

## Architecture

### Components

1. **DomainRegistrationTriggerFunction** (InkStainedWretchFunctions)
   - Azure Function with Cosmos DB trigger
   - Monitors the `DomainRegistrations` container
   - Uses a unique lease collection with prefix `DnsZone` to avoid conflicts

2. **IDnsZoneService** (OnePageAuthorLib/interfaces)
   - Interface for DNS zone management operations
   - Methods: `EnsureDnsZoneExistsAsync`, `DnsZoneExistsAsync`

3. **DnsZoneService** (OnePageAuthorLib/api)
   - Implementation of DNS zone management using Azure.ResourceManager.Dns
   - Creates DNS zones using Azure DNS REST API
   - Checks if DNS zones already exist before creation

## Configuration

### Required Application Settings

Add the following settings to your Azure Function App configuration or local.settings.json:

```json
{
  "Values": {
    "COSMOSDB_ENDPOINT_URI": "<your-cosmosdb-endpoint>",
    "COSMOSDB_PRIMARY_KEY": "<your-cosmosdb-key>",
    "COSMOSDB_DATABASE_ID": "<your-database-name>",
    "CosmosDBConnection": "<your-cosmosdb-connection-string>",
    "AZURE_SUBSCRIPTION_ID": "<your-azure-subscription-id>",
    "AZURE_DNS_RESOURCE_GROUP": "<your-dns-resource-group-name>"
  }
}
```

### Configuration Details

| Setting | Description | Example |
|---------|-------------|---------|
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB account endpoint | `https://myaccount.documents.azure.com:443/` |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary key | `base64-encoded-key` |
| `COSMOSDB_DATABASE_ID` | Database name | `OnePageAuthorDB` |
| `CosmosDBConnection` | Connection string for Cosmos DB trigger | `AccountEndpoint=https://...;AccountKey=...` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID for DNS operations | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_DNS_RESOURCE_GROUP` | Resource group containing DNS zones | `dns-zones-rg` |

## Authentication

The DNS Zone Service uses `DefaultAzureCredential` which supports multiple authentication methods in this order:

1. **Managed Identity** (recommended for production)
   - Enable system-assigned or user-assigned managed identity on the Function App
   - Grant the identity "DNS Zone Contributor" role on the resource group

2. **Environment Variables** (for local development)
   - `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`

3. **Azure CLI** (for local development)
   - Use `az login` before running locally

### Assigning Permissions

For production deployments, assign the "DNS Zone Contributor" role:

```bash
# Get the Function App's managed identity principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name <function-app-name> \
  --resource-group <function-rg> \
  --query principalId -o tsv)

# Assign DNS Zone Contributor role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "DNS Zone Contributor" \
  --resource-group <dns-resource-group>
```

## Lease Container

The trigger uses a dedicated lease container to track change feed progress:

- **Container Name**: `leases`
- **Lease Prefix**: `DnsZone`
- **Auto-Creation**: Enabled

The lease prefix `DnsZone` ensures this trigger doesn't conflict with other functions monitoring the same container.

## Function Behavior

### Trigger Conditions

The function triggers when:
- A new document is inserted into the `DomainRegistrations` container
- An existing document is updated in the `DomainRegistrations` container

### Processing Logic

For each domain registration:

1. **Validation**: Checks if the domain registration and domain information are valid
2. **Status Check**: Only processes registrations with status `Pending` or `InProgress`
3. **DNS Zone Check**: Verifies if the DNS zone already exists
4. **DNS Zone Creation**: Creates the DNS zone if it doesn't exist
5. **Logging**: Records success or failure for monitoring

### Status Filtering

The function only processes domain registrations with these statuses:
- `Pending` (0)
- `InProgress` (1)

Registrations with status `Completed`, `Failed`, or `Cancelled` are skipped.

## Dependencies

### NuGet Packages

- `Azure.ResourceManager.Dns` (1.2.0-beta.2) - Azure DNS management
- `Microsoft.Azure.Functions.Worker.Extensions.CosmosDB` (4.11.0) - Cosmos DB trigger
- `Azure.Identity` (1.16.0) - Azure authentication

### Dependency Injection

The function uses dependency injection as per existing patterns:

```csharp
builder.Services
    .AddDomainRegistrationRepository()
    .AddDomainRegistrationServices()
    .AddDnsZoneService(); // DNS zone service registration
```

## Monitoring and Logging

The function logs detailed information at various stages:

- **Information**: Processing start/end, successful DNS zone creation
- **Warning**: Invalid domain registrations, status filtering
- **Error**: Failed DNS zone creation, exceptions

View logs in:
- Azure Portal > Function App > Logs
- Application Insights > Logs
- Log Stream in Azure Portal

### Sample Log Queries (KQL)

```kql
// Find DNS zone creation events
traces
| where message contains "DNS zone"
| project timestamp, message, severityLevel
| order by timestamp desc

// Find errors in domain registration processing
traces
| where severityLevel == 3 // Error
| where operation_Name == "DomainRegistrationTrigger"
| project timestamp, message
| order by timestamp desc
```

## Testing

### Local Testing

1. Configure local.settings.json with required settings
2. Ensure Azure CLI is authenticated: `az login`
3. Start the function: `func start`
4. Add a document to the DomainRegistrations container
5. Monitor the console for log output

### Integration Testing

The function can be tested by:
1. Creating a domain registration via the HTTP endpoint
2. Observing the trigger function logs
3. Verifying the DNS zone was created in Azure Portal

## Error Handling

The function includes comprehensive error handling:

- **Invalid Domain**: Logs warning and skips processing
- **Missing Configuration**: Throws exception on startup
- **DNS API Errors**: Logs error but continues processing other documents
- **Network Issues**: Logged as errors with exception details

## Performance Considerations

- **Batch Processing**: Processes multiple domain registrations per trigger invocation
- **Lease Management**: Unique lease prefix prevents conflicts
- **Idempotency**: Checks if DNS zone exists before creation
- **Async Operations**: All operations are async for better throughput

## Limitations

- The function cannot update domain registration status directly (no ClaimsPrincipal)
- Status updates should be handled by HTTP endpoints or separate processes
- DNS zone creation is idempotent (safe to retry)

## Troubleshooting

### Common Issues

1. **"AZURE_SUBSCRIPTION_ID configuration is required"**
   - Ensure the configuration setting is present and correct

2. **"Unable to authenticate with Azure"**
   - Check managed identity is enabled and has proper permissions
   - For local development, ensure `az login` is successful

3. **"DNS zone creation failed"**
   - Verify the resource group exists
   - Check the managed identity has "DNS Zone Contributor" role
   - Review detailed error logs

4. **Trigger not firing**
   - Verify CosmosDBConnection string is correct
   - Check the lease container exists and is accessible
   - Ensure the DomainRegistrations container has documents

## Future Enhancements

Potential improvements:
- Add retry logic with exponential backoff
- Implement status update mechanism for domain registrations
- Add metrics and alerts for DNS zone creation failures
- Support for additional DNS record creation (A, CNAME, etc.)
- Batch DNS operations for better performance
