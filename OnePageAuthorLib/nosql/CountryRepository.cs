using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for Country data access using Cosmos DB with string-based IDs.
    /// </summary>
    public class CountryRepository : ICountryRepository
    {
        private readonly Container _container;

        public CountryRepository(Container container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Gets all countries for a specific language.
        /// </summary>
        public async Task<IList<Country>> GetByLanguageAsync(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return new List<Country>();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Language = @language ORDER BY c.Name")
                .WithParameter("@language", language);

            var results = new List<Country>();
            using var iterator = _container.GetItemQueryIterator<Country>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        /// <summary>
        /// Gets a country by its code and language.
        /// </summary>
        public async Task<Country?> GetByCodeAndLanguageAsync(string code, string language)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(language))
                return null;

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Code = @code AND c.Language = @language")
                .WithParameter("@code", code.ToUpperInvariant())
                .WithParameter("@language", language);

            using var iterator = _container.GetItemQueryIterator<Country>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Adds a new country to the repository.
        /// </summary>
        public async Task<Country> AddAsync(Country country)
        {
            if (country == null)
                throw new ArgumentNullException(nameof(country));

            if (string.IsNullOrWhiteSpace(country.id))
                country.id = Guid.NewGuid().ToString();

            var response = await _container.CreateItemAsync(
                country,
                new PartitionKey(country.Language));

            return response.Resource;
        }

        /// <summary>
        /// Updates an existing country.
        /// </summary>
        public async Task<Country> UpdateAsync(Country country)
        {
            if (country == null)
                throw new ArgumentNullException(nameof(country));

            if (string.IsNullOrWhiteSpace(country.id))
                throw new ArgumentException("Country id is required for update", nameof(country));

            var response = await _container.ReplaceItemAsync(
                country,
                country.id,
                new PartitionKey(country.Language));

            return response.Resource;
        }

        /// <summary>
        /// Deletes a country by its ID and language.
        /// </summary>
        public async Task<bool> DeleteAsync(string id, string language)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(language))
                return false;

            try
            {
                await _container.DeleteItemAsync<Country>(id, new PartitionKey(language));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a country exists by its code and language.
        /// </summary>
        public async Task<bool> ExistsByCodeAndLanguageAsync(string code, string language)
        {
            var country = await GetByCodeAndLanguageAsync(code, language);
            return country != null;
        }

        /// <summary>
        /// Gets all countries across all languages.
        /// </summary>
        public async Task<IList<Country>> GetAllAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c ORDER BY c.Language, c.Name");

            var results = new List<Country>();
            using var iterator = _container.GetItemQueryIterator<Country>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }
    }
}
