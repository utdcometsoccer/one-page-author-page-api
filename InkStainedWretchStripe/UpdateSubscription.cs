using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.AspNetCore.Authorization;
using InkStainedWretch.OnePageAuthorLib.API;

namespace InkStainedWretchStripe;

public class UpdateSubscription
{
    private readonly ILogger<UpdateSubscription> _logger;
    private readonly IUpdateSubscription _updater;
    private readonly IAuthenticatedFunctionTelemetryService _telemetry;

    public UpdateSubscription(
        ILogger<UpdateSubscription> logger, 
        IUpdateSubscription updater,
        IAuthenticatedFunctionTelemetryService telemetry)
    {
        _logger = logger;
        _updater = updater;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Updates a Stripe subscription (change price/quantity, proration, cancel-at-period-end, etc.).
    /// </summary>
    /// <remarks>
    /// - Method: POST
    /// - Route: /api/UpdateSubscription/{subscriptionId}
    /// - Body: UpdateSubscriptionRequest
    /// </remarks>
    [Function("UpdateSubscription")]
    [Authorize]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "UpdateSubscription/{subscriptionId}")] HttpRequest req,
        string subscriptionId,
        [FromBody] UpdateSubscriptionRequest payload)
    {
        var user = req.HttpContext.User;
        var userId = AuthenticatedFunctionTelemetryService.ExtractUserId(user);
        var userEmail = AuthenticatedFunctionTelemetryService.ExtractUserEmail(user);

        _logger.LogInformation("UpdateSubscription invoked for {SubscriptionId} by user {UserId}", subscriptionId, userId ?? "Anonymous");

        // Track the authenticated function call
        _telemetry.TrackAuthenticatedFunctionCall(
            "UpdateSubscription",
            userId,
            userEmail,
            new Dictionary<string, string> { { "SubscriptionId", subscriptionId } });

        if (payload is null)
        {
            _telemetry.TrackAuthenticatedFunctionError(
                "UpdateSubscription",
                userId,
                userEmail,
                "Request body is required",
                "ValidationError",
                new Dictionary<string, string> { { "SubscriptionId", subscriptionId } });
            return new BadRequestObjectResult(new { error = "Request body is required." });
        }

        try
        {
            var result = await _updater.UpdateAsync(subscriptionId, payload);
            
            // Track success with additional context
            var successProperties = new Dictionary<string, string>
            {
                { "SubscriptionId", subscriptionId },
                { "PriceId", payload.PriceId ?? "unchanged" },
                { "CancelAtPeriodEnd", payload.CancelAtPeriodEnd?.ToString() ?? "unchanged" }
            };
            
            if (payload.Quantity.HasValue)
            {
                successProperties.Add("NewQuantity", payload.Quantity.Value.ToString());
            }
            
            _telemetry.TrackAuthenticatedFunctionSuccess(
                "UpdateSubscription",
                userId,
                userEmail,
                successProperties);
            
            _logger.LogInformation(
                "Successfully updated subscription {SubscriptionId} by user {UserId}",
                subscriptionId,
                userId ?? "Anonymous");
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId} by user {UserId}", subscriptionId, userId ?? "Anonymous");
            _telemetry.TrackAuthenticatedFunctionError(
                "UpdateSubscription",
                userId,
                userEmail,
                ex.Message,
                ex.GetType().Name,
                new Dictionary<string, string> { { "SubscriptionId", subscriptionId } });
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
