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
        public async Task<Database> EnsureDatabaseAsync(string endpointUri, string primaryKey, string databaseId)
        {
            var cosmosClient = new CosmosClient(endpointUri, primaryKey);
            var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            return databaseResponse.Database;
        }
    }
}
