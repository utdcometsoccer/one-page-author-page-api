namespace InkStainedWretch.OnePageAuthorLib.API.Amazon
{
    /// <summary>
    /// Configuration interface for Amazon Product Advertising API settings
    /// </summary>
    public interface IAmazonProductConfig
    {
        /// <summary>
        /// AWS Access Key ID for authentication
        /// </summary>
        string AccessKey { get; }

        /// <summary>
        /// AWS Secret Access Key for authentication
        /// </summary>
        string SecretKey { get; }

        /// <summary>
        /// Amazon Associates Partner Tag (e.g., "yourtag-20")
        /// </summary>
        string PartnerTag { get; }

        /// <summary>
        /// AWS Region for API requests (e.g., "us-east-1")
        /// </summary>
        string Region { get; }

        /// <summary>
        /// Amazon marketplace domain (e.g., "www.amazon.com")
        /// </summary>
        string Marketplace { get; }

        /// <summary>
        /// API endpoint URL
        /// </summary>
        string ApiEndpoint { get; }
    }
}
