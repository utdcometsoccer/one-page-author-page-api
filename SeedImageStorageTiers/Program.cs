using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
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
    .AddCosmosDatabase(databaseId)
    .AddImageApiRepositories();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting ImageStorageTier seeding...");

    // Get the repository
    var repository = app.Services.GetRequiredService<IImageStorageTierRepository>();

    // Define the tiers
    var tiers = new[]
    {
        new ImageStorageTier
        {
            id = Guid.NewGuid().ToString(),
            Name = "Starter",
            CostInDollars = 0.00m,
            StorageInGB = 5m,
            BandwidthInGB = 25m
        },
        new ImageStorageTier
        {
            id = Guid.NewGuid().ToString(),
            Name = "Pro",
            CostInDollars = 9.99m,
            StorageInGB = 250m,
            BandwidthInGB = 1024m // 1TB = 1024GB
        },
        new ImageStorageTier
        {
            id = Guid.NewGuid().ToString(),
            Name = "Elite",
            CostInDollars = 24.99m,
            StorageInGB = 2048m, // 2TB = 2048GB
            BandwidthInGB = 10240m // 10TB = 10240GB
        }
    };

    // Check if tiers already exist and add if needed
    foreach (var tier in tiers)
    {
        try
        {
            // Try to find existing tier by name (since we partition by Name)
            var query = new Microsoft.Azure.Cosmos.QueryDefinition("SELECT * FROM c WHERE c.Name = @name")
                .WithParameter("@name", tier.Name);
            
            var container = app.Services.GetRequiredService<IContainerManager<ImageStorageTier>>();
            var cosmosContainer = await container.EnsureContainerAsync();
            
            using var iterator = cosmosContainer.GetItemQueryIterator<ImageStorageTier>(query);
            var existing = new List<ImageStorageTier>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                existing.AddRange(response.Resource);
            }

            if (existing.Any())
            {
                logger.LogInformation("Tier '{TierName}' already exists, skipping.", tier.Name);
            }
            else
            {
                await repository.AddAsync(tier);
                logger.LogInformation("Added tier: {TierName} - ${Cost}/month, {Storage}GB storage, {Bandwidth}GB bandwidth", 
                    tier.Name, tier.CostInDollars, tier.StorageInGB, tier.BandwidthInGB);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process tier '{TierName}'", tier.Name);
        }
    }

    logger.LogInformation("ImageStorageTier seeding completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to seed ImageStorageTier data");
    Environment.Exit(1);
}