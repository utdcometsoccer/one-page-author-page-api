using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for ImageStorageTier entities.
    /// </summary>
    public class ImageStorageTiersContainerManager : IContainerManager<ImageStorageTier>
    {
        private readonly Database _database;
        private readonly string _containerName = "ImageStorageTiers";

        public ImageStorageTiersContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<Container> EnsureContainerAsync()
        {
            // Partition by Name as a simple, human-readable key (unique per tier)
            var response = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/Name"
            );
            return response.Container;
        }
    }
}
