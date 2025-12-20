using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Lead entities.
    /// </summary>
    public class LeadsContainerManager : IContainerManager<Lead>
    {
        private readonly Database _database;
        private readonly string _containerName = "Leads";

        public LeadsContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Ensures the Leads container exists with partition key /emailDomain for efficient queries.
        /// </summary>
        public async Task<Container> EnsureContainerAsync()
        {
            var response = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/emailDomain"
            );
            return response.Container;
        }
    }
}
