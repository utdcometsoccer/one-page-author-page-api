using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Repository interface for Country data access operations.
    /// Uses string-based IDs for better compatibility.
    /// </summary>
    public interface ICountryRepository
    {
        /// <summary>
        /// Gets all countries for a specific language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>List of Country entities for the specified language.</returns>
        Task<IList<Country>> GetByLanguageAsync(string language);

        /// <summary>
        /// Gets a country by its code and language.
        /// </summary>
        /// <param name="code">The ISO country code.</param>
        /// <param name="language">The language code.</param>
        /// <returns>The matching Country entity, or null if not found.</returns>
        Task<Country?> GetByCodeAndLanguageAsync(string code, string language);

        /// <summary>
        /// Adds a new country to the repository.
        /// </summary>
        /// <param name="country">The Country entity to add.</param>
        /// <returns>The added Country entity.</returns>
        Task<Country> AddAsync(Country country);

        /// <summary>
        /// Updates an existing country.
        /// </summary>
        /// <param name="country">The Country entity to update.</param>
        /// <returns>The updated Country entity.</returns>
        Task<Country> UpdateAsync(Country country);

        /// <summary>
        /// Deletes a country by its ID and language.
        /// </summary>
        /// <param name="id">The ID of the country to delete.</param>
        /// <param name="language">The language (partition key) of the country to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        Task<bool> DeleteAsync(string id, string language);

        /// <summary>
        /// Checks if a country exists by its code and language.
        /// </summary>
        /// <param name="code">The ISO country code.</param>
        /// <param name="language">The language code.</param>
        /// <returns>True if the country exists, false otherwise.</returns>
        Task<bool> ExistsByCodeAndLanguageAsync(string code, string language);

        /// <summary>
        /// Gets all countries across all languages.
        /// </summary>
        /// <returns>List of all Country entities.</returns>
        Task<IList<Country>> GetAllAsync();
    }
}
