using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Azure.Cosmos;
using Article = InkStainedWretch.OnePageAuthorAPI.Entities.Article;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Article entities.
    /// </summary>
    public class ArticlesContainerManager : IContainerManager<Article>
    {
        private readonly Database _database;
        private readonly string _containerName = "Articles";

        /// <summary>
        /// Initializes a new instance of the ArticlesContainerManager class.
        /// </summary>
        /// <param name="cosmosClient">The Azure Cosmos DB client.</param>
        /// <param name="database">The Azure Cosmos DB database.</param>
    public ArticlesContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "ArticlesContainerManager: The provided Database is null.");
            _database = database;
        }

        /// <summary>
        /// Ensures the Articles container exists, creates it if it does not.
        /// </summary>
        /// <returns>The Cosmos DB container for Articles.</returns>
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
