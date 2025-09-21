using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
