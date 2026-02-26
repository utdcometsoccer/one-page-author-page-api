using System.Text.Json;
using Azure.Messaging.ServiceBus;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WhmcsWorkerService
{
    /// <summary>
    /// Background worker that dequeues WHMCS domain registration messages from the
    /// Azure Service Bus queue and calls the WHMCS REST API.
    /// This service is deployed to a VM with a static outbound IP address so that
    /// WHMCS can allowlist it.
    /// </summary>
    public sealed class Worker : BackgroundService
    {
        private const int MinNameServers = 2;
        private const int MaxNameServers = 5;
        private readonly ILogger<Worker> _logger;
        private readonly IWhmcsService _whmcsService;
        private readonly IConfiguration _configuration;

        public Worker(
            ILogger<Worker> logger,
            IWhmcsService whmcsService,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _whmcsService = whmcsService ?? throw new ArgumentNullException(nameof(whmcsService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connectionString = _configuration["SERVICE_BUS_CONNECTION_STRING"];
            var queueName = _configuration["SERVICE_BUS_WHMCS_QUEUE_NAME"] ?? "whmcs-domain-registrations";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogCritical(
                    "SERVICE_BUS_CONNECTION_STRING is not configured. " +
                    "The WHMCS worker service cannot start without a Service Bus connection string.");
                return;
            }

            _logger.LogInformation("WHMCS Worker Service starting. Listening on queue '{Queue}'", queueName);

            await using var client = new ServiceBusClient(connectionString);
            await using var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
            {
                // Process one message at a time to keep WHMCS load manageable
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false,
            });

            processor.ProcessMessageAsync += ProcessMessageAsync;
            processor.ProcessErrorAsync += ProcessErrorAsync;

            await processor.StartProcessingAsync(stoppingToken);

            _logger.LogInformation("WHMCS Worker Service is running and listening for messages");

            try
            {
                // Keep the worker alive until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }

            _logger.LogInformation("WHMCS Worker Service is stopping");
            await processor.StopProcessingAsync();
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            WhmcsDomainRegistrationMessage? message;

            try
            {
                message = JsonSerializer.Deserialize<WhmcsDomainRegistrationMessage>(body);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Failed to deserialize WHMCS queue message {MessageId}. Dead-lettering.",
                    args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "DeserializationFailure",
                    deadLetterErrorDescription: ex.Message,
                    cancellationToken: args.CancellationToken);
                return;
            }

            if (message?.DomainRegistration?.Domain == null)
            {
                _logger.LogError(
                    "WHMCS queue message {MessageId} has no domain registration data. Dead-lettering.",
                    args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "MissingDomainData",
                    deadLetterErrorDescription: "DomainRegistration or Domain is null",
                    cancellationToken: args.CancellationToken);
                return;
            }

            var domainName = message.DomainRegistration.Domain.FullDomainName;
            _logger.LogInformation(
                "Processing WHMCS registration for domain {Domain} (message ID {MessageId})",
                domainName, args.Message.MessageId);

            // Step 1: Register domain via WHMCS API
            bool registrationSuccess;
            try
            {
                registrationSuccess = await _whmcsService.RegisterDomainAsync(message.DomainRegistration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception while registering domain {Domain} via WHMCS API. " +
                    "Message will be abandoned for retry.",
                    domainName);
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
                return;
            }

            if (!registrationSuccess)
            {
                _logger.LogWarning(
                    "WHMCS domain registration returned false for domain {Domain}. " +
                    "Message will be abandoned for retry.",
                    domainName);
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
                return;
            }

            _logger.LogInformation("Successfully registered domain {Domain} via WHMCS API", domainName);

            // Step 2: Update name servers in WHMCS if available
            if (message.NameServers.Length >= MinNameServers && message.NameServers.Length <= MaxNameServers)
            {
                _logger.LogInformation(
                    "Updating {Count} name servers for domain {Domain} in WHMCS",
                    message.NameServers.Length, domainName);

                bool nsUpdateSuccess;
                try
                {
                    nsUpdateSuccess = await _whmcsService.UpdateNameServersAsync(domainName, message.NameServers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception while updating name servers for domain {Domain} in WHMCS. " +
                        "Registration was already completed; completing message anyway.",
                        domainName);
                    // Don't abandon – the registration succeeded; only NS update failed
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                    return;
                }

                if (nsUpdateSuccess)
                {
                    _logger.LogInformation(
                        "Successfully updated name servers for domain {Domain} in WHMCS", domainName);
                }
                else
                {
                    _logger.LogWarning(
                        "WHMCS name server update returned false for domain {Domain}. " +
                        "Registration was already completed; completing message anyway.",
                        domainName);
                }
            }
            else if (message.NameServers.Length > MaxNameServers
                     || (message.NameServers.Length > 0 && message.NameServers.Length < MinNameServers))
            {
                _logger.LogWarning(
                    "Received {Count} name server(s) for domain {Domain}; WHMCS requires {Min}–{Max}. Skipping name server update.",
                    message.NameServers.Length, domainName, MinNameServers, MaxNameServers);
            }
            else
            {
                _logger.LogInformation(
                    "No name servers provided for domain {Domain}; skipping WHMCS name server update", domainName);
            }

            // Mark the message as successfully processed
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            _logger.LogInformation(
                "Completed processing WHMCS registration for domain {Domain} (message ID {MessageId})",
                domainName, args.Message.MessageId);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception,
                "Service Bus processor error. Source: {Source}, Entity: {Entity}",
                args.ErrorSource, args.EntityPath);
            return Task.CompletedTask;
        }
    }
}
