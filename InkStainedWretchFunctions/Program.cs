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

builder.Services.AddSingleton(s => new CosmosClient(endpointUri, primaryKey));
builder.Services.AddSingleton(s =>
{
    var client = s.GetRequiredService<CosmosClient>();
    return client.GetDatabase(databaseId);
});

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddInkStainedWretchServices()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
