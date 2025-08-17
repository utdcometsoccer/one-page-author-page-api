using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Provides methods for retrieving locale data from the data source.
    /// </summary>
    public interface ILocaleDataService
    {
        /// <summary>
        /// Retrieves a list of <see cref="Locale"/> objects based on language and region parameters, with fallback logic.
        /// </summary>
        /// <param name="languageName">The language code (e.g., "en"). If null, uses current culture.</param>
        /// <param name="regionName">The region code (e.g., "US"). If null, uses current culture or falls back.</param>
        /// <returns>A list of <see cref="Locale"/> objects matching the criteria or fallback logic.</returns>
        Task<List<Locale>> GetLocalesAsync(string? languageName = null, string? regionName = null);
    }
}
