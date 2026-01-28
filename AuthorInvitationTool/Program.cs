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
if (args.Length < 1)
{
    PrintUsage();
    return 1;
}

string command = args[0].ToLower();

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
    var invitationRepository = app.Services.GetRequiredService<IAuthorInvitationRepository>();
    var emailService = app.Services.GetRequiredService<IEmailService>();

    switch (command)
    {
        case "create":
            return await CreateInvitation(args, invitationRepository, emailService, emailConnectionString, logger);
        
        case "list":
            return await ListInvitations(invitationRepository, logger);
        
        case "get":
            return await GetInvitation(args, invitationRepository, logger);
        
        case "update":
            return await UpdateInvitation(args, invitationRepository, logger);
        
        case "resend":
            return await ResendInvitation(args, invitationRepository, emailService, emailConnectionString, logger);
        
        default:
            Console.WriteLine($"❌ Unknown command: {command}");
            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to execute command");
    Console.WriteLine();
    Console.WriteLine("❌ Error: " + ex.Message);
    Console.WriteLine();
    if (ex.InnerException != null)
    {
        Console.WriteLine("Inner Error: " + ex.InnerException.Message);
    }
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Usage: AuthorInvitationTool <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  create <email> <domain1> [domain2 ...] [--notes \"notes\"]");
    Console.WriteLine("      Create a new invitation with one or more domains");
    Console.WriteLine();
    Console.WriteLine("  list");
    Console.WriteLine("      List all pending invitations");
    Console.WriteLine();
    Console.WriteLine("  get <invitation-id>");
    Console.WriteLine("      Get details of a specific invitation");
    Console.WriteLine();
    Console.WriteLine("  update <invitation-id> --domains <domain1> [domain2 ...] [--notes \"notes\"]");
    Console.WriteLine("      Update an existing pending invitation");
    Console.WriteLine();
    Console.WriteLine("  resend <invitation-id>");
    Console.WriteLine("      Resend invitation email");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  AuthorInvitationTool create author@example.com example.com");
    Console.WriteLine("  AuthorInvitationTool create author@example.com example.com author-site.com --notes \"Premium author\"");
    Console.WriteLine("  AuthorInvitationTool list");
    Console.WriteLine("  AuthorInvitationTool get abc-123-def-456");
    Console.WriteLine("  AuthorInvitationTool update abc-123-def-456 --domains example.com newdomain.com");
    Console.WriteLine("  AuthorInvitationTool resend abc-123-def-456");
    Console.WriteLine();
}

