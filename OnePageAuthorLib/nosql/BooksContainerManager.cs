using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Azure.Cosmos;
using Book = InkStainedWretch.OnePageAuthorAPI.Entities.Book;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Book entities.
    /// </summary>
    public class BooksContainerManager : IContainerManager<Book>
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Database _database;
        private readonly string _containerName = "Books";

        /// <summary>
        /// Initializes a new instance of the BooksContainerManager class.
        /// </summary>
        /// <param name="cosmosClient">The Azure Cosmos DB client.</param>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public BooksContainerManager(CosmosClient cosmosClient, Database database)
        {
            _cosmosClient = cosmosClient;
            _database = database;
        }

        /// <summary>
        /// Ensures the Books container exists, creates it if it does not.
        /// </summary>
        /// <returns>The Cosmos DB container for Books.</returns>
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
