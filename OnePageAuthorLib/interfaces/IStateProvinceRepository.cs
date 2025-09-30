using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Interface for StateProvinceRepository, supports querying by code and name.
    /// </summary>
    public interface IStateProvinceRepository : IGenericRepository<StateProvince>
    {
        /// <summary>
        /// Gets a state or province by its ISO 3166-2 code.
        /// </summary>
        /// <param name="code">The ISO 3166-2 code (e.g., "US-CA" for California).</param>
        /// <returns>The matching StateProvince entity, or null if not found.</returns>
        Task<StateProvince?> GetByCodeAsync(string code);

        /// <summary>
        /// Gets states or provinces by name (partial match, case-insensitive).
        /// </summary>
        /// <param name="name">The name or partial name of the state/province.</param>
        /// <returns>List of matching StateProvince entities.</returns>
        Task<IList<StateProvince>> GetByNameAsync(string name);

        /// <summary>
        /// Gets all states or provinces for a specific country code.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <returns>List of StateProvince entities for the specified country.</returns>
        Task<IList<StateProvince>> GetByCountryAsync(string countryCode);

        /// <summary>
        /// Checks if a state or province code exists in the database.
        /// </summary>
        /// <param name="code">The ISO 3166-2 code to check.</param>
        /// <returns>True if the code exists, false otherwise.</returns>
        Task<bool> ExistsByCodeAsync(string code);

        /// <summary>
        /// Gets states or provinces by culture code.
        /// </summary>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <returns>List of StateProvince entities for the specified culture.</returns>
        Task<IList<StateProvince>> GetByCultureAsync(string culture);

        /// <summary>
        /// Gets states or provinces by country code and culture.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <returns>List of StateProvince entities for the specified country and culture.</returns>
        Task<IList<StateProvince>> GetByCountryAndCultureAsync(string countryCode, string culture);
    }
}