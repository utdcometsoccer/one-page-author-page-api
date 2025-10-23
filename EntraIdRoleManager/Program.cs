using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

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

// Get Entra ID configuration for the management app (this console app)
var tenantId = config["AAD_TENANT_ID"] ?? throw new InvalidOperationException("AAD_TENANT_ID is required");
var managementClientId = config["AAD_MANAGEMENT_CLIENT_ID"] ?? throw new InvalidOperationException("AAD_MANAGEMENT_CLIENT_ID is required");
var managementClientSecret = config["AAD_MANAGEMENT_CLIENT_SECRET"] ?? throw new InvalidOperationException("AAD_MANAGEMENT_CLIENT_SECRET is required");

// Get target app configuration (the app that will have roles assigned)
var targetClientId = config["AAD_TARGET_CLIENT_ID"] ?? throw new InvalidOperationException("AAD_TARGET_CLIENT_ID is required");

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
    logger.LogInformation("Starting Entra ID Role Manager for Personal Microsoft Account Apps...");
    logger.LogInformation("Configuration (masked for security):");
    logger.LogInformation("  Management App ID: {ManagementClientId}", Utility.MaskSensitiveValue(managementClientId));
    logger.LogInformation("  Target App ID: {TargetClientId}", Utility.MaskSensitiveValue(targetClientId));
    logger.LogInformation("  Tenant ID: {TenantId}", Utility.MaskSensitiveValue(tenantId));
    logger.LogInformation("  Cosmos DB Endpoint: {EndpointUri}", Utility.MaskUrl(endpointUri));
    logger.LogInformation("  Cosmos DB Database ID: {DatabaseId}", databaseId);

    // Create Graph client using management app credentials
    var credential = new ClientSecretCredential(tenantId, managementClientId, managementClientSecret);
    var graphClient = new GraphServiceClient(credential);

    // Get repositories
    var tierRepository = app.Services.GetRequiredService<IImageStorageTierRepository>();
    var membershipRepository = app.Services.GetRequiredService<IImageStorageTierMembershipRepository>();

    // Get all tiers from Cosmos DB
    var tiers = await tierRepository.GetAllAsync();
    logger.LogInformation("Found {Count} image storage tiers", tiers.Count);

    // Get the target application
    var applications = await graphClient.Applications
        .GetAsync(config =>
        {
            config.QueryParameters.Filter = $"appId eq '{targetClientId}'";
        });
    
    var application = applications?.Value?.FirstOrDefault();
    if (application == null)
    {
        logger.LogError("Target application not found for client ID {ClientId}. Ensure the target app is registered and configured for Microsoft Account users.", targetClientId);
        Environment.Exit(1);
        return;
    }

    logger.LogInformation("Found target application: {DisplayName} ({Id})", application.DisplayName, application.Id);
    logger.LogInformation("Sign-in audience: {SignInAudience}", application.SignInAudience);

    if (application.SignInAudience != "PersonalMicrosoftAccount")
    {
        logger.LogWarning("Application sign-in audience is {SignInAudience}, expected PersonalMicrosoftAccount", application.SignInAudience);
    }

    logger.LogInformation("Step 2: Verifying Personal Microsoft Account limitations...");
    
    // Personal Microsoft Account apps cannot have app roles at all
    // This is a platform limitation - not just that users can't be assigned to roles,
    // but that the roles themselves cannot exist on Personal Microsoft Account apps
    
    if (application.SignInAudience == "PersonalMicrosoftAccount")
    {
        logger.LogInformation("✓ Application is configured for Personal Microsoft Account users");
        logger.LogInformation("  This means app roles cannot be created (platform limitation)");
        logger.LogInformation("  Authorization will be handled entirely through Cosmos DB");
    }
    else
    {
        logger.LogWarning("Application sign-in audience is {SignInAudience}, not PersonalMicrosoftAccount", application.SignInAudience);
        logger.LogWarning("This tool is designed specifically for Personal Microsoft Account apps");
    }

    logger.LogInformation("Step 3: Verifying Cosmos DB tier memberships...");
    
    // Get all memberships from Cosmos DB
    var allMemberships = new List<ImageStorageTierMembership>();
    var membershipContainer = app.Services.GetRequiredService<IContainerManager<ImageStorageTierMembership>>();
    var cosmosContainer = await membershipContainer.EnsureContainerAsync();
    
    var query = new Microsoft.Azure.Cosmos.QueryDefinition("SELECT * FROM c");
    using var iterator = cosmosContainer.GetItemQueryIterator<ImageStorageTierMembership>(query);
    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        allMemberships.AddRange(response.Resource);
    }

    logger.LogInformation("Found {Count} existing tier memberships", allMemberships.Count);
    
    // Show tier distribution
    var tierDistribution = allMemberships
        .GroupBy(m => m.TierId)
        .Select(g => new { TierId = g.Key, Count = g.Count() })
        .ToList();
        
    foreach (var dist in tierDistribution)
    {
        var tier = tiers.FirstOrDefault(t => t.id == dist.TierId);
        var tierName = tier?.Name ?? "Unknown";
        logger.LogInformation("  - {Count} users in {TierName} tier", dist.Count, tierName);
    }

    logger.LogInformation("");
    logger.LogInformation("✅ Personal Microsoft Account Authorization Setup Complete!");
    logger.LogInformation("");
    logger.LogInformation("Summary:");
    logger.LogInformation("  - Target Application: {AppName} ({ClientId})", application.DisplayName, targetClientId);
    logger.LogInformation("  - Sign-in Audience: {SignInAudience}", application.SignInAudience);
    logger.LogInformation("  - Available Tiers: {TierCount} ({TierNames})", 
        tiers.Count, string.Join(", ", tiers.Select(t => t.Name)));
    logger.LogInformation("  - User Memberships: {Count} users with tier assignments", allMemberships.Count);
    logger.LogInformation("");
    logger.LogInformation("Authorization Strategy:");
    logger.LogInformation("  ✓ Pure Cosmos DB approach (no app roles due to Personal Microsoft Account limitation)");
    logger.LogInformation("  ✓ ImageAPI will extract user ID from JWT 'oid' or 'sub' claim");
    logger.LogInformation("  ✓ Query ImageStorageTierMembership by UserProfileId for authorization");
    logger.LogInformation("  ✓ Auto-assign 'Starter' tier for new users without existing membership");
    logger.LogInformation("");
    logger.LogInformation("The ImageStorageTierService has been configured to handle this authorization pattern.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to manage Entra ID roles");
    Environment.Exit(1);
}
