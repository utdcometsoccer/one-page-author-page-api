using System.Collections.ObjectModel;
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
        /// Ensures the Authors container exists with the required composite index.
        /// Creates the container if it does not exist, or updates its indexing policy if it already exists.
        /// The composite index on (AuthorName, id) is required for the ORDER BY query in GetAllPagedAsync.
        /// </summary>
        /// <returns>The Cosmos DB container for Authors.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerProperties = new ContainerProperties(
                id: _containerName,
                partitionKeyPath: "/EmailAddress"
            );

            // Composite index required for: SELECT * FROM c ORDER BY c.AuthorName, c.id
            containerProperties.IndexingPolicy.CompositeIndexes.Add(
                new Collection<CompositePath>
                {
                    new CompositePath { Path = "/AuthorName", Order = CompositePathSortOrder.Ascending },
                    new CompositePath { Path = "/id", Order = CompositePathSortOrder.Ascending }
                }
            );

            var containerResponse = await _database.CreateContainerIfNotExistsAsync(containerProperties);
            var container = containerResponse.Container;

            // If the container already existed (HTTP 200), update its indexing policy
            // so existing deployments also receive the composite index.
            if (containerResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                await container.ReplaceContainerAsync(containerProperties);
            }

            return container;
        }
    }
}
