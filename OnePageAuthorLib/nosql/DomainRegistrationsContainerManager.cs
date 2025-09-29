using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for DomainRegistration entities.
    /// </summary>
    public class DomainRegistrationsContainerManager : IContainerManager<DomainRegistration>
    {
        private readonly Database _database;
        private readonly string _containerName = "DomainRegistrations";

        public DomainRegistrationsContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Ensures the DomainRegistrations container exists with partition key /upn.
        /// </summary>
        public async Task<Container> EnsureContainerAsync()
        {
            var response = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/upn"
            );
            return response.Container;
        }
    }
}