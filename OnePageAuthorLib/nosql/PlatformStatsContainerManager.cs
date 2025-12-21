using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for PlatformStats entities.
    /// </summary>
    public class PlatformStatsContainerManager : IContainerManager<PlatformStats>
    {
        private readonly Database _database;
        private readonly string _containerName = "PlatformStats";

        /// <summary>
        /// Initializes a new instance of the PlatformStatsContainerManager class.
        /// </summary>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public PlatformStatsContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "PlatformStatsContainerManager: The provided Database is null.");
            _database = database;
        }

        /// <summary>
        /// Ensures the PlatformStats container exists, creates it if it does not.
        /// Uses /id as partition key since we only have one stats record.
        /// </summary>
        /// <returns>The Cosmos DB container for PlatformStats.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/id"
            );
            return containerResponse.Container;
        }
    }
}
