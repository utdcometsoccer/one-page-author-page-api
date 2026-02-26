using System.Text.Json;
using Azure.Messaging.ServiceBus;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Sends WHMCS domain registration messages to an Azure Service Bus queue.
    /// The queue decouples the Azure Function from the WHMCS REST API and allows
    /// the VM-hosted worker service (with a static outbound IP) to make WHMCS calls.
    /// </summary>
    public class WhmcsQueueService : IWhmcsQueueService, IAsyncDisposable
    {
        private readonly ILogger<WhmcsQueueService> _logger;
        private readonly ServiceBusClient? _client;
        private readonly ServiceBusSender? _sender;
        private readonly bool _isConfigured;

        public WhmcsQueueService(
            ILogger<WhmcsQueueService> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var connectionString = configuration["SERVICE_BUS_CONNECTION_STRING"];
            var queueName = configuration["SERVICE_BUS_WHMCS_QUEUE_NAME"];

            _isConfigured = !string.IsNullOrWhiteSpace(connectionString)
                         && !string.IsNullOrWhiteSpace(queueName);

            if (!_isConfigured)
            {
                _logger.LogWarning(
                    "WHMCS queue service is not configured. " +
                    "Set SERVICE_BUS_CONNECTION_STRING and SERVICE_BUS_WHMCS_QUEUE_NAME to enable.");
                return;
            }

            var client = new ServiceBusClient(connectionString);
            _client = client;
            _sender = client.CreateSender(queueName);
        }

        /// <inheritdoc/>
        public async Task EnqueueDomainRegistrationAsync(DomainRegistration registration, string[] nameServers)
        {
            if (!_isConfigured || _sender == null)
            {
                _logger.LogWarning(
                    "WHMCS queue service is not configured; skipping enqueue for domain {Domain}",
                    registration?.Domain?.FullDomainName ?? "(unknown)");
                return;
            }

            if (registration == null)
                throw new ArgumentNullException(nameof(registration));

            if (nameServers == null)
                throw new ArgumentNullException(nameof(nameServers));

            var message = new WhmcsDomainRegistrationMessage
            {
                DomainRegistration = registration,
                NameServers = nameServers,
            };

            var json = JsonSerializer.Serialize(message);
            var sbMessage = new ServiceBusMessage(json)
            {
                MessageId = message.MessageId,
                ContentType = "application/json",
                Subject = $"whmcs-register:{registration.Domain?.FullDomainName}",
            };

            _logger.LogInformation(
                "Enqueueing WHMCS registration for domain {Domain} (message ID {MessageId})",
                registration.Domain?.FullDomainName, message.MessageId);

            await _sender.SendMessageAsync(sbMessage);

            _logger.LogInformation(
                "Successfully enqueued WHMCS registration for domain {Domain} (message ID {MessageId})",
                registration.Domain?.FullDomainName, message.MessageId);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_sender != null)
                await _sender.DisposeAsync();
            if (_client != null)
                await _client.DisposeAsync();
        }
    }
}
