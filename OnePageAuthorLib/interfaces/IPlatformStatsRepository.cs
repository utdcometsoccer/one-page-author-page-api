using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Repository interface for managing platform statistics.
    /// </summary>
    public interface IPlatformStatsRepository
    {
        /// <summary>
        /// Gets the current platform statistics.
        /// </summary>
        /// <returns>The current platform stats, or null if not found.</returns>
        Task<PlatformStats?> GetCurrentStatsAsync();

        /// <summary>
        /// Updates or creates the platform statistics.
        /// </summary>
        /// <param name="stats">The stats to save.</param>
        /// <returns>The saved stats.</returns>
        Task<PlatformStats> UpsertStatsAsync(PlatformStats stats);
    }
}
