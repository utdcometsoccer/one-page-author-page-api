using System.Text.Json;

namespace InkStainedWretch.OnePageAuthorLib.API.Penguin
{
    /// <summary>
    /// Service interface for interacting with Penguin Random House API
    /// </summary>
    public interface IPenguinRandomHouseService
    {
        /// <summary>
        /// Searches for authors by name and returns the raw JSON response
        /// </summary>
        /// <param name="authorName">Name of the author to search for</param>
        /// <returns>Raw JSON response from the API</returns>
        Task<JsonDocument> SearchAuthorsAsync(string authorName);

        /// <summary>
        /// Gets titles by author key and returns the raw JSON response
        /// </summary>
        /// <param name="authorKey">Author key from previous search</param>
        /// <param name="rows">Number of rows to return</param>
        /// <param name="start">Starting position for pagination</param>
        /// <returns>Raw JSON response from the API</returns>
        Task<JsonDocument> GetTitlesByAuthorAsync(string authorKey, int rows, int start = 0);
    }
}