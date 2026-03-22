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
        /// <summary>Domain is not available for registration; move to dead-letter sub-queue.</summary>
        DeadLetterDomainUnavailable,
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
        private readonly string? _clientId;

        // -----------------------------------------------------------------------
        // Structured EventIds — used to filter and correlate log entries in KQL.
        // Service lifecycle: 1001–1099
        // Message processing: 2001–2099
        // Service Bus errors: 3001–3099
        // -----------------------------------------------------------------------
        private static readonly EventId EvtServiceStarting = new(1001, "WhmcsWorkerStarting");
        private static readonly EventId EvtServiceRunning = new(1002, "WhmcsWorkerRunning");
        private static readonly EventId EvtServiceStopping = new(1003, "WhmcsWorkerStopping");

        private static readonly EventId EvtMessageReceived = new(2001, "MessageReceived");
        private static readonly EventId EvtMessageDeserializeFail = new(2002, "MessageDeserializeFailed");
        private static readonly EventId EvtMessageMissingData = new(2003, "MessageMissingData");

        private static readonly EventId EvtProcessingStarted = new(2010, "ProcessingStarted");
        private static readonly EventId EvtRegistrationStarted = new(2011, "RegistrationStarted");
        private static readonly EventId EvtRegistrationSucceeded = new(2012, "RegistrationSucceeded");
        private static readonly EventId EvtRegistrationFailed = new(2013, "RegistrationFailed");
        private static readonly EventId EvtRegistrationException = new(2014, "RegistrationException");

        private static readonly EventId EvtNsUpdateStarted = new(2021, "NameServerUpdateStarted");
        private static readonly EventId EvtNsUpdateSucceeded = new(2022, "NameServerUpdateSucceeded");
        private static readonly EventId EvtNsUpdateFailed = new(2023, "NameServerUpdateFailed");
        private static readonly EventId EvtNsUpdateException = new(2024, "NameServerUpdateException");
        private static readonly EventId EvtNsUpdateSkipped = new(2025, "NameServerUpdateSkipped");

        private static readonly EventId EvtProcessingCompleted = new(2030, "ProcessingCompleted");

        private static readonly EventId EvtWhoisCheckStarted = new(2040, "WhoisCheckStarted");
        private static readonly EventId EvtWhoisCheckAvailable = new(2041, "WhoisCheckAvailable");
        private static readonly EventId EvtWhoisCheckUnavailable = new(2042, "WhoisCheckUnavailable");
        private static readonly EventId EvtWhoisCheckException = new(2043, "WhoisCheckException");

        private static readonly EventId EvtAddOrderStarted = new(2050, "AddOrderStarted");
        private static readonly EventId EvtAddOrderSucceeded = new(2051, "AddOrderSucceeded");
        private static readonly EventId EvtAddOrderFailed = new(2052, "AddOrderFailed");
        private static readonly EventId EvtAddOrderException = new(2053, "AddOrderException");

        private static readonly EventId EvtServiceBusError = new(3001, "ServiceBusError");

        public Worker(
            ILogger<Worker> logger,
            IWhmcsService whmcsService,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _whmcsService = whmcsService ?? throw new ArgumentNullException(nameof(whmcsService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _clientId = _configuration["WHMCS_CLIENT_ID"];
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
            var bodyLength = args.Message.Body.ToMemory().Length;
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
                case MessageProcessingOutcome.DeadLetterDomainUnavailable:
                    await args.DeadLetterMessageAsync(args.Message,
                        deadLetterReason: "DomainUnavailable",
                        deadLetterErrorDescription: "Domain is not available for registration",
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

            // Step 1: Check domain availability via WHMCS DomainWhois API
            bool isAvailable;
            var whoisStopwatch = Stopwatch.StartNew();

            _logger.LogInformation(EvtWhoisCheckStarted,
                "Checking availability of domain {Domain} via WHMCS DomainWhois (message ID {MessageId})",
                domainName, messageId);

            try
            {
                isAvailable = await _whmcsService.CheckDomainAvailabilityAsync(domainName);
                whoisStopwatch.Stop();
            }
            catch (Exception ex)
            {
                whoisStopwatch.Stop();
                _logger.LogError(EvtWhoisCheckException, ex,
                    "Exception while checking domain availability for {Domain} via WHMCS DomainWhois. " +
                    "Message will be abandoned for retry.",
                    domainName);
                return MessageProcessingOutcome.Abandon;
            }

            _logger.LogDebug(EvtWhoisCheckStarted,
                "WHMCS DomainWhois completed in {WhoisElapsedMs}ms for domain {Domain}, Available={Available}",
                whoisStopwatch.ElapsedMilliseconds, domainName, isAvailable);

            if (!isAvailable)
            {
                _logger.LogWarning(EvtWhoisCheckUnavailable,
                    "Domain {Domain} is not available for registration (message ID {MessageId}). " +
                    "Dead-lettering message.",
                    domainName, messageId);
                return MessageProcessingOutcome.DeadLetterDomainUnavailable;
            }

            _logger.LogInformation(EvtWhoisCheckAvailable,
                "Domain {Domain} is available for registration", domainName);

            // Step 2: Place domain order via WHMCS AddOrder API (includes name servers)
            bool orderSuccess;
            var orderStopwatch = Stopwatch.StartNew();

            _logger.LogDebug(EvtAddOrderStarted,
                "Calling WHMCS AddOrderAsync for domain {Domain} (message ID {MessageId})",
                domainName, messageId);

            if (nameServers.Length > 0)
            {
                _logger.LogTrace(EvtAddOrderStarted,
                    "Name servers to include in order for domain {Domain}: [{NameServers}]",
                    domainName, string.Join(", ", nameServers));
            }

            try
            {
                orderSuccess = await _whmcsService.AddOrderAsync(message.DomainRegistration, nameServers, _clientId);
                orderStopwatch.Stop();
            }
            catch (Exception ex)
            {
                orderStopwatch.Stop();
                _logger.LogError(EvtAddOrderException, ex,
                    "Exception while placing domain order for {Domain} via WHMCS AddOrder. " +
                    "Message will be abandoned for retry.",
                    domainName);
                return MessageProcessingOutcome.Abandon;
            }

            _logger.LogDebug(EvtAddOrderStarted,
                "WHMCS AddOrderAsync completed in {OrderElapsedMs}ms for domain {Domain}, Result={Result}",
                orderStopwatch.ElapsedMilliseconds, domainName, orderSuccess);

            if (!orderSuccess)
            {
                _logger.LogWarning(EvtAddOrderFailed,
                    "WHMCS domain order returned false for domain {Domain}. " +
                    "Message will be abandoned for retry.",
                    domainName);
                return MessageProcessingOutcome.Abandon;
            }

            _logger.LogInformation(EvtAddOrderSucceeded,
                "Successfully placed domain order for {Domain} via WHMCS API", domainName);

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

