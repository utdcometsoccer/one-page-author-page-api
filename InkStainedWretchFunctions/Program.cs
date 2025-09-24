using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var endpointUri = configuration["COSMOSDB_ENDPOINT_URI"]; 
var primaryKey = configuration["COSMOSDB_PRIMARY_KEY"]; 
var databaseId = configuration["COSMOSDB_DATABASE_ID"]; 

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddCosmosClient(endpointUri!, primaryKey!)
    .AddCosmosDatabase(databaseId!)
    .AddInkStainedWretchServices()
    .AddPenguinRandomHouseServices()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
