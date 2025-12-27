using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.AspNetCore.Authorization;
using InkStainedWretch.OnePageAuthorLib.API;

namespace InkStainedWretchStripe;

public class ListSubscription
{
    private readonly ILogger<ListSubscription> _logger;
    private readonly IListSubscriptions _listSubscriptions;
    private readonly IAuthenticatedFunctionTelemetryService _telemetry;

    public ListSubscription(
        ILogger<ListSubscription> logger, 
        IListSubscriptions listSubscriptions,
        IAuthenticatedFunctionTelemetryService telemetry)
    {
        _logger = logger;
        _listSubscriptions = listSubscriptions;
        _telemetry = telemetry;
    }

    [Function("ListSubscription")]
    [Authorize]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ListSubscription/{customerId}")] HttpRequest req,
        string customerId)
    {
        var user = req.HttpContext.User;
        var userId = AuthenticatedFunctionTelemetryService.ExtractUserId(user);
        var userEmail = AuthenticatedFunctionTelemetryService.ExtractUserEmail(user);

        _logger.LogInformation("Listing subscriptions for customer {CustomerId} by user {UserId}", customerId, userId ?? "Anonymous");

        // Track the authenticated function call
        _telemetry.TrackAuthenticatedFunctionCall(
            "ListSubscription",
            userId,
            userEmail,
            new Dictionary<string, string> { { "CustomerId", customerId } });

        if (string.IsNullOrWhiteSpace(customerId))
        {
            _telemetry.TrackAuthenticatedFunctionError(
                "ListSubscription",
                userId,
                userEmail,
                "Route parameter 'customerId' is required",
                "ValidationError");
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
            
            // Track success with additional context
            var successProperties = new Dictionary<string, string>
            {
                { "CustomerId", customerId },
                { "Status", status ?? "all" },
                { "ExpandPaymentIntent", expandPI.ToString() }
            };
            
            var successMetrics = new Dictionary<string, double>
            {
                { "SubscriptionCount", result.Subscriptions?.Data?.Count ?? 0 },
                { "HasMore", result.Subscriptions?.HasMore ?? false ? 1 : 0 }
            };
            
            _telemetry.TrackAuthenticatedFunctionSuccess(
                "ListSubscription",
                userId,
                userEmail,
                successProperties,
                successMetrics);
            
            _logger.LogInformation(
                "Successfully listed {Count} subscriptions for customer {CustomerId} by user {UserId}",
                result.Subscriptions?.Data?.Count ?? 0,
                customerId,
                userId ?? "Anonymous");
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing subscriptions for customer {CustomerId} by user {UserId}", customerId, userId ?? "Anonymous");
            _telemetry.TrackAuthenticatedFunctionError(
                "ListSubscription",
                userId,
                userEmail,
                ex.Message,
                ex.GetType().Name,
                new Dictionary<string, string> { { "CustomerId", customerId } });
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
