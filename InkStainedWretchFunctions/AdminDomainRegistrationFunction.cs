using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;
using System.Text.Json;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Admin HTTP endpoints for domain registration management.
    /// These endpoints require the caller to have the "Admin" role claim in their JWT token.
    /// </summary>
    /// <remarks>
    /// AdminGetIncompleteDomainRegistrations:
    /// - Method: GET
    /// - Route: /api/management/domain-registrations
    /// - Auth: Bearer JWT with "Admin" role claim
    /// - Returns all incomplete domain registrations (Pending, InProgress, Failed) regardless of UPN
    ///
    /// AdminGetAllDomainRegistrationsPaged:
    /// - Method: GET
    /// - Route: /api/management/domain-registrations/all
    /// - Auth: Bearer JWT with "Admin" role claim
    /// - Returns a page of all domain registrations across all statuses
    ///
    /// AdminUpdateDomainRegistrationStatus:
    /// - Method: PATCH
    /// - Route: /api/management/domain-registrations/{registrationId}/status
    /// - Auth: Bearer JWT with "Admin" role claim
    /// - Changes the status of the specified domain registration
    ///
    /// AdminCompleteDomainRegistration:
    /// - Method: POST
    /// - Route: /api/management/domain-registrations/{registrationId}/complete
    /// - Auth: Bearer JWT with "Admin" role claim
    /// - Enqueues the domain registration to the WHMCS Service Bus queue, ensures the DNS zone
    ///   exists, and adds the domain to Azure Front Door. The VM-hosted WHMCS worker service
    ///   dequeues the message and calls the WHMCS REST API from a static IP address.
    /// </remarks>
    public class AdminDomainRegistrationFunction
    {
        private const string AdminRole = "Admin";
        private const int MinNameServersForWhmcs = 2;
        private const int MaxNameServersForWhmcs = 5;

        private readonly ILogger<AdminDomainRegistrationFunction> _logger;
        private readonly IJwtValidationService _jwtValidationService;
        private readonly IRoleChecker _roleChecker;
        private readonly IDomainRegistrationRepository _domainRegistrationRepository;
        private readonly IFrontDoorService _frontDoorService;
        private readonly IWhmcsQueueService _whmcsQueueService;
        private readonly IDnsZoneService _dnsZoneService;

        public AdminDomainRegistrationFunction(
            ILogger<AdminDomainRegistrationFunction> logger,
            IJwtValidationService jwtValidationService,
            IRoleChecker roleChecker,
            IDomainRegistrationRepository domainRegistrationRepository,
            IFrontDoorService frontDoorService,
            IWhmcsQueueService whmcsQueueService,
            IDnsZoneService dnsZoneService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
            _roleChecker = roleChecker ?? throw new ArgumentNullException(nameof(roleChecker));
            _domainRegistrationRepository = domainRegistrationRepository ?? throw new ArgumentNullException(nameof(domainRegistrationRepository));
            _frontDoorService = frontDoorService ?? throw new ArgumentNullException(nameof(frontDoorService));
            _whmcsQueueService = whmcsQueueService ?? throw new ArgumentNullException(nameof(whmcsQueueService));
            _dnsZoneService = dnsZoneService ?? throw new ArgumentNullException(nameof(dnsZoneService));
        }

        /// <summary>
        /// Gets all incomplete domain registrations across all users.
        /// Returns registrations with a status of Pending, InProgress, or Failed, regardless of the owner's UPN.
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Returns a list of incomplete domain registration responses (may be empty).</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>403 Forbidden</term>
        /// <description>Caller does not have the Admin role</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Unexpected error during processing</description>
        /// </item>
        /// </list>
        /// </returns>
        [Function("AdminGetIncompleteDomainRegistrations")]
        public async Task<IActionResult> AdminGetIncompleteDomainRegistrations(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/domain-registrations")] HttpRequest req)
        {
            _logger.LogInformation("AdminGetIncompleteDomainRegistrations function processed a request");

            // Validate JWT token and get authenticated user
            var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
            if (authError != null)
            {
                return authError;
            }

            // Check admin role claim
            if (!_roleChecker.HasRole(authenticatedUser!, AdminRole))
            {
                _logger.LogWarning(
                    "User attempted to access admin endpoint without Admin role. Claims: {Claims}",
                    JwtAuthenticationHelper.GetNonPiiClaimsForLogging(authenticatedUser!));
                return new ObjectResult(new { error = "Admin role required" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            try
            {
                int? maxResults = null;
                if (req.Query.TryGetValue("maxResults", out var maxResultsStr)
                    && int.TryParse(maxResultsStr, out var parsedMax)
                    && parsedMax > 0)
                {
                    maxResults = parsedMax;
                }

                var registrations = await _domainRegistrationRepository.GetAllIncompleteAsync(maxResults);

                var validRegistrations = registrations
                    .Where(r => r.Domain != null && r.ContactInformation != null)
                    .ToList();

                var skippedCount = registrations.Count() - validRegistrations.Count;
                if (skippedCount > 0)
                {
                    _logger.LogWarning(
                        "Skipped {SkippedCount} incomplete domain registrations due to missing Domain or ContactInformation",
                        skippedCount);
                }

                var response = validRegistrations
                    .Select(r =>
                    {
                        var dto = DomainRegistrationResponse.FromEntity(r);
                        dto.ContactInformation = null;
                        return dto;
                    })
                    .ToList();

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incomplete domain registrations");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Gets a paged list of all domain registrations across all users and all statuses.
        /// </summary>
        /// <param name="req">HTTP request. Accepts optional <c>page</c> (default 1) and <c>pageSize</c> (default 20) query parameters.</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Returns a paged list of domain registration responses (may be empty).</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>403 Forbidden</term>
        /// <description>Caller does not have the Admin role</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Unexpected error during processing</description>
        /// </item>
        /// </list>
        /// </returns>
        [Function("AdminGetAllDomainRegistrationsPaged")]
        public async Task<IActionResult> AdminGetAllDomainRegistrationsPaged(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/domain-registrations/all")] HttpRequest req)
        {
            _logger.LogInformation("AdminGetAllDomainRegistrationsPaged function processed a request");

            var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
            if (authError != null)
            {
                return authError;
            }

            if (!_roleChecker.HasRole(authenticatedUser!, AdminRole))
            {
                _logger.LogWarning(
                    "User attempted to access admin endpoint without Admin role. Claims: {Claims}",
                    JwtAuthenticationHelper.GetNonPiiClaimsForLogging(authenticatedUser!));
                return new ObjectResult(new { error = "Admin role required" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            int page = 1;
            int pageSize = 20;

            if (req.Query.TryGetValue("page", out var pageStr)
                && int.TryParse(pageStr, out var parsedPage)
                && parsedPage > 0)
            {
                page = parsedPage;
            }

            if (req.Query.TryGetValue("pageSize", out var pageSizeStr)
                && int.TryParse(pageSizeStr, out var parsedPageSize)
                && parsedPageSize > 0)
            {
                pageSize = parsedPageSize;
            }

            try
            {
                var registrations = (await _domainRegistrationRepository.GetAllPagedAsync(page, pageSize)).ToList();

                var validRegistrations = registrations
                    .Where(r => r.Domain != null && r.ContactInformation != null)
                    .ToList();

                var skippedCount = registrations.Count - validRegistrations.Count;
                if (skippedCount > 0)
                {
                    _logger.LogWarning(
                        "Skipped {SkippedCount} domain registrations due to missing Domain or ContactInformation",
                        skippedCount);
                }

                var response = validRegistrations
                    .Select(r =>
                    {
                        var dto = DomainRegistrationResponse.FromEntity(r);
                        dto.ContactInformation = null;
                        return dto;
                    })
                    .ToList();

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged domain registrations");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Changes the status of a domain registration.
        /// </summary>
        /// <param name="req">HTTP request containing a JSON body with the new <see cref="DomainRegistrationStatus"/>.</param>
        /// <param name="registrationId">The domain registration ID whose status should be updated.</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Returns the updated DomainRegistrationResponse.</description>
        /// </item>
        /// <item>
        /// <term>400 Bad Request</term>
        /// <description>Missing or invalid registration ID, or missing/invalid request body</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>403 Forbidden</term>
        /// <description>Caller does not have the Admin role</description>
        /// </item>
        /// <item>
        /// <term>404 Not Found</term>
        /// <description>Domain registration not found</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Unexpected error during processing</description>
        /// </item>
        /// </list>
        /// </returns>
        [Function("AdminUpdateDomainRegistrationStatus")]
        public async Task<IActionResult> AdminUpdateDomainRegistrationStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "management/domain-registrations/{registrationId}/status")] HttpRequest req,
            string registrationId)
        {
            _logger.LogInformation("AdminUpdateDomainRegistrationStatus function processed a request for ID: {RegistrationId}", registrationId);

            var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
            if (authError != null)
            {
                return authError;
            }

            if (!_roleChecker.HasRole(authenticatedUser!, AdminRole))
            {
                _logger.LogWarning(
                    "User attempted to access admin endpoint without Admin role. Claims: {Claims}",
                    JwtAuthenticationHelper.GetNonPiiClaimsForLogging(authenticatedUser!));
                return new ObjectResult(new { error = "Admin role required" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            if (string.IsNullOrWhiteSpace(registrationId))
            {
                return new BadRequestObjectResult(new { error = "Registration ID is required" });
            }

            AdminUpdateStatusRequest? statusRequest;
            try
            {
                statusRequest = await JsonSerializer.DeserializeAsync<AdminUpdateStatusRequest>(
                    req.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize status update request body");
                return new BadRequestObjectResult(new { error = "Invalid request body" });
            }

            if (statusRequest == null)
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            if (!Enum.IsDefined(typeof(DomainRegistrationStatus), statusRequest.Status))
            {
                return new BadRequestObjectResult(new { error = $"Invalid status value: {statusRequest.Status}" });
            }

            DomainRegistration? registration;
            try
            {
                registration = await _domainRegistrationRepository.GetByIdCrossPartitionAsync(registrationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving domain registration {RegistrationId}", registrationId);
                return new StatusCodeResult(500);
            }

            if (registration == null)
            {
                _logger.LogWarning("Domain registration {RegistrationId} not found", registrationId);
                return new NotFoundObjectResult(new { error = $"Domain registration {registrationId} not found" });
            }

            var previousStatus = registration.Status;
            registration.Status = statusRequest.Status;
            registration.LastUpdatedAt = DateTime.UtcNow;

            try
            {
                var updated = await _domainRegistrationRepository.UpdateAsync(registration);
                _logger.LogInformation(
                    "Admin updated domain registration {RegistrationId} status from {PreviousStatus} to {NewStatus}",
                    registrationId, previousStatus, updated.Status);
                return new OkObjectResult(DomainRegistrationResponse.FromEntity(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating domain registration {RegistrationId} status", registrationId);
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Dispatches the domain provisioning workflow for a partially registered author site without
        /// requiring a Stripe payment. Ensures the DNS zone exists, enqueues the WHMCS domain
        /// registration to the Service Bus queue (the VM worker calls the WHMCS API from a static IP),
        /// and adds the domain to Azure Front Door. The registration status is set to InProgress
        /// because the WHMCS step is processed asynchronously by the worker service.
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="registrationId">The domain registration ID to complete</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Domain provisioning workflow dispatched - returns updated DomainRegistrationResponse.
        /// Status is InProgress because WHMCS registration is processed asynchronously by the worker service.</description>
        /// </item>
        /// <item>
        /// <term>400 Bad Request</term>
        /// <description>Missing registration ID or missing domain information on the record</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>403 Forbidden</term>
        /// <description>Caller does not have the Admin role</description>
        /// </item>
        /// <item>
        /// <term>404 Not Found</term>
        /// <description>Domain registration not found</description>
        /// </item>
        /// <item>
        /// <term>409 Conflict</term>
        /// <description>Domain registration is already Completed or Cancelled</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Unexpected error during processing</description>
        /// </item>
        /// </list>
        /// </returns>
        [Function("AdminCompleteDomainRegistration")]
        public async Task<IActionResult> AdminCompleteDomainRegistration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "management/domain-registrations/{registrationId}/complete")] HttpRequest req,
            string registrationId)
        {
            _logger.LogInformation("AdminCompleteDomainRegistration function processed a request for ID: {RegistrationId}", registrationId);

            // Validate JWT token and get authenticated user
            var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
            if (authError != null)
            {
                return authError;
            }

            // Check admin role claim
            if (!_roleChecker.HasRole(authenticatedUser!, AdminRole))
            {
                _logger.LogWarning(
                    "User attempted to access admin endpoint without Admin role. Claims: {Claims}",
                    JwtAuthenticationHelper.GetNonPiiClaimsForLogging(authenticatedUser!));
                return new ObjectResult(new { error = "Admin role required" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            if (string.IsNullOrWhiteSpace(registrationId))
            {
                return new BadRequestObjectResult(new { error = "Registration ID is required" });
            }

            // Get domain registration (cross-partition since admin may not know the owner's UPN)
            DomainRegistration? registration;
            try
            {
                registration = await _domainRegistrationRepository.GetByIdCrossPartitionAsync(registrationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving domain registration {RegistrationId}", registrationId);
                return new StatusCodeResult(500);
            }

            if (registration == null)
            {
                _logger.LogWarning("Domain registration {RegistrationId} not found", registrationId);
                return new NotFoundObjectResult(new { error = $"Domain registration {registrationId} not found" });
            }

            // Guard: do not re-provision registrations that are already in a terminal status
            if (registration.Status == DomainRegistrationStatus.Completed ||
                registration.Status == DomainRegistrationStatus.Cancelled)
            {
                _logger.LogWarning(
                    "Attempt to complete domain registration {RegistrationId} which is already in terminal status {Status}",
                    registrationId,
                    registration.Status);
                return new ConflictObjectResult(new
                {
                    error = $"Domain registration {registrationId} is already {registration.Status} and cannot be completed again."
                });
            }

            if (registration.Domain == null)
            {
                _logger.LogWarning("Domain registration {RegistrationId} is missing domain information", registrationId);
                return new BadRequestObjectResult(new { error = $"Domain information is missing or invalid for registration {registrationId}" });
            }

            var domainName = registration.Domain.FullDomainName;
            _logger.LogInformation("Admin dispatching domain provisioning for {DomainName} (registration {RegistrationId})", domainName, registrationId);

            // Step 1: Ensure DNS zone exists and retrieve Azure DNS name servers.
            // This runs in the function app (no static IP needed for Azure DNS management).
            string[] nameServers = [];
            try
            {
                var dnsZoneReady = await _dnsZoneService.EnsureDnsZoneExistsAsync(registration);
                if (dnsZoneReady)
                {
                    _logger.LogInformation("DNS zone ready for domain {DomainName}, retrieving name servers", domainName);
                    nameServers = await _dnsZoneService.GetNameServersAsync(domainName) ?? [];

                    if (nameServers.Length >= MinNameServersForWhmcs && nameServers.Length <= MaxNameServersForWhmcs)
                    {
                        _logger.LogInformation("Retrieved {Count} name servers for domain {DomainName}", nameServers.Length, domainName);
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
                    _logger.LogWarning("Failed to ensure DNS zone for domain {DomainName}; enqueueing without name servers", domainName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DNS zone setup for domain {DomainName}", domainName);
            }

            // Step 2: Enqueue domain registration to the WHMCS Service Bus queue.
            // The VM-hosted worker service will dequeue this message and call the
            // WHMCS REST API from a static IP address (required by WHMCS allowlisting).
            bool enqueueSucceeded = false;
            try
            {
                await _whmcsQueueService.EnqueueDomainRegistrationAsync(registration, nameServers);
                enqueueSucceeded = true;
                _logger.LogInformation("WHMCS registration enqueued for domain {DomainName}", domainName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue WHMCS registration for domain {DomainName}; domain will remain InProgress until retried", domainName);
            }

            // Step 3: Add domain to Azure Front Door.
            // This uses Azure SDK calls (no static IP needed).
            try
            {
                var frontDoorSuccess = await _frontDoorService.AddDomainToFrontDoorAsync(registration);
                if (frontDoorSuccess)
                {
                    _logger.LogInformation("Successfully added domain {DomainName} to Azure Front Door", domainName);
                }
                else
                {
                    _logger.LogError("Failed to add domain {DomainName} to Azure Front Door", domainName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while adding domain {DomainName} to Azure Front Door", domainName);
            }

            // Update status to InProgress: the WHMCS step is processed asynchronously by the
            // VM worker service, so Completed cannot be confirmed here. The status reflects that
            // all synchronous steps have been attempted; log a warning when the enqueue did not
            // succeed so operators know the registration needs to be retried.
            if (!enqueueSucceeded)
            {
                _logger.LogWarning(
                    "WHMCS registration was NOT enqueued for domain {DomainName} (registration {RegistrationId}). " +
                    "Status is set to InProgress so the admin endpoint can be called again to retry.",
                    domainName, registrationId);
            }

            registration.Status = DomainRegistrationStatus.InProgress;
            registration.LastUpdatedAt = DateTime.UtcNow;

            try
            {
                var updated = await _domainRegistrationRepository.UpdateAsync(registration);
                _logger.LogInformation("Domain registration {RegistrationId} status updated to {Status}", registrationId, updated.Status);
                return new OkObjectResult(DomainRegistrationResponse.FromEntity(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating domain registration {RegistrationId} after completion workflow", registrationId);
                return new StatusCodeResult(500);
            }
        }
    }
}
