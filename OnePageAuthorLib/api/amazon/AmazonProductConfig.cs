using Microsoft.Extensions.Configuration;

namespace InkStainedWretch.OnePageAuthorLib.API.Amazon
{
    /// <summary>
    /// Configuration implementation for Amazon Product Advertising API settings
    /// Reads settings from local configuration (appsettings.json, local.settings.json, environment variables)
    /// </summary>
    public class AmazonProductConfig : IAmazonProductConfig
    {
        private readonly IConfiguration _configuration;

        public AmazonProductConfig(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// AWS Access Key ID for authentication
        /// </summary>
        public string AccessKey => GetRequiredConfig("AMAZON_PRODUCT_ACCESS_KEY");

        /// <summary>
        /// AWS Secret Access Key for authentication
        /// </summary>
        public string SecretKey => GetRequiredConfig("AMAZON_PRODUCT_SECRET_KEY");

        /// <summary>
        /// Amazon Associates Partner Tag (e.g., "yourtag-20")
        /// </summary>
        public string PartnerTag => GetRequiredConfig("AMAZON_PRODUCT_PARTNER_TAG");

        /// <summary>
        /// AWS Region for API requests (e.g., "us-east-1")
        /// </summary>
        public string Region => GetRequiredConfig("AMAZON_PRODUCT_REGION");

        /// <summary>
        /// Amazon marketplace domain (e.g., "www.amazon.com")
        /// </summary>
        public string Marketplace => GetRequiredConfig("AMAZON_PRODUCT_MARKETPLACE");

        /// <summary>
        /// API endpoint URL
        /// </summary>
        public string ApiEndpoint => GetRequiredConfig("AMAZON_PRODUCT_API_ENDPOINT");

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
