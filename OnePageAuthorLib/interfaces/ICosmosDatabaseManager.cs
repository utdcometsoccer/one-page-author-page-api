using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Public interface for CosmosDatabaseManager.
    /// </summary>
    public interface ICosmosDatabaseManager
    {
        /// <summary>
        /// Creates an Azure Cosmos NoSQL database if it does not exist.
        /// </summary>
        /// <param name="endpointUri">The endpoint URI for the Cosmos DB account.</param>
        /// <param name="primaryKey">The primary key for the Cosmos DB account.</param>
        /// <param name="databaseId">The name of the database to create or access.</param>
        /// <returns>The Cosmos DB Database object.</returns>
        Task<Database> EnsureDatabaseAsync(string endpointUri, string primaryKey, string databaseId);
    }
}
