using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for ImageStorageTierMembership entities.
    /// </summary>
    public class ImageStorageTierMembershipsContainerManager : IContainerManager<ImageStorageTierMembership>
    {
        private readonly Database _database;
        private readonly string _containerName = "ImageStorageTierMemberships";

        public ImageStorageTierMembershipsContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<Container> EnsureContainerAsync()
        {
            // Partition memberships by UserProfileId to make per-user queries efficient
            var response = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/UserProfileId"
            );
            return response.Container;
        }
    }
}
