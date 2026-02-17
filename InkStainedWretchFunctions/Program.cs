using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Functions.Testing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Stripe;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

// Add User Secrets support for development
// This allows secrets to be stored securely outside of source code
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var configuration = builder.Configuration;
var endpointUri = configuration["COSMOSDB_ENDPOINT_URI"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
var primaryKey = configuration["COSMOSDB_PRIMARY_KEY"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
var databaseId = configuration["COSMOSDB_DATABASE_ID"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

// Stripe configuration (optional for domain registration updates)
var stripeApiKey = configuration["STRIPE_API_KEY"];

// Email service configuration (optional for author invitation emails)
var emailConnectionString = configuration["ACS_CONNECTION_STRING"];
var emailSenderAddress = configuration["ACS_SENDER_ADDRESS"] ?? "DoNotReply@onepageauthor.com";

// Log Cosmos DB configuration (masked for security)
Console.WriteLine($"Cosmos DB Endpoint configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskUrl(endpointUri)}");
Console.WriteLine($"Cosmos DB Primary Key configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(primaryKey)}");
Console.WriteLine($"Cosmos DB Database ID configured: {databaseId}");

// Sanitization status: show if trimming removed quotes/whitespace
string Sanitize(string? v) => v?.Trim().Trim('\'').Trim('"') ?? v ?? string.Empty;
var sanitizedEndpoint = Sanitize(endpointUri);
var sanitizedPrimaryKey = Sanitize(primaryKey);
var sanitizedDatabaseId = Sanitize(databaseId);
var sanitizationApplied =
    (!string.Equals(endpointUri, sanitizedEndpoint, StringComparison.Ordinal)) ||
    (!string.Equals(primaryKey, sanitizedPrimaryKey, StringComparison.Ordinal)) ||
    (!string.Equals(databaseId, sanitizedDatabaseId, StringComparison.Ordinal));
Console.WriteLine($"Config sanitization applied: {(sanitizationApplied ? "yes" : "no")}");

if (!string.IsNullOrWhiteSpace(stripeApiKey))
{
    Console.WriteLine($"Stripe API key configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(stripeApiKey)}");
}
else
{
    Console.WriteLine("Warning: STRIPE_API_KEY not configured. Subscription validation for domain updates will not work.");
}

if (!string.IsNullOrWhiteSpace(emailConnectionString))
{
    Console.WriteLine($"Azure Communication Services connection string configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(emailConnectionString)}");
    Console.WriteLine($"Email sender address configured: {emailSenderAddress}");
}
else
{
    Console.WriteLine("Warning: ACS_CONNECTION_STRING not configured. Invitation emails will not be sent.");
}

builder.ConfigureFunctionsWebApplication();

// Configure Entra ID (Azure AD) authentication with optional multiple issuers
var tenantId = configuration["AAD_TENANT_ID"];
var audience = configuration["AAD_AUDIENCE"] ?? configuration["AAD_CLIENT_ID"];
var authority = configuration["AAD_AUTHORITY"] ?? (string.IsNullOrWhiteSpace(tenantId) ? null : $"https://login.microsoftonline.com/{tenantId}/v2.0");
var validIssuersRaw = configuration["AAD_VALID_ISSUERS"];
string[]? validIssuers = InkStainedWretch.OnePageAuthorAPI.Utility.ParseValidIssuers(validIssuersRaw);

// Masked confirmation logs
Console.WriteLine($"Azure AD Tenant ID configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(tenantId)}");
Console.WriteLine($"Azure AD Audience configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(audience)}");
Console.WriteLine($"Azure AD Authority configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskUrl(authority)}");
Console.WriteLine($"Azure AD Valid Issuers configured: {(validIssuers is null ? "(not set)" : string.Join(", ", validIssuers.Select(i => InkStainedWretch.OnePageAuthorAPI.Utility.MaskUrl(i))))}");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }
        if (!string.IsNullOrWhiteSpace(audience))
        {
            options.Audience = audience;
        }
        
        // Enable automatic metadata refresh when signing key is not found
        // This helps prevent SecurityTokenSignatureKeyNotFoundException when Azure AD rotates keys
        options.RefreshOnIssuerKeyNotFound = true;
        
        // Configure automatic refresh of signing keys from OpenID Connect metadata
        // Prefer an explicit OPEN_ID_CONNECT_METADATA_URL if provided; otherwise derive from authority
        var metadataAddress = configuration["OPEN_ID_CONNECT_METADATA_URL"];
        if (string.IsNullOrWhiteSpace(metadataAddress) && !string.IsNullOrWhiteSpace(authority))
        {
            metadataAddress = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
        }

        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever())
            {
                // Refresh metadata every 6 hours (default is 24 hours)
                AutomaticRefreshInterval = TimeSpan.FromHours(6),
                // Minimum time between refreshes to prevent hammering the endpoint
                RefreshInterval = TimeSpan.FromMinutes(30)
            };
        }
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = audience,
            ValidIssuer = validIssuers is null ? authority : null,
            ValidIssuers = validIssuers
        };
    });

