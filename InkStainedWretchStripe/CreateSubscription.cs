using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;

namespace InkStainedWretchStripe;

/// <summary>
/// HTTP endpoint to create a Stripe subscription.
/// </summary>
/// <remarks>
/// - Method: POST
/// - Route: /api/CreateSubscription
/// - Auth: Anonymous
/// - Body: JSON with PascalCase properties matching <see cref="InkStainedWretch.OnePageAuthorLib.Entities.Stripe.CreateSubscriptionRequest"/>
///   { "PriceId": "price_...", "CustomerId": "cus_..." }
/// - Response: 200 OK with <see cref="InkStainedWretch.OnePageAuthorLib.Entities.Stripe.SubscriptionCreateResponse"/>
///   { "SubscriptionId": "sub_...", "ClientSecret": "..." }
/// - 400 on invalid JSON or missing required fields
///
/// TypeScript client example (using fetch):
///
/// export type CreateSubscriptionRequest = {
///   PriceId: string;
///   CustomerId: string;
/// };
///
/// export type SubscriptionCreateResponse = {
///   SubscriptionId: string;
///   ClientSecret: string;
/// };
///
/// export async function createSubscription(
///   baseUrl: string,
///   payload: CreateSubscriptionRequest
/// ): Promise<SubscriptionCreateResponse> {
///   const res = await fetch(`${baseUrl}/api/CreateSubscription`, {
///     method: "POST",
///     headers: { "Content-Type": "application/json" },
///     body: JSON.stringify(payload),
///   });
///   if (!res.ok) {
///     const text = await res.text();
///     throw new Error(`HTTP ${res.status}: ${text}`);
///   }
///   return (await res.json()) as SubscriptionCreateResponse;
/// }
///
/// Example call:
///   await createSubscription("https://localhost:7292", { PriceId: "price_123", CustomerId: "cus_456" });
/// </remarks>
public class CreateSubscription
{
    private readonly ILogger<CreateSubscription> _logger;
    private readonly ISubscriptionService _subscriptions;

    public CreateSubscription(ILogger<CreateSubscription> logger, ISubscriptionService subscriptions)
    {
        _logger = logger;
        _subscriptions = subscriptions;
    }

    /// <summary>
    /// Creates a Stripe subscription from the provided request body.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <returns>200 with SubscriptionCreateResponse; 400 on invalid input.</returns>
    [Function("CreateSubscription")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        [FromBody] CreateSubscriptionRequest payload)
    {
        _logger.LogInformation("CreateSubscription invoked.");

        if (payload is null)
        {
            return new BadRequestObjectResult(new { error = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(payload.PriceId))
        {
            return new BadRequestObjectResult(new { error = "PriceId is required." });
        }

        var result = await _subscriptions.CreateAsync(payload);
        return new OkObjectResult(result);
    }
}
