using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.AspNetCore.Authorization;
using InkStainedWretch.OnePageAuthorLib.API;

namespace InkStainedWretchStripe;

/// <summary>
/// Azure Function for finding subscriptions by customer email and domain name.
/// </summary>
/// <remarks>
/// - Method: GET
/// - Route: /api/FindSubscription
/// - Auth: Function (requires JWT authentication)
/// - Query Parameters:
///   - email: Customer email address (required)
///   - domain: Domain name associated with the subscription (required)
/// - Response: 200 OK with <see cref="FindSubscriptionResponse"/>
/// - 400 on missing required parameters
/// - 500 on server errors
///
/// TypeScript client example (using fetch):
///
/// export type FindSubscriptionResponse = {
///   CustomerId: string;
///   Subscriptions: SubscriptionDto[];
///   CustomerFound: boolean;
///   SubscriptionsFound: boolean;
/// };
///
/// export async function findSubscription(
///   baseUrl: string,
///   email: string,
///   domain: string,
///   token: string
/// ): Promise&lt;FindSubscriptionResponse&gt; {
///   const params = new URLSearchParams({ email, domain });
///   const res = await fetch(`${baseUrl}/api/FindSubscription?${params}`, {
///     method: "GET",
///     headers: { "Authorization": `Bearer ${token}` },
///   });
///   if (!res.ok) {
///     const text = await res.text();
///     throw new Error(`HTTP ${res.status}: ${text}`);
///   }
///   return (await res.json()) as FindSubscriptionResponse;
/// }
///
/// Example call:
///   const result = await findSubscription(
///     "https://localhost:7292", 
///     "customer@example.com",
///     "example.com",
///     "jwt-token"
///   );
/// </remarks>
public class FindSubscription
{
    private readonly ILogger<FindSubscription> _logger;
    private readonly IFindSubscriptions _findSubscriptions;
    private readonly IAuthenticatedFunctionTelemetryService _telemetry;

    public FindSubscription(
        ILogger<FindSubscription> logger, 
        IFindSubscriptions findSubscriptions,
        IAuthenticatedFunctionTelemetryService telemetry)
    {
        _logger = logger;
        _findSubscriptions = findSubscriptions;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Finds subscriptions for a customer by email address and domain name.
    /// </summary>
    /// <param name="req">HTTP request containing email and domain query parameters.</param>
    /// <returns>200 with FindSubscriptionResponse; 400 on invalid input; 500 on error.</returns>
    [Function("FindSubscription")]
    [Authorize]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "FindSubscription")] HttpRequest req)
    {
        var user = req.HttpContext.User;
        var userId = AuthenticatedFunctionTelemetryService.ExtractUserId(user);
        var userEmail = AuthenticatedFunctionTelemetryService.ExtractUserEmail(user);

        _logger.LogInformation("FindSubscription invoked by user {UserId}", userId ?? "Anonymous");

        // Track the authenticated function call
        _telemetry.TrackAuthenticatedFunctionCall(
            "FindSubscription",
            userId,
            userEmail);

        // Extract query parameters
        if (!req.Query.TryGetValue("email", out var emailVals) || string.IsNullOrWhiteSpace(emailVals.ToString()))
        {
            _telemetry.TrackAuthenticatedFunctionError(
                "FindSubscription",
                userId,
                userEmail,
                "Query parameter 'email' is required",
                "ValidationError");
            return new BadRequestObjectResult(new { error = "Query parameter 'email' is required." });
        }

        if (!req.Query.TryGetValue("domain", out var domainVals) || string.IsNullOrWhiteSpace(domainVals.ToString()))
        {
            _telemetry.TrackAuthenticatedFunctionError(
                "FindSubscription",
                userId,
                userEmail,
                "Query parameter 'domain' is required",
                "ValidationError");
            return new BadRequestObjectResult(new { error = "Query parameter 'domain' is required." });
        }

        string email = emailVals.ToString();
        string domain = domainVals.ToString();

        _logger.LogInformation("Finding subscriptions for email {Email} and domain {Domain} requested by user {UserId}", email, domain, userId ?? "Anonymous");

        try
        {
            var result = await _findSubscriptions.FindByEmailAndDomainAsync(email, domain);
            
            // Track success with additional context
            var successProperties = new Dictionary<string, string>
            {
                { "TargetEmail", email },
                { "Domain", domain },
                { "CustomerFound", result.CustomerFound.ToString() },
                { "SubscriptionsFound", result.SubscriptionsFound.ToString() }
            };
            
            var successMetrics = new Dictionary<string, double>
            {
                { "SubscriptionCount", result.Subscriptions?.Count ?? 0 }
            };
            
            _telemetry.TrackAuthenticatedFunctionSuccess(
                "FindSubscription",
                userId,
                userEmail,
                successProperties,
                successMetrics);
            
            _logger.LogInformation(
                "Successfully found {Count} subscriptions for email {Email} and domain {Domain} by user {UserId}",
                result.Subscriptions?.Count ?? 0,
                email,
                domain,
                userId ?? "Anonymous");
            
            return new OkObjectResult(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for FindSubscription by user {UserId}", userId ?? "Anonymous");
            _telemetry.TrackAuthenticatedFunctionError(
                "FindSubscription",
                userId,
                userEmail,
                ex.Message,
                "ArgumentException",
                new Dictionary<string, string>
                {
                    { "TargetEmail", email },
                    { "Domain", domain }
                });
            return new BadRequestObjectResult(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding subscriptions for email {Email} and domain {Domain} by user {UserId}", email, domain, userId ?? "Anonymous");
            _telemetry.TrackAuthenticatedFunctionError(
                "FindSubscription",
                userId,
                userEmail,
                ex.Message,
                ex.GetType().Name,
                new Dictionary<string, string>
                {
                    { "TargetEmail", email },
                    { "Domain", domain }
                });
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
