using Azure.Monitor.OpenTelemetry.Exporter;
using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging;

static bool IsLikelyRunningUnderSystemd()
{
    if (!OperatingSystem.IsLinux())
    {
        return false;
    }

    // systemd sets INVOCATION_ID for services (and user sessions).
    // It is a strong signal that sd_notify is available.
    return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVOCATION_ID"));
}

static bool ApplySystemdEnvironmentDefaultsFromConfig()
{
    if (!IsLikelyRunningUnderSystemd())
    {
        return false;
    }

    var appliedAny = false;

    // These are *defaults* we can ship through our env/secrets pipeline without
    // overriding systemd's own NOTIFY_SOCKET/WATCHDOG_* variables.
    var defaultNotifySocket = Environment.GetEnvironmentVariable("WHMCS_SYSTEMD_NOTIFY_SOCKET");
    var defaultWatchdogUsec = Environment.GetEnvironmentVariable("WHMCS_SYSTEMD_WATCHDOG_USEC");

    // If systemd variables are missing/blank (commonly due to accidental overrides), restore them.
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NOTIFY_SOCKET")))
    {
        var candidate = string.IsNullOrWhiteSpace(defaultNotifySocket)
            ? "/run/systemd/notify"
            : defaultNotifySocket;

        Environment.SetEnvironmentVariable("NOTIFY_SOCKET", candidate);
        appliedAny = true;
    }

    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WATCHDOG_USEC")))
    {
        // Default to 30s (microseconds) so we ping well within WatchdogSec=60s.
        var candidate = string.IsNullOrWhiteSpace(defaultWatchdogUsec)
            ? "30000000"
            : defaultWatchdogUsec;

        Environment.SetEnvironmentVariable("WATCHDOG_USEC", candidate);
        appliedAny = true;
    }

    // Ensure WATCHDOG_PID matches our process to avoid the SDK disabling watchdog pings.
    var watchdogPid = Environment.GetEnvironmentVariable("WATCHDOG_PID");
    if (string.IsNullOrWhiteSpace(watchdogPid) || watchdogPid != Environment.ProcessId.ToString())
    {
        Environment.SetEnvironmentVariable("WATCHDOG_PID", Environment.ProcessId.ToString());
        appliedAny = true;
    }

    return appliedAny;
}

var systemdDefaultsApplied = ApplySystemdEnvironmentDefaultsFromConfig();

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

        // Ensure systemd watchdog pings are sent even if the framework's systemd integration
        // doesn't start a watchdog loop (or if the service's env vars were accidentally overridden).
        services.AddHostedService<WhmcsWorkerService.SystemdWatchdogHostedService>();

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

// Systemd watchdog diagnostics (helps validate that WATCHDOG_* env vars are actually present)
logger.LogInformation("systemd env defaults applied by worker: {Status}", systemdDefaultsApplied ? "yes" : "no");
logger.LogInformation("systemd defaults configured: WHMCS_SYSTEMD_NOTIFY_SOCKET={NotifySocket} WHMCS_SYSTEMD_WATCHDOG_USEC={WatchdogUsec}",
    config["WHMCS_SYSTEMD_NOTIFY_SOCKET"] ?? "(not set)",
    config["WHMCS_SYSTEMD_WATCHDOG_USEC"] ?? "(not set)");

var notifySocket = Environment.GetEnvironmentVariable("NOTIFY_SOCKET");
var watchdogUsec = Environment.GetEnvironmentVariable("WATCHDOG_USEC");
var watchdogPid = Environment.GetEnvironmentVariable("WATCHDOG_PID");
logger.LogInformation("systemd notify socket present: {Status}", !string.IsNullOrWhiteSpace(notifySocket) ? "yes" : "NO");
logger.LogInformation("systemd watchdog enabled (WATCHDOG_USEC present): {Status}", !string.IsNullOrWhiteSpace(watchdogUsec) ? "yes" : "no");
logger.LogInformation("systemd watchdog details: WATCHDOG_USEC={WatchdogUsec} WATCHDOG_PID={WatchdogPid} CurrentPid={CurrentPid}",
    watchdogUsec ?? "(not set)",
    watchdogPid ?? "(not set)",
    Environment.ProcessId);

await host.RunAsync();

// Required for User Secrets generic type parameter
public partial class Program { }

