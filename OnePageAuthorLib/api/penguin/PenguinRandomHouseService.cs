using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Penguin
{
    /// <summary>
    /// Service implementation for calling Penguin Random House API
    /// </summary>
    public class PenguinRandomHouseService : IPenguinRandomHouseService
    {
        private readonly HttpClient _httpClient;
        private readonly IPenguinRandomHouseConfig _config;
        private readonly ILogger<PenguinRandomHouseService> _logger;

        public PenguinRandomHouseService(
            HttpClient httpClient,
            IPenguinRandomHouseConfig config,
            ILogger<PenguinRandomHouseService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Searches for authors by name and returns the raw JSON response
        /// </summary>
        /// <param name="authorName">Name of the author to search for</param>
        /// <returns>Raw JSON response from the API</returns>
        public async Task<JsonDocument> SearchAuthorsAsync(string authorName)
        {
            if (string.IsNullOrEmpty(authorName))
            {
                throw new ArgumentException("Author name cannot be null or empty", nameof(authorName));
            }

            try
            {
                // Build the URL with parameter substitution (matching the TypeScript version)
                var endpoint = _config.SearchApiEndpoint
                    .Replace("{domain}", _config.Domain)
                    .Replace("{query}", Uri.EscapeDataString(authorName))
                    .Replace("{api_key}", _config.ApiKey);

                var url = $"{_config.ApiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                _logger.LogInformation("Calling Penguin Random House API: {Url}", url);

                // Make the API call
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Penguin Random House API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Penguin Random House API error: {response.StatusCode}");
                }

                // Get the JSON response
                var jsonString = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("API response received: {Length} characters", jsonString.Length);

                // Parse and return as JsonDocument to preserve the original structure
                return JsonDocument.Parse(jsonString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search for authors with name: {AuthorName}", authorName);
                throw new InvalidOperationException($"Failed to search for authors: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets titles by author key and returns the raw JSON response
        /// </summary>
        /// <param name="authorKey">Author key from previous search</param>
        /// <param name="rows">Number of rows to return</param>
        /// <param name="start">Starting position for pagination</param>
        /// <returns>Raw JSON response from the API</returns>
        public async Task<JsonDocument> GetTitlesByAuthorAsync(string authorKey, int rows, int start = 0)
        {
            if (string.IsNullOrEmpty(authorKey))
            {
                throw new ArgumentException("Author key cannot be null or empty", nameof(authorKey));
            }

            if (rows <= 0)
            {
                throw new ArgumentException("Rows must be greater than 0", nameof(rows));
            }

            if (start < 0)
            {
                throw new ArgumentException("Start must be greater than or equal to 0", nameof(start));
            }

            try
            {
                // Build the URL with parameter substitution
                var endpoint = _config.ListTitlesByAuthorApiEndpoint
                    .Replace("{domain}", _config.Domain)
                    .Replace("{authorKey}", Uri.EscapeDataString(authorKey))
                    .Replace("{rows}", rows.ToString())
                    .Replace("{start}", start.ToString())
                    .Replace("{api_key}", _config.ApiKey);

                var url = $"{_config.ApiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

                _logger.LogInformation("Calling Penguin Random House API for titles: {Url}", url);

                // Make the API call
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Penguin Random House API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Penguin Random House API error: {response.StatusCode}");
                }

                // Get the JSON response
                var jsonString = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("API response received: {Length} characters", jsonString.Length);

                // Parse and return as JsonDocument to preserve the original structure
                return JsonDocument.Parse(jsonString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get titles for author key: {AuthorKey}", authorKey);
                throw new InvalidOperationException($"Failed to get titles for author: {ex.Message}", ex);
            }
        }
    }
}