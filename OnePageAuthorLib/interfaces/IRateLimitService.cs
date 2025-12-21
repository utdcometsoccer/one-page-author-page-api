namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service interface for rate limiting requests by IP address.
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Checks if a request from the given IP address should be allowed based on rate limits.
        /// </summary>
        /// <param name="ipAddress">IP address to check</param>
        /// <param name="endpoint">Endpoint identifier (e.g., "leads")</param>
        /// <returns>True if request is allowed, false if rate limit exceeded</returns>
        Task<bool> IsRequestAllowedAsync(string ipAddress, string endpoint);

        /// <summary>
        /// Records a request from the given IP address.
        /// </summary>
        /// <param name="ipAddress">IP address making the request</param>
        /// <param name="endpoint">Endpoint identifier</param>
        Task RecordRequestAsync(string ipAddress, string endpoint);

        /// <summary>
        /// Gets the number of remaining requests allowed for the IP address.
        /// </summary>
        /// <param name="ipAddress">IP address to check</param>
        /// <param name="endpoint">Endpoint identifier</param>
        /// <returns>Number of remaining requests</returns>
        Task<int> GetRemainingRequestsAsync(string ipAddress, string endpoint);
    }
}
