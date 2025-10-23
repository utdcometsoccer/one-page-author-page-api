using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using InkStainedWretch.OnePageAuthorAPI;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Read Entra (Azure AD) settings from environment
var tenantId = Environment.GetEnvironmentVariable("AAD_TENANT_ID");
var audience = Environment.GetEnvironmentVariable("AAD_AUDIENCE") ?? Environment.GetEnvironmentVariable("AAD_CLIENT_ID");
var authority = Environment.GetEnvironmentVariable("AAD_AUTHORITY") ?? (string.IsNullOrWhiteSpace(tenantId) ? null : $"https://login.microsoftonline.com/{tenantId}/v2.0");

// Add AuthN/Z
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }
        if (!string.IsNullOrWhiteSpace(audience))
        {
            options.Audience = audience;
        }
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Accept both azp/aud depending on app registration configuration
            ValidAudience = audience,
            ValidIssuer = authority
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Require the access token to contain scp 'read' (space-delimited)
    options.AddPolicy("RequireScope.Read", policy =>
        policy.RequireAssertion(ctx =>
        {
            var scp = ctx.User.FindFirst("scp")?.Value;
            if (string.IsNullOrWhiteSpace(scp)) return false;
            return scp.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .Any(s => string.Equals(s, "read", StringComparison.OrdinalIgnoreCase));
        }));

    // Require an app role assignment 'Admin'
    options.AddPolicy("RequireRole.Admin", policy =>
        policy.RequireClaim("roles", "Admin"));
});

// Note: In Functions isolated v2, calling ConfigureFunctionsWebApplication wires the ASP.NET Core pipeline.
// Authentication/Authorization middleware are added automatically when services are registered above.

// Cosmos + repositories
var endpointUri = Environment.GetEnvironmentVariable("COSMOSDB_ENDPOINT_URI") ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
var primaryKey = Environment.GetEnvironmentVariable("COSMOSDB_PRIMARY_KEY") ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
var databaseId = Environment.GetEnvironmentVariable("COSMOSDB_DATABASE_ID") ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

builder.Services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddUserProfileRepository()
    .AddImageApiRepositories()
    .AddImageApiServices()
    .AddJwtAuthentication() // Add JWT authentication services from OnePageAuthorLib
    .AddUserProfileServices()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Azure Blob Storage
builder.Services.AddSingleton(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING is required");
    return new Azure.Storage.Blobs.BlobServiceClient(connectionString);
});

builder.Build().Run();
