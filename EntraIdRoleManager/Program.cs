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
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

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

// Get Entra ID configuration
var tenantId = config["AAD_TENANT_ID"] ?? throw new InvalidOperationException("AAD_TENANT_ID is required");
var clientId = config["AAD_CLIENT_ID"] ?? throw new InvalidOperationException("AAD_CLIENT_ID is required");
var clientSecret = config["AAD_CLIENT_SECRET"] ?? throw new InvalidOperationException("AAD_CLIENT_SECRET is required");

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
    logger.LogInformation("Starting Entra ID Role Manager...");

    // Create Graph client
    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    var graphClient = new GraphServiceClient(credential);

    // Get repositories
    var tierRepository = app.Services.GetRequiredService<IImageStorageTierRepository>();
    var membershipRepository = app.Services.GetRequiredService<IImageStorageTierMembershipRepository>();

    // Get all tiers from Cosmos DB
    var tiers = await tierRepository.GetAllAsync();
    logger.LogInformation("Found {Count} image storage tiers", tiers.Count);

    // Create a map to store tier ID to role ID mapping
    var tierToRoleMap = new Dictionary<string, string>();

    // Step 1: Create Entra ID App Roles for each tier
    logger.LogInformation("Step 1: Creating Entra ID App Roles...");
    
    // Get the service principal for this application
    var servicePrincipals = await graphClient.ServicePrincipals
        .GetAsync(config =>
        {
            config.QueryParameters.Filter = $"appId eq '{clientId}'";
        });
    
    var servicePrincipal = servicePrincipals?.Value?.FirstOrDefault();
    if (servicePrincipal == null)
    {
        logger.LogError("Service principal not found for client ID {ClientId}", clientId);
        Environment.Exit(1);
        return;
    }

    logger.LogInformation("Found service principal: {DisplayName} ({Id})", servicePrincipal.DisplayName, servicePrincipal.Id);

    // Get the application
    var applications = await graphClient.Applications
        .GetAsync(config =>
        {
            config.QueryParameters.Filter = $"appId eq '{clientId}'";
        });
    
    var application = applications?.Value?.FirstOrDefault();
    if (application == null)
    {
        logger.LogError("Application not found for client ID {ClientId}", clientId);
        Environment.Exit(1);
        return;
    }

    logger.LogInformation("Found application: {DisplayName} ({Id})", application.DisplayName, application.Id);

    // Get existing app roles
    var existingRoles = application.AppRoles ?? new List<AppRole>();
    
    foreach (var tier in tiers)
    {
        // Check if role already exists
        var existingRole = existingRoles.FirstOrDefault(r => r.DisplayName == $"ImageStorageTier.{tier.Name}");
        
        if (existingRole != null)
        {
            logger.LogInformation("Role 'ImageStorageTier.{TierName}' already exists with ID {RoleId}", tier.Name, existingRole.Id);
            tierToRoleMap[tier.id] = existingRole.Id.ToString()!;
        }
        else
        {
            // Create new app role
            var newRole = new AppRole
            {
                Id = Guid.NewGuid(),
                DisplayName = $"ImageStorageTier.{tier.Name}",
                Description = $"Users in the {tier.Name} image storage tier (${tier.CostInDollars}/month, {tier.StorageInGB}GB storage, {tier.BandwidthInGB}GB bandwidth)",
                Value = $"ImageStorageTier.{tier.Name}",
                IsEnabled = true,
                AllowedMemberTypes = new List<string> { "User" }
            };

            existingRoles.Add(newRole);
            tierToRoleMap[tier.id] = newRole.Id.ToString()!;
            
            logger.LogInformation("Created role definition for 'ImageStorageTier.{TierName}' with ID {RoleId}", tier.Name, newRole.Id);
        }
    }

    // Update the application with new roles
    var updateApp = new Application
    {
        AppRoles = existingRoles
    };
    
    await graphClient.Applications[application.Id].PatchAsync(updateApp);
    logger.LogInformation("Updated application with app roles");

    // Wait a moment for Azure AD to propagate the changes
    logger.LogInformation("Waiting 10 seconds for role changes to propagate...");
    await Task.Delay(10000);

    // Step 2: Assign users to roles based on ImageStorageTierMembership
    logger.LogInformation("Step 2: Migrating existing tier memberships to Entra ID roles...");
    
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

    logger.LogInformation("Found {Count} existing tier memberships to migrate", allMemberships.Count);

    foreach (var membership in allMemberships)
    {
        try
        {
            if (!tierToRoleMap.TryGetValue(membership.TierId, out var roleId))
            {
                logger.LogWarning("Tier ID {TierId} not found in tier-to-role map, skipping membership {MembershipId}", 
                    membership.TierId, membership.id);
                continue;
            }

            var tier = tiers.FirstOrDefault(t => t.id == membership.TierId);
            var tierName = tier?.Name ?? "Unknown";

            // Create app role assignment
            var roleAssignment = new AppRoleAssignment
            {
                PrincipalId = Guid.Parse(membership.UserProfileId),
                ResourceId = Guid.Parse(servicePrincipal.Id!),
                AppRoleId = Guid.Parse(roleId)
            };

            try
            {
                await graphClient.ServicePrincipals[servicePrincipal.Id].AppRoleAssignedTo.PostAsync(roleAssignment);
                logger.LogInformation("Assigned user {UserId} to role 'ImageStorageTier.{TierName}'", 
                    membership.UserProfileId, tierName);
            }
            catch (Exception ex) when (ex.Message.Contains("already exists"))
            {
                logger.LogInformation("User {UserId} already assigned to role 'ImageStorageTier.{TierName}'", 
                    membership.UserProfileId, tierName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to assign user {UserId} to role for tier {TierId}", 
                membership.UserProfileId, membership.TierId);
        }
    }

    logger.LogInformation("Entra ID Role Manager completed successfully!");
    logger.LogInformation("Summary:");
    logger.LogInformation("  - Created/verified {Count} app roles", tiers.Count);
    logger.LogInformation("  - Processed {Count} user assignments", allMemberships.Count);
    logger.LogInformation("");
    logger.LogInformation("Next steps:");
    logger.LogInformation("  1. Update ImageAPI to use Entra ID roles from JWT token");
    logger.LogInformation("  2. Remove ImageStorageTierMembership usage from runtime code");
    logger.LogInformation("  3. Configure automatic assignment of users without roles to Starter tier");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to manage Entra ID roles");
    Environment.Exit(1);
}
