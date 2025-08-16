using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();


// Register IAuthorDataService using ServiceFactory and environment variables
string endpointUri = Environment.GetEnvironmentVariable("COSMOSDB_ENDPOINT_URI") ?? "";
string primaryKey = Environment.GetEnvironmentVariable("COSMOSDB_PRIMARY_KEY") ?? "";
string databaseId = Environment.GetEnvironmentVariable("COSMOSDB_DATABASE_ID") ?? "";
builder.Services.AddSingleton(_ => InkStainedWretch.OnePageAuthorAPI.ServiceFactory.CreateAuthorDataService(
    null!, null!, null!, null!));

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();


    builder.Build().Run();
