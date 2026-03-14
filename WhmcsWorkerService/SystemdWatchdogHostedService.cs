using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WhmcsWorkerService;

internal sealed class SystemdWatchdogHostedService : BackgroundService
{
    private readonly ILogger<SystemdWatchdogHostedService> _logger;

    public SystemdWatchdogHostedService(ILogger<SystemdWatchdogHostedService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        // Only run when we strongly suspect we're hosted by systemd.
        var invocationId = Environment.GetEnvironmentVariable("INVOCATION_ID");
        if (string.IsNullOrWhiteSpace(invocationId))
        {
            return;
        }

        var notifySocket = Environment.GetEnvironmentVariable("NOTIFY_SOCKET");
        if (string.IsNullOrWhiteSpace(notifySocket))
        {
            _logger.LogWarning("systemd watchdog: NOTIFY_SOCKET not set; watchdog pings disabled");
            return;
        }

        var watchdogUsecString = Environment.GetEnvironmentVariable("WATCHDOG_USEC");
        if (string.IsNullOrWhiteSpace(watchdogUsecString) ||
            !long.TryParse(watchdogUsecString, out var watchdogUsec) ||
            watchdogUsec <= 0)
        {
            _logger.LogWarning("systemd watchdog: WATCHDOG_USEC is missing/invalid; watchdog pings disabled");
            return;
        }

        var watchdogPidString = Environment.GetEnvironmentVariable("WATCHDOG_PID");
        if (!string.IsNullOrWhiteSpace(watchdogPidString) &&
            int.TryParse(watchdogPidString, out var watchdogPid) &&
            watchdogPid != Environment.ProcessId)
        {
            _logger.LogWarning(
                "systemd watchdog: WATCHDOG_PID={WatchdogPid} does not match current PID {CurrentPid}; pings disabled",
                watchdogPid,
                Environment.ProcessId);
            return;
        }

        // Ping at half the watchdog interval as recommended by systemd.
        var intervalMs = watchdogUsec / 2 / 1000;
        if (intervalMs < 5000)
        {
            intervalMs = 5000;
        }

        var interval = TimeSpan.FromMilliseconds(intervalMs);

        _logger.LogInformation(
            "systemd watchdog: enabled (WATCHDOG_USEC={WatchdogUsec}). Sending WATCHDOG=1 every {IntervalSeconds:n0}s",
            watchdogUsec,
            interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendNotifyAsync(notifySocket, "WATCHDOG=1\n", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "systemd watchdog: failed to send WATCHDOG=1");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private static ValueTask<int> SendNotifyAsync(string notifySocket, string payload, CancellationToken cancellationToken)
    {
        var endpointPath = notifySocket;
        if (endpointPath.StartsWith('@'))
        {
            // Abstract namespace Unix sockets: leading '@' maps to leading NUL.
            endpointPath = "\0" + endpointPath[1..];
        }

        var endpoint = new UnixDomainSocketEndPoint(endpointPath);

        using var socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified);
        var bytes = Encoding.UTF8.GetBytes(payload);

        return socket.SendToAsync(bytes, SocketFlags.None, endpoint, cancellationToken);
    }
}
