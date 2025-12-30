using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;

namespace InkStainedWretchStripe;

public class WebHook
{
    private readonly ILogger<WebHook> _logger;
    private readonly IStripeWebhookHandler _handler;

    public WebHook(ILogger<WebHook> logger, IStripeWebhookHandler handler)
    {
        _logger = logger;
        _handler = handler;
    }

    [Function("WebHook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "WebHook")] HttpRequest req)
    {
        var signature = req.Headers["Stripe-Signature"].FirstOrDefault();
        _logger.LogInformation("Webhook invoked. Signature header present: {HasSig}", !string.IsNullOrEmpty(signature));

        // Read raw body as string for Stripe signature verification
        string payload;
        try
        {
            using (var reader = new StreamReader(req.Body, System.Text.Encoding.UTF8))
            {
                payload = await reader.ReadToEndAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read request body");
            return new BadRequestObjectResult(new { error = "Failed to read request body" });
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            _logger.LogWarning("Stripe webhook invoked with empty request body.");
            return new BadRequestObjectResult(new { error = "Request body must not be empty." });
        }

        var result = await _handler.HandleAsync(payload, signature);
        if (!result.Success)
        {
            return new BadRequestObjectResult(new { error = result.Message });
        }
        return new OkObjectResult(new { ok = true, message = result.Message });
    }
}
