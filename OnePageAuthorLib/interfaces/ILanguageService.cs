using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service interface for managing Language operations.
    /// </summary>
    public interface ILanguageService
    {
        /// <summary>
        /// Gets a language by its ISO 639-1 code and request language.
        /// </summary>
        /// <param name="code">The ISO 639-1 two-letter language code (e.g., "en", "es").</param>
        /// <param name="requestLanguage">The language in which to return the name (e.g., "en", "es").</param>
        /// <returns>The matching Language entity, or null if not found.</returns>
        Task<Language?> GetLanguageByCodeAsync(string code, string requestLanguage);

        /// <summary>
        /// Gets all languages localized for the specified request language.
        /// </summary>
        /// <param name="requestLanguage">The language in which to return names (e.g., "en", "es").</param>
        /// <returns>List of Language entities for the specified request language.</returns>
        Task<IList<Language>> GetLanguagesByRequestLanguageAsync(string requestLanguage);

        /// <summary>
        /// Validates if a language code is valid and exists.
        /// </summary>
        /// <param name="code">The ISO 639-1 code to validate.</param>
        /// <param name="requestLanguage">The request language context.</param>
        /// <returns>True if the code is valid and exists, false otherwise.</returns>
        Task<bool> ValidateLanguageCodeAsync(string code, string requestLanguage);

        /// <summary>
        /// Creates a new language.
        /// </summary>
        /// <param name="language">The Language entity to create.</param>
        /// <returns>The created Language entity.</returns>
        Task<Language> CreateLanguageAsync(Language language);

        /// <summary>
        /// Updates an existing language.
        /// </summary>
        /// <param name="language">The Language entity to update.</param>
        /// <returns>The updated Language entity.</returns>
        Task<Language> UpdateLanguageAsync(Language language);

        /// <summary>
        /// Deletes a language by its ID.
        /// </summary>
        /// <param name="id">The ID of the Language to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        Task<bool> DeleteLanguageAsync(string id);

        /// <summary>
        /// Gets all language entities.
        /// </summary>
        /// <returns>List of all Language entities.</returns>
        Task<IList<Language>> GetAllLanguagesAsync();

        /// <summary>
        /// Deletes all language entities.
        /// </summary>
        /// <returns>The number of entities deleted.</returns>
        Task<int> DeleteAllLanguagesAsync();
    }
}
