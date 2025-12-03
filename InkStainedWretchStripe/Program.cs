using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

var builder = FunctionsApplication.CreateBuilder(args);
var config = builder.Configuration;
var stripeApiKey = config["STRIPE_API_KEY"] ?? throw new InvalidOperationException("Configuration value 'STRIPE_API_KEY' is missing or empty. Please set it to your Stripe API key.");
// Configure Stripe client lifetime
// Best practice: StripeClient is thread-safe; reuse via Singleton for app-wide API key.
// If you ever need per-user/tenant keys, switch to Scoped and construct with the appropriate key per request.
// Masked confirmation log (do not log full secret)
Console.WriteLine($"Stripe API key configured: {Utility.MaskSensitiveValue(stripeApiKey)}");

// Optional: remove global static assignment to avoid accidental drift.

builder.ConfigureFunctionsWebApplication();

// Entra ID (Azure AD) auth configuration - store for manual validation
var tenantId = config["AAD_TENANT_ID"];
var audience = config["AAD_AUDIENCE"] ?? config["AAD_CLIENT_ID"];
var authority = config["AAD_AUTHORITY"] ?? (string.IsNullOrWhiteSpace(tenantId) ? null : $"https://login.microsoftonline.com/{tenantId}/v2.0");

// Masked confirmation logs (do not log full sensitive values)
Console.WriteLine($"Azure AD Tenant ID configured: {Utility.MaskSensitiveValue(tenantId)}");
Console.WriteLine($"Azure AD Audience configured: {Utility.MaskSensitiveValue(audience)}");
Console.WriteLine($"Azure AD Authority configured: {Utility.MaskUrl(authority)}");

// Cosmos + repositories for user profiles
var endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
var primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
var databaseId = config["COSMOSDB_DATABASE_ID"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

// Masked confirmation logs for Cosmos DB configuration
Console.WriteLine($"Cosmos DB Endpoint configured: {Utility.MaskUrl(endpointUri)}");
Console.WriteLine($"Cosmos DB Primary Key configured: {Utility.MaskSensitiveValue(primaryKey)}");
Console.WriteLine($"Cosmos DB Database ID configured: {databaseId}");

builder.Services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddUserProfileRepository()
    .AddImageApiRepositories()
    .AddJwtAuthentication() // Add JWT authentication services from OnePageAuthorLib
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    // Register a StripeClient for DI so services can depend on it
    .AddSingleton<StripeClient>(_ => new StripeClient(stripeApiKey))
    .AddStripeServices()
    .AddStripeOrchestrators()
    .AddUserProfileServices();

builder.Build().Run();
