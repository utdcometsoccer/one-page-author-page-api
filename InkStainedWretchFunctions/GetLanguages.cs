using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function for retrieving Language data by request language.
/// </summary>
public class GetLanguages
{
    private readonly ILogger<GetLanguages> _logger;
    private readonly ILanguageService _languageService;
    private readonly IJwtValidationService _jwtValidationService;

    public GetLanguages(
        ILogger<GetLanguages> logger,
        ILanguageService languageService,
        IJwtValidationService jwtValidationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
    }

    /// <summary>
    /// Gets languages by request language code.
    /// Returns language names localized in the requested language.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="language">The request language code (e.g., "en", "es", "fr", "ar", "zh").</param>
    /// <returns>List of Language entities for the specified request language.</returns>
    [Function("GetLanguages")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "languages/{language}")] HttpRequest req,
        string language)
    {
        _logger.LogInformation($"Received request for Languages with language: {language}");

        // Validate language parameter
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogWarning("Language parameter is null or empty");
            return new BadRequestObjectResult(new { error = "Language parameter is required" });
        }

        // Authenticate the request using JWT token
        var (user, errorResult) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (errorResult != null)
        {
            return errorResult;
        }

        try
        {
            // Normalize language code to lowercase
            var normalizedLanguage = language.ToLowerInvariant();

            // Get Languages by request language
            var languages = await _languageService.GetLanguagesByRequestLanguageAsync(normalizedLanguage);

            if (languages == null || !languages.Any())
            {
                _logger.LogInformation($"No Languages found for language: {language}");
                return new NotFoundObjectResult(new { message = $"No Languages found for language: {language}" });
            }

            _logger.LogInformation($"Successfully retrieved {languages.Count} Languages for language: {language}");

            // Return the results in a clean format optimized for API consumers
            var result = languages.Select(l => new
            {
                code = l.Code,
                name = l.Name
            }).ToList();

            return new OkObjectResult(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Invalid language parameter: {language}");
            return new BadRequestObjectResult(new { error = $"Invalid language format: {language}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving Languages for language: {language}");
            return new ObjectResult(new { error = "Internal server error occurred while retrieving Languages" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

}
