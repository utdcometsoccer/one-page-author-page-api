using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// HTTP endpoint to create and manage domain registrations.
    /// </summary>
    /// <remarks>
    /// CreateDomainRegistration:
    /// - Method: POST
    /// - Route: /api/domain-registrations
    /// - Auth: Function (requires function key)
    /// - Body: JSON with PascalCase properties matching <see cref="InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations.CreateDomainRegistrationRequest"/>
    /// - Response: 201 Created with <see cref="InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations.DomainRegistrationResponse"/>
    /// - 400 on invalid JSON or missing required fields
    /// - 401 on invalid/missing authentication
    /// </remarks>
    public class DomainRegistrationFunction
    {
        private readonly ILogger<DomainRegistrationFunction> _logger;
        private readonly IJwtValidationService _jwtValidationService;
        private readonly IUserProfileService _userProfileService;
        private readonly IDomainRegistrationService _domainRegistrationService;

        public DomainRegistrationFunction(
            ILogger<DomainRegistrationFunction> logger,
            IJwtValidationService jwtValidationService,
            IUserProfileService userProfileService,
            IDomainRegistrationService domainRegistrationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
            _domainRegistrationService = domainRegistrationService ?? throw new ArgumentNullException(nameof(domainRegistrationService));
        }

        /// <summary>
        /// Creates a new domain registration for the authenticated user.
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="payload">Domain registration request payload</param>
        /// <returns>Created domain registration or error response</returns>
        [Function("CreateDomainRegistration")]
        public async Task<IActionResult> CreateDomainRegistration(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "domain-registrations")] HttpRequest req,
            [FromBody] CreateDomainRegistrationRequest payload)
        {
            _logger.LogInformation("CreateDomainRegistration function processed a request.");

            // Validate JWT token and get authenticated user
            var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
            if (authError != null)
            {
                return authError;
            }

            try
            {
                // Ensure user profile exists
                var userProfile = await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
                _logger.LogInformation("User profile validated for user: {Upn}", userProfile.Upn);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "User profile validation failed for CreateDomainRegistration");
                return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
            }

            if (payload is null)
            {
                return new BadRequestObjectResult(new { error = "Request body is required." });
            }

            // Validate request data
            if (payload.Domain == null)
            {
                return new BadRequestObjectResult(new { error = "Domain information is required." });
            }

            if (payload.ContactInformation == null)
            {
                return new BadRequestObjectResult(new { error = "Contact information is required." });
            }

            try
            {
                // Create domain registration
                var domainRegistration = await _domainRegistrationService.CreateDomainRegistrationAsync(
                    authenticatedUser!,
                    payload.Domain.ToEntity(),
                    payload.ContactInformation.ToEntity());

                _logger.LogInformation("Domain registration created with ID: {RegistrationId}", domainRegistration.id);

                // Return created response
                var response = DomainRegistrationResponse.FromEntity(domainRegistration);
                return new CreatedResult($"/api/domain-registrations/{domainRegistration.id}", response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in domain registration request");
                return new BadRequestObjectResult(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in domain registration request");
                return new BadRequestObjectResult(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating domain registration");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Gets all domain registrations for the authenticated user.
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <returns>List of domain registrations or error response</returns>
        [Function("GetDomainRegistrations")]
        public async Task<IActionResult> GetDomainRegistrations(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "domain-registrations")] HttpRequest req)
        {
            _logger.LogInformation("GetDomainRegistrations function processed a request.");

            try
            {
                // Validate JWT token and get authenticated user
                var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
                if (authError != null)
                {
                    return authError;
                }

                // Ensure user profile exists
                var userProfile = await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
                _logger.LogInformation("User profile validated for user: {Upn}", userProfile.Upn);

                // Get user's domain registrations
                var domainRegistrations = await _domainRegistrationService.GetUserDomainRegistrationsAsync(authenticatedUser!);

                // Convert to response DTOs
                var response = domainRegistrations.Select(DomainRegistrationResponse.FromEntity).ToList();

                _logger.LogInformation("Retrieved {Count} domain registrations for user: {Upn}", 
                    response.Count, userProfile.Upn);

                return new OkObjectResult(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in get domain registrations request");
                return new BadRequestObjectResult(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving domain registrations");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Gets a specific domain registration by ID for the authenticated user.
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="registrationId">The registration ID from the route</param>
        /// <returns>Domain registration or error response</returns>
        [Function("GetDomainRegistrationById")]
        public async Task<IActionResult> GetDomainRegistrationById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "domain-registrations/{registrationId}")] HttpRequest req,
            string registrationId)
        {
            _logger.LogInformation("GetDomainRegistrationById function processed a request for ID: {RegistrationId}", registrationId);

            try
            {
                // Validate JWT token and get authenticated user
                var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
                if (authError != null)
                {
                    return authError;
                }

                // Ensure user profile exists
                var userProfile = await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
                _logger.LogInformation("User profile validated for user: {Upn}", userProfile.Upn);

                if (string.IsNullOrWhiteSpace(registrationId))
                {
                    return new BadRequestObjectResult("Registration ID is required");
                }

                // Get specific domain registration
                var domainRegistration = await _domainRegistrationService.GetDomainRegistrationByIdAsync(
                    authenticatedUser!, registrationId);

                if (domainRegistration == null)
                {
                    _logger.LogInformation("Domain registration {RegistrationId} not found for user: {Upn}", 
                        registrationId, userProfile.Upn);
                    return new NotFoundObjectResult($"Domain registration {registrationId} not found");
                }

                // Convert to response DTO
                var response = DomainRegistrationResponse.FromEntity(domainRegistration);

                _logger.LogInformation("Retrieved domain registration {RegistrationId} for user: {Upn}", 
                    registrationId, userProfile.Upn);

                return new OkObjectResult(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in get domain registration by ID request");
                return new BadRequestObjectResult(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving domain registration by ID: {RegistrationId}", registrationId);
                return new StatusCodeResult(500);
            }
        }
    }
}