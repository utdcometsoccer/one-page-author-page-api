using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Interface for LocaleRepository, supports querying by id and other properties.
    /// </summary>
    public interface ILocaleRepository : IGenericRepository<Locale>
    {
    /// <summary>
    /// Gets all locales (async).
    /// </summary>
    Task<IList<Locale>> GetAllAsync();

        /// <summary>
        /// Gets locales by language name and optional region name (async).
        /// </summary>
        Task<IList<Locale>> GetByLanguageAndRegionAsync(string languageName, string? regionName = null);

        /// <summary>
        /// Gets a locale by its id (async).
        /// </summary>
        Task<Locale?> GetByIdAsync(string id);
    }
}
