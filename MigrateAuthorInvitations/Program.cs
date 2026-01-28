using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("   Author Invitation Migration Tool");
Console.WriteLine("   Migrates existing invitations to support multiple domains");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

// Build host with configuration
var builder = Host.CreateApplicationBuilder(args);

// Configuration setup
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

var config = builder.Configuration;

// Get Cosmos configuration
var endpointUri = config["CosmosDb:EndpointUri"] ?? config["COSMOSDB_ENDPOINT_URI"];
var primaryKey = config["CosmosDb:PrimaryKey"] ?? config["COSMOSDB_PRIMARY_KEY"];
var databaseId = config["CosmosDb:DatabaseId"] ?? config["COSMOSDB_DATABASE_ID"] ?? "OnePageAuthor";

if (string.IsNullOrWhiteSpace(endpointUri))
{
    Console.WriteLine("❌ Error: COSMOSDB_ENDPOINT_URI is required.");
    return 1;
}

if (string.IsNullOrWhiteSpace(primaryKey))
{
    Console.WriteLine("❌ Error: COSMOSDB_PRIMARY_KEY is required.");
    return 1;
}

// Configure services
builder.Services
    .AddLogging(logging => logging.AddConsole())
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddAuthorInvitationRepository();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting Author Invitation Migration Tool...");
    logger.LogInformation("Database: {DatabaseId}", databaseId);

    var invitationRepository = app.Services.GetRequiredService<IAuthorInvitationRepository>();

    // Get all pending invitations
    Console.WriteLine("Fetching all pending invitations...");
    var invitations = await invitationRepository.GetPendingInvitationsAsync();
    
    Console.WriteLine($"Found {invitations.Count} pending invitation(s)");
    Console.WriteLine();

    if (!invitations.Any())
    {
        Console.WriteLine("✅ No pending invitations found. Nothing to migrate.");
        return 0;
    }

    // Check which invitations need migration
    var needsMigration = invitations.Where(i => 
        (i.DomainNames == null || i.DomainNames.Count == 0) && 
        !string.IsNullOrWhiteSpace(i.DomainName)).ToList();

    if (!needsMigration.Any())
    {
        Console.WriteLine("✅ All invitations are already migrated. Nothing to do.");
        return 0;
    }

    Console.WriteLine($"Found {needsMigration.Count} invitation(s) that need migration:");
    Console.WriteLine();

    foreach (var invitation in needsMigration)
    {
        Console.WriteLine($"  ID: {invitation.id}");
        Console.WriteLine($"    Email: {invitation.EmailAddress}");
        Console.WriteLine($"    DomainName: {invitation.DomainName}");
        Console.WriteLine($"    DomainNames: {(invitation.DomainNames?.Count ?? 0)} items");
        Console.WriteLine();
    }

    Console.Write("Do you want to migrate these invitations? (y/n): ");
    var response = Console.ReadLine()?.Trim().ToLower();
    if (response != "y" && response != "yes")
    {
        Console.WriteLine("Migration cancelled.");
        return 0;
    }

    Console.WriteLine();
    Console.WriteLine("Starting migration...");
    Console.WriteLine();

    int migratedCount = 0;
    int errorCount = 0;

    foreach (var invitation in needsMigration)
    {
        try
        {
            // Ensure DomainNames is populated from DomainName
            if (invitation.DomainNames == null || invitation.DomainNames.Count == 0)
            {
                invitation.DomainNames = new List<string> { invitation.DomainName };
            }

            // Update the invitation
            await invitationRepository.UpdateAsync(invitation);
            
            Console.WriteLine($"✅ Migrated: {invitation.EmailAddress} ({invitation.id})");
            migratedCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error migrating {invitation.EmailAddress}: {ex.Message}");
            logger.LogError(ex, "Failed to migrate invitation {InvitationId}", invitation.id);
            errorCount++;
        }
    }

    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine($"✅ Migration completed!");
    Console.WriteLine($"   Migrated: {migratedCount}");
    if (errorCount > 0)
    {
        Console.WriteLine($"   Errors: {errorCount}");
    }
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    
    return errorCount > 0 ? 1 : 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Migration failed");
    Console.WriteLine();
    Console.WriteLine("❌ Error: " + ex.Message);
    Console.WriteLine();
    if (ex.InnerException != null)
    {
        Console.WriteLine("Inner Error: " + ex.InnerException.Message);
    }
    return 1;
}
