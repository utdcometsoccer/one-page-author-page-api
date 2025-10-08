using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Cosmos DB trigger function that creates DNS zones when domain registrations are added or modified.
    /// Uses a unique lease collection to avoid conflicts with other triggers on the same container.
    /// </summary>
    public class DomainRegistrationTriggerFunction
    {
        private readonly ILogger<DomainRegistrationTriggerFunction> _logger;
        private readonly IDnsZoneService _dnsZoneService;
        private readonly IDomainRegistrationService _domainRegistrationService;

        public DomainRegistrationTriggerFunction(
            ILogger<DomainRegistrationTriggerFunction> logger,
            IDnsZoneService dnsZoneService,
            IDomainRegistrationService domainRegistrationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dnsZoneService = dnsZoneService ?? throw new ArgumentNullException(nameof(dnsZoneService));
            _domainRegistrationService = domainRegistrationService ?? throw new ArgumentNullException(nameof(domainRegistrationService));
        }

        /// <summary>
        /// Triggered when documents are inserted or updated in the DomainRegistrations container.
        /// Creates Azure DNS zones for newly registered domains.
        /// </summary>
        /// <param name="input">List of domain registrations that were added or modified</param>
        [Function("DomainRegistrationTrigger")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "%COSMOSDB_DATABASE_ID%",
                containerName: "DomainRegistrations",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "DnsZone",
                CreateLeaseContainerIfNotExists = true)]
            IReadOnlyList<DomainRegistration> input)
        {
            if (input == null || input.Count == 0)
            {
                _logger.LogInformation("No domain registrations to process");
                return;
            }

            _logger.LogInformation("Processing {Count} domain registration(s)", input.Count);

            foreach (var domainRegistration in input)
            {
                try
                {
                    if (domainRegistration?.Domain == null)
                    {
                        _logger.LogWarning("Domain registration or domain is null, skipping");
                        continue;
                    }

                    var domainName = domainRegistration.Domain.FullDomainName;
                    _logger.LogInformation("Processing domain registration for: {DomainName} (ID: {Id}, Status: {Status})",
                        domainName, domainRegistration.id, domainRegistration.Status);

                    // Only process pending or in-progress registrations
                    if (domainRegistration.Status != DomainRegistrationStatus.Pending &&
                        domainRegistration.Status != DomainRegistrationStatus.InProgress)
                    {
                        _logger.LogInformation("Domain registration {Id} status is {Status}, skipping DNS zone creation",
                            domainRegistration.id, domainRegistration.Status);
                        continue;
                    }

                    // Attempt to create the DNS zone
                    var success = await _dnsZoneService.EnsureDnsZoneExistsAsync(domainRegistration);

                    if (success)
                    {
                        _logger.LogInformation("DNS zone successfully created/verified for domain: {DomainName}", domainName);
                        
                        // Update the domain registration status to completed if it was pending
                        if (domainRegistration.Status == DomainRegistrationStatus.Pending)
                        {
                            // Note: We can't update the status here directly as we don't have ClaimsPrincipal
                            // The status update should be handled by an HTTP endpoint or separate process
                            _logger.LogInformation("DNS zone provisioning completed for domain: {DomainName}", domainName);
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to create DNS zone for domain: {DomainName}", domainName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing domain registration with ID: {Id}",
                        domainRegistration?.id ?? "unknown");
                }
            }

            _logger.LogInformation("Completed processing domain registrations");
        }
    }
}
