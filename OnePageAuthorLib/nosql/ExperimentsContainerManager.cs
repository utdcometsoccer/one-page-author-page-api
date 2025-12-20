using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Experiment entities.
    /// </summary>
    public class ExperimentsContainerManager : IContainerManager<Experiment>
    {
        private readonly Database _database;
        private const string ContainerName = "Experiments";
        private const string PartitionKeyPath = "/Page";

        public ExperimentsContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Ensures the Experiments container exists and returns it.
        /// </summary>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerProperties = new ContainerProperties
            {
                Id = ContainerName,
                PartitionKeyPath = PartitionKeyPath
            };

            var response = await _database.CreateContainerIfNotExistsAsync(
                containerProperties,
                throughput: 400);

            return response.Container;
        }
    }
}
