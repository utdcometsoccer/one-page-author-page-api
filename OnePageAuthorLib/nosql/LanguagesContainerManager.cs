using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Language entities.
    /// </summary>
    public class LanguagesContainerManager : IContainerManager<Language>
    {
        private readonly Database _database;
        private readonly string _containerName = "Languages";

        /// <summary>
        /// Initializes a new instance of the LanguagesContainerManager class.
        /// </summary>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public LanguagesContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "LanguagesContainerManager: The provided Database is null.");
            _database = database;
        }

        /// <summary>
        /// Ensures the Languages container exists, creates it if it does not.
        /// Uses RequestLanguage as the partition key for efficient lookups by request language.
        /// </summary>
        /// <returns>The Cosmos DB container for Languages.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/RequestLanguage"
            );
            return containerResponse.Container;
        }
    }
}
