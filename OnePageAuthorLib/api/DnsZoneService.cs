using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for managing Azure DNS zones.
    /// </summary>
    public class DnsZoneService : IDnsZoneService
    {
        private readonly ILogger<DnsZoneService> _logger;
        private readonly ArmClient _armClient;
        private readonly string _subscriptionId;
        private readonly string _resourceGroupName;

        public DnsZoneService(
            ILogger<DnsZoneService> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Get Azure credentials and configuration
            _subscriptionId = configuration["AZURE_SUBSCRIPTION_ID"] 
                ?? throw new InvalidOperationException("AZURE_SUBSCRIPTION_ID configuration is required");
            _resourceGroupName = configuration["AZURE_DNS_RESOURCE_GROUP"] 
                ?? throw new InvalidOperationException("AZURE_DNS_RESOURCE_GROUP configuration is required");

            // Initialize Azure Resource Manager client with DefaultAzureCredential
            // This supports multiple authentication methods: managed identity, environment variables, Azure CLI, etc.
            var credential = new DefaultAzureCredential();
            _armClient = new ArmClient(credential);

            _logger.LogInformation("DnsZoneService initialized for subscription {SubscriptionId}, resource group {ResourceGroup}",
                _subscriptionId, _resourceGroupName);
        }

        public async Task<bool> EnsureDnsZoneExistsAsync(DomainRegistration domainRegistration)
        {
            if (domainRegistration?.Domain == null)
            {
                _logger.LogWarning("Domain registration or domain is null");
                return false;
            }

            var domainName = domainRegistration.Domain.FullDomainName;
            
            if (string.IsNullOrWhiteSpace(domainName))
            {
                _logger.LogWarning("Domain name is empty");
                return false;
            }

            _logger.LogInformation("Ensuring DNS zone exists for domain: {DomainName}", domainName);

            try
            {
                // Check if the DNS zone already exists
                if (await DnsZoneExistsAsync(domainName))
                {
                    _logger.LogInformation("DNS zone already exists for domain: {DomainName}", domainName);
                    return true;
                }

                // Create the DNS zone
                await CreateDnsZoneAsync(domainName);
                _logger.LogInformation("DNS zone created successfully for domain: {DomainName}", domainName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring DNS zone exists for domain: {DomainName}", domainName);
                return false;
            }
        }

        public async Task<bool> DnsZoneExistsAsync(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                _logger.LogWarning("Domain name is empty in DnsZoneExistsAsync");
                return false;
            }

            try
            {
                // Get the subscription
                var subscription = await _armClient.GetSubscriptionResource(
                    new ResourceIdentifier($"/subscriptions/{_subscriptionId}")).GetAsync();

                // Get the resource group
                var resourceGroup = await subscription.Value.GetResourceGroups()
                    .GetAsync(_resourceGroupName);

                // Try to get the DNS zone
                var dnsZones = resourceGroup.Value.GetDnsZones();
                var response = await dnsZones.ExistsAsync(domainName);

                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("DNS zone does not exist for domain: {DomainName}", domainName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if DNS zone exists for domain: {DomainName}", domainName);
                return false;
            }
        }

        private async Task CreateDnsZoneAsync(string domainName)
        {
            _logger.LogInformation("Creating DNS zone for domain: {DomainName}", domainName);

            // Get the subscription
            var subscription = await _armClient.GetSubscriptionResource(
                new ResourceIdentifier($"/subscriptions/{_subscriptionId}")).GetAsync();

            // Get the resource group
            var resourceGroup = await subscription.Value.GetResourceGroups()
                .GetAsync(_resourceGroupName);

            // Create the DNS zone
            var dnsZones = resourceGroup.Value.GetDnsZones();
            var dnsZoneData = new DnsZoneData("Global")
            {
                // Set location to "Global" as DNS zones are global resources
            };

            await dnsZones.CreateOrUpdateAsync(
                WaitUntil.Completed,
                domainName,
                dnsZoneData);

            _logger.LogInformation("DNS zone creation completed for domain: {DomainName}", domainName);
        }

        public async Task<string[]?> GetNameServersAsync(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                _logger.LogWarning("Domain name is empty in GetNameServersAsync");
                return null;
            }

            _logger.LogInformation("Retrieving name servers for domain: {DomainName}", domainName);

            try
            {
                // Get the subscription
                var subscription = await _armClient.GetSubscriptionResource(
                    new ResourceIdentifier($"/subscriptions/{_subscriptionId}")).GetAsync();

                // Get the resource group
                var resourceGroup = await subscription.Value.GetResourceGroups()
                    .GetAsync(_resourceGroupName);

                // Get the DNS zone
                var dnsZones = resourceGroup.Value.GetDnsZones();
                var dnsZone = await dnsZones.GetAsync(domainName);

                if (dnsZone?.Value?.Data?.NameServers == null || dnsZone.Value.Data.NameServers.Count == 0)
                {
                    _logger.LogWarning("No name servers found for domain: {DomainName}", domainName);
                    return null;
                }

                var nameServers = dnsZone.Value.Data.NameServers.ToArray();
                _logger.LogInformation("Retrieved {Count} name servers for domain {DomainName}: {NameServers}", 
                    nameServers.Length, domainName, string.Join(", ", nameServers));

                return nameServers;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("DNS zone not found for domain: {DomainName}", domainName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving name servers for domain: {DomainName}", domainName);
                return null;
            }
        }
    }
}
