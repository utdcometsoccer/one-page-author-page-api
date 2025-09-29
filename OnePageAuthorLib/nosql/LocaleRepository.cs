using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for Locale entities, supports querying by id.
    /// </summary>
    public class LocaleRepository : GenericRepository<Locale>, ILocaleRepository
    {

        public LocaleRepository(Container container) : base(container)
        {

        }

        public LocaleRepository(IDataContainer container) : base(container)
        {

        }

        public async Task<IList<Locale>> GetAllAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var results = new List<Locale>();
            using (var iterator = _container.GetItemQueryIterator<Locale>(query))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.Resource);
                }
            }
            return results;
        }

        /// <summary>
        /// Gets locales by language name and optional region name.
        /// </summary>
        public async Task<IList<Locale>> GetByLanguageAndRegionAsync(string languageName, string? regionName = null)
        {
            QueryDefinition query;
            if (!string.IsNullOrWhiteSpace(regionName))
            {
                query = new QueryDefinition("SELECT * FROM c WHERE c.LanguageName = @lang AND c.RegionName = @region")
                    .WithParameter("@lang", languageName)
                    .WithParameter("@region", regionName);
            }
            else
            {
                query = new QueryDefinition("SELECT * FROM c WHERE c.LanguageName = @lang")
                    .WithParameter("@lang", languageName);
            }
            var results = new List<Locale>();
            using (var iterator = _container.GetItemQueryIterator<Locale>(query))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.Resource);
                }
            }
            return results;
        }

        public async Task<Locale?> GetByIdAsync(string id)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);
            using (var iterator = _container.GetItemQueryIterator<Locale>(query))
            {
                return iterator.HasMoreResults ? (await iterator.ReadNextAsync()).FirstOrDefault() : null;
            }
        }
    }
}
