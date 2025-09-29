using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Social entities.
    /// </summary>
    public class SocialsContainerManager : IContainerManager<Social>
    {
        private readonly Database _database;
        private readonly string _containerName = "Socials";

        /// <summary>
        /// Initializes a new instance of the SocialsContainerManager class.
        /// </summary>
        /// <param name="cosmosClient">The Azure Cosmos DB client.</param>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public SocialsContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "SocialsContainerManager: The provided Database is null.");
            _database = database;
        }

        /// <summary>
        /// Ensures the Socials container exists, creates it if it does not.
        /// </summary>
        /// <returns>The Cosmos DB container for Socials.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/AuthorID"
            );
            return containerResponse.Container;
        }
    }
}
