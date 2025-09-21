using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


var builder = FunctionsApplication.CreateBuilder(args);
var config = builder.Configuration;
StripeConfiguration.ApiKey = config["STRIPE_API_KEY"] ?? throw new InvalidOperationException("Configuration value 'STRIPE_API_KEY' is missing or empty. Please set it to your Stripe API key.");
// Masked confirmation log (do not log full secret)
var masked = StripeConfiguration.ApiKey?.Length >= 8
    ? $"{StripeConfiguration.ApiKey[..4]}****{StripeConfiguration.ApiKey[^4..]}"
    : "(set)";
Console.WriteLine($"Stripe API key configured: {masked}");
builder.ConfigureFunctionsWebApplication();

// Entra ID (Azure AD) auth configuration
var tenantId = config["AAD_TENANT_ID"];
var audience = config["AAD_AUDIENCE"] ?? config["AAD_CLIENT_ID"];
var authority = config["AAD_AUTHORITY"] ?? (string.IsNullOrWhiteSpace(tenantId) ? null : $"https://login.microsoftonline.com/{tenantId}/v2.0");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrWhiteSpace(authority)) options.Authority = authority;
        if (!string.IsNullOrWhiteSpace(audience)) options.Audience = audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authority,
            ValidAudience = audience
        };
    });

builder.Services.AddAuthorization();

// Cosmos + repositories for user profiles
var endpointUri = config["COSMOSDB_ENDPOINT_URI"]; 
var primaryKey = config["COSMOSDB_PRIMARY_KEY"]; 
var databaseId = config["COSMOSDB_DATABASE_ID"]; 

builder.Services
    .AddCosmosClient(endpointUri!, primaryKey!)
    .AddCosmosDatabase(databaseId!)
    .AddUserProfileRepository()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddStripeServices()
    .AddStripeOrchestrators();

builder.Build().Run();
