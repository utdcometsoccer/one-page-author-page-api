using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for StateProvince entities.
    /// </summary>
    public class StateProvincesContainerManager : IContainerManager<StateProvince>
    {
        private readonly Database _database;
        private readonly string _containerName = "StateProvinces";

        /// <summary>
        /// Initializes a new instance of the StateProvincesContainerManager class.
        /// </summary>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public StateProvincesContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "StateProvincesContainerManager: The provided Database is null.");
            _database = database;
        }

        /// <summary>
        /// Ensures the StateProvinces container exists, creates it if it does not.
        /// Uses Code as the partition key for efficient lookups by state/province code.
        /// </summary>
        /// <returns>The Cosmos DB container for StateProvinces.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/Culture"
            );
            return containerResponse.Container;
        }
    }
}