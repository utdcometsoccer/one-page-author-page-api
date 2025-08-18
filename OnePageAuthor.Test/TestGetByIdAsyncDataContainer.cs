using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test
{
    public partial class LocaleRepositoryTests
    {
        private class TestGetByIdAsyncDataContainer : IDataContainer
        {
            private readonly List<Locale> _locales;
            private readonly string _idToReturn;
            public TestGetByIdAsyncDataContainer(List<Locale> locales, string idToReturn)
            {
                _locales = locales;
                _idToReturn = idToReturn;
            }
            public FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition query, string? continuationToken = null, QueryRequestOptions? requestOptions = null)
            {
                var filtered = _locales.Where(l => l.id == _idToReturn).Cast<T>().ToList();
                return new LocalFeedIterator<T>(filtered);
            }
            public Task<T> ReadItemAsync<T>(string id, PartitionKey partitionKey)
            {
                var item = _locales.Where(l => l.id == _idToReturn).Cast<T>().FirstOrDefault();
                return Task.FromResult((T)item!);
            }
            public Task<ItemResponse<T>> CreateItemAsync<T>(T item) => throw new NotImplementedException();
            public Task<ItemResponse<T>> ReplaceItemAsync<T>(T item, string id, PartitionKey partitionKey) => throw new NotImplementedException();
            public Task DeleteItemAsync<T>(string id, PartitionKey partitionKey) => throw new NotImplementedException();
        }
    }
}
