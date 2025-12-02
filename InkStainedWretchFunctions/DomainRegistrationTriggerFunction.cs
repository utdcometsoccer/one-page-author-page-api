using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function triggered by changes to the DomainRegistrations Cosmos DB container.
    /// Processes new domain registrations and adds them to Azure Front Door if they don't already exist.
    /// </summary>
    /// <remarks>
    /// This function uses a Cosmos DB trigger with a unique lease collection to allow multiple functions
    /// to trigger from the same container without conflicts.
    /// </remarks>
    public class DomainRegistrationTriggerFunction
    {
        private readonly ILogger<DomainRegistrationTriggerFunction> _logger;
        private readonly IFrontDoorService _frontDoorService;
        private readonly IDomainRegistrationService _domainRegistrationService;

        public DomainRegistrationTriggerFunction(
            ILogger<DomainRegistrationTriggerFunction> logger,
            IFrontDoorService frontDoorService,
            IDomainRegistrationService domainRegistrationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _frontDoorService = frontDoorService ?? throw new ArgumentNullException(nameof(frontDoorService));
            _domainRegistrationService = domainRegistrationService ?? throw new ArgumentNullException(nameof(domainRegistrationService));
        }

        /// <summary>
        /// Processes changes to domain registrations and adds new domains to Azure Front Door.
        /// </summary>
        /// <param name="input">List of changed domain registrations from Cosmos DB</param>
        [Function("DomainRegistrationTrigger")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "%COSMOSDB_DATABASE_ID%",
                containerName: "DomainRegistrations",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "domainregistration",
                CreateLeaseContainerIfNotExists = true)]
            IReadOnlyList<DomainRegistration> input)
        {
            if (input == null || input.Count == 0)
            {
                _logger.LogInformation("DomainRegistrationTrigger received no documents");
                return;
            }

            _logger.LogInformation("DomainRegistrationTrigger processing {Count} domain registration(s)", input.Count);

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

                    // Attempt to add domain to Front Door
                    var success = await _frontDoorService.AddDomainToFrontDoorAsync(registration);

                    if (success)
                    {
                        _logger.LogInformation("Successfully processed domain {DomainName} for Front Door", domainName);
                        // Note: Status update would require ClaimsPrincipal, which we don't have in a trigger function
                        // Consider using a separate update mechanism or orchestrator if status updates are needed
                    }
                    else
                    {
                        _logger.LogError("Failed to add domain {DomainName} to Front Door", domainName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing domain registration {Id}", registration?.id);
                    // Don't throw - continue processing other registrations
                }
            }

            _logger.LogInformation("DomainRegistrationTrigger completed processing {Count} registration(s)", input.Count);
        }
    }
}
