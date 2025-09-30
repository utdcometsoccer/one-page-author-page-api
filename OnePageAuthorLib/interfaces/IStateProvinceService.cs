using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service interface for managing StateProvince operations.
    /// </summary>
    public interface IStateProvinceService
    {
        /// <summary>
        /// Gets a state or province by its ISO 3166-2 code.
        /// </summary>
        /// <param name="code">The ISO 3166-2 code (e.g., "US-CA" for California).</param>
        /// <returns>The matching StateProvince entity, or null if not found.</returns>
        Task<StateProvince?> GetStateProvinceByCodeAsync(string code);

        /// <summary>
        /// Gets states or provinces by name (partial match, case-insensitive).
        /// </summary>
        /// <param name="name">The name or partial name of the state/province.</param>
        /// <returns>List of matching StateProvince entities.</returns>
        Task<IList<StateProvince>> SearchStateProvincesByNameAsync(string name);

        /// <summary>
        /// Gets all states or provinces for a specific country code.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <returns>List of StateProvince entities for the specified country.</returns>
        Task<IList<StateProvince>> GetStateProvincesByCountryAsync(string countryCode);

        /// <summary>
        /// Validates if a state or province code is valid and exists.
        /// </summary>
        /// <param name="code">The ISO 3166-2 code to validate.</param>
        /// <returns>True if the code is valid and exists, false otherwise.</returns>
        Task<bool> ValidateStateProvinceCodeAsync(string code);

        /// <summary>
        /// Creates a new state or province.
        /// </summary>
        /// <param name="stateProvince">The StateProvince entity to create.</param>
        /// <returns>The created StateProvince entity.</returns>
        Task<StateProvince> CreateStateProvinceAsync(StateProvince stateProvince);

        /// <summary>
        /// Updates an existing state or province.
        /// </summary>
        /// <param name="stateProvince">The StateProvince entity to update.</param>
        /// <returns>The updated StateProvince entity.</returns>
        Task<StateProvince> UpdateStateProvinceAsync(StateProvince stateProvince);

        /// <summary>
        /// Deletes a state or province by its ID.
        /// </summary>
        /// <param name="id">The ID of the StateProvince to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        Task<bool> DeleteStateProvinceAsync(string id);

        /// <summary>
        /// Gets states or provinces by culture code.
        /// </summary>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <returns>List of StateProvince entities for the specified culture.</returns>
        Task<IList<StateProvince>> GetStateProvincesByCultureAsync(string culture);

        /// <summary>
        /// Gets states or provinces by country code and culture.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <returns>List of StateProvince entities for the specified country and culture.</returns>
        Task<IList<StateProvince>> GetStateProvincesByCountryAndCultureAsync(string countryCode, string culture);
    }
}