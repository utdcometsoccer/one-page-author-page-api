using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InkStainedWretchesConfig
{
    /// <summary>
    /// Azure Function for retrieving Penguin Random House API configuration from Key Vault.
    /// </summary>
    public class GetPenguinApiKey
    {
        private readonly ILogger<GetPenguinApiKey> _logger;
        private readonly IKeyVaultConfigService _keyVaultService;

        public GetPenguinApiKey(ILogger<GetPenguinApiKey> logger, IKeyVaultConfigService keyVaultService)
        {
            _logger = logger;
            _keyVaultService = keyVaultService;
        }

        /// <summary>
        /// Gets the Penguin Random House API key from Key Vault.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <returns>HTTP response with Penguin API key.</returns>
        [Function("GetPenguinApiKey")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "config/penguin-api-key")] HttpRequestData req)
        {
            _logger.LogInformation("GetPenguinApiKey function processing request.");

            try
            {
                // Get Penguin Random House API key from Key Vault
                var apiKey = await _keyVaultService.GetSecretWithFallbackAsync(
                    "PENGUIN-RANDOM-HOUSE-API-KEY",
                    "PENGUIN_RANDOM_HOUSE_API_KEY");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogWarning("Penguin Random House API key not found in Key Vault or environment.");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteAsJsonAsync(new { error = "Penguin Random House API key not configured" });
                    return notFoundResponse;
                }

                _logger.LogInformation("Successfully retrieved Penguin Random House API key.");
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new 
                { 
                    apiKey = apiKey,
                    source = _keyVaultService.IsKeyVaultEnabled() ? "KeyVault" : "Environment"
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Penguin Random House API key.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
                return errorResponse;
            }
        }
    }
}
