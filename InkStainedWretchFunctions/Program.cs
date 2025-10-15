using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Functions.Testing;
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
    .AddUserProfileRepository()
    .AddAuthorDataService() // Add Author data service for GetAuthors function
    .AddInkStainedWretchServices()
    .AddPenguinRandomHouseServices()
    .AddAmazonProductServices() // Add Amazon Product API services
    .AddJwtAuthentication() // Add JWT authentication services from OnePageAuthorLib
    .AddUserProfileServices()
    .AddDomainRegistrationRepository() // Add domain registration repository
    .AddDomainRegistrationServices() // Add domain registration services
    .AddStateProvinceRepository() // Add StateProvince repository
    .AddStateProvinceServices() // Add StateProvince services
    .AddDnsZoneService() // Add DNS zone service for domain registration triggers
    .AddFrontDoorServices() // Add Azure Front Door services for domain management
    .AddGoogleDomainsService() // Add Google Domains service for domain registration
    .AddTestingServices() // Add testing services for mock implementations and test harnesses
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
