using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.AspNetCore.Authorization;

namespace InkStainedWretchStripe;

public class CancelSubscription
{
    private readonly ILogger<CancelSubscription> _logger;
    private readonly ICancelSubscription _canceller;

    public CancelSubscription(ILogger<CancelSubscription> logger, ICancelSubscription canceller)
    {
        _logger = logger;
        _canceller = canceller;
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
    [Authorize]
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

        var result = await _canceller.CancelAsync(subscriptionId, payload);
        return new OkObjectResult(result);
    }
}
