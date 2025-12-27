using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretchStripe;

public class GetStripeCheckoutSession
{
    private readonly ILogger<GetStripeCheckoutSession> _logger;
    private readonly ICheckoutSessionService _checkoutService;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public GetStripeCheckoutSession(ILogger<GetStripeCheckoutSession> logger, ICheckoutSessionService checkoutService, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _checkoutService = checkoutService;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    [Function("GetStripeCheckoutSession")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetStripeCheckoutSession/{sessionId}")] HttpRequest req,
        string sessionId)
    {
        _logger.LogInformation("Processing request to retrieve Stripe checkout session");

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
            _logger.LogWarning(ex, "User profile validation failed for GetStripeCheckoutSession");
            return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new BadRequestObjectResult(new { error = "Route parameter 'sessionId' is required." });
        }

        try
        {
            var result = await _checkoutService.GetAsync(sessionId);
            if (result == null)
            {
                return new NotFoundObjectResult(new { error = "Checkout session not found" });
            }
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Stripe checkout session {SessionId}", sessionId);
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
