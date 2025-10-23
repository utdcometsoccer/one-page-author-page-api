using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for Language entities using string-based IDs with RequestLanguage as partition key.
    /// Inherits from StringGenericRepository for standard CRUD operations.
    /// </summary>
    public class StringLanguageRepository : StringGenericRepository<Language>, ILanguageRepository
    {
        /// <summary>
        /// Initializes a new instance of the StringLanguageRepository class.
        /// </summary>
        /// <param name="container">The Cosmos DB container for Language entities.</param>
        public StringLanguageRepository(Container container) : base(container)
        {
        }

        /// <summary>
        /// Override AddAsync to use RequestLanguage as partition key.
        /// </summary>
        public override async Task<Language> AddAsync(Language entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (string.IsNullOrWhiteSpace(entity.RequestLanguage))
                throw new ArgumentException("Language entity must have a non-empty RequestLanguage property");

            // Use RequestLanguage as partition key
            var partitionKey = new PartitionKey(entity.RequestLanguage);
            var response = await _container.CreateItemAsync(entity, partitionKey);
            return response.Resource;
        }

        /// <summary>
        /// Override UpdateAsync to use RequestLanguage as partition key.
        /// </summary>
        public override async Task<Language> UpdateAsync(Language entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (string.IsNullOrWhiteSpace(entity.id))
                throw new ArgumentException("Language entity must have a non-empty id property");

            if (string.IsNullOrWhiteSpace(entity.RequestLanguage))
                throw new ArgumentException("Language entity must have a non-empty RequestLanguage property");

            // Use RequestLanguage as partition key
            var partitionKey = new PartitionKey(entity.RequestLanguage);
            var response = await _container.ReplaceItemAsync(entity, entity.id, partitionKey);
            return response.Resource;
        }

        /// <summary>
        /// Override GetByIdAsync to handle partition key lookup.
        /// </summary>
        public override async Task<Language?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            // Since we don't know the partition key, we need to query by id
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            using var iterator = _container.GetItemQueryIterator<Language>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Gets a language by its ISO 639-1 code and request language.
        /// </summary>
        /// <param name="code">The ISO 639-1 two-letter language code (e.g., "en", "es").</param>
        /// <param name="requestLanguage">The language in which to return the name (e.g., "en", "es").</param>
        /// <returns>The matching Language entity, or null if not found.</returns>
        public async Task<Language?> GetByCodeAndRequestLanguageAsync(string code, string requestLanguage)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(requestLanguage))
                return null;

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Code = @code AND c.RequestLanguage = @requestLanguage")
                .WithParameter("@code", code.ToLowerInvariant())
                .WithParameter("@requestLanguage", requestLanguage.ToLowerInvariant());

            using var iterator = _container.GetItemQueryIterator<Language>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Gets all languages localized for the specified request language.
        /// </summary>
        /// <param name="requestLanguage">The language in which to return names (e.g., "en", "es").</param>
        /// <returns>List of Language entities for the specified request language.</returns>
        public async Task<IList<Language>> GetByRequestLanguageAsync(string requestLanguage)
        {
            if (string.IsNullOrWhiteSpace(requestLanguage))
                return new List<Language>();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.RequestLanguage = @requestLanguage ORDER BY c.Name")
                .WithParameter("@requestLanguage", requestLanguage.ToLowerInvariant());

            var results = new List<Language>();
            using var iterator = _container.GetItemQueryIterator<Language>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        /// <summary>
        /// Checks if a language code exists for the given request language.
        /// </summary>
        /// <param name="code">The ISO 639-1 two-letter language code to check.</param>
        /// <param name="requestLanguage">The request language context.</param>
        /// <returns>True if the language exists, false otherwise.</returns>
        public async Task<bool> ExistsByCodeAsync(string code, string requestLanguage)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(requestLanguage))
                return false;

            var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.Code = @code AND c.RequestLanguage = @requestLanguage")
                .WithParameter("@code", code.ToLowerInvariant())
                .WithParameter("@requestLanguage", requestLanguage.ToLowerInvariant());

            using var iterator = _container.GetItemQueryIterator<int>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault() > 0;
            }

            return false;
        }

        /// <summary>
        /// Override DeleteAsync to handle partition key lookup.
        /// </summary>
        public new async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            try
            {
                // First, find the entity to get its partition key
                var language = await GetByIdAsync(id);
                if (language == null || string.IsNullOrWhiteSpace(language.RequestLanguage))
                    return false;

                var partitionKey = new PartitionKey(language.RequestLanguage);
                await _container.DeleteItemAsync<Language>(id, partitionKey);
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}