var services = builder.Services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddUserProfileRepository()
    .AddAuthorRepositories() // Register author repositories (IAuthorRepository, IGenericRepository<Book>, etc.)
    .AddAuthorDataService() // Add Author data service for GetAuthors function
    .AddInkStainedWretchServices()
    .AddPenguinRandomHouseServices()
    .AddAmazonProductServices() // Add Amazon Product API services
    .AddWikipediaServices() // Add Wikipedia API services
    .AddJwtAuthentication() // Add JWT authentication services from OnePageAuthorLib
    .AddUserProfileServices()
    .AddDomainRegistrationRepository() // Add domain registration repository
    .AddDomainRegistrationServices() // Add domain registration services
    .AddStateProvinceRepository() // Add StateProvince repository
    .AddStateProvinceServices() // Add StateProvince services
    .AddCountryRepository() // Add Country repository
    .AddCountryServices() // Add Country services
    .AddLanguageRepository() // Add Language repository
    .AddLanguageServices() // Add Language services
    .AddPlatformStatsRepository() // Add PlatformStats repository
    .AddPlatformStatsService() // Add PlatformStats service for landing page social proof
    .AddDnsZoneService() // Add DNS zone service for domain registration triggers
    .AddFrontDoorServices() // Add Azure Front Door services for domain management
    .AddWhmcsService() // Add WHMCS service for domain registration via WHMCS API
    .AddReferralRepository() // Add Referral repository for referral program
    .AddReferralServices() // Add Referral services for referral program
    .AddLeadRepository() // Add Lead repository for lead capture
    .AddLeadServices() // Add Lead services for lead capture and management
    .AddTestingServices() // Add testing services for mock implementations and test harnesses
    .AddExperimentRepository() // Add Experiment repository for A/B testing
    .AddExperimentServices() // Add Experiment services for A/B testing
    .AddTestimonialRepository() // Add Testimonial repository for testimonials management
    .AddAuthorInvitationRepository() // Add AuthorInvitation repository for managing author invitations
    .AddImageApiRepositories();

// Add Stripe services if API key is configured (needed for subscription validation)
if (!string.IsNullOrWhiteSpace(stripeApiKey))
{
    services.AddSingleton<StripeClient>(_ => new StripeClient(stripeApiKey))
            .AddStripeServices()
            .AddStripeOrchestrators();
}

// Add Email service if connection string is configured (needed for invitation emails)
if (!string.IsNullOrWhiteSpace(emailConnectionString))
{
    services.AddEmailService(emailConnectionString, emailSenderAddress);
}

// OpenTelemetry -> Azure Monitor (Application Insights backend)
services
    .AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Build().Run();

// Program class needed for User Secrets generic type parameter
public partial class Program { }
