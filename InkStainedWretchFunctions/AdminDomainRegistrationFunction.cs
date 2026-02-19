using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Admin HTTP endpoints for domain registration management.
    /// These endpoints require the caller to have the "Admin" role claim in their JWT token.
    /// </summary>
    /// <remarks>
    /// AdminCompleteDomainRegistration:
    /// - Method: POST
    /// - Route: /api/admin/domain-registrations/{registrationId}/complete
    /// - Auth: Bearer JWT with "Admin" role claim
    /// - Completes a pending domain registration (WHMCS + DNS + Front Door) without Stripe payment
    /// </remarks>
    public class AdminDomainRegistrationFunction
    {
        private const string AdminRole = "Admin";

        private readonly ILogger<AdminDomainRegistrationFunction> _logger;
        private readonly IJwtValidationService _jwtValidationService;
        private readonly IDomainRegistrationRepository _domainRegistrationRepository;
        private readonly IFrontDoorService _frontDoorService;
        private readonly IWhmcsService _whmcsService;
        private readonly IDnsZoneService _dnsZoneService;

        public AdminDomainRegistrationFunction(
            ILogger<AdminDomainRegistrationFunction> logger,
            IJwtValidationService jwtValidationService,
            IDomainRegistrationRepository domainRegistrationRepository,
            IFrontDoorService frontDoorService,
            IWhmcsService whmcsService,
            IDnsZoneService dnsZoneService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
            _domainRegistrationRepository = domainRegistrationRepository ?? throw new ArgumentNullException(nameof(domainRegistrationRepository));
            _frontDoorService = frontDoorService ?? throw new ArgumentNullException(nameof(frontDoorService));
            _whmcsService = whmcsService ?? throw new ArgumentNullException(nameof(whmcsService));
            _dnsZoneService = dnsZoneService ?? throw new ArgumentNullException(nameof(dnsZoneService));
        }

        /// <summary>
        /// Completes domain creation for a partially registered author site without requiring a Stripe payment.
        /// Executes the full domain provisioning workflow: WHMCS domain registration, DNS zone setup,
        /// name server update, and Azure Front Door configuration.
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="registrationId">The domain registration ID to complete</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Domain creation workflow completed - returns updated DomainRegistrationResponse.
        /// Status is Completed when all steps succeed, InProgress when some steps failed.</description>
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/domain-registrations/{registrationId}/complete")] HttpRequest req,
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
            var isAdmin = authenticatedUser!.FindAll("roles").Any(c => c.Value == AdminRole)
                       || authenticatedUser.IsInRole(AdminRole);
            if (!isAdmin)
            {
                _logger.LogWarning("User attempted to access admin endpoint without Admin role");
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
            _logger.LogInformation("Admin completing domain creation for {DomainName} (registration {RegistrationId})", domainName, registrationId);

            // Step 1: Register domain via WHMCS API
            bool whmcsSuccess = false;
            try
            {
                whmcsSuccess = await _whmcsService.RegisterDomainAsync(registration);
                if (whmcsSuccess)
                {
                    _logger.LogInformation("Successfully registered domain {DomainName} via WHMCS API", domainName);
                }
                else
                {
                    _logger.LogWarning("WHMCS registration returned false for domain {DomainName}", domainName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while registering domain {DomainName} via WHMCS API", domainName);
            }

            // Steps 2 & 3: DNS zone creation and name server update (only if WHMCS succeeded)
            bool dnsSuccess = false;
            if (whmcsSuccess)
            {
                try
                {
                    var dnsZoneReady = await _dnsZoneService.EnsureDnsZoneExistsAsync(registration);
                    if (dnsZoneReady)
                    {
                        _logger.LogInformation("DNS zone ready for domain {DomainName}, retrieving name servers", domainName);
                        var nameServers = await _dnsZoneService.GetNameServersAsync(domainName);

                        if (nameServers != null && nameServers.Length >= 2 && nameServers.Length <= 5)
                        {
                            bool nsUpdated = await _whmcsService.UpdateNameServersAsync(domainName, nameServers);
                            if (nsUpdated)
                            {
                                _logger.LogInformation("Successfully updated name servers for domain {DomainName}", domainName);
                                dnsSuccess = true;
                            }
                            else
                            {
                                _logger.LogWarning("Failed to update name servers for domain {DomainName} in WHMCS", domainName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Could not retrieve valid name servers for domain {DomainName}, skipping name server update", domainName);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to ensure DNS zone for domain {DomainName}", domainName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during DNS zone setup for domain {DomainName}", domainName);
                }
            }

            // Step 4: Add domain to Azure Front Door
            bool frontDoorSuccess = false;
            try
            {
                frontDoorSuccess = await _frontDoorService.AddDomainToFrontDoorAsync(registration);
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

            // Update status: Completed when all steps succeed, InProgress when partial
            registration.Status = (whmcsSuccess && dnsSuccess && frontDoorSuccess)
                ? DomainRegistrationStatus.Completed
                : DomainRegistrationStatus.InProgress;
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
