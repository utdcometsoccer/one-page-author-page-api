using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Generic repository for entities with string-based id.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public class StringGenericRepository<TEntity> : IStringGenericRepository<TEntity> where TEntity : class
    {
        protected readonly Container _container;

        public StringGenericRepository(Container container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Gets an entity by its string id.
        /// </summary>
        /// <param name="id">The string id of the entity.</param>
        /// <returns>The entity if found, otherwise null.</returns>
        public async Task<TEntity?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            try
            {
                var response = await _container.ReadItemAsync<TEntity>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all entities in the container.
        /// </summary>
        /// <returns>A list of all entities.</returns>
        public async Task<IList<TEntity>> GetAllAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var results = new List<TEntity>();
            
            using var iterator = _container.GetItemQueryIterator<TEntity>(query);
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
        public async Task<TEntity> AddAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Use reflection to get the id property for partitioning
            var idProperty = entity.GetType().GetProperty("id");
            var id = idProperty?.GetValue(entity)?.ToString();
            
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Entity must have a non-empty id property");

            var response = await _container.CreateItemAsync(entity, new PartitionKey(id));
            return response.Resource;
        }

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The updated entity.</returns>
        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Use reflection to get the id property for partitioning
            var idProperty = entity.GetType().GetProperty("id");
            var id = idProperty?.GetValue(entity)?.ToString();
            
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Entity must have a non-empty id property");

            var response = await _container.ReplaceItemAsync(entity, id, new PartitionKey(id));
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
                await _container.DeleteItemAsync<TEntity>(id, new PartitionKey(id));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}