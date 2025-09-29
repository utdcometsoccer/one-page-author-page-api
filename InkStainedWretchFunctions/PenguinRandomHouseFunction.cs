using System.Net;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.API.Penguin;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function for calling Penguin Random House API
    /// </summary>
    public class PenguinRandomHouseFunction
    {
        private readonly IPenguinRandomHouseService _penguinService;
        private readonly ILogger<PenguinRandomHouseFunction> _logger;

        public PenguinRandomHouseFunction(
            IPenguinRandomHouseService penguinService,
            ILogger<PenguinRandomHouseFunction> logger)
        {
            _penguinService = penguinService ?? throw new ArgumentNullException(nameof(penguinService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Searches for authors by name and returns the unmodified JSON response from Penguin Random House API
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="authorName">Author name from route parameter</param>
        /// <returns>Unmodified JSON response from the API</returns>
        [Function("SearchPenguinAuthors")]
        public async Task<HttpResponseData> SearchAuthors(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "penguin/authors/{authorName}")] HttpRequestData req,
            string authorName)
        {
            _logger.LogInformation("SearchPenguinAuthors function processed a request.");

            try
            {
                // Validate authorName from route parameter
                if (string.IsNullOrEmpty(authorName))
                {
                    _logger.LogWarning("No author name provided in route parameter");
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Author name is required in the route parameter.");
                    return badRequestResponse;
                }

                // URL decode the author name in case it has special characters
                authorName = Uri.UnescapeDataString(authorName);

                _logger.LogInformation("Searching for author: {AuthorName}", authorName);

                // Call the Penguin Random House API
                using var jsonResult = await _penguinService.SearchAuthorsAsync(authorName);

                // Create response with the unmodified JSON
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");

                // Write the raw JSON response
                var jsonString = JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await response.WriteStringAsync(jsonString);

                _logger.LogInformation("Successfully returned Penguin Random House API response for author: {AuthorName}", authorName);
                return response;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request parameters");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync($"Invalid request: {ex.Message}");
                return badRequestResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Penguin Random House API");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadGateway);
                await errorResponse.WriteStringAsync($"External API error: {ex.Message}");
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchPenguinAuthors function");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An unexpected error occurred");
                return errorResponse;
            }
        }

        /// <summary>
        /// Gets titles by author key and returns the unmodified JSON response from Penguin Random House API
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="authorKey">Author key from route parameter</param>
        /// <returns>Unmodified JSON response from the API</returns>
        [Function("GetPenguinTitlesByAuthor")]
        public async Task<HttpResponseData> GetTitlesByAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "penguin/authors/{authorKey}/titles")] HttpRequestData req,
            string authorKey)
        {
            _logger.LogInformation("GetPenguinTitlesByAuthor function processed a request.");

            try
            {
                // Validate authorKey from route parameter
                if (string.IsNullOrEmpty(authorKey))
                {
                    _logger.LogWarning("No author key provided in route parameter");
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Author key is required in the route parameter.");
                    return badRequestResponse;
                }

                // URL decode the author key in case it has special characters
                authorKey = Uri.UnescapeDataString(authorKey);

                // Get optional parameters from query string
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var rowsParam = query["rows"] ?? "10";
                var startParam = query["start"] ?? "0";

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

                // Create response with the unmodified JSON
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");

                // Write the raw JSON response
                var jsonString = JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await response.WriteStringAsync(jsonString);

                _logger.LogInformation("Successfully returned Penguin Random House API response for author key: {AuthorKey}", authorKey);
                return response;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request parameters");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync($"Invalid request: {ex.Message}");
                return badRequestResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Penguin Random House API");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadGateway);
                await errorResponse.WriteStringAsync($"External API error: {ex.Message}");
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetPenguinTitlesByAuthor function");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An unexpected error occurred");
                return errorResponse;
            }
        }
    }
}