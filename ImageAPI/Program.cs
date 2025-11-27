using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Get configuration (includes User Secrets in development)
var config = builder.Configuration;

// Read Entra (Azure AD) settings from configuration (includes User Secrets)
var tenantId = config["AAD_TENANT_ID"];
var audience = config["AAD_AUDIENCE"] ?? config["AAD_CLIENT_ID"];
var authority = config["AAD_AUTHORITY"] ?? (string.IsNullOrWhiteSpace(tenantId) ? null : $"https://login.microsoftonline.com/{tenantId}/v2.0");

// Log Azure AD configuration (masked for security)
Console.WriteLine($"Azure AD Tenant ID configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(tenantId)}");
Console.WriteLine($"Azure AD Audience configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(audience)}");
Console.WriteLine($"Azure AD Authority configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskUrl(authority)}");

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

// Cosmos + repositories (reads from User Secrets in development)
var endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
var primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
var databaseId = config["COSMOSDB_DATABASE_ID"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

// Log Cosmos DB configuration (masked for security)
Console.WriteLine($"Cosmos DB Endpoint configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskUrl(endpointUri)}");
Console.WriteLine($"Cosmos DB Primary Key configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(primaryKey)}");
Console.WriteLine($"Cosmos DB Database ID configured: {databaseId}");

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

// Azure Blob Storage (reads from User Secrets in development)
builder.Services.AddSingleton(sp =>
{
    var connectionString = config["AZURE_STORAGE_CONNECTION_STRING"] ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING is required");
    
    // Log Azure Storage configuration (masked for security)
    Console.WriteLine($"Azure Storage Connection String configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(connectionString)}");
    
    return new Azure.Storage.Blobs.BlobServiceClient(connectionString);
});

builder.Build().Run();
