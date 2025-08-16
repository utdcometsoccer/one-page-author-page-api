using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Generic interface for Cosmos DB container managers.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by the container.</typeparam>
    public interface IContainerManager<TEntity>
    {
        /// <summary>
        /// Ensures the container exists, creates it if it does not.
        /// </summary>
        /// <returns>The Cosmos DB container for the entity.</returns>
        Task<Container> EnsureContainerAsync();
    }
}
