using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Locale entities.
    /// </summary>
    public class LocalesContainerManager : IContainerManager<Locale>
    {
        private readonly Database _database;
        private readonly string _containerName = "Locales";

        /// <summary>
        /// Initializes a new instance of the LocalesContainerManager class.
        /// </summary>
        /// <param name="cosmosClient">The Azure Cosmos DB client.</param>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public LocalesContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "LocalesContainerManager: The provided Database is null.");
            _database = database;
        }

        /// <summary>
        /// Ensures the Locales container exists, creates it if it does not.
        /// </summary>
        /// <returns>The Cosmos DB container for Locales.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/LanguageName"
            );
            return containerResponse.Container;
        }
    }
}
