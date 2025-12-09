using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InkStainedWretchesConfig
{
    /// <summary>
    /// Azure Function for retrieving application configuration from Key Vault.
    /// </summary>
    public class GetApplicationConfig
    {
        private readonly ILogger<GetApplicationConfig> _logger;
        private readonly IKeyVaultConfigService _keyVaultService;

        public GetApplicationConfig(ILogger<GetApplicationConfig> logger, IKeyVaultConfigService keyVaultService)
        {
            _logger = logger;
            _keyVaultService = keyVaultService;
        }

        /// <summary>
        /// Gets the Application Insights connection string from Key Vault.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <returns>HTTP response with Application Insights connection string.</returns>
        [Function("GetApplicationConfig")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "config/applicationinsights")] HttpRequestData req)
        {
            _logger.LogInformation("GetApplicationConfig function processing request.");

            try
            {
                // Get Application Insights connection string from Key Vault
                var connectionString = await _keyVaultService.GetSecretWithFallbackAsync(
                    "APPLICATIONINSIGHTS-CONNECTION-STRING",
                    "APPLICATIONINSIGHTS_CONNECTION_STRING");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogWarning("Application Insights connection string not found in Key Vault or environment.");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteAsJsonAsync(new { error = "Application Insights connection string not configured" });
                    return notFoundResponse;
                }

                _logger.LogInformation("Successfully retrieved Application Insights connection string.");
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new 
                { 
                    connectionString = connectionString,
                    source = _keyVaultService.IsKeyVaultEnabled() ? "KeyVault" : "Environment"
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Application Insights configuration.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
                return errorResponse;
            }
        }
    }
}
