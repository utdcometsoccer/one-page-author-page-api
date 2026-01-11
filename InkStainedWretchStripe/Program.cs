using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Stripe;

var builder = FunctionsApplication.CreateBuilder(args);
var config = builder.Configuration;
var stripeApiKey = config["STRIPE_API_KEY"] ?? throw new InvalidOperationException("Configuration value 'STRIPE_API_KEY' is missing or empty. Please set it to your Stripe API key.");
// Configure Stripe client lifetime
// Best practice: StripeClient is thread-safe; reuse via Singleton for app-wide API key.
// If you ever need per-user/tenant keys, switch to Scoped and construct with the appropriate key per request.
// Masked confirmation log (do not log full secret)
Console.WriteLine($"Stripe API key configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(stripeApiKey)}");

// Optional: remove global static assignment to avoid accidental drift.

builder.ConfigureFunctionsWebApplication();

// Entra ID (Azure AD) auth configuration - store for manual validation
var tenantId = config["AAD_TENANT_ID"];
var audience = config["AAD_AUDIENCE"] ?? config["AAD_CLIENT_ID"];
var authority = config["AAD_AUTHORITY"] ?? (string.IsNullOrWhiteSpace(tenantId) ? null : $"https://login.microsoftonline.com/{tenantId}/v2.0");

// Masked confirmation logs (do not log full sensitive values)
Console.WriteLine($"Azure AD Tenant ID configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(tenantId)}");
Console.WriteLine($"Azure AD Audience configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(audience)}");
Console.WriteLine($"Azure AD Authority configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskUrl(authority)}");

// Optional: allow multiple issuers via AAD_VALID_ISSUERS (comma-delimited)
var validIssuersRaw = config["AAD_VALID_ISSUERS"];
string[]? validIssuers = InkStainedWretch.OnePageAuthorAPI.Utility.ParseValidIssuers(validIssuersRaw);
Console.WriteLine($"Azure AD Valid Issuers configured: {(validIssuers is null ? "(not set)" : string.Join(", ", validIssuers.Select(i => InkStainedWretch.OnePageAuthorAPI.Utility.MaskUrl(i))))}");

// Configure JwtBearer to accept multiple issuers/audience with automatic key refresh
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
        
        // Configure automatic refresh of signing keys from OpenID Connect metadata
        // This prevents SecurityTokenSignatureKeyNotFoundException when Azure AD rotates keys
        if (!string.IsNullOrWhiteSpace(authority))
        {
            var metadataAddress = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever())
            {
                // Refresh metadata every 6 hours (default is 24 hours)
                AutomaticRefreshInterval = TimeSpan.FromHours(6),
                // Minimum time between refreshes to prevent hammering the endpoint
                RefreshInterval = TimeSpan.FromMinutes(30)
            };
        }
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = audience,
            ValidIssuer = validIssuers is null ? authority : null,
            ValidIssuers = validIssuers
        };
    });

// Cosmos + repositories for user profiles
var endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
var primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
var databaseId = config["COSMOSDB_DATABASE_ID"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

// Masked confirmation logs for Cosmos DB configuration
Console.WriteLine($"Cosmos DB Endpoint configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskUrl(endpointUri)}");
Console.WriteLine($"Cosmos DB Primary Key configured: {InkStainedWretch.OnePageAuthorAPI.Utility.MaskSensitiveValue(primaryKey)}");
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

builder.Services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddUserProfileRepository()
    .AddImageApiRepositories()
    .AddJwtAuthentication() // Add JWT authentication services from OnePageAuthorLib
    .AddUserIdentityServices()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    // Register a StripeClient for DI so services can depend on it
    .AddSingleton<StripeClient>(_ => new StripeClient(stripeApiKey))
    .AddStripeServices()
    .AddStripeOrchestrators()
    .AddUserProfileServices();

builder.Build().Run();
