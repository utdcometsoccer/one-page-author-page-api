using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using InkStainedWretch.OnePageAuthorAPI.API;

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
    private readonly IEnsureCustomerForUser _ensureCustomerForUser;

    public CreateStripeCustomer(ILogger<CreateStripeCustomer> logger, IEnsureCustomerForUser ensureCustomerForUser)
    {
        _logger = logger;
        _ensureCustomerForUser = ensureCustomerForUser;
    }

    [Function("CreateStripeCustomer")]
    [Authorize]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] CreateCustomerRequest payload)
    {
        _logger.LogInformation("CreateStripeCustomer invoked.");
        var user = req.HttpContext.User;
        if (payload is null)
        {
            return new BadRequestObjectResult(new { error = "Request body is required." });
        }
        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return new BadRequestObjectResult(new { error = "Email is required." });
        }

        try
        {
            var response = await _ensureCustomerForUser.EnsureAsync(user, payload);
            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Stripe customer for current user.");
            return new ObjectResult(new { error = ex.Message }) { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}
