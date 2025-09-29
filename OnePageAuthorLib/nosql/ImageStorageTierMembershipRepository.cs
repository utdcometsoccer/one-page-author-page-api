using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    public class ImageStorageTierMembershipRepository : GenericRepository<ImageStorageTierMembership>, IImageStorageTierMembershipRepository
    {
        public ImageStorageTierMembershipRepository(Container container) : base(container) { }
        public ImageStorageTierMembershipRepository(IDataContainer container) : base(container) { }

        public async Task<IList<ImageStorageTierMembership>> GetByUserProfileIdAsync(string userProfileId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.UserProfileId = @uid").WithParameter("@uid", userProfileId);
            var results = new List<ImageStorageTierMembership>();
            using var iterator = _container.GetItemQueryIterator<ImageStorageTierMembership>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }
            return results;
        }

        public async Task<ImageStorageTierMembership?> GetForUserAsync(string userProfileId)
        {
            var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.UserProfileId = @uid ORDER BY c.id DESC").WithParameter("@uid", userProfileId);
            using var iterator = _container.GetItemQueryIterator<ImageStorageTierMembership>(query);
            return iterator.HasMoreResults ? (await iterator.ReadNextAsync()).FirstOrDefault() : null;
        }
    }
}
