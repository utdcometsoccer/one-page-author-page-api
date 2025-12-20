using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Referral entities.
    /// </summary>
    public class ReferralsContainerManager : IContainerManager<Referral>
    {
        private readonly Database _database;
        private readonly string _containerName = "Referrals";

        /// <summary>
        /// Initializes a new instance of the ReferralsContainerManager class.
        /// </summary>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public ReferralsContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "ReferralsContainerManager: The provided Database is null.");
            _database = database;
        }

        /// <summary>
        /// Ensures the Referrals container exists, creates it if it does not.
        /// </summary>
        /// <returns>The Cosmos DB container for Referrals.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/ReferrerId"
            );
            return containerResponse.Container;
        }
    }
}
