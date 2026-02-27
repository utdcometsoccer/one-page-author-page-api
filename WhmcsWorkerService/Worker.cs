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
    /// Outcome of processing a single WHMCS queue message, used to drive the
    /// appropriate Service Bus disposition (complete / abandon / dead-letter).
    /// </summary>
    internal enum MessageProcessingOutcome
    {
        /// <summary>Message was processed successfully; remove from queue.</summary>
        Complete,
        /// <summary>Transient failure; return message to queue for retry.</summary>
        Abandon,
        /// <summary>Message JSON was malformed; move to dead-letter sub-queue.</summary>
        DeadLetterInvalidJson,
        /// <summary>Message is missing required domain data; move to dead-letter sub-queue.</summary>
        DeadLetterMissingData,
    }

    /// <summary>
    /// Background worker that dequeues WHMCS domain registration messages from the
    /// Azure Service Bus queue and calls the WHMCS REST API.
    /// This service is deployed to a VM with a static outbound IP address so that
    /// WHMCS can allowlist it.
    /// </summary>
    public sealed class Worker : BackgroundService
    {
        internal const int MinNameServers = 2;
        internal const int MaxNameServers = 5;
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
            var outcome = await ProcessWhmcsMessageAsync(
                args.Message.Body.ToString(),
                args.Message.MessageId,
                args.CancellationToken);

            switch (outcome)
            {
                case MessageProcessingOutcome.Complete:
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                    break;
                case MessageProcessingOutcome.Abandon:
                    await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
                    break;
                case MessageProcessingOutcome.DeadLetterInvalidJson:
                    await args.DeadLetterMessageAsync(args.Message,
                        deadLetterReason: "DeserializationFailure",
                        deadLetterErrorDescription: "Message body could not be deserialized as WhmcsDomainRegistrationMessage",
                        cancellationToken: args.CancellationToken);
                    break;
                case MessageProcessingOutcome.DeadLetterMissingData:
                    await args.DeadLetterMessageAsync(args.Message,
                        deadLetterReason: "MissingDomainData",
                        deadLetterErrorDescription: "DomainRegistration or Domain is null",
                        cancellationToken: args.CancellationToken);
                    break;
            }
        }

        /// <summary>
        /// Core processing logic for a WHMCS domain registration queue message.
        /// Separated from the Service Bus event handler for testability.
        /// </summary>
        internal async Task<MessageProcessingOutcome> ProcessWhmcsMessageAsync(
            string messageBody,
            string messageId,
            CancellationToken cancellationToken = default)
        {
            WhmcsDomainRegistrationMessage? message;

            try
            {
                message = JsonSerializer.Deserialize<WhmcsDomainRegistrationMessage>(messageBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Failed to deserialize WHMCS queue message {MessageId}. Dead-lettering.",
                    messageId);
                return MessageProcessingOutcome.DeadLetterInvalidJson;
            }

            if (message?.DomainRegistration?.Domain == null)
            {
                _logger.LogError(
                    "WHMCS queue message {MessageId} has no domain registration data. Dead-lettering.",
                    messageId);
                return MessageProcessingOutcome.DeadLetterMissingData;
            }

            // Normalize null NameServers (may be omitted or null in the JSON payload)
            var nameServers = message.NameServers ?? [];

            var domainName = message.DomainRegistration.Domain.FullDomainName;
            _logger.LogInformation(
                "Processing WHMCS registration for domain {Domain} (message ID {MessageId})",
                domainName, messageId);

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
                return MessageProcessingOutcome.Abandon;
            }

            if (!registrationSuccess)
            {
                _logger.LogWarning(
                    "WHMCS domain registration returned false for domain {Domain}. " +
                    "Message will be abandoned for retry.",
                    domainName);
                return MessageProcessingOutcome.Abandon;
            }

            _logger.LogInformation("Successfully registered domain {Domain} via WHMCS API", domainName);

            // Step 2: Update name servers in WHMCS if available
            if (nameServers.Length >= MinNameServers && nameServers.Length <= MaxNameServers)
            {
                _logger.LogInformation(
                    "Updating {Count} name servers for domain {Domain} in WHMCS",
                    nameServers.Length, domainName);

                bool nsUpdateSuccess;
                try
                {
                    nsUpdateSuccess = await _whmcsService.UpdateNameServersAsync(domainName, nameServers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception while updating name servers for domain {Domain} in WHMCS. " +
                        "Registration was already completed; completing message anyway.",
                        domainName);
                    // Don't abandon – the registration succeeded; only NS update failed
                    return MessageProcessingOutcome.Complete;
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
            else if (nameServers.Length > MaxNameServers
                     || (nameServers.Length > 0 && nameServers.Length < MinNameServers))
            {
                _logger.LogWarning(
                    "Received {Count} name server(s) for domain {Domain}; WHMCS requires {Min}–{Max}. Skipping name server update.",
                    nameServers.Length, domainName, MinNameServers, MaxNameServers);
            }
            else
            {
                _logger.LogInformation(
                    "No name servers provided for domain {Domain}; skipping WHMCS name server update", domainName);
            }

            _logger.LogInformation(
                "Completed processing WHMCS registration for domain {Domain} (message ID {MessageId})",
                domainName, messageId);

            return MessageProcessingOutcome.Complete;
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
