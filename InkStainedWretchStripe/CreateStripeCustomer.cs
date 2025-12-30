using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorLib.Extensions;

namespace InkStainedWretchStripe;

/// <summary>
/// Azure Function for creating a Stripe customer.
/// Handles HTTP POST requests, validates the incoming payload, and delegates customer creation logic.
/// </summary>
/// <remarks>
/// Expects a JSON body containing customer details. Validates the presence and format of required fields.
/// Returns appropriate HTTP responses for validation errors or successful customer creation.
/// </remarks>
public class CreateStripeCustomer
{
    private readonly ILogger<CreateStripeCustomer> _logger;
    private readonly IEnsureCustomerForUser _ensureCustomerForUser;
    private readonly IJwtValidationService _jwtValidationService;

    /// <summary>
    /// Initializes a new instance of the CreateStripeCustomer class.
    /// </summary>
    /// <param name="logger">Logger instance for logging function activity.</param>
    /// <param name="ensureCustomerForUser">Service responsible for executing Stripe customer creation logic.</param>
    /// <param name="jwtValidationService">Service for JWT token validation.</param>
    public CreateStripeCustomer(ILogger<CreateStripeCustomer> logger, IEnsureCustomerForUser ensureCustomerForUser, IJwtValidationService jwtValidationService)
    {
        _logger = logger;
        _ensureCustomerForUser = ensureCustomerForUser;
        _jwtValidationService = jwtValidationService;
    }

    [Function("CreateStripeCustomer")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        [FromBody] CreateCustomerRequest payload)
    {
        _logger.LogInformation("CreateStripeCustomer invoked.");

        // Validate JWT token and get authenticated user
        var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (authError != null)
        {
            return authError;
        }

        if (payload is null)
        {
            return ErrorResponseExtensions.CreateErrorResult(
                StatusCodes.Status400BadRequest,
                "Request body is required.");
        }
        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return ErrorResponseExtensions.CreateErrorResult(
                StatusCodes.Status400BadRequest,
                "Email is required.");
        }

        try
        {
            // Use the authenticated user from JWT validation
            var response = await _ensureCustomerForUser.EnsureAsync(authenticatedUser!, payload);
            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            return ErrorResponseExtensions.HandleException(ex, _logger);
        }
    }
}
