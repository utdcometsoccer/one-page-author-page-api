using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{

    /// <summary>
    /// Manages creation and access to an Azure Cosmos NoSQL database.
    /// </summary>
    public class CosmosDatabaseManager : ICosmosDatabaseManager
    {
        /// <summary>
        /// Creates an Azure Cosmos NoSQL database if it does not exist.
        /// </summary>
        /// <param name="endpointUri">The endpoint URI for the Cosmos DB account.</param>
        /// <param name="primaryKey">The primary key for the Cosmos DB account.</param>
        /// <param name="databaseId">The name of the database to create or access.</param>
        /// <returns>The Cosmos DB Database object.</returns>
        private readonly CosmosClient? _cosmosClient;

        public CosmosDatabaseManager() { }

        public CosmosDatabaseManager(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        }

        public async Task<Database> EnsureDatabaseAsync(string endpointUri, string primaryKey, string databaseId)
        {
            if (string.IsNullOrWhiteSpace(endpointUri))
                throw new ArgumentException("CosmosDatabaseManager: endpointUri cannot be null or empty.", nameof(endpointUri));
            if (string.IsNullOrWhiteSpace(primaryKey))
                throw new ArgumentException("CosmosDatabaseManager: primaryKey cannot be null or empty.", nameof(primaryKey));
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentException("CosmosDatabaseManager: databaseId cannot be null or empty.", nameof(databaseId));

            var client = _cosmosClient ?? new CosmosClient(endpointUri, primaryKey);
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseId);
            return databaseResponse.Database;
        }
    }
}
