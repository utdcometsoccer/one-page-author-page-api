using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for UserProfile entities.
    /// </summary>
    public class UserProfilesContainerManager : IContainerManager<UserProfile>
    {
        private readonly Database _database;
        private readonly string _containerName = "UserProfiles";

        public UserProfilesContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Ensures the UserProfiles container exists with partition key /Upn.
        /// </summary>
        public async Task<Container> EnsureContainerAsync()
        {
            var response = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/Upn"
            );
            return response.Container;
        }
    }
}
