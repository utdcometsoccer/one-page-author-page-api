using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using System;

namespace InkStainedWretchStripe;

public class ListSubscription
{
    private readonly ILogger<ListSubscription> _logger;
    private readonly IListSubscriptions _listSubscriptions;

    public ListSubscription(ILogger<ListSubscription> logger, IListSubscriptions listSubscriptions)
    {
        _logger = logger;
        _listSubscriptions = listSubscriptions;
    }

    [Function("ListSubscription")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ListSubscription/{customerId}")] HttpRequest req,
        string customerId)
    {
        _logger.LogInformation("Listing subscriptions for customer {CustomerId}", customerId);

        if (string.IsNullOrWhiteSpace(customerId))
        {
            return new BadRequestObjectResult(new { error = "Route parameter 'customerId' is required." });
        }

        try
        {
            // Optional query parameters
            string? status = req.Query.TryGetValue("status", out var statusVals) ? statusVals.ToString() : null;

            int? limit = null;
            if (req.Query.TryGetValue("limit", out var limitVals) && int.TryParse(limitVals, out var limitParsed))
            {
                limit = limitParsed;
            }

            string? startingAfter = req.Query.TryGetValue("startingAfter", out var saVals) ? saVals.ToString() : null;

            bool expandPI = false;
            if (req.Query.TryGetValue("expandLatestInvoicePaymentIntent", out var expVals) && bool.TryParse(expVals, out var expandParsed))
            {
                expandPI = expandParsed;
            }

            SubscriptionsResponse result = await _listSubscriptions.ListAsync(
                customerId: customerId,
                status: status,
                limit: limit,
                startingAfter: startingAfter,
                expandLatestInvoicePaymentIntent: expandPI);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing subscriptions for customer {CustomerId}", customerId);
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
