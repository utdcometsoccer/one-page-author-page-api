using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.AspNetCore.Authorization;
using InkStainedWretch.OnePageAuthorLib.API;

namespace InkStainedWretchStripe;

public class InvoicePreview
{
    private readonly ILogger<InvoicePreview> _logger;
    private readonly IInvoicePreview _previewer;
    private readonly IAuthenticatedFunctionTelemetryService _telemetry;

    public InvoicePreview(
        ILogger<InvoicePreview> logger, 
        IInvoicePreview previewer,
        IAuthenticatedFunctionTelemetryService telemetry)
    {
        _logger = logger;
        _previewer = previewer;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Generates a Stripe upcoming invoice preview for a customer/subscription change.
    /// </summary>
    /// <remarks>
    /// - Method: POST
    /// - Route: /api/InvoicePreview
    /// - Body: InvoicePreviewRequest
    /// </remarks>
    [Function("InvoicePreview")]
    [Authorize]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] InvoicePreviewRequest payload)
    {
        var user = req.HttpContext.User;
        var userId = AuthenticatedFunctionTelemetryService.ExtractUserId(user);
        var userEmail = AuthenticatedFunctionTelemetryService.ExtractUserEmail(user);

        _logger.LogInformation("InvoicePreview invoked for customer {CustomerId} by user {UserId}", payload?.CustomerId ?? "null", userId ?? "Anonymous");

        // Track the authenticated function call
        _telemetry.TrackAuthenticatedFunctionCall(
            "InvoicePreview",
            userId,
            userEmail,
            new Dictionary<string, string> { { "CustomerId", payload?.CustomerId ?? "null" } });

        if (payload is null || string.IsNullOrWhiteSpace(payload.CustomerId))
        {
            _telemetry.TrackAuthenticatedFunctionError(
                "InvoicePreview",
                userId,
                userEmail,
                "CustomerId is required",
                "ValidationError");
            return new BadRequestObjectResult(new { error = "CustomerId is required." });
        }

        try
        {
            var result = await _previewer.PreviewAsync(payload);
            
            // Track success with additional context
            var successProperties = new Dictionary<string, string>
            {
                { "CustomerId", payload.CustomerId },
                { "SubscriptionId", payload.SubscriptionId ?? "none" }
            };
            
            if (!string.IsNullOrEmpty(payload.PriceId))
            {
                successProperties.Add("PriceId", payload.PriceId);
            }
            
            _telemetry.TrackAuthenticatedFunctionSuccess(
                "InvoicePreview",
                userId,
                userEmail,
                successProperties);
            
            _logger.LogInformation(
                "Successfully generated invoice preview for customer {CustomerId} by user {UserId}",
                payload.CustomerId,
                userId ?? "Anonymous");
            
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice preview for customer {CustomerId} by user {UserId}", payload.CustomerId, userId ?? "Anonymous");
            _telemetry.TrackAuthenticatedFunctionError(
                "InvoicePreview",
                userId,
                userEmail,
                ex.Message,
                ex.GetType().Name,
                new Dictionary<string, string> { { "CustomerId", payload.CustomerId } });
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
