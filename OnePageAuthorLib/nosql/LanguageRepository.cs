using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for Language entities, supports querying by code and request language.
    /// </summary>
    public class LanguageRepository : ILanguageRepository
    {
        protected readonly Container _container;

        /// <summary>
        /// Initializes a new instance of the LanguageRepository class.
        /// </summary>
        /// <param name="container">The Cosmos DB container for Language entities.</param>
        public LanguageRepository(Container container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Initializes a new instance of the LanguageRepository class.
        /// </summary>
        /// <param name="container">The IDataContainer wrapper for Language entities.</param>
        public LanguageRepository(IDataContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            
            // Store the wrapper, but we need actual Container for some operations
            // We'll use the wrapper methods instead
            if (container is CosmosContainerWrapper wrapper)
            {
                // Extract underlying container via reflection
                var field = wrapper.GetType().GetField("_container", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _container = (Container?)field?.GetValue(wrapper) 
                    ?? throw new ArgumentException("Could not extract Container from CosmosContainerWrapper");
            }
            else
            {
                throw new ArgumentException("IDataContainer must be a CosmosContainerWrapper");
            }
        }

        /// <summary>
        /// Gets an entity by its string id.
        /// </summary>
        /// <param name="id">The string id of the entity.</param>
        /// <returns>The entity if found, otherwise null.</returns>
        public async Task<Language?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

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
        /// Gets all entities in the container.
        /// </summary>
        /// <returns>A list of all entities.</returns>
        public async Task<IList<Language>> GetAllAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c ORDER BY c.RequestLanguage, c.Name");

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
        /// Adds a new entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The added entity.</returns>
        public async Task<Language> AddAsync(Language entity)
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
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The updated entity.</returns>
        public async Task<Language> UpdateAsync(Language entity)
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
        /// Deletes an entity by its string id.
        /// </summary>
        /// <param name="id">The string id of the entity.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        public async Task<bool> DeleteAsync(string id)
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
    }
}
