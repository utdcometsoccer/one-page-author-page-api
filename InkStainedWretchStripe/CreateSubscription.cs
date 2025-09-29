using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;
using System.Security.Claims;

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
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public CreateSubscription(ILogger<CreateSubscription> logger, ISubscriptionService subscriptions, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _subscriptions = subscriptions;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Creates a Stripe subscription from the provided request body.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <returns>200 with SubscriptionCreateResponse; 400 on invalid input.</returns>
    [Function("CreateSubscription")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] CreateSubscriptionRequest payload)
    {
        _logger.LogInformation("CreateSubscription invoked.");

        // Validate JWT token and get authenticated user
        var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (authError != null)
        {
            return authError;
        }

        try
        {
            // Ensure user profile exists
            await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User profile validation failed for CreateSubscription");
            return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
        }

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
