using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretchStripe;

/// <summary>
/// Azure Function for creating a Stripe customer.
/// Handles HTTP POST requests, validates the incoming payload, and delegates customer creation logic.
/// </summary>
/// <remarks>
/// Expects a JSON body containing customer details. Validates the presence and format of required fields.
/// Returns appropriate HTTP responses for validation errors or successful customer creation.
/// </remarks>
/// <param name="logger">Logger instance for logging function activity.</param>
/// <param name="createStripeCustomer">Service responsible for executing Stripe customer creation logic.</param>
public class CreateStripeCustomer
{
    private readonly ILogger<CreateStripeCustomer> _logger;
    private readonly ICreateCustomer _createStripeCustomer;

    public CreateStripeCustomer(ILogger<CreateStripeCustomer> logger, ICreateCustomer createStripeCustomer)
    {
        _logger = logger;
        _createStripeCustomer = createStripeCustomer;
    }

    [Function("CreateStripeCustomer")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("CreateStripeCustomer invoked.");

        CreateCustomerRequest? payload;
        try
        {
            payload = await JsonSerializer.DeserializeAsync<CreateCustomerRequest>(req.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException jex)
        {
            _logger.LogWarning(jex, "Invalid JSON in request body.");
            return new BadRequestObjectResult(new { error = "Invalid JSON body." });
        }

        // Pattern matching with switch to validate payload
        IActionResult result = payload switch
        {
            null => new BadRequestObjectResult(new { error = "Request body is required." }),
            { Email: var e } when string.IsNullOrWhiteSpace(e) => new BadRequestObjectResult(new { error = "Email is required." }),
            _ => new OkObjectResult(_createStripeCustomer.Execute(payload))
        };

        // TODO: Integrate with Stripe SDK here to create a customer
        return result;
    }
}
