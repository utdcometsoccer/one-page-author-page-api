using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Author entities.
    /// </summary>
    public class AuthorsContainerManager : IContainerManager<Author>
    {
        private readonly Database _database;
        private readonly string _containerName = "Authors";

        /// <summary>
        /// Initializes a new instance of the AuthorsContainerManager class.
        /// </summary>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public AuthorsContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "AuthorsContainerManager: The provided Database is null.");           
            _database = database;
        }

        /// <summary>
        /// Ensures the Authors container exists, creates it if it does not.
        /// Uses a composite partition key: SecondLevelDomain.TopLevelDomain-LanguageName-RegionName
        /// </summary>
        /// <returns>The Cosmos DB container for Authors.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/EmailAddress"
            );
            return containerResponse.Container;
        }
    }
}
