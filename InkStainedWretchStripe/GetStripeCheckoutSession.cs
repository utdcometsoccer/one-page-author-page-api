using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretchStripe;

public class GetStripeCheckoutSession
{
    private readonly ILogger<GetStripeCheckoutSession> _logger;
    private readonly ICheckoutSessionService _checkoutService;

    public GetStripeCheckoutSession(ILogger<GetStripeCheckoutSession> logger, ICheckoutSessionService checkoutService)
    {
        _logger = logger;
        _checkoutService = checkoutService;
    }

    [Function("GetStripeCheckoutSession")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetStripeCheckoutSession/{sessionId}")] HttpRequest req,
        string sessionId)
    {
        _logger.LogInformation("Processing request to retrieve Stripe checkout session");

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
