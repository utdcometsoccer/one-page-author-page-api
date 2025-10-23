using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Functions.Testing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

// Log Cosmos DB configuration (masked for security)
Console.WriteLine($"Cosmos DB Endpoint configured: {Utility.MaskUrl(endpointUri)}");
Console.WriteLine($"Cosmos DB Primary Key configured: {Utility.MaskSensitiveValue(primaryKey)}");
Console.WriteLine($"Cosmos DB Database ID configured: {databaseId}");

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
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
    .AddCountryRepository() // Add Country repository
    .AddCountryServices() // Add Country services
    .AddLanguageRepository() // Add Language repository
    .AddLanguageServices() // Add Language services
    .AddDnsZoneService() // Add DNS zone service for domain registration triggers
    .AddFrontDoorServices() // Add Azure Front Door services for domain management
    .AddGoogleDomainsService() // Add Google Domains service for domain registration
    .AddTestingServices() // Add testing services for mock implementations and test harnesses
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();

// Program class needed for User Secrets generic type parameter
public partial class Program { }
