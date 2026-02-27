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
    })
    .ConfigureLogging(logging =>
    {
        logging.AddSystemdConsole(options =>
        {
            options.IncludeScopes = true;
        });
    });

var host = builder.Build();

// Log startup configuration status using ILogger
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var config = host.Services.GetRequiredService<IConfiguration>();

var sbConnection = config["SERVICE_BUS_CONNECTION_STRING"];
var sbQueue = config["SERVICE_BUS_WHMCS_QUEUE_NAME"] ?? "whmcs-domain-registrations";
var whmcsUrl = config["WHMCS_API_URL"];
var whmcsId = config["WHMCS_API_IDENTIFIER"];

logger.LogInformation("Service Bus connection configured: {Status}", !string.IsNullOrWhiteSpace(sbConnection) ? "yes" : "NO");
logger.LogInformation("Service Bus queue: {Queue}", sbQueue);
logger.LogInformation("WHMCS API URL configured: {Status}", !string.IsNullOrWhiteSpace(whmcsUrl) ? "yes" : "NO");
logger.LogInformation("WHMCS API Identifier configured: {Status}", !string.IsNullOrWhiteSpace(whmcsId) ? "yes" : "NO");

await host.RunAsync();

// Required for User Secrets generic type parameter
public partial class Program { }
