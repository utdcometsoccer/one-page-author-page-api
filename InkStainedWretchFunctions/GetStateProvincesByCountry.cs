using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using System.Security.Claims;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function for retrieving StateProvince data by country and culture.
/// </summary>
public class GetStateProvincesByCountry
{
    private readonly ILogger<GetStateProvincesByCountry> _logger;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly IJwtValidationService _jwtValidationService;

    public GetStateProvincesByCountry(
        ILogger<GetStateProvincesByCountry> logger,
        IStateProvinceService stateProvinceService,
        IJwtValidationService jwtValidationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateProvinceService = stateProvinceService ?? throw new ArgumentNullException(nameof(stateProvinceService));
        _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
    }

    /// <summary>
    /// Gets states and provinces by country code and culture.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="countryCode">The two-letter country code (e.g., "US", "CA", "MX").</param>
    /// <param name="culture">The culture code (e.g., "en-US", "fr-CA", "es-MX").</param>
    /// <returns>List of StateProvince entities for the specified country and culture.</returns>
    [Function("GetStateProvincesByCountry")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stateprovinces/{countryCode}/{culture}")] HttpRequest req,
        string countryCode,
        string culture)
    {
        _logger.LogInformation($"Received request for StateProvinces with country: {countryCode}, culture: {culture}");

        // Validate parameters
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            _logger.LogWarning("CountryCode parameter is null or empty");
            return new BadRequestObjectResult(new { error = "CountryCode parameter is required" });
        }

        if (string.IsNullOrWhiteSpace(culture))
        {
            _logger.LogWarning("Culture parameter is null or empty");
            return new BadRequestObjectResult(new { error = "Culture parameter is required" });
        }

        // Validate country code format (should be 2 letters)
        if (countryCode.Length != 2)
        {
            _logger.LogWarning($"Invalid country code format: {countryCode}");
            return new BadRequestObjectResult(new { error = "CountryCode must be a 2-letter ISO country code (e.g., US, CA, MX)" });
        }

        // Authenticate the request using JWT token
        var (user, errorResult) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Check if user has the required scope for StateProvince reading
        if (user != null && !HasRequiredScope(user))
        {
            _logger.LogWarning("User does not have required StateProvince.Read scope");
            return new ObjectResult(new { error = "Insufficient permissions" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        try
        {
            // Get StateProvinces by country and culture
            var stateProvinces = await _stateProvinceService.GetStateProvincesByCountryAndCultureAsync(countryCode.ToUpperInvariant(), culture);

            if (stateProvinces == null || !stateProvinces.Any())
            {
                _logger.LogInformation($"No StateProvinces found for country: {countryCode}, culture: {culture}");
                return new NotFoundObjectResult(new { message = $"No StateProvinces found for country: {countryCode}, culture: {culture}" });
            }

            _logger.LogInformation($"Successfully retrieved {stateProvinces.Count} StateProvinces for country: {countryCode}, culture: {culture}");

            // Return the results
            var result = new
            {
                Country = countryCode.ToUpperInvariant(),
                Culture = culture,
                Count = stateProvinces.Count,
                StateProvinces = stateProvinces.Select(sp => new
                {
                    sp.Code,
                    sp.Name,
                    sp.Country,
                    sp.Culture
                }).OrderBy(sp => sp.Name)
            };

            return new OkObjectResult(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Invalid parameters - Country: {countryCode}, Culture: {culture}");
            return new BadRequestObjectResult(new { error = $"Invalid country code or culture format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving StateProvinces for country: {countryCode}, culture: {culture}");
            return new ObjectResult(new { error = "Internal server error occurred while retrieving StateProvinces" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <summary>
    /// Checks if the user has the required scope for StateProvince operations.
    /// </summary>
    /// <param name="user">The claims principal representing the authenticated user.</param>
    /// <returns>True if the user has the required scope, false otherwise.</returns>
    private static bool HasRequiredScope(ClaimsPrincipal user)
    {
        // Check for StateProvince.Read scope or a general Read scope
        var scopes = user.FindAll("scope").Select(c => c.Value);
        return scopes.Any(s => s.Contains("StateProvince.Read") || s.Contains("Read") || s.Contains("User.Read"));
    }
}