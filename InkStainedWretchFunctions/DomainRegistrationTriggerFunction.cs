using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function triggered by changes to the DomainRegistrations Cosmos DB container.
    /// Processes new domain registrations by:
    /// 1. Ensuring Azure DNS zone exists and retrieving name servers
    /// 2. Enqueuing a WHMCS registration message to the Service Bus queue
    ///    (the VM-hosted worker service will call the WHMCS REST API from a static IP)
    /// 3. Adding domains to Azure Front Door
    /// </summary>
    /// <remarks>
    /// This function uses a Cosmos DB trigger with a unique lease collection to allow multiple functions
    /// to trigger from the same container without conflicts.
    /// WHMCS calls are intentionally delegated to the Service Bus queue so that they originate
    /// from the VM's static IP address, which WHMCS can allowlist.
    /// </remarks>
    public class DomainRegistrationTriggerFunction
    {
        private const int MinNameServersForWhmcs = 2;
        private const int MaxNameServersForWhmcs = 5;
        private readonly ILogger<DomainRegistrationTriggerFunction> _logger;
        private readonly IFrontDoorService _frontDoorService;
        private readonly IWhmcsQueueService _whmcsQueueService;
        private readonly IDnsZoneService _dnsZoneService;

        public DomainRegistrationTriggerFunction(
            ILogger<DomainRegistrationTriggerFunction> logger,
            IFrontDoorService frontDoorService,
            IWhmcsQueueService whmcsQueueService,
            IDnsZoneService dnsZoneService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _frontDoorService = frontDoorService ?? throw new ArgumentNullException(nameof(frontDoorService));
            _whmcsQueueService = whmcsQueueService ?? throw new ArgumentNullException(nameof(whmcsQueueService));
            _dnsZoneService = dnsZoneService ?? throw new ArgumentNullException(nameof(dnsZoneService));
        }

        /// <summary>
        /// Processes changes to domain registrations by:
        /// 1. Ensuring DNS zone exists and retrieving Azure DNS name servers
        /// 2. Enqueuing a WHMCS registration message (domain + name servers) to Service Bus
        /// 3. Adding domains to Azure Front Door
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

                    // Step 1: Ensure DNS zone exists and retrieve Azure DNS name servers.
                    // This runs in the function app (no static IP needed for Azure DNS management).
                    string[] nameServers = [];
                    _logger.LogInformation("Ensuring DNS zone exists for domain {DomainName}", domainName);
                    var dnsZoneReady = await _dnsZoneService.EnsureDnsZoneExistsAsync(registration);

                    if (dnsZoneReady)
                    {
                        _logger.LogInformation("DNS zone exists for domain {DomainName}, retrieving name servers", domainName);
                        nameServers = await _dnsZoneService.GetNameServersAsync(domainName) ?? [];

                        if (nameServers.Length >= MinNameServersForWhmcs && nameServers.Length <= MaxNameServersForWhmcs)
                        {
                            _logger.LogInformation("Retrieved {Count} name servers for domain {DomainName}", 
                                nameServers.Length, domainName);
                        }
                        else if (nameServers.Length == 0)
                        {
                            _logger.LogWarning(
                                "No name servers retrieved for domain {DomainName}; " +
                                "name server update will be skipped by the worker.",
                                domainName);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Retrieved {Count} name server(s) for domain {DomainName}; " +
                                "WHMCS requires {Min}–{Max}. Name server update will be skipped by the worker.",
                                nameServers.Length, domainName, MinNameServersForWhmcs, MaxNameServersForWhmcs);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to ensure DNS zone exists for domain {DomainName}; enqueueing without name servers", domainName);
                    }

                    // Step 2: Enqueue the registration to the Service Bus queue.
                    // The VM-hosted worker service will dequeue this message and call the
                    // WHMCS REST API from a static IP address (required by WHMCS allowlisting).
                    _logger.LogInformation("Enqueueing WHMCS registration for domain {DomainName}", domainName);
                    try
                    {
                        await _whmcsQueueService.EnqueueDomainRegistrationAsync(registration, nameServers);
                        _logger.LogInformation("Successfully enqueued WHMCS registration for domain {DomainName}", domainName);
                    }
                    catch (Exception queueEx)
                    {
                        _logger.LogError(queueEx, "Failed to enqueue WHMCS registration for domain {DomainName}", domainName);
                        // Continue to Front Door addition even if enqueue fails
                    }

                    // Step 3: Add domain to Front Door.
                    // This also uses Azure SDK calls (no static IP needed).
                    var frontDoorSuccess = await _frontDoorService.AddDomainToFrontDoorAsync(registration);

                    if (frontDoorSuccess)
                    {
                        _logger.LogInformation("Successfully processed domain {DomainName} for Front Door", domainName);
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
