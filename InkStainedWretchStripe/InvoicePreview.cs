using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretchStripe;

public class InvoicePreview
{
    private readonly ILogger<InvoicePreview> _logger;
    private readonly IInvoicePreview _previewer;

    public InvoicePreview(ILogger<InvoicePreview> logger, IInvoicePreview previewer)
    {
        _logger = logger;
        _previewer = previewer;
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
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] InvoicePreviewRequest payload)
    {
        if (payload is null || string.IsNullOrWhiteSpace(payload.CustomerId))
        {
            return new BadRequestObjectResult(new { error = "CustomerId is required." });
        }

        _logger.LogInformation("InvoicePreview invoked for {CustomerId}", payload.CustomerId);
        var result = await _previewer.PreviewAsync(payload);
        return new OkObjectResult(result);
    }
}
