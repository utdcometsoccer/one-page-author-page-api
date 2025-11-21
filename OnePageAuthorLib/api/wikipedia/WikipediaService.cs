using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Wikipedia
{
    /// <summary>
    /// Service implementation for calling Wikipedia REST API and MediaWiki API
    /// </summary>
    public class WikipediaService : IWikipediaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WikipediaService> _logger;

        public WikipediaService(
            HttpClient httpClient,
            ILogger<WikipediaService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets structured facts about a person from Wikipedia using both REST API and MediaWiki API
        /// </summary>
        /// <param name="personName">Name of the person to search for</param>
        /// <param name="language">Wikipedia language code (e.g., "en", "es", "fr")</param>
        /// <returns>Structured information about the person</returns>
        public async Task<WikipediaPersonFactsResponse> GetPersonFactsAsync(string personName, string language = "en")
        {
            if (string.IsNullOrWhiteSpace(personName))
            {
                throw new ArgumentException("Person name cannot be null or empty", nameof(personName));
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Language code cannot be null or empty", nameof(language));
            }

            // Normalize language code
            language = language.ToLowerInvariant();

            // Prepare person name for API calls
            // REST API expects underscores for spaces in the page title
            // MediaWiki API expects spaces in the title parameter
            var normalizedName = personName.Trim();
            var restApiName = normalizedName.Replace(' ', '_');

            _logger.LogInformation("Fetching Wikipedia facts for person: {PersonName} in language: {Language}", personName, language);

            try
            {
                // Fetch data from both APIs concurrently
                var summaryTask = GetSummaryAsync(restApiName, language);
                var extractTask = GetExtractAsync(normalizedName, language);

                await Task.WhenAll(summaryTask, extractTask);

                var summary = await summaryTask;
                var extract = await extractTask;

                // Combine data from both sources
                var response = new WikipediaPersonFactsResponse
                {
                    Title = summary.Title ?? string.Empty,
                    Description = summary.Description ?? string.Empty,
                    Extract = summary.Extract ?? string.Empty,
                    LeadParagraph = extract ?? string.Empty,
                    Thumbnail = summary.Thumbnail,
                    CanonicalUrl = summary.CanonicalUrl ?? string.Empty,
                    Language = language
                };

                _logger.LogInformation("Successfully retrieved Wikipedia facts for: {PersonName}", personName);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Wikipedia APIs for person: {PersonName} in language: {Language}", personName, language);
                throw; // Re-throw to preserve exception type for proper handling in Azure Function
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting Wikipedia facts for person: {PersonName} in language: {Language}", personName, language);
                throw; // Re-throw to preserve exception type
            }
        }

        /// <summary>
        /// Gets summary information from Wikipedia REST API
        /// </summary>
        /// <param name="pageTitle">Page title with underscores for spaces (will be URL encoded)</param>
        /// <param name="language">Language code</param>
        private async Task<(string? Title, string? Description, string? Extract, ThumbnailInfo? Thumbnail, string? CanonicalUrl)> GetSummaryAsync(string pageTitle, string language)
        {
            // URL encode the page title for the REST API
            var encodedTitle = Uri.EscapeDataString(pageTitle);
            var url = $"https://{language}.wikipedia.org/api/rest_v1/page/summary/{encodedTitle}";
            _logger.LogDebug("Calling Wikipedia REST API: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Wikipedia REST API returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);

                // Return empty data instead of throwing to allow partial results
                return (null, null, null, null, null);
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            // Extract fields from the REST API response
            var title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
            var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
            var extract = root.TryGetProperty("extract", out var extractProp) ? extractProp.GetString() : null;
            var canonicalUrl = root.TryGetProperty("content_urls", out var urlsProp) &&
                               urlsProp.TryGetProperty("desktop", out var desktopProp) &&
                               desktopProp.TryGetProperty("page", out var pageProp)
                               ? pageProp.GetString()
                               : null;

            ThumbnailInfo? thumbnail = null;
            if (root.TryGetProperty("thumbnail", out var thumbProp))
            {
                thumbnail = new ThumbnailInfo
                {
                    Source = thumbProp.TryGetProperty("source", out var sourceProp) ? sourceProp.GetString() ?? string.Empty : string.Empty,
                    Width = thumbProp.TryGetProperty("width", out var widthProp) ? widthProp.GetInt32() : 0,
                    Height = thumbProp.TryGetProperty("height", out var heightProp) ? heightProp.GetInt32() : 0
                };
            }

            return (title, description, extract, thumbnail, canonicalUrl);
        }

        /// <summary>
        /// Gets lead paragraph from MediaWiki API
        /// </summary>
        /// <param name="pageTitle">Page title with spaces (will be URL encoded)</param>
        /// <param name="language">Language code</param>
        private async Task<string?> GetExtractAsync(string pageTitle, string language)
        {
            // URL encode the page title for query parameter
            var encodedTitle = Uri.EscapeDataString(pageTitle);
            var url = $"https://{language}.wikipedia.org/w/api.php?action=query&prop=extracts&exintro&explaintext&titles={encodedTitle}&format=json";
            _logger.LogDebug("Calling MediaWiki API: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("MediaWiki API returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);

                // Return null instead of throwing to allow partial results
                return null;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            // Navigate through the MediaWiki API response structure
            if (root.TryGetProperty("query", out var queryProp) &&
                queryProp.TryGetProperty("pages", out var pagesProp))
            {
                // The pages object has page IDs as keys, we need to get the first one
                foreach (var page in pagesProp.EnumerateObject())
                {
                    if (page.Value.TryGetProperty("extract", out var extractProp))
                    {
                        return extractProp.GetString();
                    }
                }
            }

            return null;
        }
    }
}
