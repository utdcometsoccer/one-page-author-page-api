# WHMCS Worker Service Rebuild Prompt

*Generated: 2026-03-04*

This document contains a GitHub Copilot prompt to recreate the `WhmcsWorkerService` from scratch based on its current implementation. Use this prompt when setting up the worker in a new environment, performing a major refactor, or onboarding new developers to the component.

---

## Current Architecture Summary

The `WhmcsWorkerService` is a .NET Worker Service (systemd daemon) deployed to an Azure Linux VM. It has a **static outbound IP address** so the WHMCS API can allowlist it. It dequeues messages from Azure Service Bus and calls the WHMCS REST API.

- **Source:** `WhmcsWorkerService/Worker.cs`, `WhmcsWorkerService/Program.cs`
- **Runtime:** .NET 10, `linux-x64`, framework-dependent (requires `aspnetcore-runtime-10.0` on the VM)
- **Process manager:** systemd (`whmcs-worker.service` unit file)
- **Environment variables:** Read from `/etc/whmcs-worker/environment` on the VM (typically written/updated via `infra/vm.bicep` in CI/CD)

---

## Rebuild Prompt

```
Recreate the WhmcsWorkerService .NET Worker Service for the OnePageAuthor platform.

PROJECT REQUIREMENTS
====================
- Project type: .NET Worker Service (not Azure Functions, not ASP.NET)
- Target framework: net10.0
- Runtime identifier: linux-x64 (framework-dependent)
- Namespace: WhmcsWorkerService
- Solution: OnePageAuthorAPI.sln

PROJECT FILE: WhmcsWorkerService/WhmcsWorkerService.csproj
----------------------------------------------------------
Include these package references:
- Azure.Messaging.ServiceBus
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Hosting.Systemd
- Microsoft.Extensions.Logging
Reference OnePageAuthorLib for IWhmcsService, DomainRegistration entities,
and service registration extensions.

PROGRAM.CS: WhmcsWorkerService/Program.cs
-----------------------------------------
Use Host.CreateDefaultBuilder(args) with:

1. .UseSystemd()
   Enables systemd integration:
   - Journal (structured) logging
   - Notify protocol (sd_notify) for readiness signaling
   - Watchdog support

2. .ConfigureAppConfiguration:
   - config.AddUserSecrets<Program>(optional: true)  // local dev only
   - config.AddEnvironmentVariables()
   The systemd unit file reads /etc/whmcs-worker/environment; its contents
   are exposed to the process as environment variables via EnvironmentFile=.

3. .ConfigureServices:
   - services.AddWhmcsService()          // from OnePageAuthorLib/ServiceFactory.cs
   - services.AddHostedService<Worker>() // the background processor

4. .ConfigureLogging:
   - logging.AddSystemdConsole(options => options.IncludeScopes = true)
   Do NOT add Console logging separately — SystemdConsole formats output
   for journald.

5. After building the host, retrieve ILogger<Program> and IConfiguration,
   then log the startup configuration status for these variables (log only
   whether they are configured, NOT the actual values):
   - SERVICE_BUS_CONNECTION_STRING: configured / NOT CONFIGURED
   - SERVICE_BUS_WHMCS_QUEUE_NAME: value of queue name (not a secret)
   - WHMCS_API_URL: configured / NOT CONFIGURED
   - WHMCS_API_IDENTIFIER: configured / NOT CONFIGURED

6. Call await host.RunAsync().

7. Add `public partial class Program { }` at the bottom of the file
   (required for User Secrets generic type parameter in integration tests).

WORKER.CS: WhmcsWorkerService/Worker.cs
----------------------------------------
Implement a sealed class Worker : BackgroundService with:

Constructor parameters:
- ILogger<Worker> logger
- IWhmcsService whmcsService
- IConfiguration configuration

Constants:
- internal const int MinNameServers = 2
- internal const int MaxNameServers = 5

Override ExecuteAsync(CancellationToken stoppingToken):
1. Read SERVICE_BUS_CONNECTION_STRING from configuration.
   If null/empty, log a Critical message and return (do not throw).
2. Read SERVICE_BUS_WHMCS_QUEUE_NAME from configuration (default: "whmcs-domain-registrations").
3. Create ServiceBusClient and ServiceBusProcessor with:
   MaxConcurrentCalls = 1    (keep WHMCS load manageable)
   AutoCompleteMessages = false
4. Register event handlers:
   processor.ProcessMessageAsync += ProcessMessageAsync
   processor.ProcessErrorAsync += ProcessErrorAsync
5. Start the processor.
6. Use await Task.Delay(Timeout.Infinite, stoppingToken) to keep alive.
7. On OperationCanceledException (from stoppingToken), swallow and continue to cleanup.
8. Stop the processor before returning.

ProcessMessageAsync handler:
- Calls ProcessWhmcsMessageAsync(body, messageId, cancellationToken)
- Dispatches ServiceBus disposition based on MessageProcessingOutcome:
  - Complete → args.CompleteMessageAsync
  - Abandon → args.AbandonMessageAsync
  - DeadLetterInvalidJson → args.DeadLetterMessageAsync(reason: "DeserializationFailure",
      description: "Message body could not be deserialized as WhmcsDomainRegistrationMessage")
  - DeadLetterMissingData → args.DeadLetterMessageAsync(reason: "MissingDomainData",
      description: "DomainRegistration or Domain is null")

ProcessWhmcsMessageAsync(string messageBody, string messageId, CancellationToken):
This is internal (not private) for testability — it is tested directly in unit tests.
Returns Task<MessageProcessingOutcome>.

Steps:
1. Deserialize messageBody as WhmcsDomainRegistrationMessage using
   System.Text.Json.JsonSerializer.Deserialize<WhmcsDomainRegistrationMessage>.
   On JsonException: log Error with messageId, return DeadLetterInvalidJson.

2. If message?.DomainRegistration?.Domain == null:
   Log Error with messageId, return DeadLetterMissingData.

3. Normalize: var nameServers = message.NameServers ?? [];

4. Step 1 — Register domain:
   Call _whmcsService.RegisterDomainAsync(message.DomainRegistration).
   On exception: log Error (with domain name and messageId), return Abandon.
   If returns false: log Warning, return Abandon.
   Log Information on success.

5. Step 2 — Update name servers (conditional):
   If nameServers.Length >= MinNameServers && nameServers.Length <= MaxNameServers:
     Call _whmcsService.UpdateNameServersAsync(domainName, nameServers).
     On exception: log Error, return Complete (registration already succeeded).
     If returns false: log Warning, continue to Complete.
     Log Information on success.
   Else if nameServers.Length > MaxNameServers
        || (nameServers.Length > 0 && nameServers.Length < MinNameServers):
     Log Warning about invalid count, skip update.
   Else (no name servers provided):
     Log Information that NS update is skipped.

6. Return Complete.

ProcessErrorAsync handler:
- Log Error with args.Exception, args.ErrorSource, args.EntityPath.
- Return Task.CompletedTask.

INTERNAL ENUM: MessageProcessingOutcome
Defined in Worker.cs (same file):
  Complete, Abandon, DeadLetterInvalidJson, DeadLetterMissingData

SYSTEMD UNIT FILE: WhmcsWorkerService/whmcs-worker.service
------------------------------------------------------------
[Unit]
Description=WHMCS Worker Service for OnePageAuthor domain registration
After=network.target

[Service]
Type=notify
User=whmcsworker
WorkingDirectory=/opt/whmcs-worker
ExecStart=/opt/whmcs-worker/WhmcsWorkerService
EnvironmentFile=/etc/whmcs-worker/environment
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal
SyslogIdentifier=whmcs-worker

[Install]
WantedBy=multi-user.target

DEPLOYMENT: The unit file is copied to /etc/systemd/system/ by the GitHub
Actions deployment script (see the workflow). The file should be included
in the publish output so it is present in the deployment zip.

README: WhmcsWorkerService/README.md
-------------------------------------
Write a short README covering:
1. Purpose and role in the system
2. Required environment variables:
   - SERVICE_BUS_CONNECTION_STRING (Azure Service Bus connection string with
     Listen permissions on the whmcs-domain-registrations queue)
   - SERVICE_BUS_WHMCS_QUEUE_NAME (optional, default: whmcs-domain-registrations)
   - WHMCS_API_URL
   - WHMCS_API_IDENTIFIER
   - WHMCS_API_SECRET
3. How messages flow: DomainRegistrationTriggerFunction → Service Bus → WhmcsWorkerService → WHMCS API
4. Static IP requirement: the VM must have a static public IP which must be
   added to the WHMCS API credential allowlist
5. Deployment: handled by GitHub Actions (main_onepageauthorapi.yml) via
   az vm run-command invoke (no inbound SSH required)
6. Monitoring: use `journalctl -u whmcs-worker -f` on the VM to watch logs;
   also visible in Application Insights if the worker sends telemetry

UNIT TESTS: OnePageAuthor.Test/WhmcsWorkerService/WorkerTests.cs
-----------------------------------------------------------------
Write unit tests using xUnit and Moq for the Worker class.
Use the test naming convention: MethodName_Scenario_ExpectedBehavior.

Tests to implement:
1. ProcessWhmcsMessageAsync_ValidMessage_RegistersDomainAndReturnsComplete
   - Mock IWhmcsService.RegisterDomainAsync returning true
   - Mock IWhmcsService.UpdateNameServersAsync returning true (2 name servers)
   - Assert outcome == MessageProcessingOutcome.Complete

2. ProcessWhmcsMessageAsync_InvalidJson_ReturnsDeadLetterInvalidJson
   - Pass malformed JSON as messageBody
   - Assert outcome == MessageProcessingOutcome.DeadLetterInvalidJson

3. ProcessWhmcsMessageAsync_NullDomainRegistration_ReturnsDeadLetterMissingData
   - Pass valid JSON where DomainRegistration is null
   - Assert outcome == MessageProcessingOutcome.DeadLetterMissingData

4. ProcessWhmcsMessageAsync_NullDomain_ReturnsDeadLetterMissingData
   - Pass valid JSON where DomainRegistration.Domain is null
   - Assert outcome == MessageProcessingOutcome.DeadLetterMissingData

5. ProcessWhmcsMessageAsync_RegisterDomainReturnsFalse_ReturnsAbandon
   - Mock RegisterDomainAsync returning false
   - Assert outcome == MessageProcessingOutcome.Abandon

6. ProcessWhmcsMessageAsync_RegisterDomainThrows_ReturnsAbandon
   - Mock RegisterDomainAsync throwing HttpRequestException
   - Assert outcome == MessageProcessingOutcome.Abandon

7. ProcessWhmcsMessageAsync_NSUpdateFails_StillReturnsComplete
   - Mock RegisterDomainAsync returning true
   - Mock UpdateNameServersAsync throwing exception
   - Assert outcome == MessageProcessingOutcome.Complete
   (registration succeeded; NS update failure must not dead-letter)

8. ProcessWhmcsMessageAsync_NoNameServers_SkipsNSUpdate_ReturnsComplete
   - Message with empty NameServers array
   - Assert UpdateNameServersAsync is never called
   - Assert outcome == MessageProcessingOutcome.Complete

9. ProcessWhmcsMessageAsync_TooFewNameServers_SkipsNSUpdate_ReturnsComplete
   - Message with 1 name server (below MinNameServers = 2)
   - Assert UpdateNameServersAsync is never called
   - Assert outcome == MessageProcessingOutcome.Complete

10. ProcessWhmcsMessageAsync_TooManyNameServers_SkipsNSUpdate_ReturnsComplete
    - Message with 6 name servers (above MaxNameServers = 5)
    - Assert UpdateNameServersAsync is never called
    - Assert outcome == MessageProcessingOutcome.Complete

CONSTRAINTS:
- All business logic stays in the Worker class and IWhmcsService implementations.
- Do not add HTTP endpoints or Azure Functions to this project.
- Do not use a DI container directly in Worker — inject all dependencies
  via constructor.
- Keep MaxConcurrentCalls = 1 to avoid overloading WHMCS.
- Worker must handle a clean shutdown (OperationCanceledException from stoppingToken)
  without logging errors.
```
