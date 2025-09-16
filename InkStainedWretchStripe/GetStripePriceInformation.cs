using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretchStripe;

public class GetStripePriceInformation
{
    private readonly ILogger<GetStripePriceInformation> _logger;
    private readonly IPriceServiceWrapper _priceService;

    public GetStripePriceInformation(ILogger<GetStripePriceInformation> logger, IPriceServiceWrapper priceService)
    {
        _logger = logger;
        _priceService = priceService;
    }

    [Function("GetStripePriceInformation")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("Processing POST request to get Stripe price information");

        try
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            var request = JsonSerializer.Deserialize<PriceListRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request is null)
            {
                return new BadRequestObjectResult(new { error = "Invalid request payload" });
            }

            var result = await _priceService.GetPricesAsync(request);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe price information request");
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
