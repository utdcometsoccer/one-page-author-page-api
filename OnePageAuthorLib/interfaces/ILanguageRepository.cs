using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Interface for LanguageRepository, supports querying languages by code and request language.
    /// </summary>
    public interface ILanguageRepository : IStringGenericRepository<Language>
    {
        /// <summary>
        /// Gets a language by its ISO 639-1 code and request language.
        /// </summary>
        /// <param name="code">The ISO 639-1 two-letter language code (e.g., "en", "es").</param>
        /// <param name="requestLanguage">The language in which to return the name (e.g., "en", "es").</param>
        /// <returns>The matching Language entity, or null if not found.</returns>
        Task<Language?> GetByCodeAndRequestLanguageAsync(string code, string requestLanguage);

        /// <summary>
        /// Gets all languages localized for the specified request language.
        /// </summary>
        /// <param name="requestLanguage">The language in which to return names (e.g., "en", "es").</param>
        /// <returns>List of Language entities for the specified request language.</returns>
        Task<IList<Language>> GetByRequestLanguageAsync(string requestLanguage);

        /// <summary>
        /// Checks if a language code exists for the given request language.
        /// </summary>
        /// <param name="code">The ISO 639-1 two-letter language code to check.</param>
        /// <param name="requestLanguage">The request language context.</param>
        /// <returns>True if the language exists, false otherwise.</returns>
        Task<bool> ExistsByCodeAsync(string code, string requestLanguage);
    }
}
