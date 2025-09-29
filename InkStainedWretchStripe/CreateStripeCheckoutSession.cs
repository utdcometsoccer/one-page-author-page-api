using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;
using System.Security.Claims;

namespace InkStainedWretchStripe;

public class CreateStripeCheckoutSession
{
    private readonly ILogger<CreateStripeCheckoutSession> _logger;
    private readonly ICheckoutSessionService _checkoutService;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public CreateStripeCheckoutSession(ILogger<CreateStripeCheckoutSession> logger, ICheckoutSessionService checkoutService, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _checkoutService = checkoutService;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    [Function("CreateStripeCheckoutSession")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] CreateCheckoutSessionRequest payload)
    {
        _logger.LogInformation("Processing request to create Stripe checkout session");

        // Validate JWT token and get authenticated user
        var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (authError != null)
        {
            return authError;
        }

        try
        {
            // Ensure user profile exists
            await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User profile validation failed for CreateStripeCheckoutSession");
            return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
        }

        try
        {
            if (payload is null)
            {
                return new BadRequestObjectResult(new { error = "Request body is required or invalid" });
            }

            var response = await _checkoutService.CreateAsync(payload);
            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session");
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
