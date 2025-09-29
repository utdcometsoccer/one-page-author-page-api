using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    public class ImageStorageTierRepository : GenericRepository<ImageStorageTier>, IImageStorageTierRepository
    {
        public ImageStorageTierRepository(Container container) : base(container) { }
        public ImageStorageTierRepository(IDataContainer container) : base(container) { }

        public async Task<IList<ImageStorageTier>> GetAllAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var results = new List<ImageStorageTier>();
            using var iterator = _container.GetItemQueryIterator<ImageStorageTier>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }
            return results;
        }

        public async Task<ImageStorageTier?> GetByIdAsync(string id)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id);
            using var iterator = _container.GetItemQueryIterator<ImageStorageTier>(query);
            return iterator.HasMoreResults ? (await iterator.ReadNextAsync()).FirstOrDefault() : null;
        }

        public async Task<ImageStorageTier?> GetByNameAsync(string name)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Name = @name").WithParameter("@name", name);
            using var iterator = _container.GetItemQueryIterator<ImageStorageTier>(query);
            return iterator.HasMoreResults ? (await iterator.ReadNextAsync()).FirstOrDefault() : null;
        }
    }
}
