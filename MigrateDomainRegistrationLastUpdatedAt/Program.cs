using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configuration setup
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

var config = builder.Configuration;

// Get Cosmos configuration
var endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
var primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
var databaseId = config["COSMOSDB_DATABASE_ID"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

// Configure services
builder.Services
    .AddLogging(logging => logging.AddConsole())
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId);

// Register container manager for DomainRegistration
builder.Services.AddSingleton<IContainerManager<DomainRegistration>, DomainRegistrationsContainerManager>();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting DomainRegistration LastUpdatedAt migration...");
    logger.LogInformation("Configuration (masked for security):");
    logger.LogInformation("  Cosmos DB Endpoint: {EndpointUri}", Utility.MaskUrl(endpointUri));
    logger.LogInformation("  Cosmos DB Database ID: {DatabaseId}", databaseId);

    // Get the container
    var containerManager = app.Services.GetRequiredService<IContainerManager<DomainRegistration>>();
    var container = await containerManager.EnsureContainerAsync();

    // Query all domain registrations that don't have LastUpdatedAt set or have default value
    var query = new QueryDefinition(
        "SELECT * FROM c WHERE c.lastUpdatedAt = null OR NOT IS_DEFINED(c.lastUpdatedAt) OR c.lastUpdatedAt = '0001-01-01T00:00:00Z'");

    using var iterator = container.GetItemQueryIterator<DomainRegistration>(query);
    
    var recordsToUpdate = new List<DomainRegistration>();
    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        recordsToUpdate.AddRange(response.Resource);
    }

    logger.LogInformation("Found {Count} domain registration(s) to migrate", recordsToUpdate.Count);

    if (recordsToUpdate.Count == 0)
    {
        logger.LogInformation("No records need migration. All records already have LastUpdatedAt set.");
        return;
    }

    var updateCount = 0;
    var errorCount = 0;

    foreach (var record in recordsToUpdate)
    {
        try
        {
            // Set LastUpdatedAt to CreatedAt (the date of the first request)
            record.LastUpdatedAt = record.CreatedAt;

            // Update the record in Cosmos DB
            await container.ReplaceItemAsync(
                record, 
                record.id, 
                new PartitionKey(record.Upn));

            updateCount++;
            logger.LogInformation("Updated record {Id} (UPN: {Upn}): Set LastUpdatedAt to {LastUpdatedAt}", 
                record.id, record.Upn, record.LastUpdatedAt);
        }
        catch (Exception ex)
        {
            errorCount++;
            logger.LogError(ex, "Failed to update record {Id} (UPN: {Upn})", record.id, record.Upn);
        }
    }

    logger.LogInformation("Migration completed! Updated: {UpdateCount}, Errors: {ErrorCount}", updateCount, errorCount);

    if (errorCount > 0)
    {
        logger.LogWarning("Migration completed with errors. Please review the logs above.");
        Environment.Exit(1);
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to migrate DomainRegistration data");
    Environment.Exit(1);
}
