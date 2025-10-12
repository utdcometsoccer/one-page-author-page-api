using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function triggered by changes to the DomainRegistrations Cosmos DB container.
    /// Registers domains using the Google Domains API when new registrations are created.
    /// </summary>
    /// <remarks>
    /// This function uses a Cosmos DB trigger with a unique lease prefix to allow multiple functions
    /// to trigger from the same container without conflicts.
    /// </remarks>
    public class GoogleDomainRegistrationFunction
    {
        private readonly ILogger<GoogleDomainRegistrationFunction> _logger;
        private readonly IGoogleDomainsService _googleDomainsService;

        public GoogleDomainRegistrationFunction(
            ILogger<GoogleDomainRegistrationFunction> logger,
            IGoogleDomainsService googleDomainsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _googleDomainsService = googleDomainsService ?? throw new ArgumentNullException(nameof(googleDomainsService));
        }

        /// <summary>
        /// Processes changes to domain registrations and registers domains via Google Domains API.
        /// </summary>
        /// <param name="input">List of changed domain registrations from Cosmos DB</param>
        [Function("GoogleDomainRegistration")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "%COSMOSDB_DATABASE_ID%",
                containerName: "DomainRegistrations",
                Connection = "COSMOSDB_CONNECTION_STRING",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "googledomainregistration",
                CreateLeaseContainerIfNotExists = true)]
            IReadOnlyList<DomainRegistration> input)
        {
            if (input == null || input.Count == 0)
            {
                _logger.LogInformation("GoogleDomainRegistrationFunction received no documents");
                return;
            }

            _logger.LogInformation("GoogleDomainRegistrationFunction processing {Count} domain registration(s)", input.Count);

            foreach (var registration in input)
            {
                try
                {
                    if (registration?.Domain == null)
                    {
                        _logger.LogWarning("Skipping registration {Id} - missing domain information", registration?.id);
                        continue;
                    }

                    var domainName = registration.Domain.FullDomainName;
                    _logger.LogInformation("Processing domain registration {Id} for domain {DomainName}",
                        registration.id, domainName);

                    // Only process pending registrations
                    if (registration.Status != DomainRegistrationStatus.Pending)
                    {
                        _logger.LogInformation("Skipping domain {DomainName} - status is {Status}, expected Pending",
                            domainName, registration.Status);
                        continue;
                    }

                    // Attempt to register domain with Google Domains
                    var success = await _googleDomainsService.RegisterDomainAsync(registration);

                    if (success)
                    {
                        _logger.LogInformation("Successfully initiated domain registration for {DomainName} via Google Domains API",
                            domainName);
                        // Note: Status update would require ClaimsPrincipal, which we don't have in a trigger function
                        // The domain registration is a long-running operation that should be tracked separately
                    }
                    else
                    {
                        _logger.LogError("Failed to register domain {DomainName} via Google Domains API", domainName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing domain registration {Id}", registration?.id);
                    // Don't throw - continue processing other registrations
                }
            }

            _logger.LogInformation("GoogleDomainRegistrationFunction completed processing {Count} registration(s)", input.Count);
        }
    }
}
