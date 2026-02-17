using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InkStainedWretch.OnePageAuthorAPI;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

static bool IsDevelopmentEnvironment()
{
    var environmentName =
        Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ??
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
}

if (IsDevelopmentEnvironment())
{
    // Ensure local dev can source settings from `dotnet user-secrets`.
    // `UserSecretsId` is defined in the project file.
    builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
}

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

builder.Services
    .AddAuthorDataService() // Register Author data service via DI extension
    .AddLocaleDataService() // Register Locale data service via DI extension
    .AddDomainRegistrationRepository() // Register Domain Registration repository via DI extension
    .AddUserIdentityServices()

    // OpenTelemetry -> Azure Monitor (Application Insights backend).
    // NOTE: As of APPLICATION_INSIGHTS_UPGRADE_MIGRATION_PLAN.md (Phase 2),
    // OnePageAuthorLib still contains TelemetryClient usage (e.g., AuthenticatedFunctionTelemetryService,
    // StripeTelemetryService) and a Microsoft.ApplicationInsights 2.x reference.
    // This means there is a temporary dual telemetry pipeline (legacy AI SDK + OpenTelemetry).
    // Follow-up task: migrate OnePageAuthorLib to OpenTelemetry-native patterns
    // (ActivitySource, ILogger, Meter) and remove TelemetryClient/ApplicationInsights usage.
    .AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter(options =>
    {
        var connectionString = config["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            options.ConnectionString = connectionString;
        }
    });

builder.Build().Run();
