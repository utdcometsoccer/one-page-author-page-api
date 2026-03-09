using Azure.Monitor.OpenTelemetry.Exporter;
using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .UseSystemd() // Enable systemd integration (journal logging, notify)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>(optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Register the WHMCS API service (used by the worker to make WHMCS API calls)
        services.AddWhmcsService();

        // Register the background worker
        services.AddHostedService<WhmcsWorkerService.Worker>();

        // OpenTelemetry -> Azure Monitor (Application Insights backend)
        // Enabled when APPLICATIONINSIGHTS_CONNECTION_STRING is set.
        // WithLogging() adds the ILogger -> OpenTelemetry bridge so that all
        // structured ILogger entries are forwarded to Azure Monitor.
        var aiConnectionString = context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(aiConnectionString))
        {
            services.AddOpenTelemetry()
                .WithLogging()
                .UseAzureMonitorExporter(options =>
                {
                    options.ConnectionString = aiConnectionString;
                });
        }
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.AddSystemdConsole(options =>
        {
            options.IncludeScopes = true;
        });

        // Feature flag: WHMCS_WORKER_LOG_LEVEL controls minimum log level.
        // Accepted values: Trace, Debug, Information (default), Warning, Error, Critical.
        // Setting Debug or Trace enables verbose telemetry for troubleshooting.
        // appsettings.json category filters (e.g. Microsoft=Warning) remain in effect
        // unless the flag overrides the overall floor.
        var logLevelSetting = context.Configuration["WHMCS_WORKER_LOG_LEVEL"];
        if (Enum.TryParse<LogLevel>(logLevelSetting, ignoreCase: true, out var logLevel))
        {
            logging.SetMinimumLevel(logLevel);
        }
    });

var host = builder.Build();

// Log startup configuration status using ILogger
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var config = host.Services.GetRequiredService<IConfiguration>();

var sbConnection = config["SERVICE_BUS_CONNECTION_STRING"];
var sbQueue = config["SERVICE_BUS_WHMCS_QUEUE_NAME"] ?? "whmcs-domain-registrations";
var whmcsUrl = config["WHMCS_API_URL"];
var whmcsId = config["WHMCS_API_IDENTIFIER"];
var logLevelValue = config["WHMCS_WORKER_LOG_LEVEL"] ?? "Information";
var aiConfigured = !string.IsNullOrWhiteSpace(config["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

logger.LogInformation("Service Bus connection configured: {Status}", !string.IsNullOrWhiteSpace(sbConnection) ? "yes" : "NO");
logger.LogInformation("Service Bus queue: {Queue}", sbQueue);
logger.LogInformation("WHMCS API URL configured: {Status}", !string.IsNullOrWhiteSpace(whmcsUrl) ? "yes" : "NO");
logger.LogInformation("WHMCS API Identifier configured: {Status}", !string.IsNullOrWhiteSpace(whmcsId) ? "yes" : "NO");
logger.LogInformation("Log level: {LogLevel}", logLevelValue);
logger.LogInformation("Application Insights telemetry: {Status}", aiConfigured ? "enabled" : "disabled (set APPLICATIONINSIGHTS_CONNECTION_STRING to enable)");

await host.RunAsync();

// Required for User Secrets generic type parameter
public partial class Program { }

