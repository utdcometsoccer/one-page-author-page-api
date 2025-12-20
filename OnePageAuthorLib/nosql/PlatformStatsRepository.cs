using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for platform statistics stored in Cosmos DB.
    /// </summary>
    public class PlatformStatsRepository : IPlatformStatsRepository
    {
        private readonly IDataContainer _container;
        private const string StatsId = "current";

        public PlatformStatsRepository(Container container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "PlatformStatsRepository: The provided Cosmos DB container is null.");
            _container = new CosmosContainerWrapper(container);
        }

        public PlatformStatsRepository(IDataContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Gets the current platform statistics.
        /// </summary>
        /// <returns>The current platform stats, or null if not found.</returns>
        public async Task<PlatformStats?> GetCurrentStatsAsync()
        {
            try
            {
                return await _container.ReadItemAsync<PlatformStats>(StatsId, new PartitionKey(StatsId));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Updates or creates the platform statistics using upsert pattern.
        /// </summary>
        /// <param name="stats">The stats to save.</param>
        /// <returns>The saved stats.</returns>
        public async Task<PlatformStats> UpsertStatsAsync(PlatformStats stats)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            // Ensure the id is set to "current" for consistency
            stats.id = StatsId;
            stats.LastUpdated = DateTime.UtcNow.ToString("O");

            try
            {
                // Try to replace if exists
                var response = await _container.ReplaceItemAsync(stats, StatsId, new PartitionKey(StatsId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // If not found, create it
                var response = await _container.CreateItemAsync(stats, new PartitionKey(StatsId));
                return response.Resource;
            }
        }
    }
}
