using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Interface for AuthorRepository, supports querying by domain and locale properties.
    /// </summary>
    public interface IAuthorRepository : IGenericRepository<Author>
    {
        /// <summary>
        /// Gets authors by TopLevelDomain and SecondLevelDomain where IsDefault is true.
        /// </summary>
        /// <param name="topLevelDomain">Top-level domain (e.g., "com").</param>
        /// <param name="secondLevelDomain">Second-level domain (e.g., "example").</param>
        /// <returns>List of matching Author entities with IsDefault true.</returns>
        Task<IList<Author>> GetByDomainAndDefaultAsync(string topLevelDomain, string secondLevelDomain);

        /// <summary>
        /// Gets authors by TopLevelDomain and SecondLevelDomain.
        /// </summary>
        /// <param name="topLevelDomain">Top-level domain (e.g., "com").</param>
        /// <param name="secondLevelDomain">Second-level domain (e.g., "example").</param>
        /// <returns>List of matching Author entities.</returns>
        Task<IList<Author>> GetByDomainAsync(string topLevelDomain, string secondLevelDomain);

        /// <summary>
        /// Gets authors by TopLevelDomain, SecondLevelDomain, LanguageName, and RegionName.
        /// </summary>
        /// <param name="topLevelDomain">Top-level domain (e.g., "com").</param>
        /// <param name="secondLevelDomain">Second-level domain (e.g., "example").</param>
        /// <param name="languageName">Language name (e.g., "en").</param>
        /// <param name="regionName">Region name (e.g., "US").</param>
        /// <returns>List of matching Author entities.</returns>
        Task<IList<Author>> GetByDomainAndLocaleAsync(string topLevelDomain, string secondLevelDomain, string languageName, string regionName);
    }
}
