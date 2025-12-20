using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Functions.Testing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

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

// Log Cosmos DB configuration (masked for security)
Console.WriteLine($"Cosmos DB Endpoint configured: {Utility.MaskUrl(endpointUri)}");
Console.WriteLine($"Cosmos DB Primary Key configured: {Utility.MaskSensitiveValue(primaryKey)}");
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
    Console.WriteLine($"Stripe API key configured: {Utility.MaskSensitiveValue(stripeApiKey)}");
}
else
{
    Console.WriteLine("Warning: STRIPE_API_KEY not configured. Subscription validation for domain updates will not work.");
}

builder.ConfigureFunctionsWebApplication();

var services = builder.Services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddUserProfileRepository()
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
    .AddDnsZoneService() // Add DNS zone service for domain registration triggers
    .AddFrontDoorServices() // Add Azure Front Door services for domain management
    .AddGoogleDomainsService() // Add Google Domains service for domain registration
    .AddLeadRepository() // Add Lead repository for lead capture
    .AddLeadServices() // Add Lead services for lead capture and management
    .AddTestingServices() // Add testing services for mock implementations and test harnesses
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Add Stripe services if API key is configured (needed for subscription validation)
if (!string.IsNullOrWhiteSpace(stripeApiKey))
{
    services.AddSingleton<StripeClient>(_ => new StripeClient(stripeApiKey))
            .AddStripeServices()
            .AddStripeOrchestrators();
}

builder.Build().Run();

// Program class needed for User Secrets generic type parameter
public partial class Program { }
