using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    public interface IDataContainer
    {
        Task<T> ReadItemAsync<T>(string id, PartitionKey partitionKey);
        FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition query, string? continuationToken = null, QueryRequestOptions? requestOptions = null);
        Task<ItemResponse<T>> CreateItemAsync<T>(T item);
        Task<ItemResponse<T>> CreateItemAsync<T>(T item, PartitionKey partitionKey);
        Task<ItemResponse<T>> ReplaceItemAsync<T>(T item, string id, PartitionKey partitionKey);
        Task DeleteItemAsync<T>(string id, PartitionKey partitionKey);
    }

    public class CosmosContainerWrapper : IDataContainer
    {
        private readonly Container _container;
        public CosmosContainerWrapper(Container container)
        {
            _container = container;
        }
        public async Task<T> ReadItemAsync<T>(string id, PartitionKey partitionKey)
        {
            var response = await _container.ReadItemAsync<T>(id, partitionKey);
            return response.Resource;
        }
        public FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition query, string? continuationToken = null, QueryRequestOptions? requestOptions = null)
            => _container.GetItemQueryIterator<T>(query, continuationToken, requestOptions);
        public Task<ItemResponse<T>> CreateItemAsync<T>(T item)
            => _container.CreateItemAsync(item);
        public Task<ItemResponse<T>> CreateItemAsync<T>(T item, PartitionKey partitionKey)
            => _container.CreateItemAsync(item, partitionKey);
        public Task<ItemResponse<T>> ReplaceItemAsync<T>(T item, string id, PartitionKey partitionKey)
            => _container.ReplaceItemAsync(item, id, partitionKey);
        public Task DeleteItemAsync<T>(string id, PartitionKey partitionKey)
            => _container.DeleteItemAsync<T>(id, partitionKey);
    }

}
