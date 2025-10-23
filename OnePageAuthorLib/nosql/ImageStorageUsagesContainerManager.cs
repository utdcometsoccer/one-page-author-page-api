using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    public class ImageStorageUsagesContainerManager : IContainerManager<ImageStorageUsage>
    {
        private readonly Database _database;
        private const string ContainerName = "ImageStorageUsages";
        private const string PartitionKeyPath = "/UserProfileId";

        public ImageStorageUsagesContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<Container> EnsureContainerAsync()
        {
            var containerProperties = new ContainerProperties
            {
                Id = ContainerName,
                PartitionKeyPath = PartitionKeyPath
            };

            var response = await _database.CreateContainerIfNotExistsAsync(containerProperties);
            return response.Container;
        }
    }
}
