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
        /// <param name="req">HTTP request containing the domain registration data</param>
        /// <param name="payload">Domain registration request payload with domain details</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>201 Created</term>
        /// <description>Domain registration created successfully - returns DomainRegistrationResponse</description>
        /// </item>
        /// <item>
        /// <term>400 Bad Request</term>
        /// <description>Invalid request body or missing required fields</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>409 Conflict</term>
        /// <description>Domain already registered</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Unexpected server error during registration</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <example>
        /// <para><strong>TypeScript Example:</strong></para>
        /// <code>
        /// interface CreateDomainRequest {
        ///   DomainName: string;           // e.g., "example.com"
        ///   RegistrationPeriodYears: number; // 1-10 years
        ///   AutoRenew: boolean;           // true for auto-renewal
        ///   PrivacyProtection: boolean;   // true for privacy protection
        ///   ContactInfo: {
        ///     FirstName: string;
        ///     LastName: string;
        ///     Email: string;
        ///     Phone: string;
        ///     Address: {
        ///       Street: string;
        ///       City: string;
        ///       State: string;
        ///       PostalCode: string;
        ///       Country: string;
        ///     };
        ///   };
        /// }
        /// 
        /// const registerDomain = async (domain: CreateDomainRequest, token: string) => {
        ///   const response = await fetch('/api/domain-registrations', {
        ///     method: 'POST',
        ///     headers: {
        ///       'Authorization': `Bearer ${token}`,
        ///       'Content-Type': 'application/json'
        ///     },
        ///     body: JSON.stringify(domain)
        ///   });
        /// 
        ///   if (response.ok) {
        ///     return await response.json();
        ///   } else if (response.status === 409) {
        ///     throw new Error('Domain is already registered');
        ///   } else if (response.status === 400) {
        ///     const error = await response.json();
        ///     throw new Error(`Invalid request: ${error.error}`);
        ///   }
        /// 
        ///   throw new Error('Domain registration failed');
        /// };
        /// 
        /// // Usage
        /// try {
        ///   const result = await registerDomain({
        ///     DomainName: "myblog.com",
        ///     RegistrationPeriodYears: 2,
        ///     AutoRenew: true,
        ///     PrivacyProtection: true,
        ///     ContactInfo: {
        ///       FirstName: "John",
        ///       LastName: "Doe",
        ///       Email: "john@example.com",
        ///       Phone: "+1234567890",
        ///       Address: {
        ///         Street: "123 Main St",
        ///         City: "Anytown",
        ///         State: "CA",
        ///         PostalCode: "12345",
        ///         Country: "US"
        ///       }
        ///     }
        ///   }, userToken);
        ///   console.log('Domain registered:', result);
        /// } catch (error) {
        ///   console.error('Registration failed:', error.message);
        /// }
        /// </code>
        /// </example>
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
        /// <param name="req">HTTP request (no additional parameters required)</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Array of user's domain registrations with full details</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Unexpected server error during retrieval</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <example>
        /// <para><strong>TypeScript Example:</strong></para>
        /// <code>
        /// interface DomainRegistration {
        ///   Id: string;
        ///   DomainName: string;
        ///   RegistrationDate: string;     // ISO 8601 datetime
        ///   ExpirationDate: string;       // ISO 8601 datetime
        ///   AutoRenew: boolean;
        ///   PrivacyProtection: boolean;
        ///   Status: string;               // "Active", "Pending", "Expired", etc.
        ///   NameServers: string[];
        ///   ContactInfo: {
        ///     FirstName: string;
        ///     LastName: string;
        ///     Email: string;
        ///     Phone: string;
        ///   };
        /// }
        /// 
        /// const fetchUserDomains = async (token: string): Promise&lt;DomainRegistration[]&gt; => {
        ///   const response = await fetch('/api/domain-registrations', {
        ///     method: 'GET',
        ///     headers: {
        ///       'Authorization': `Bearer ${token}`,
        ///       'Content-Type': 'application/json'
        ///     }
        ///   });
        /// 
        ///   if (response.ok) {
        ///     return await response.json();
        ///   } else if (response.status === 401) {
        ///     throw new Error('Unauthorized - invalid or missing token');
        ///   }
        /// 
        ///   throw new Error('Failed to fetch domain registrations');
        /// };
        /// 
        /// // Usage with React/TypeScript
        /// const DomainList: React.FC = () => {
        ///   const [domains, setDomains] = useState&lt;DomainRegistration[]&gt;([]);
        ///   const [loading, setLoading] = useState(true);
        /// 
        ///   useEffect(() => {
        ///     const loadDomains = async () => {
        ///       try {
        ///         const userDomains = await fetchUserDomains(userToken);
        ///         setDomains(userDomains);
        ///       } catch (error) {
        ///         console.error('Failed to load domains:', error);
        ///       } finally {
        ///         setLoading(false);
        ///       }
        ///     };
        /// 
        ///     loadDomains();
        ///   }, [userToken]);
        /// 
        ///   if (loading) return &lt;div&gt;Loading domains...&lt;/div&gt;;
        /// 
        ///   return (
        ///     &lt;div&gt;
        ///       &lt;h2&gt;My Domains ({domains.length})&lt;/h2&gt;
        ///       {domains.map(domain =&gt; (
        ///         &lt;div key={domain.Id} className="domain-card"&gt;
        ///           &lt;h3&gt;{domain.DomainName}&lt;/h3&gt;
        ///           &lt;p&gt;Status: {domain.Status}&lt;/p&gt;
        ///           &lt;p&gt;Expires: {new Date(domain.ExpirationDate).toLocaleDateString()}&lt;/p&gt;
        ///           &lt;p&gt;Auto-renew: {domain.AutoRenew ? 'Yes' : 'No'}&lt;/p&gt;
        ///         &lt;/div&gt;
        ///       ))}
        ///     &lt;/div&gt;
        ///   );
        /// };
        /// </code>
        /// </example>
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

        /// <summary>
        /// Updates an existing domain registration for the authenticated user.
        /// Requires an active subscription to perform updates.
        /// </summary>
        /// <param name="req">HTTP request containing the update data</param>
        /// <param name="registrationId">The registration ID from the route</param>
        /// <param name="payload">Domain registration update payload with optional fields</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Domain registration updated successfully - returns DomainRegistrationResponse</description>
        /// </item>
        /// <item>
        /// <term>400 Bad Request</term>
        /// <description>Invalid request body, validation failed, or missing registration ID</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>403 Forbidden</term>
        /// <description>User does not have an active subscription</description>
        /// </item>
        /// <item>
        /// <term>404 Not Found</term>
        /// <description>Domain registration not found or doesn't belong to user</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Unexpected server error during update</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <example>
        /// <para><strong>TypeScript Example:</strong></para>
        /// <code>
        /// interface UpdateDomainRequest {
        ///   Domain?: {                        // Optional - only updated if provided
        ///     TopLevelDomain: string;
        ///     SecondLevelDomain: string;
        ///   };
        ///   ContactInformation?: {            // Optional - only updated if provided
        ///     FirstName: string;
        ///     LastName: string;
        ///     Email: string;
        ///     Phone: string;
        ///     Address: {
        ///       Street: string;
        ///       City: string;
        ///       State: string;
        ///       PostalCode: string;
        ///       Country: string;
        ///     };
        ///   };
        ///   Status?: string;                  // Optional - only updated if provided
        /// }
        /// 
        /// const updateDomain = async (
        ///   registrationId: string,
        ///   updates: UpdateDomainRequest,
        ///   token: string
        /// ) => {
        ///   const response = await fetch(`/api/domain-registrations/${registrationId}`, {
        ///     method: 'PUT',
        ///     headers: {
        ///       'Authorization': `Bearer ${token}`,
        ///       'Content-Type': 'application/json'
        ///     },
        ///     body: JSON.stringify(updates)
        ///   });
        /// 
        ///   if (response.ok) {
        ///     return await response.json();
        ///   } else if (response.status === 403) {
        ///     throw new Error('Active subscription required to update domain');
        ///   } else if (response.status === 404) {
        ///     throw new Error('Domain registration not found');
        ///   } else if (response.status === 400) {
        ///     const error = await response.json();
        ///     throw new Error(`Invalid request: ${error.error}`);
        ///   }
        /// 
        ///   throw new Error('Domain update failed');
        /// };
        /// 
        /// // Usage - update contact information only
        /// try {
        ///   const result = await updateDomain(
        ///     'registration-123',
        ///     {
        ///       ContactInformation: {
        ///         FirstName: "Jane",
        ///         LastName: "Smith",
        ///         Email: "jane@example.com",
        ///         Phone: "+1234567890",
        ///         Address: {
        ///           Street: "456 Oak Ave",
        ///           City: "Newtown",
        ///           State: "NY",
        ///           PostalCode: "54321",
        ///           Country: "US"
        ///         }
        ///       }
        ///     },
        ///     userToken
        ///   );
        ///   console.log('Domain updated:', result);
        /// } catch (error) {
        ///   console.error('Update failed:', error.message);
        /// }
        /// </code>
        /// </example>
        [Function("UpdateDomainRegistration")]
        public async Task<IActionResult> UpdateDomainRegistration(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "domain-registrations/{registrationId}")] HttpRequest req,
            string registrationId,
            [FromBody] UpdateDomainRegistrationRequest payload)
        {
            _logger.LogInformation("UpdateDomainRegistration function processed a request for ID: {RegistrationId}", registrationId);

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
                _logger.LogWarning(ex, "User profile validation failed for UpdateDomainRegistration");
                return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
            }

            if (string.IsNullOrWhiteSpace(registrationId))
            {
                return new BadRequestObjectResult(new { error = "Registration ID is required" });
            }

            if (payload is null)
            {
                return new BadRequestObjectResult(new { error = "Request body is required." });
            }

            // Validate that at least one field is provided for update
            if (payload.Domain == null && payload.ContactInformation == null && payload.Status == null)
            {
                return new BadRequestObjectResult(new { error = "At least one field must be provided for update (Domain, ContactInformation, or Status)." });
            }

            try
            {
                // Update domain registration with subscription validation
                var updatedRegistration = await _domainRegistrationService.UpdateDomainRegistrationAsync(
                    authenticatedUser!,
                    registrationId,
                    payload.Domain?.ToEntity(),
                    payload.ContactInformation?.ToEntity(),
                    payload.Status);

                if (updatedRegistration == null)
                {
                    _logger.LogInformation("Domain registration {RegistrationId} not found for user", registrationId);
                    return new NotFoundObjectResult(new { error = $"Domain registration {registrationId} not found" });
                }

                _logger.LogInformation("Domain registration {RegistrationId} updated successfully", registrationId);

                // Return updated response
                var response = DomainRegistrationResponse.FromEntity(updatedRegistration);
                return new OkObjectResult(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("subscription"))
            {
                _logger.LogWarning(ex, "Subscription validation failed for user updating domain registration {RegistrationId}", registrationId);
                return new ObjectResult(new { error = ex.Message })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in domain registration update request");
                return new BadRequestObjectResult(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in domain registration update request");
                return new BadRequestObjectResult(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating domain registration {RegistrationId}", registrationId);
                return new StatusCodeResult(500);
            }
        }
    }
}