static async Task<int> CreateInvitation(
    string[] args,
    IAuthorInvitationRepository invitationRepository,
    IEmailService emailService,
    string emailConnectionString,
    ILogger logger)
{
    if (args.Length < 3)
    {
        Console.WriteLine("❌ Error: 'create' command requires at least email and one domain");
        Console.WriteLine("Usage: AuthorInvitationTool create <email> <domain1> [domain2 ...] [--notes \"notes\"]");
        return 1;
    }

    string emailAddress = args[1];
    var domains = new List<string>();
    string? notes = null;

    // Parse arguments
    for (int i = 2; i < args.Length; i++)
    {
        if (args[i] == "--notes" && i + 1 < args.Length)
        {
            notes = args[i + 1];
            i++; // Skip next argument as it's the notes value
        }
        else if (!args[i].StartsWith("--"))
        {
            domains.Add(args[i]);
        }
    }

    if (!domains.Any())
    {
        Console.WriteLine("❌ Error: At least one domain is required");
        return 1;
    }

    // Validate email format
    if (!IsValidEmail(emailAddress))
    {
        Console.WriteLine($"❌ Error: Invalid email address format: {emailAddress}");
        return 1;
    }

    // Validate domain formats
    foreach (var domain in domains)
    {
        if (!IsValidDomain(domain))
        {
            Console.WriteLine($"❌ Error: Invalid domain name format: {domain}");
            return 1;
        }
    }

    logger.LogInformation("Starting Author Invitation Tool...");
    logger.LogInformation("Email: {Email}", emailAddress);
    logger.LogInformation("Domains: {Domains}", string.Join(", ", domains));
    if (!string.IsNullOrWhiteSpace(notes))
    {
        logger.LogInformation("Notes: {Notes}", notes);
    }

    // Check if invitation already exists
    var existingInvitation = await invitationRepository.GetByEmailAsync(emailAddress);
    if (existingInvitation != null)
    {
        Console.WriteLine();
        Console.WriteLine($"⚠️  An invitation already exists for {emailAddress}");
        Console.WriteLine($"   ID: {existingInvitation.id}");
        Console.WriteLine($"   Status: {existingInvitation.Status}");
        Console.WriteLine($"   Domains: {string.Join(", ", existingInvitation.DomainNames)}");
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
    var invitation = new AuthorInvitation(emailAddress, domains, notes);
    var savedInvitation = await invitationRepository.AddAsync(invitation);
    
    Console.WriteLine("✅ Invitation created successfully!");
    Console.WriteLine($"   Invitation ID: {savedInvitation.id}");
    Console.WriteLine($"   Email: {savedInvitation.EmailAddress}");
    Console.WriteLine($"   Domains: {string.Join(", ", savedInvitation.DomainNames)}");
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
            string.Join(", ", savedInvitation.DomainNames),
            savedInvitation.id);

        if (emailSent)
        {
            Console.WriteLine("✅ Invitation email sent successfully!");
            savedInvitation.LastEmailSentAt = DateTime.UtcNow;
            await invitationRepository.UpdateAsync(savedInvitation);
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

static async Task<int> ListInvitations(
    IAuthorInvitationRepository invitationRepository,
    ILogger logger)
{
    logger.LogInformation("Listing all pending invitations...");
    
    var invitations = await invitationRepository.GetPendingInvitationsAsync();
    
    if (!invitations.Any())
    {
        Console.WriteLine("No pending invitations found.");
        return 0;
    }

    Console.WriteLine($"Found {invitations.Count} pending invitation(s):");
    Console.WriteLine();
    
    foreach (var invitation in invitations.OrderByDescending(i => i.CreatedAt))
    {
        Console.WriteLine($"ID: {invitation.id}");
        Console.WriteLine($"  Email: {invitation.EmailAddress}");
        Console.WriteLine($"  Domains: {string.Join(", ", invitation.DomainNames)}");
        Console.WriteLine($"  Status: {invitation.Status}");
        Console.WriteLine($"  Created: {invitation.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"  Expires: {invitation.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
        if (invitation.LastEmailSentAt.HasValue)
        {
            Console.WriteLine($"  Last Email Sent: {invitation.LastEmailSentAt:yyyy-MM-dd HH:mm:ss} UTC");
        }
        if (!string.IsNullOrWhiteSpace(invitation.Notes))
        {
            Console.WriteLine($"  Notes: {invitation.Notes}");
        }
        Console.WriteLine();
    }

    return 0;
}

static async Task<int> GetInvitation(
    string[] args,
    IAuthorInvitationRepository invitationRepository,
    ILogger logger)
{
    if (args.Length < 2)
    {
        Console.WriteLine("❌ Error: 'get' command requires invitation ID");
        Console.WriteLine("Usage: AuthorInvitationTool get <invitation-id>");
        return 1;
    }

    string invitationId = args[1];
    logger.LogInformation("Getting invitation {InvitationId}...", invitationId);
    
    var invitation = await invitationRepository.GetByIdAsync(invitationId);
    
    if (invitation == null)
    {
        Console.WriteLine($"❌ Invitation with ID {invitationId} not found.");
        return 1;
    }

    Console.WriteLine($"Invitation Details:");
    Console.WriteLine($"  ID: {invitation.id}");
    Console.WriteLine($"  Email: {invitation.EmailAddress}");
    Console.WriteLine($"  Domains: {string.Join(", ", invitation.DomainNames)}");
    Console.WriteLine($"  Status: {invitation.Status}");
    Console.WriteLine($"  Created: {invitation.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine($"  Expires: {invitation.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
    if (invitation.LastUpdatedAt.HasValue)
    {
        Console.WriteLine($"  Last Updated: {invitation.LastUpdatedAt:yyyy-MM-dd HH:mm:ss} UTC");
    }
    if (invitation.LastEmailSentAt.HasValue)
    {
        Console.WriteLine($"  Last Email Sent: {invitation.LastEmailSentAt:yyyy-MM-dd HH:mm:ss} UTC");
    }
    if (!string.IsNullOrWhiteSpace(invitation.Notes))
    {
        Console.WriteLine($"  Notes: {invitation.Notes}");
    }

    return 0;
}

static async Task<int> UpdateInvitation(
    string[] args,
    IAuthorInvitationRepository invitationRepository,
    ILogger logger)
{
    if (args.Length < 2)
    {
        Console.WriteLine("❌ Error: 'update' command requires invitation ID");
        Console.WriteLine("Usage: AuthorInvitationTool update <invitation-id> --domains <domain1> [domain2 ...] [--notes \"notes\"]");
        return 1;
    }

    string invitationId = args[1];
    var domains = new List<string>();
    string? notes = null;

    // Parse arguments
    bool inDomains = false;
    for (int i = 2; i < args.Length; i++)
    {
        if (args[i] == "--domains")
        {
            inDomains = true;
        }
        else if (args[i] == "--notes" && i + 1 < args.Length)
        {
            notes = args[i + 1];
            i++; // Skip next argument as it's the notes value
            inDomains = false;
        }
        else if (inDomains && !args[i].StartsWith("--"))
        {
            domains.Add(args[i]);
        }
    }

    logger.LogInformation("Updating invitation {InvitationId}...", invitationId);
    
    var invitation = await invitationRepository.GetByIdAsync(invitationId);
    
    if (invitation == null)
    {
        Console.WriteLine($"❌ Invitation with ID {invitationId} not found.");
        return 1;
    }

    if (invitation.Status != "Pending")
    {
        Console.WriteLine($"❌ Cannot update invitation with status '{invitation.Status}'. Only pending invitations can be updated.");
        return 1;
    }

    // Update fields
    if (domains.Any())
    {
        // Validate domain formats
        foreach (var domain in domains)
        {
            if (!IsValidDomain(domain))
            {
                Console.WriteLine($"❌ Error: Invalid domain name format: {domain}");
                return 1;
            }
        }
        invitation.DomainNames = domains;
#pragma warning disable CS0618 // Type or member is obsolete
        invitation.DomainName = domains.First();
#pragma warning restore CS0618
    }

    if (notes != null)
    {
        invitation.Notes = notes;
    }

    invitation.LastUpdatedAt = DateTime.UtcNow;

    var updatedInvitation = await invitationRepository.UpdateAsync(invitation);
    
    Console.WriteLine("✅ Invitation updated successfully!");
    Console.WriteLine($"  ID: {updatedInvitation.id}");
    Console.WriteLine($"  Email: {updatedInvitation.EmailAddress}");
    Console.WriteLine($"  Domains: {string.Join(", ", updatedInvitation.DomainNames)}");
    Console.WriteLine($"  Status: {updatedInvitation.Status}");
    Console.WriteLine($"  Last Updated: {updatedInvitation.LastUpdatedAt:yyyy-MM-dd HH:mm:ss} UTC");
    if (!string.IsNullOrWhiteSpace(updatedInvitation.Notes))
    {
        Console.WriteLine($"  Notes: {updatedInvitation.Notes}");
    }

    return 0;
}

static async Task<int> ResendInvitation(
    string[] args,
    IAuthorInvitationRepository invitationRepository,
    IEmailService emailService,
    string emailConnectionString,
    ILogger logger)
{
    if (args.Length < 2)
    {
        Console.WriteLine("❌ Error: 'resend' command requires invitation ID");
        Console.WriteLine("Usage: AuthorInvitationTool resend <invitation-id>");
        return 1;
    }

    string invitationId = args[1];
    logger.LogInformation("Resending invitation {InvitationId}...", invitationId);
    
    var invitation = await invitationRepository.GetByIdAsync(invitationId);
    
    if (invitation == null)
    {
        Console.WriteLine($"❌ Invitation with ID {invitationId} not found.");
        return 1;
    }

    if (invitation.Status != "Pending")
    {
        Console.WriteLine($"❌ Cannot resend invitation with status '{invitation.Status}'. Only pending invitations can be resent.");
        return 1;
    }

    if (emailConnectionString == "not-configured")
    {
        Console.WriteLine("❌ Email service not configured - cannot resend invitation.");
        Console.WriteLine("   Configure Azure Communication Services to enable email notifications.");
        return 1;
    }

    Console.WriteLine("Resending invitation email...");
    var emailSent = await emailService.SendInvitationEmailAsync(
        invitation.EmailAddress,
        string.Join(", ", invitation.DomainNames),
        invitation.id);

    if (emailSent)
    {
        Console.WriteLine("✅ Invitation email resent successfully!");
        invitation.LastEmailSentAt = DateTime.UtcNow;
        await invitationRepository.UpdateAsync(invitation);
        Console.WriteLine($"   Last Email Sent: {invitation.LastEmailSentAt:yyyy-MM-dd HH:mm:ss} UTC");
    }
    else
    {
        Console.WriteLine("❌ Failed to resend invitation email.");
        return 1;
    }

    return 0;
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
