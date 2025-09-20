using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretchStripe;

public class UpdateSubscription
{
    private readonly ILogger<UpdateSubscription> _logger;
    private readonly IUpdateSubscription _updater;

    public UpdateSubscription(ILogger<UpdateSubscription> logger, IUpdateSubscription updater)
    {
        _logger = logger;
        _updater = updater;
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
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UpdateSubscription/{subscriptionId}")] HttpRequest req,
        string subscriptionId,
        [FromBody] UpdateSubscriptionRequest payload)
    {
        if (payload is null)
        {
            return new BadRequestObjectResult(new { error = "Request body is required." });
        }

        _logger.LogInformation("UpdateSubscription invoked for {SubscriptionId}", subscriptionId);

        var result = await _updater.UpdateAsync(subscriptionId, payload);
        return new OkObjectResult(result);
    }
}
