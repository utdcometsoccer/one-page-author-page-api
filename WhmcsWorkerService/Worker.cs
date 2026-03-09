using System.Diagnostics;
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

        // -----------------------------------------------------------------------
        // Structured EventIds — used to filter and correlate log entries in KQL.
        // Service lifecycle: 1001–1099
        // Message processing: 2001–2099
        // Service Bus errors: 3001–3099
        // -----------------------------------------------------------------------
        private static readonly EventId EvtServiceStarting        = new(1001, "WhmcsWorkerStarting");
        private static readonly EventId EvtServiceRunning         = new(1002, "WhmcsWorkerRunning");
        private static readonly EventId EvtServiceStopping        = new(1003, "WhmcsWorkerStopping");

        private static readonly EventId EvtMessageReceived        = new(2001, "MessageReceived");
        private static readonly EventId EvtMessageDeserializeFail = new(2002, "MessageDeserializeFailed");
        private static readonly EventId EvtMessageMissingData     = new(2003, "MessageMissingData");

        private static readonly EventId EvtProcessingStarted      = new(2010, "ProcessingStarted");
        private static readonly EventId EvtRegistrationStarted    = new(2011, "RegistrationStarted");
        private static readonly EventId EvtRegistrationSucceeded  = new(2012, "RegistrationSucceeded");
        private static readonly EventId EvtRegistrationFailed     = new(2013, "RegistrationFailed");
        private static readonly EventId EvtRegistrationException  = new(2014, "RegistrationException");

        private static readonly EventId EvtNsUpdateStarted        = new(2021, "NameServerUpdateStarted");
        private static readonly EventId EvtNsUpdateSucceeded      = new(2022, "NameServerUpdateSucceeded");
        private static readonly EventId EvtNsUpdateFailed         = new(2023, "NameServerUpdateFailed");
        private static readonly EventId EvtNsUpdateException      = new(2024, "NameServerUpdateException");
        private static readonly EventId EvtNsUpdateSkipped        = new(2025, "NameServerUpdateSkipped");

        private static readonly EventId EvtProcessingCompleted    = new(2030, "ProcessingCompleted");
        private static readonly EventId EvtServiceBusError        = new(3001, "ServiceBusError");

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

            _logger.LogInformation(EvtServiceStarting,
                "WHMCS Worker Service starting. Listening on queue '{Queue}'", queueName);

            _logger.LogDebug(EvtServiceStarting,
                "WHMCS Worker Service processor options: Queue={Queue}, MaxConcurrentCalls=1, AutoCompleteMessages=false",
                queueName);

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

            _logger.LogInformation(EvtServiceRunning,
                "WHMCS Worker Service is running and listening for messages");

            try
            {
                // Keep the worker alive until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }

            _logger.LogInformation(EvtServiceStopping, "WHMCS Worker Service is stopping");
            await processor.StopProcessingAsync();
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var messageId = args.Message.MessageId;
            var bodyLength = args.Message.Body.ToArray().Length;
            var enqueuedTime = args.Message.EnqueuedTime;
            var deliveryCount = args.Message.DeliveryCount;
            var sequenceNumber = args.Message.SequenceNumber;

            _logger.LogDebug(EvtMessageReceived,
                "Service Bus message received: MessageId={MessageId}, BodyBytes={BodyBytes}, " +
                "EnqueuedAt={EnqueuedAt:O}, DeliveryCount={DeliveryCount}, SequenceNumber={SequenceNumber}",
                messageId, bodyLength, enqueuedTime, deliveryCount, sequenceNumber);

            var outcome = await ProcessWhmcsMessageAsync(
                args.Message.Body.ToString(),
                messageId,
                args.CancellationToken);

            _logger.LogDebug(EvtProcessingCompleted,
                "Service Bus disposition for message {MessageId}: {Outcome}",
                messageId, outcome);

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
            var totalStopwatch = Stopwatch.StartNew();

            _logger.LogTrace(EvtProcessingStarted,
                "Deserializing WHMCS queue message {MessageId} (body length: {BodyLength} chars)",
                messageId, messageBody.Length);

            WhmcsDomainRegistrationMessage? message;

            try
            {
                message = JsonSerializer.Deserialize<WhmcsDomainRegistrationMessage>(messageBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(EvtMessageDeserializeFail, ex,
                    "Failed to deserialize WHMCS queue message {MessageId}. Dead-lettering.",
                    messageId);
                return MessageProcessingOutcome.DeadLetterInvalidJson;
            }

            if (message?.DomainRegistration?.Domain == null)
            {
                _logger.LogError(EvtMessageMissingData,
                    "WHMCS queue message {MessageId} has no domain registration data. Dead-lettering.",
                    messageId);
                return MessageProcessingOutcome.DeadLetterMissingData;
            }

            // Normalize null NameServers (may be omitted or null in the JSON payload)
            var nameServers = message.NameServers ?? [];

            var domainName = message.DomainRegistration.Domain.FullDomainName;

            _logger.LogInformation(EvtProcessingStarted,
                "Processing WHMCS registration for domain {Domain} (message ID {MessageId})",
                domainName, messageId);

            _logger.LogDebug(EvtProcessingStarted,
                "Message metadata: Domain={Domain}, MessageId={MessageId}, " +
                "EnqueuedAt={EnqueuedAt:O}, NameServerCount={NameServerCount}, RegistrationId={RegistrationId}",
                domainName, messageId, message.EnqueuedAt, nameServers.Length, message.DomainRegistration.id);

            if (nameServers.Length > 0)
            {
                _logger.LogTrace(EvtProcessingStarted,
                    "Name servers in message for domain {Domain}: [{NameServers}]",
                    domainName, string.Join(", ", nameServers));
            }

            // Step 1: Register domain via WHMCS API
            bool registrationSuccess;
            var registrationStopwatch = Stopwatch.StartNew();

            _logger.LogDebug(EvtRegistrationStarted,
                "Calling WHMCS RegisterDomainAsync for domain {Domain} (message ID {MessageId})",
                domainName, messageId);

            try
            {
                registrationSuccess = await _whmcsService.RegisterDomainAsync(message.DomainRegistration);
                registrationStopwatch.Stop();
            }
            catch (Exception ex)
            {
                registrationStopwatch.Stop();
                _logger.LogError(EvtRegistrationException, ex,
                    "Exception while registering domain {Domain} via WHMCS API. " +
                    "Message will be abandoned for retry.",
                    domainName);
                _logger.LogDebug(EvtRegistrationException,
                    "WHMCS RegisterDomainAsync threw after {ElapsedMs}ms for domain {Domain}",
                    registrationStopwatch.ElapsedMilliseconds, domainName);
                return MessageProcessingOutcome.Abandon;
            }

            _logger.LogDebug(EvtRegistrationSucceeded,
                "WHMCS RegisterDomainAsync completed in {RegistrationElapsedMs}ms for domain {Domain}, Result={Result}",
                registrationStopwatch.ElapsedMilliseconds, domainName, registrationSuccess);

            if (!registrationSuccess)
            {
                _logger.LogWarning(EvtRegistrationFailed,
                    "WHMCS domain registration returned false for domain {Domain}. " +
                    "Message will be abandoned for retry.",
                    domainName);
                return MessageProcessingOutcome.Abandon;
            }

            _logger.LogInformation(EvtRegistrationSucceeded,
                "Successfully registered domain {Domain} via WHMCS API", domainName);

            // Step 2: Update name servers in WHMCS if available
            if (nameServers.Length >= MinNameServers && nameServers.Length <= MaxNameServers)
            {
                _logger.LogInformation(EvtNsUpdateStarted,
                    "Updating {Count} name servers for domain {Domain} in WHMCS",
                    nameServers.Length, domainName);

                _logger.LogTrace(EvtNsUpdateStarted,
                    "Name servers to set for domain {Domain}: [{NameServers}]",
                    domainName, string.Join(", ", nameServers));

                bool nsUpdateSuccess;
                var nsStopwatch = Stopwatch.StartNew();

                try
                {
                    nsUpdateSuccess = await _whmcsService.UpdateNameServersAsync(domainName, nameServers);
                    nsStopwatch.Stop();
                }
                catch (Exception ex)
                {
                    nsStopwatch.Stop();
                    _logger.LogError(EvtNsUpdateException, ex,
                        "Exception while updating name servers for domain {Domain} in WHMCS. " +
                        "Registration was already completed; completing message anyway.",
                        domainName);
                    _logger.LogDebug(EvtNsUpdateException,
                        "WHMCS UpdateNameServersAsync threw after {NsElapsedMs}ms for domain {Domain}",
                        nsStopwatch.ElapsedMilliseconds, domainName);
                    // Don't abandon – the registration succeeded; only NS update failed
                    return MessageProcessingOutcome.Complete;
                }

                _logger.LogDebug(EvtNsUpdateSucceeded,
                    "WHMCS UpdateNameServersAsync completed in {NsElapsedMs}ms for domain {Domain}, Result={Result}",
                    nsStopwatch.ElapsedMilliseconds, domainName, nsUpdateSuccess);

                if (nsUpdateSuccess)
                {
                    _logger.LogInformation(EvtNsUpdateSucceeded,
                        "Successfully updated name servers for domain {Domain} in WHMCS", domainName);
                }
                else
                {
                    _logger.LogWarning(EvtNsUpdateFailed,
                        "WHMCS name server update returned false for domain {Domain}. " +
                        "Registration was already completed; completing message anyway.",
                        domainName);
                }
            }
            else if (nameServers.Length > MaxNameServers
                     || (nameServers.Length > 0 && nameServers.Length < MinNameServers))
            {
                _logger.LogWarning(EvtNsUpdateSkipped,
                    "Received {Count} name server(s) for domain {Domain}; WHMCS requires {Min}–{Max}. Skipping name server update.",
                    nameServers.Length, domainName, MinNameServers, MaxNameServers);
            }
            else
            {
                _logger.LogInformation(EvtNsUpdateSkipped,
                    "No name servers provided for domain {Domain}; skipping WHMCS name server update", domainName);
            }

            totalStopwatch.Stop();

            _logger.LogInformation(EvtProcessingCompleted,
                "Completed processing WHMCS registration for domain {Domain} (message ID {MessageId})",
                domainName, messageId);

            _logger.LogDebug(EvtProcessingCompleted,
                "Total processing time for domain {Domain} (message ID {MessageId}): {TotalElapsedMs}ms",
                domainName, messageId, totalStopwatch.ElapsedMilliseconds);

            return MessageProcessingOutcome.Complete;
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(EvtServiceBusError, args.Exception,
                "Service Bus processor error. Source: {Source}, Entity: {Entity}",
                args.ErrorSource, args.EntityPath);
            return Task.CompletedTask;
        }
    }
}

