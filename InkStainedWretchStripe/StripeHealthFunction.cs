using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace InkStainedWretchStripe;

/// <summary>
/// Response model for the Stripe health check endpoint.
/// </summary>
public class StripeHealthResponse
{
    /// <summary>
    /// Indicates whether the backend is configured for "test" or "live" Stripe mode.
    /// </summary>
    [JsonPropertyName("stripeMode")]
    public string StripeMode { get; set; } = "unknown";

    /// <summary>
    /// Indicates whether the Stripe secret key is properly configured.
    /// </summary>
    [JsonPropertyName("stripeConnected")]
    public bool StripeConnected { get; set; }

    /// <summary>
    /// API version number for tracking.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// Azure Function for Stripe health check endpoint.
/// Returns Stripe configuration status including mode (test/live) and connection status.
/// </summary>
/// <remarks>
/// This endpoint is unauthenticated and provides non-sensitive configuration information
/// to help frontend applications validate Stripe configuration before user interactions.
/// </remarks>
public class StripeHealthFunction
{
    private readonly ILogger<StripeHealthFunction> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the StripeHealthFunction class.
    /// </summary>
    /// <param name="logger">Logger instance for logging function activity.</param>
    /// <param name="configuration">Configuration instance to access environment variables.</param>
    public StripeHealthFunction(ILogger<StripeHealthFunction> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Handles GET requests to the Stripe health check endpoint.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>JSON response containing Stripe mode, connection status, and API version.</returns>
    [Function("StripeHealth")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stripe/health")]
        HttpRequest req)
    {
        _logger.LogInformation("Stripe health check requested");

        try
        {
            // Get Stripe API key from configuration
            var stripeApiKey = _configuration["STRIPE_API_KEY"];

            // Determine mode from API key
            string stripeMode = "unknown";
            bool stripeConnected = false;

            if (!string.IsNullOrEmpty(stripeApiKey))
            {
                stripeConnected = true;

                if (stripeApiKey.StartsWith("sk_test_"))
                {
                    stripeMode = "test";
                }
                else if (stripeApiKey.StartsWith("sk_live_"))
                {
                    stripeMode = "live";
                }
            }

            // Create response object
            var healthResponse = new StripeHealthResponse
            {
                StripeMode = stripeMode,
                StripeConnected = stripeConnected,
                Version = "1.0.0"
            };

            _logger.LogInformation("Stripe health check completed: mode={StripeMode}, connected={StripeConnected}", stripeMode, stripeConnected);

            return new OkObjectResult(healthResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Stripe health check");

            return new ObjectResult(new { error = "Internal server error" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
