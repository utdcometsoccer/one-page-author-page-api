using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function triggered by changes to the DomainRegistrations Cosmos DB container.
    /// Processes new domain registrations by registering them via WHMCS API and adding them to Azure Front Door.
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
        private readonly IWhmcsService _whmcsService;

        public DomainRegistrationTriggerFunction(
            ILogger<DomainRegistrationTriggerFunction> logger,
            IFrontDoorService frontDoorService,
            IDomainRegistrationService domainRegistrationService,
            IWhmcsService whmcsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _frontDoorService = frontDoorService ?? throw new ArgumentNullException(nameof(frontDoorService));
            _domainRegistrationService = domainRegistrationService ?? throw new ArgumentNullException(nameof(domainRegistrationService));
            _whmcsService = whmcsService ?? throw new ArgumentNullException(nameof(whmcsService));
        }

        /// <summary>
        /// Processes changes to domain registrations by registering domains via WHMCS and adding them to Azure Front Door.
        /// </summary>
        /// <param name="input">List of changed domain registrations from Cosmos DB</param>
        [Function("DomainRegistrationTrigger")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "%COSMOSDB_DATABASE_ID%",
                containerName: "DomainRegistrations",
                Connection = "COSMOSDB_CONNECTION_STRING",
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

                    // Step 1: Register domain via WHMCS API
                    _logger.LogInformation("Registering domain {DomainName} via WHMCS API", domainName);
                    var registrationSuccess = await _whmcsService.RegisterDomainAsync(registration);

                    if (!registrationSuccess)
                    {
                        _logger.LogError("Failed to register domain {DomainName} via WHMCS API", domainName);
                        // Continue to try Front Door addition even if WHMCS registration fails
                        // as the domain might already be registered externally
                    }
                    else
                    {
                        _logger.LogInformation("Successfully registered domain {DomainName} via WHMCS API", domainName);
                    }

                    // Step 2: Add domain to Front Door
                    var frontDoorSuccess = await _frontDoorService.AddDomainToFrontDoorAsync(registration);

                    if (frontDoorSuccess)
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
