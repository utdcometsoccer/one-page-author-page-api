using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Generic repository for entities with Guid-based id and authorId, converting to string as necessary.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly IDataContainer _container;

        public GenericRepository(Container container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "GenericRepository: The provided Cosmos DB container is null. Ensure you are passing a valid container instance.");
            _container = new CosmosContainerWrapper(container);
        }

        public GenericRepository(IDataContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<TEntity?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _container.ReadItemAsync<TEntity>(id.ToString(), new PartitionKey(id.ToString()));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IList<TEntity>> GetByAuthorIdAsync(Guid authorId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.AuthorID = @authorId")
                .WithParameter("@authorId", authorId.ToString());
            var results = new List<TEntity>();
            using (var iterator = _container.GetItemQueryIterator<TEntity>(query))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response.Resource);
                }
            }
            return results;
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            var response = await _container.CreateItemAsync(entity);
            return response.Resource;
        }

        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            // Assumes entity has an 'id' property as string
            var idProp = typeof(TEntity).GetProperty("id");
            if (idProp == null)
                throw new InvalidOperationException("Entity must have an 'id' property.");
            var idValue = idProp.GetValue(entity)?.ToString();
            if (string.IsNullOrEmpty(idValue))
                throw new InvalidOperationException("Entity 'id' property must not be null or empty.");
            var response = await _container.ReplaceItemAsync(entity, idValue, new PartitionKey(idValue));
            return response.Resource;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                await _container.DeleteItemAsync<TEntity>(id.ToString(), new PartitionKey(id.ToString()));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
