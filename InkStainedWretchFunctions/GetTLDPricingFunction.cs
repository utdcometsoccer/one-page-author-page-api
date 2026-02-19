using System.Text.Json;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function for retrieving TLD pricing information from WHMCS API
    /// </summary>
    public class GetTLDPricingFunction
    {
        private readonly IWhmcsService _whmcsService;
        private readonly ILogger<GetTLDPricingFunction> _logger;
        private readonly IJwtValidationService _jwtValidationService;
        private readonly IUserProfileService _userProfileService;
        private readonly IConfiguration _configuration;

        public GetTLDPricingFunction(
            IWhmcsService whmcsService,
            ILogger<GetTLDPricingFunction> logger,
            IJwtValidationService jwtValidationService,
            IUserProfileService userProfileService,
            IConfiguration configuration)
        {
            _whmcsService = whmcsService ?? throw new ArgumentNullException(nameof(whmcsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets TLD pricing information from WHMCS API.
        /// </summary>
        /// <param name="req">HTTP request with authentication</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>JSON response from WHMCS GetTLDPricing API</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Unexpected error occurred</description>
        /// </item>
        /// <item>
        /// <term>502 Bad Gateway</term>
        /// <description>WHMCS API error, configuration issue, or HTTP request failure</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <example>
        /// <para><strong>TypeScript Example:</strong></para>
        /// <code>
        /// interface TLDPricing {
        ///   result: string;
        ///   pricing: {
        ///     [tld: string]: {
        ///       registration: { [years: string]: number };
        ///       renewal: { [years: string]: number };
        ///       transfer: { [years: string]: number };
        ///     }
        ///   };
        /// }
        /// 
        /// const getTLDPricing = async (token: string, clientId?: string, currencyId?: number): Promise&lt;TLDPricing&gt; => {
        ///   // Build query parameters
        ///   const params = new URLSearchParams();
        ///   if (clientId) params.append('clientId', clientId);
        ///   if (currencyId) params.append('currencyId', currencyId.toString());
        ///   
        ///   const queryString = params.toString();
        ///   const url = `/api/whmcs/tld-pricing${queryString ? `?${queryString}` : ''}`;
        ///   
        ///   const response = await fetch(url, {
        ///     method: 'GET',
        ///     headers: {
        ///       'Authorization': `Bearer ${token}`,
        ///       'Content-Type': 'application/json'
        ///     }
        ///   });
        /// 
        ///   if (!response.ok) {
        ///     if (response.status === 401) {
        ///       throw new Error('Unauthorized - invalid or missing token');
        ///     }
        ///     throw new Error('Failed to retrieve TLD pricing');
        ///   }
        /// 
        ///   return await response.json();
        /// };
        /// 
        /// // Usage with React/TypeScript
        /// const TLDPricingDisplay: React.FC = () => {
        ///   const [pricing, setPricing] = useState&lt;TLDPricing | null&gt;(null);
        ///   const [loading, setLoading] = useState(true);
        /// 
        ///   useEffect(() => {
        ///     const loadPricing = async () => {
        ///       try {
        ///         const pricingData = await getTLDPricing(userToken);
        ///         setPricing(pricingData);
        ///       } catch (error) {
        ///         console.error('Failed to load TLD pricing:', error);
        ///       } finally {
        ///         setLoading(false);
        ///       }
        ///     };
        /// 
        ///     loadPricing();
        ///   }, [userToken]);
        /// 
        ///   if (loading) return &lt;div&gt;Loading pricing...&lt;/div&gt;;
        ///   if (!pricing) return &lt;div&gt;Failed to load pricing&lt;/div&gt;;
        /// 
        ///   return (
        ///     &lt;div&gt;
        ///       &lt;h2&gt;TLD Pricing&lt;/h2&gt;
        ///       {Object.entries(pricing.pricing).map(([tld, prices]) =&gt; (
        ///         &lt;div key={tld}&gt;
        ///           &lt;h3&gt;.{tld}&lt;/h3&gt;
        ///           &lt;p&gt;Registration (1 year): ${prices.registration['1']}&lt;/p&gt;
        ///           &lt;p&gt;Renewal (1 year): ${prices.renewal['1']}&lt;/p&gt;
        ///         &lt;/div&gt;
        ///       ))}
        ///     &lt;/div&gt;
        ///   );
        /// };
        /// </code>
        /// </example>
        [Function("GetTLDPricing")]
        public async Task<IActionResult> GetPricing(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "whmcs/tld-pricing")] HttpRequest req)
        {
            _logger.LogInformation("GetTLDPricing function processed a request.");

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
                _logger.LogWarning(ex, "User profile validation failed for GetTLDPricing");
                return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
            }

            try
            {
                // Get optional query parameters; load clientId from configuration first,
                // then override with the request query parameter if provided.
                var clientId = _configuration["WHMCS_CLIENT_ID"];
                var clientIdParam = req.Query["clientId"].FirstOrDefault();
                if (!string.IsNullOrEmpty(clientIdParam))
                {
                    clientId = clientIdParam;
                }
                var currencyIdParam = req.Query["currencyId"].FirstOrDefault();

                int? currencyId = null;
                if (!string.IsNullOrEmpty(currencyIdParam) && int.TryParse(currencyIdParam, out var parsedCurrencyId))
                {
                    currencyId = parsedCurrencyId;
                }

                _logger.LogInformation("Retrieving TLD pricing from WHMCS API with clientId: {ClientId}, currencyId: {CurrencyId}",
                    clientId ?? "(none)", currencyId?.ToString() ?? "(none)");

                // Call the WHMCS API
                using var jsonResult = await _whmcsService.GetTLDPricingAsync(clientId, currencyId);

                // Return the JSON result as an OK response
                var jsonString = JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogInformation("Successfully returned WHMCS TLD pricing data");
                return new ContentResult
                {
                    Content = jsonString,
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "WHMCS service is not configured or returned error");
                return new ObjectResult(new { error = ex.Message })
                {
                    StatusCode = 502 // Bad Gateway
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling WHMCS API");
                return new ObjectResult(new { error = $"External API error: {ex.Message}" })
                {
                    StatusCode = 502 // Bad Gateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetTLDPricing function");
                return new ObjectResult(new { error = "An unexpected error occurred" })
                {
                    StatusCode = 500 // Internal Server Error
                };
            }
        }
    }
}
