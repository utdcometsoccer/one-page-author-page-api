using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using InkStainedWretchStripe;

var builder = FunctionsApplication.CreateBuilder(args);
var config = builder.Configuration;
StripeConfiguration.ApiKey = config["STRIPE_API_KEY"] ?? throw new InvalidOperationException("Configuration value 'STRIPE_API_KEY' is missing or empty. Please set it to your Stripe API key.");
// Masked confirmation log (do not log full secret)
var masked = StripeConfiguration.ApiKey?.Length >= 8
    ? $"{StripeConfiguration.ApiKey[..4]}****{StripeConfiguration.ApiKey[^4..]}"
    : "(set)";
Console.WriteLine($"Stripe API key configured: {masked}");

builder.ConfigureFunctionsWebApplication();

// Entra ID (Azure AD) auth configuration - store for manual validation
var tenantId = config["AAD_TENANT_ID"];
var audience = config["AAD_AUDIENCE"] ?? config["AAD_CLIENT_ID"];
var authority = config["AAD_AUTHORITY"] ?? (string.IsNullOrWhiteSpace(tenantId) ? null : $"https://login.microsoftonline.com/{tenantId}/v2.0");

// Masked confirmation logs (do not log full sensitive values)
var maskedTenantId = !string.IsNullOrWhiteSpace(tenantId) && tenantId.Length >= 8
    ? $"{tenantId[..4]}****{tenantId[^4..]}"
    : (string.IsNullOrWhiteSpace(tenantId) ? "(not set)" : "(set)");

var maskedAudience = !string.IsNullOrWhiteSpace(audience) && audience.Length >= 8
    ? $"{audience[..4]}****{audience[^4..]}"
    : (string.IsNullOrWhiteSpace(audience) ? "(not set)" : "(set)");

var maskedAuthority = !string.IsNullOrWhiteSpace(authority) && authority.Length >= 12
    ? $"{authority[..8]}****{authority[^4..]}"
    : (string.IsNullOrWhiteSpace(authority) ? "(not set)" : "(set)");

Console.WriteLine($"Azure AD Tenant ID configured: {maskedTenantId}");
Console.WriteLine($"Azure AD Audience configured: {maskedAudience}");
Console.WriteLine($"Azure AD Authority configured: {maskedAuthority}");

// Cosmos + repositories for user profiles
var endpointUri = config["COSMOSDB_ENDPOINT_URI"];
var primaryKey = config["COSMOSDB_PRIMARY_KEY"];
var databaseId = config["COSMOSDB_DATABASE_ID"];

builder.Services
    .AddCosmosClient(endpointUri!, primaryKey!)
    .AddCosmosDatabase(databaseId!)
    .AddUserProfileRepository()
    .AddImageApiRepositories()
    .AddJwtAuthentication() // Add JWT authentication services from OnePageAuthorLib
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddStripeServices()
    .AddStripeOrchestrators()
    .AddUserProfileServices();

builder.Build().Run();
