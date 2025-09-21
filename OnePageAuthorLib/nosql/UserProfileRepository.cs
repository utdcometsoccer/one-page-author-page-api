using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for UserProfile with partition key Upn.
    /// </summary>
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly IDataContainer _container;

        public UserProfileRepository(Container container)
        {
            _container = new CosmosContainerWrapper(container);
        }

        public UserProfileRepository(IDataContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<UserProfile?> GetByIdAsync(Guid id)
        {
            // For user profiles, id is an arbitrary string. We cannot derive partition from Guid id.
            // Provide a fallback query by id.
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id.ToString());
            using var iterator = _container.GetItemQueryIterator<UserProfile>(query);
            if (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                return page.Resource.FirstOrDefault();
            }
            return null;
        }

        public Task<IList<UserProfile>> GetByAuthorIdAsync(Guid authorId)
        {
            // Not applicable to profiles; return empty list.
            return Task.FromResult<IList<UserProfile>>(new List<UserProfile>());
        }

        public async Task<UserProfile> AddAsync(UserProfile entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Upn))
                throw new InvalidOperationException("UserProfile.Upn is required for partition key.");
            if (string.IsNullOrWhiteSpace(entity.id))
            {
                entity.id = Guid.NewGuid().ToString();
            }
            var response = await _container.CreateItemAsync(entity, new PartitionKey(entity.Upn));
            return response.Resource;
        }

        public async Task<UserProfile> UpdateAsync(UserProfile entity)
        {
            if (string.IsNullOrWhiteSpace(entity.id))
                throw new InvalidOperationException("UserProfile.id must be provided.");
            if (string.IsNullOrWhiteSpace(entity.Upn))
                throw new InvalidOperationException("UserProfile.Upn is required for partition key.");
            var response = await _container.ReplaceItemAsync(entity, entity.id, new PartitionKey(entity.Upn));
            return response.Resource;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            // We don't know the partition (Upn). Fallback to query then delete.
            var existing = await GetByIdAsync(id);
            if (existing == null || string.IsNullOrWhiteSpace(existing.Upn) || string.IsNullOrWhiteSpace(existing.id))
                return false;
            await _container.DeleteItemAsync<UserProfile>(existing.id, new PartitionKey(existing.Upn));
            return true;
        }

        // Convenience helpers
        public async Task<UserProfile?> GetByUpnAsync(string upn)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Upn = @upn")
                .WithParameter("@upn", upn);
            using var iterator = _container.GetItemQueryIterator<UserProfile>(query);
            if (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                return page.Resource.FirstOrDefault();
            }
            return null;
        }
    }
}
