using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InkStainedWretch.OnePageAuthorAPI;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var config = builder.Configuration;

string? endpointUri = config["COSMOSDB_ENDPOINT_URI"];
if (string.IsNullOrWhiteSpace(endpointUri))
    throw new InvalidOperationException("Configuration value 'COSMOSDB_ENDPOINT_URI' is missing or empty. Please set it to your Cosmos DB endpoint URI.");

string? primaryKey = config["COSMOSDB_PRIMARY_KEY"];
if (string.IsNullOrWhiteSpace(primaryKey))
    throw new InvalidOperationException("Configuration value 'COSMOSDB_PRIMARY_KEY' is missing or empty. Please set it to your Cosmos DB primary key.");

string? databaseId = config["COSMOSDB_DATABASE_ID"];
if (string.IsNullOrWhiteSpace(databaseId))
    throw new InvalidOperationException("Configuration value 'COSMOSDB_DATABASE_ID' is missing or empty. Please set it to your Cosmos DB database ID.");

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

// Register Cosmos and ensure Database exists
builder.Services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId);

// Add Application Insights telemetry for Azure Functions Worker
builder.Services
    .AddAuthorDataService() // Register Author data service via DI extension
    .AddLocaleDataService() // Register Locale data service via DI extension
    .AddDomainRegistrationRepository() // Register Domain Registration repository via DI extension
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
