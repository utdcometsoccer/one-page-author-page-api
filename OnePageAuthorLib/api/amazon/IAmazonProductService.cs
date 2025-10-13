using System.Text.Json;

namespace InkStainedWretch.OnePageAuthorLib.API.Amazon
{
    /// <summary>
    /// Service interface for interacting with Amazon Product Advertising API
    /// </summary>
    public interface IAmazonProductService
    {
        /// <summary>
        /// Searches for books by author name and returns the raw JSON response
        /// </summary>
        /// <param name="authorName">Name of the author to search for</param>
        /// <param name="itemPage">Page number for pagination (default: 1)</param>
        /// <returns>Raw JSON response from the API</returns>
        Task<JsonDocument> SearchBooksByAuthorAsync(string authorName, int itemPage = 1);
    }
}
