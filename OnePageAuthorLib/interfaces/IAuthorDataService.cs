namespace InkStainedWretch.OnePageAuthorAPI.API
{
    public interface IAuthorDataService
    {
        /// <summary>
        /// Gets the first author matching the provided domain and locale, along with all associated data, as an AuthorResponse.
        /// </summary>
        /// <param name="topLevelDomain">Top-level domain (e.g., "com").</param>
        /// <param name="secondLevelDomain">Second-level domain (e.g., "example").</param>
        /// <param name="languageName">Language name (e.g., "en").</param>
        /// <param name="regionName">Optional region name (e.g., "US").</param>
        /// <returns>AuthorResponse object with all associated data, or null if not found.</returns>
        Task<AuthorResponse?> GetAuthorWithDataAsync(string topLevelDomain, string secondLevelDomain, string languageName, string? regionName = null);
    }
}
