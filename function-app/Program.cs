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

// Register Cosmos and ensure Database exists
builder.Services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId);

// Add Application Insights telemetry for Azure Functions Worker
builder.Services
    .AddAuthorDataService() // Register Author data service via DI extension
    .AddLocaleDataService() // Register Locale data service via DI extension
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
