using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretchStripe;

public class CreateStripeCheckoutSession
{
    private readonly ILogger<CreateStripeCheckoutSession> _logger;
    private readonly ICheckoutSessionService _checkoutService;

    public CreateStripeCheckoutSession(ILogger<CreateStripeCheckoutSession> logger, ICheckoutSessionService checkoutService)
    {
        _logger = logger;
        _checkoutService = checkoutService;
    }

    [Function("CreateStripeCheckoutSession")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] CreateCheckoutSessionRequest payload)
    {
        _logger.LogInformation("Processing request to create Stripe checkout session");
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
