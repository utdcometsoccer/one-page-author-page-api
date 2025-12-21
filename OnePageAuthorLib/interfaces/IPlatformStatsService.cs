using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service interface for managing platform statistics.
    /// Implements caching to reduce database load.
    /// </summary>
    public interface IPlatformStatsService
    {
        /// <summary>
        /// Gets the current platform statistics with caching.
        /// Returns cached value if available and not expired (1 hour TTL).
        /// </summary>
        /// <returns>The current platform stats or default values if not found.</returns>
        Task<PlatformStats> GetPlatformStatsAsync();

        /// <summary>
        /// Computes and updates platform statistics by querying various data sources.
        /// This method should be called periodically (e.g., nightly) to refresh stats.
        /// </summary>
        /// <returns>The updated platform stats.</returns>
        Task<PlatformStats> ComputeAndUpdateStatsAsync();
    }
}
