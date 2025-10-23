using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Country entities.
    /// Partition key is /Language.
    /// </summary>
    public class CountriesContainerManager : IContainerManager<Country>
    {
        private readonly Database _database;
        private const string ContainerName = "Countries";
        private const string PartitionKeyPath = "/Language";

        public CountriesContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Ensures the Countries container exists with the correct partition key.
        /// </summary>
        /// <returns>The Countries container instance.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: ContainerName,
                partitionKeyPath: PartitionKeyPath
            );

            return containerResponse.Container;
        }
    }
}
