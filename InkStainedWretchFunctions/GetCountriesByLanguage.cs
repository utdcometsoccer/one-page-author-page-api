using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function for retrieving Country data by language.
/// </summary>
public class GetCountriesByLanguage
{
    private readonly ILogger<GetCountriesByLanguage> _logger;
    private readonly ICountryService _countryService;
    private readonly IJwtValidationService _jwtValidationService;

    public GetCountriesByLanguage(
        ILogger<GetCountriesByLanguage> logger,
        ICountryService countryService,
        IJwtValidationService jwtValidationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _countryService = countryService ?? throw new ArgumentNullException(nameof(countryService));
        _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
    }

    /// <summary>
    /// Gets countries by language code.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="language">The language code (e.g., "en", "es", "fr", "ar", "zh-CN", "zh-TW").</param>
    /// <returns>List of Country entities for the specified language.</returns>
    [Function("GetCountriesByLanguage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "countries/{language}")] HttpRequest req,
        string language)
    {
        _logger.LogInformation($"Received request for Countries with language: {language}");

        // Validate language parameter
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogWarning("Language parameter is null or empty");
            return new BadRequestObjectResult(new { error = "Language parameter is required" });
        }

        // Normalize language to lowercase
        language = language.ToLowerInvariant();

        // Authenticate the request using JWT token
        var (user, errorResult) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (errorResult != null)
        {
            return errorResult;
        }

        try
        {
            // Get Countries by language
            var countries = await _countryService.GetCountriesByLanguageAsync(language);

            if (countries == null || !countries.Any())
            {
                _logger.LogInformation($"No Countries found for language: {language}");
                return new NotFoundObjectResult(new { message = $"No Countries found for language: {language}" });
            }

            _logger.LogInformation($"Successfully retrieved {countries.Count} Countries for language: {language}");

            // Return the results in a clean format
            var result = countries.Select(c => new
            {
                code = c.Code,
                name = c.Name
            }).OrderBy(c => c.name).ToList();

            return new OkObjectResult(new
            {
                Language = language,
                Count = result.Count,
                Countries = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Invalid language parameter: {language}");
            return new BadRequestObjectResult(new { error = $"Invalid language format: {language}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving Countries for language: {language}");
            return new ObjectResult(new { error = "Internal server error occurred while retrieving Countries" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

}
