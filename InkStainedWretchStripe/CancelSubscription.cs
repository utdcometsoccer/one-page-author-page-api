using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretchStripe;

public class CancelSubscription
{
    private readonly ILogger<CancelSubscription> _logger;
    private readonly ICancelSubscription _canceller;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public CancelSubscription(ILogger<CancelSubscription> logger, ICancelSubscription canceller, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _canceller = canceller;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Cancels a Stripe subscription.
    /// </summary>
    /// <remarks>
    /// - Method: POST
    /// - Route: /api/CancelSubscription/{subscriptionId}
    /// - Body (optional): { "InvoiceNow": true, "Prorate": false }
    /// - Response: 200 OK with CancelSubscriptionResponse
    /// </remarks>
    [Function("CancelSubscription")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "CancelSubscription/{subscriptionId}")] HttpRequest req,
        string subscriptionId,
        [FromBody] CancelSubscriptionRequest? payload)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return new BadRequestObjectResult(new { error = "subscriptionId is required" });
        }

        _logger.LogInformation("CancelSubscription invoked for {SubscriptionId}", subscriptionId);

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
            _logger.LogWarning(ex, "User profile validation failed for CancelSubscription");
            return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
        }

        var result = await _canceller.CancelAsync(subscriptionId, payload);
        return new OkObjectResult(result);
    }
}
