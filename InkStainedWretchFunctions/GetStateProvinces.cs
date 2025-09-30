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
/// Azure Function for retrieving StateProvince data by culture.
/// </summary>
public class GetStateProvinces
{
    private readonly ILogger<GetStateProvinces> _logger;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly IJwtValidationService _jwtValidationService;

    public GetStateProvinces(
        ILogger<GetStateProvinces> logger,
        IStateProvinceService stateProvinceService,
        IJwtValidationService jwtValidationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateProvinceService = stateProvinceService ?? throw new ArgumentNullException(nameof(stateProvinceService));
        _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
    }

    /// <summary>
    /// Gets states and provinces by culture code.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="culture">The culture code (e.g., "en-US", "fr-CA", "es-MX").</param>
    /// <returns>List of StateProvince entities for the specified culture.</returns>
    [Function("GetStateProvinces")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stateprovinces/{culture}")] HttpRequest req,
        string culture)
    {
        _logger.LogInformation($"Received request for StateProvinces with culture: {culture}");

        // Validate culture parameter
        if (string.IsNullOrWhiteSpace(culture))
        {
            _logger.LogWarning("Culture parameter is null or empty");
            return new BadRequestObjectResult(new { error = "Culture parameter is required" });
        }

        // Authenticate the request using JWT token
        var (user, errorResult) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (errorResult != null)
        {
            return errorResult;
        }

        try
        {
            // Get StateProvinces by culture
            var stateProvinces = await _stateProvinceService.GetStateProvincesByCultureAsync(culture);

            if (stateProvinces == null || !stateProvinces.Any())
            {
                _logger.LogInformation($"No StateProvinces found for culture: {culture}");
                return new NotFoundObjectResult(new { message = $"No StateProvinces found for culture: {culture}" });
            }

            _logger.LogInformation($"Successfully retrieved {stateProvinces.Count} StateProvinces for culture: {culture}");

            // Return the results grouped by country for better organization
            var groupedResults = stateProvinces
                .GroupBy(sp => sp.Country)
                .Select(g => new
                {
                    Country = g.Key,
                    Culture = culture,
                    StateProvinces = g.Select(sp => new
                    {
                        sp.Code,
                        sp.Name,
                        sp.Country,
                        sp.Culture
                    }).OrderBy(sp => sp.Name)
                })
                .OrderBy(g => g.Country);

            return new OkObjectResult(new
            {
                Culture = culture,
                TotalCount = stateProvinces.Count,
                Data = groupedResults
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Invalid culture parameter: {culture}");
            return new BadRequestObjectResult(new { error = $"Invalid culture format: {culture}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving StateProvinces for culture: {culture}");
            return new ObjectResult(new { error = "Internal server error occurred while retrieving StateProvinces" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

}