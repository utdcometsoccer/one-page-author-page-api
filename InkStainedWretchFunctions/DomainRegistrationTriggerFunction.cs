using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function triggered by changes to the DomainRegistrations Cosmos DB container.
    /// Processes new domain registrations by:
    /// 1. Registering domains via WHMCS API
    /// 2. Ensuring Azure DNS zone exists and retrieving name servers
    /// 3. Updating WHMCS domain with Azure DNS name servers
    /// 4. Adding domains to Azure Front Door
    /// </summary>
    /// <remarks>
    /// This function uses a Cosmos DB trigger with a unique lease collection to allow multiple functions
    /// to trigger from the same container without conflicts.
    /// </remarks>
    public class DomainRegistrationTriggerFunction
    {
        private readonly ILogger<DomainRegistrationTriggerFunction> _logger;
        private readonly IFrontDoorService _frontDoorService;
        private readonly IWhmcsService _whmcsService;
        private readonly IDnsZoneService _dnsZoneService;

        public DomainRegistrationTriggerFunction(
            ILogger<DomainRegistrationTriggerFunction> logger,
            IFrontDoorService frontDoorService,
            IWhmcsService whmcsService,
            IDnsZoneService dnsZoneService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _frontDoorService = frontDoorService ?? throw new ArgumentNullException(nameof(frontDoorService));
            _whmcsService = whmcsService ?? throw new ArgumentNullException(nameof(whmcsService));
            _dnsZoneService = dnsZoneService ?? throw new ArgumentNullException(nameof(dnsZoneService));
        }

        /// <summary>
        /// Processes changes to domain registrations by:
        /// 1. Registering domains via WHMCS
        /// 2. Ensuring DNS zone exists and retrieving Azure DNS name servers
        /// 3. Updating WHMCS with Azure DNS name servers
        /// 4. Adding domains to Azure Front Door
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
                    bool registrationSuccess = false;
                    try
                    {
                        registrationSuccess = await _whmcsService.RegisterDomainAsync(registration);
                    }
                    catch (Exception whmcsEx)
                    {
                        _logger.LogError(whmcsEx, "Exception while registering domain {DomainName} via WHMCS API", domainName);
                        // Continue to Front Door despite WHMCS exception
                    }

                    if (!registrationSuccess)
                    {
                        _logger.LogWarning("Failed to register domain {DomainName} via WHMCS API", domainName);
                        // Continue to try Front Door addition even if WHMCS registration fails
                        // as the domain might already be registered externally
                    }
                    else
                    {
                        _logger.LogInformation("Successfully registered domain {DomainName} via WHMCS API", domainName);

                        // Step 2: Ensure DNS zone exists and retrieve name servers
                        _logger.LogInformation("Ensuring DNS zone exists for domain {DomainName}", domainName);
                        var dnsZoneCreated = await _dnsZoneService.EnsureDnsZoneExistsAsync(registration);
                        
                        if (dnsZoneCreated)
                        {
                            _logger.LogInformation("DNS zone exists for domain {DomainName}, retrieving name servers", domainName);
                            
                            // Retrieve Azure DNS name servers
                            var nameServers = await _dnsZoneService.GetNameServersAsync(domainName);
                            
                            if (nameServers != null && nameServers.Length > 0)
                            {
                                _logger.LogInformation("Retrieved {Count} name servers for domain {DomainName}, updating WHMCS", 
                                    nameServers.Length, domainName);
                                
                                // Step 3: Update WHMCS domain with Azure DNS name servers
                                bool nameServerUpdateSuccess = false;
                                try
                                {
                                    nameServerUpdateSuccess = await _whmcsService.UpdateNameServersAsync(domainName, nameServers);
                                }
                                catch (Exception nsEx)
                                {
                                    _logger.LogError(nsEx, "Exception while updating name servers for domain {DomainName} in WHMCS", domainName);
                                }
                                
                                if (nameServerUpdateSuccess)
                                {
                                    _logger.LogInformation("Successfully updated name servers for domain {DomainName} in WHMCS", domainName);
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to update name servers for domain {DomainName} in WHMCS", domainName);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("No name servers retrieved for domain {DomainName}, skipping WHMCS name server update", domainName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to ensure DNS zone exists for domain {DomainName}, skipping name server update", domainName);
                        }
                    }

                    // Step 4: Add domain to Front Door
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
