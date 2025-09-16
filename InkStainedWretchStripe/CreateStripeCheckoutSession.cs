using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
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
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Processing request to create Stripe checkout session");
        try
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            var payload = JsonSerializer.Deserialize<CreateCheckoutSessionRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload is null)
            {
                return new BadRequestObjectResult(new { error = "Invalid request payload" });
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
