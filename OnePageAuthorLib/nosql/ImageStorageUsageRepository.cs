using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    public class ImageStorageUsageRepository : GenericRepository<ImageStorageUsage>, IImageStorageUsageRepository
    {
        public ImageStorageUsageRepository(Container container) : base(container) { }
        public ImageStorageUsageRepository(IDataContainer container) : base(container) { }

        public async Task<ImageStorageUsage?> GetByUserProfileIdAsync(string userProfileId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.UserProfileId = @uid")
                .WithParameter("@uid", userProfileId);
            using var iterator = _container.GetItemQueryIterator<ImageStorageUsage>(query);
            return iterator.HasMoreResults ? (await iterator.ReadNextAsync()).FirstOrDefault() : null;
        }

        public async Task<ImageStorageUsage> GetOrCreateAsync(string userProfileId)
        {
            var existing = await GetByUserProfileIdAsync(userProfileId);
            if (existing != null)
            {
                return existing;
            }

            // Create new usage record
            var newUsage = new ImageStorageUsage
            {
                id = userProfileId, // Use userProfileId as the id for easy lookup
                UserProfileId = userProfileId,
                StorageUsedInBytes = 0,
                BandwidthUsedInBytes = 0,
                LastUpdated = DateTime.UtcNow
            };

            await AddAsync(newUsage);
            return newUsage;
        }
    }
}
