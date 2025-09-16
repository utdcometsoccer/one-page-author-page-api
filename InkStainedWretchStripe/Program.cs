using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;


var builder = FunctionsApplication.CreateBuilder(args);
var config = builder.Configuration;
StripeConfiguration.ApiKey = config["STRIPE_API_KEY"] ?? throw new InvalidOperationException("Configuration value 'STRIPE_API_KEY' is missing or empty. Please set it to your Stripe API key.");
// Masked confirmation log (do not log full secret)
var masked = StripeConfiguration.ApiKey?.Length >= 8
    ? $"{StripeConfiguration.ApiKey[..4]}****{StripeConfiguration.ApiKey[^4..]}"
    : "(set)";
Console.WriteLine($"Stripe API key configured: {masked}");
builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddStripeServices();

builder.Build().Run();
