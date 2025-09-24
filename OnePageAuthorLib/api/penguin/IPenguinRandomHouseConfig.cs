namespace InkStainedWretch.OnePageAuthorLib.API.Penguin
{
    /// <summary>
    /// Configuration interface for Penguin Random House API settings
    /// </summary>
    public interface IPenguinRandomHouseConfig
    {
        /// <summary>
        /// Base URL for the Penguin Random House API
        /// </summary>
        string ApiUrl { get; }

        /// <summary>
        /// API Key for authentication
        /// </summary>
        string ApiKey { get; }

        /// <summary>
        /// Domain for API requests (e.g., "PRH.US")
        /// </summary>
        string Domain { get; }

        /// <summary>
        /// Search API endpoint template with placeholders
        /// </summary>
        string SearchApiEndpoint { get; }

        /// <summary>
        /// List titles by author API endpoint template with placeholders
        /// </summary>
        string ListTitlesByAuthorApiEndpoint { get; }

        /// <summary>
        /// Base URL for Penguin Random House website
        /// </summary>
        string WebsiteUrl { get; }
    }
}