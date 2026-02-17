using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var config = builder.Configuration;

// Log Key Vault configuration (masked for security)
var keyVaultUrl = config["KEY_VAULT_URL"];
var useKeyVault = config["USE_KEY_VAULT"];

Console.WriteLine($"Key Vault URL configured: {Utility.MaskUrl(keyVaultUrl ?? "Not set")}");
Console.WriteLine($"USE_KEY_VAULT: {useKeyVault ?? "false"}");

builder.Services
    .AddKeyVaultConfigService()

    // OpenTelemetry -> Azure Monitor (Application Insights backend)
    .AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Build().Run();
