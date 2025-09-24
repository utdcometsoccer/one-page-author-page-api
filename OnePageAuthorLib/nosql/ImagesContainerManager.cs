using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL.ImageAPI
{
    /// <summary>
    /// Manages the Cosmos DB container for Image entities.
    /// </summary>
    public class ImagesContainerManager : IContainerManager<Image>
    {
        private readonly Database _database;
        private readonly string _containerName = "Images";

        public ImagesContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<Container> EnsureContainerAsync()
        {
            // Partition by UserProfileId as each user can have many images
            var response = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/UserProfileId"
            );
            return response.Container;
        }
    }
}