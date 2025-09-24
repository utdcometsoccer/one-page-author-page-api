using Microsoft.Extensions.Configuration;

namespace InkStainedWretch.OnePageAuthorLib.API.Penguin
{
    /// <summary>
    /// Configuration implementation for Penguin Random House API settings
    /// Reads settings from local configuration (appsettings.json, local.settings.json, environment variables)
    /// </summary>
    public class PenguinRandomHouseConfig : IPenguinRandomHouseConfig
    {
        private readonly IConfiguration _configuration;

        public PenguinRandomHouseConfig(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Base URL for the Penguin Random House API
        /// </summary>
        public string ApiUrl => GetRequiredConfig("PENGUIN_RANDOM_HOUSE_API_URL");

        /// <summary>
        /// API Key for authentication
        /// </summary>
        public string ApiKey => GetRequiredConfig("PENGUIN_RANDOM_HOUSE_API_KEY");

        /// <summary>
        /// Domain for API requests (e.g., "PRH.US")
        /// </summary>
        public string Domain => GetRequiredConfig("PENGUIN_RANDOM_HOUSE_API_DOMAIN");

        /// <summary>
        /// Search API endpoint template with placeholders
        /// </summary>
        public string SearchApiEndpoint => GetRequiredConfig("PENGUIN_RANDOM_HOUSE_SEARCH_API");

        /// <summary>
        /// List titles by author API endpoint template with placeholders
        /// </summary>
        public string ListTitlesByAuthorApiEndpoint => GetRequiredConfig("PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API");

        /// <summary>
        /// Base URL for Penguin Random House website
        /// </summary>
        public string WebsiteUrl => GetRequiredConfig("PENGUIN_RANDOM_HOUSE_URL");

        /// <summary>
        /// Gets a required configuration value and throws if not found
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>Configuration value</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration key is missing or empty</exception>
        private string GetRequiredConfig(string key)
        {
            var value = _configuration[key];
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"Required configuration '{key}' is missing or empty. Please check your local.settings.json or environment variables.");
            }
            return value;
        }
    }
}