using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("   Author Invitation Tool - One Page Author Platform");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

// Parse command-line arguments
if (args.Length < 2)
{
    Console.WriteLine("Usage: AuthorInvitationTool <email> <domain> [notes]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  email   - The email address of the author to invite");
    Console.WriteLine("  domain  - The domain name to link to the author's account (e.g., example.com)");
    Console.WriteLine("  notes   - Optional notes about the invitation");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  AuthorInvitationTool author@example.com example.com \"Invitation for John Doe\"");
    Console.WriteLine();
    return 1;
}

string emailAddress = args[0];
string domainName = args[1];
string? notes = args.Length > 2 ? args[2] : null;

// Validate email format
if (!IsValidEmail(emailAddress))
{
    Console.WriteLine($"❌ Error: Invalid email address format: {emailAddress}");
    return 1;
}

// Validate domain format
if (!IsValidDomain(domainName))
{
    Console.WriteLine($"❌ Error: Invalid domain name format: {domainName}");
    return 1;
}

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
    Console.WriteLine("❌ Error: COSMOSDB_ENDPOINT_URI is required. Set it in appsettings.json, user secrets, or environment variables.");
    return 1;
}

if (string.IsNullOrWhiteSpace(primaryKey))
{
    Console.WriteLine("❌ Error: COSMOSDB_PRIMARY_KEY is required. Set it in appsettings.json, user secrets, or environment variables.");
    return 1;
}

// Get Email service configuration
var emailConnectionString = config["Email:AzureCommunicationServices:ConnectionString"] ?? config["ACS_CONNECTION_STRING"];
var emailSenderAddress = config["Email:AzureCommunicationServices:SenderAddress"] ?? config["ACS_SENDER_ADDRESS"] ?? "DoNotReply@onepageauthor.com";

if (string.IsNullOrWhiteSpace(emailConnectionString))
{
    Console.WriteLine("⚠️  Warning: Azure Communication Services connection string not configured. Email will not be sent.");
    Console.WriteLine("   Set ACS_CONNECTION_STRING in environment variables or appsettings.json");
    emailConnectionString = "not-configured";
}

// Configure services
builder.Services
    .AddLogging(logging => logging.AddConsole())
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddAuthorInvitationRepository()
    .AddEmailService(emailConnectionString, emailSenderAddress);

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting Author Invitation Tool...");
    logger.LogInformation("Email: {Email}", emailAddress);
    logger.LogInformation("Domain: {Domain}", domainName);
    if (!string.IsNullOrWhiteSpace(notes))
    {
        logger.LogInformation("Notes: {Notes}", notes);
    }
    logger.LogInformation("Database: {DatabaseId}", databaseId);

    // Get services
    var invitationRepository = app.Services.GetRequiredService<IAuthorInvitationRepository>();
    var emailService = app.Services.GetRequiredService<IEmailService>();

    // Check if invitation already exists
    var existingInvitation = await invitationRepository.GetByEmailAsync(emailAddress);
    if (existingInvitation != null)
    {
        Console.WriteLine();
        Console.WriteLine($"⚠️  An invitation already exists for {emailAddress}");
        Console.WriteLine($"   Status: {existingInvitation.Status}");
        Console.WriteLine($"   Domain: {existingInvitation.DomainName}");
        Console.WriteLine($"   Created: {existingInvitation.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"   Expires: {existingInvitation.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine();
        Console.Write("Do you want to create a new invitation anyway? (y/n): ");
        var response = Console.ReadLine()?.Trim().ToLower();
        if (response != "y" && response != "yes")
        {
            Console.WriteLine("Operation cancelled.");
            return 0;
        }
    }

    // Create the invitation
    Console.WriteLine();
    Console.WriteLine("Creating invitation...");
    var invitation = new AuthorInvitation(emailAddress, domainName, notes);
    var savedInvitation = await invitationRepository.AddAsync(invitation);
    
    Console.WriteLine("✅ Invitation created successfully!");
    Console.WriteLine($"   Invitation ID: {savedInvitation.id}");
    Console.WriteLine($"   Email: {savedInvitation.EmailAddress}");
    Console.WriteLine($"   Domain: {savedInvitation.DomainName}");
    Console.WriteLine($"   Status: {savedInvitation.Status}");
    Console.WriteLine($"   Created: {savedInvitation.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine($"   Expires: {savedInvitation.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");

    // Send invitation email
    if (emailConnectionString != "not-configured")
    {
        Console.WriteLine();
        Console.WriteLine("Sending invitation email...");
        var emailSent = await emailService.SendInvitationEmailAsync(
            savedInvitation.EmailAddress,
            savedInvitation.DomainName,
            savedInvitation.id);

        if (emailSent)
        {
            Console.WriteLine("✅ Invitation email sent successfully!");
        }
        else
        {
            Console.WriteLine("⚠️  Warning: Failed to send invitation email.");
            Console.WriteLine("   The invitation has been created in the database, but the email notification failed.");
        }
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine("⚠️  Email service not configured - invitation created but email not sent.");
        Console.WriteLine("   Configure Azure Communication Services to enable email notifications.");
    }

    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("✅ Operation completed successfully!");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to create author invitation");
    Console.WriteLine();
    Console.WriteLine("❌ Error: " + ex.Message);
    Console.WriteLine();
    if (ex.InnerException != null)
    {
        Console.WriteLine("Inner Error: " + ex.InnerException.Message);
    }
    return 1;
}

// Helper methods
static bool IsValidEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return false;

    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}

static bool IsValidDomain(string domain)
{
    if (string.IsNullOrWhiteSpace(domain))
        return false;

    // Basic domain validation - at least one dot and no spaces
    return domain.Contains('.') && !domain.Contains(' ') && domain.Length > 3;
}
