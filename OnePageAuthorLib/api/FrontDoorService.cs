using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Cdn;
using Azure.ResourceManager.Cdn.Models;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for managing Azure Front Door domain operations.
    /// </summary>
    public class FrontDoorService : IFrontDoorService
    {
        private readonly ILogger<FrontDoorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ArmClient _armClient;
        private readonly string _subscriptionId;
        private readonly string _resourceGroupName;
        private readonly string _frontDoorProfileName;

        public FrontDoorService(
            ILogger<FrontDoorService> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Get configuration values
            _subscriptionId = _configuration["AZURE_SUBSCRIPTION_ID"] 
                ?? throw new InvalidOperationException("AZURE_SUBSCRIPTION_ID configuration is required");
            _resourceGroupName = _configuration["AZURE_RESOURCE_GROUP_NAME"] 
                ?? throw new InvalidOperationException("AZURE_RESOURCE_GROUP_NAME configuration is required");
            _frontDoorProfileName = _configuration["AZURE_FRONTDOOR_PROFILE_NAME"] 
                ?? throw new InvalidOperationException("AZURE_FRONTDOOR_PROFILE_NAME configuration is required");

            // Initialize ARM client with DefaultAzureCredential (supports managed identity)
            var credential = new DefaultAzureCredential();
            _armClient = new ArmClient(credential);
        }

        /// <summary>
        /// Checks if a domain already exists in Azure Front Door.
        /// </summary>
        public async Task<bool> DomainExistsAsync(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentException("Domain name cannot be null or empty", nameof(domainName));
            }

            try
            {
                _logger.LogInformation("Checking if domain {DomainName} exists in Front Door", domainName);

                var subscription = await _armClient.GetSubscriptionResource(
                    new ResourceIdentifier($"/subscriptions/{_subscriptionId}")).GetAsync();
                
                var resourceGroups = subscription.Value.GetResourceGroups();
                var resourceGroup = await resourceGroups.GetAsync(_resourceGroupName);

                var profiles = resourceGroup.Value.GetProfiles();
                var profile = await profiles.GetAsync(_frontDoorProfileName);

                var customDomains = profile.Value.GetFrontDoorCustomDomains();
                
                await foreach (var domain in customDomains.GetAllAsync())
                {
                    if (domain.Data.HostName?.Equals(domainName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _logger.LogInformation("Domain {DomainName} already exists in Front Door", domainName);
                        return true;
                    }
                }

                _logger.LogInformation("Domain {DomainName} does not exist in Front Door", domainName);
                return false;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure request failed while checking if domain {DomainName} exists", domainName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if domain {DomainName} exists in Front Door", domainName);
                throw;
            }
        }

        /// <summary>
        /// Adds a new domain to Azure Front Door if it doesn't already exist.
        /// </summary>
        public async Task<bool> AddDomainToFrontDoorAsync(DomainRegistration domainRegistration)
        {
            if (domainRegistration?.Domain == null)
            {
                throw new ArgumentException("Domain registration and domain information are required", nameof(domainRegistration));
            }

            var domainName = domainRegistration.Domain.FullDomainName;

            try
            {
                _logger.LogInformation("Attempting to add domain {DomainName} to Front Door", domainName);

                // Check if domain already exists
                if (await DomainExistsAsync(domainName))
                {
                    _logger.LogInformation("Domain {DomainName} already exists in Front Door, skipping creation", domainName);
                    return true;
                }

                // Get the Front Door profile
                var subscription = await _armClient.GetSubscriptionResource(
                    new ResourceIdentifier($"/subscriptions/{_subscriptionId}")).GetAsync();
                
                var resourceGroups = subscription.Value.GetResourceGroups();
                var resourceGroup = await resourceGroups.GetAsync(_resourceGroupName);

                var profiles = resourceGroup.Value.GetProfiles();
                var profile = await profiles.GetAsync(_frontDoorProfileName);

                // Create custom domain
                var customDomains = profile.Value.GetFrontDoorCustomDomains();
                
                // Create a safe name for the domain (Azure resource names have restrictions)
                var safeDomainName = domainName.Replace(".", "-");
                
                var customDomainData = new FrontDoorCustomDomainData
                {
                    HostName = domainName,
                    TlsSettings = new FrontDoorCustomDomainHttpsContent(FrontDoorCertificateType.ManagedCertificate)
                    {
                        MinimumTlsVersion = FrontDoorMinimumTlsVersion.Tls1_2
                    }
                };

                _logger.LogInformation("Creating custom domain {DomainName} in Front Door with resource name {SafeName}", 
                    domainName, safeDomainName);

                var operation = await customDomains.CreateOrUpdateAsync(
                    WaitUntil.Started, 
                    safeDomainName, 
                    customDomainData);

                _logger.LogInformation("Successfully initiated creation of domain {DomainName} in Front Door", domainName);
                return true;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure request failed while adding domain {DomainName} to Front Door. Status: {Status}, ErrorCode: {ErrorCode}", 
                    domainName, ex.Status, ex.ErrorCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding domain {DomainName} to Front Door", domainName);
                return false;
            }
        }
    }
}
