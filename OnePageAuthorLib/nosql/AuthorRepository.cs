using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for Author entities, supports querying by domain and locale properties.
    /// </summary>
    public class AuthorRepository : GenericRepository<Author>, IAuthorRepository
    {
        private readonly Container _container;

        public AuthorRepository(Container container) : base(container)
        {
            _container = container;
        }

        /// <summary>
        /// Gets authors by TopLevelDomain, SecondLevelDomain, LanguageName, and RegionName.
        /// </summary>
        /// <param name="topLevelDomain">Top-level domain (e.g., "com").</param>
        /// <param name="secondLevelDomain">Second-level domain (e.g., "example").</param>
        /// <param name="languageName">Language name (e.g., "en").</param>
        /// <param name="regionName">Region name (e.g., "US").</param>
        /// <returns>List of matching Author entities.</returns>
        public async Task<IList<Author>> GetByDomainAndLocaleAsync(string topLevelDomain, string secondLevelDomain, string languageName, string regionName)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.TopLevelDomain = @tld AND c.SecondLevelDomain = @sld AND c.LanguageName = @lang AND c.RegionName = @region")
                .WithParameter("@tld", topLevelDomain)
                .WithParameter("@sld", secondLevelDomain)
                .WithParameter("@lang", languageName)
                .WithParameter("@region", regionName);
            var iterator = _container.GetItemQueryIterator<Author>(query);
            var results = new List<Author>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }
            return results;
        }
    }
}
