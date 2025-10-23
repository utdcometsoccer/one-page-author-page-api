using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service interface for managing Country operations.
    /// </summary>
    public interface ICountryService
    {
        /// <summary>
        /// Gets all countries for a specific language code.
        /// </summary>
        /// <param name="language">The language code (e.g., "en", "es", "fr", "ar", "zh-CN", "zh-TW").</param>
        /// <returns>List of Country entities for the specified language.</returns>
        Task<IList<Country>> GetCountriesByLanguageAsync(string language);

        /// <summary>
        /// Gets a country by its ISO code and language.
        /// </summary>
        /// <param name="code">The ISO 3166-1 alpha-2 country code (e.g., "US", "CA").</param>
        /// <param name="language">The language code.</param>
        /// <returns>The matching Country entity, or null if not found.</returns>
        Task<Country?> GetCountryByCodeAndLanguageAsync(string code, string language);

        /// <summary>
        /// Creates a new country.
        /// </summary>
        /// <param name="country">The Country entity to create.</param>
        /// <returns>The created Country entity.</returns>
        Task<Country> CreateCountryAsync(Country country);

        /// <summary>
        /// Updates an existing country.
        /// </summary>
        /// <param name="country">The Country entity to update.</param>
        /// <returns>The updated Country entity.</returns>
        Task<Country> UpdateCountryAsync(Country country);

        /// <summary>
        /// Deletes a country by its ID.
        /// </summary>
        /// <param name="id">The ID of the Country to delete.</param>
        /// <param name="language">The language (partition key) of the Country to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        Task<bool> DeleteCountryAsync(string id, string language);

        /// <summary>
        /// Gets all country entities.
        /// </summary>
        /// <returns>List of all Country entities.</returns>
        Task<IList<Country>> GetAllCountriesAsync();

        /// <summary>
        /// Deletes all country entities.
        /// </summary>
        /// <returns>The number of entities deleted.</returns>
        Task<int> DeleteAllCountriesAsync();
    }
}
