using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL.ImageAPI
{
    public class ImageRepository : GenericRepository<Image>, IImageRepository
    {
        public ImageRepository(Container container) : base(container) { }
        public ImageRepository(IDataContainer container) : base(container) { }

        public async Task<IList<Image>> GetByUserProfileIdAsync(string userProfileId)
        {
            var query = "SELECT * FROM c WHERE c.UserProfileId = @userProfileId";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@userProfileId", userProfileId);

            var results = new List<Image>();
            using var iterator = _container.GetItemQueryIterator<Image>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userProfileId)
            });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<long> GetTotalSizeByUserProfileIdAsync(string userProfileId)
        {
            var query = "SELECT VALUE SUM(c.Size) FROM c WHERE c.UserProfileId = @userProfileId";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@userProfileId", userProfileId);

            using var iterator = _container.GetItemQueryIterator<long?>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userProfileId)
            });

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault() ?? 0L;
            }

            return 0L;
        }

        public async Task<int> GetCountByUserProfileIdAsync(string userProfileId)
        {
            var query = "SELECT VALUE COUNT(1) FROM c WHERE c.UserProfileId = @userProfileId";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@userProfileId", userProfileId);

            using var iterator = _container.GetItemQueryIterator<int>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userProfileId)
            });

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return 0;
        }
    }
}