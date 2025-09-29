using System.Net;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.API.Penguin;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function for calling Penguin Random House API
    /// </summary>
    public class PenguinRandomHouseFunction
    {
        private readonly IPenguinRandomHouseService _penguinService;
        private readonly ILogger<PenguinRandomHouseFunction> _logger;
        private readonly IJwtValidationService _jwtValidationService;
        private readonly IUserProfileService _userProfileService;

        public PenguinRandomHouseFunction(
            IPenguinRandomHouseService penguinService,
            ILogger<PenguinRandomHouseFunction> logger,
            IJwtValidationService jwtValidationService,
            IUserProfileService userProfileService)
        {
            _penguinService = penguinService ?? throw new ArgumentNullException(nameof(penguinService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        }

        /// <summary>
        /// Searches for authors by name and returns the unmodified JSON response from Penguin Random House API
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="authorName">Author name from route parameter</param>
        /// <returns>Unmodified JSON response from the API</returns>
        [Function("SearchPenguinAuthors")]
        public async Task<IActionResult> SearchAuthors(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "penguin/authors/{authorName}")] HttpRequest req,
            string authorName)
        {
            _logger.LogInformation("SearchPenguinAuthors function processed a request.");

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
                _logger.LogWarning(ex, "User profile validation failed for SearchPenguinAuthors");
                return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
            }

            try
            {
                // Validate authorName from route parameter
                if (string.IsNullOrEmpty(authorName))
                {
                    _logger.LogWarning("No author name provided in route parameter");
                    return new BadRequestObjectResult(new { error = "Author name is required in the route parameter." });
                }

                // URL decode the author name in case it has special characters
                authorName = Uri.UnescapeDataString(authorName);

                _logger.LogInformation("Searching for author: {AuthorName}", authorName);

                // Call the Penguin Random House API
                using var jsonResult = await _penguinService.SearchAuthorsAsync(authorName);

                // Return the JSON result as an OK response
                var jsonString = JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogInformation("Successfully returned Penguin Random House API response for author: {AuthorName}", authorName);
                return new ContentResult
                {
                    Content = jsonString,
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request parameters");
                return new BadRequestObjectResult(new { error = $"Invalid request: {ex.Message}" });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Penguin Random House API");
                return new ObjectResult(new { error = $"External API error: {ex.Message}" })
                {
                    StatusCode = 502 // Bad Gateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchPenguinAuthors function");
                return new ObjectResult(new { error = "An unexpected error occurred" })
                {
                    StatusCode = 500 // Internal Server Error
                };
            }
        }

        /// <summary>
        /// Gets titles by author key and returns the unmodified JSON response from Penguin Random House API
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="authorKey">Author key from route parameter</param>
        /// <returns>Unmodified JSON response from the API</returns>
        [Function("GetPenguinTitlesByAuthor")]
        public async Task<IActionResult> GetTitlesByAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "penguin/authors/{authorKey}/titles")] HttpRequest req,
            string authorKey)
        {
            _logger.LogInformation("GetPenguinTitlesByAuthor function processed a request.");

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
                _logger.LogWarning(ex, "User profile validation failed for GetPenguinTitlesByAuthor");
                return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
            }

            try
            {
                // Validate authorKey from route parameter
                if (string.IsNullOrEmpty(authorKey))
                {
                    _logger.LogWarning("No author key provided in route parameter");
                    return new BadRequestObjectResult(new { error = "Author key is required in the route parameter." });
                }

                // URL decode the author key in case it has special characters
                authorKey = Uri.UnescapeDataString(authorKey);

                // Get optional parameters from query string
                var rowsParam = req.Query["rows"].FirstOrDefault() ?? "10";
                var startParam = req.Query["start"].FirstOrDefault() ?? "0";

                if (!int.TryParse(rowsParam, out var rows) || rows <= 0)
                {
                    rows = 10; // Default value
                }

                if (!int.TryParse(startParam, out var start) || start < 0)
                {
                    start = 0; // Default value
                }

                _logger.LogInformation("Getting titles for author key: {AuthorKey}, rows: {Rows}, start: {Start}", authorKey, rows, start);

                // Call the Penguin Random House API
                using var jsonResult = await _penguinService.GetTitlesByAuthorAsync(authorKey, rows, start);

                // Return the JSON result as an OK response
                var jsonString = JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogInformation("Successfully returned Penguin Random House API response for author key: {AuthorKey}", authorKey);
                return new ContentResult
                {
                    Content = jsonString,
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request parameters");
                return new BadRequestObjectResult(new { error = $"Invalid request: {ex.Message}" });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Penguin Random House API");
                return new ObjectResult(new { error = $"External API error: {ex.Message}" })
                {
                    StatusCode = 502 // Bad Gateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetPenguinTitlesByAuthor function");
                return new ObjectResult(new { error = "An unexpected error occurred" })
                {
                    StatusCode = 500 // Internal Server Error
                };
            }
        }
    }
}