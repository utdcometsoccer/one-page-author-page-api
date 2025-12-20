using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Repository interface for managing Experiment entities in Cosmos DB.
    /// </summary>
    public interface IExperimentRepository
    {
        /// <summary>
        /// Gets all active experiments for a specific page.
        /// </summary>
        /// <param name="page">The page identifier (e.g., 'landing', 'pricing').</param>
        /// <returns>List of active experiments for the page.</returns>
        Task<List<Experiment>> GetActiveExperimentsByPageAsync(string page);

        /// <summary>
        /// Gets an experiment by its ID.
        /// </summary>
        /// <param name="id">The experiment ID.</param>
        /// <param name="page">The page identifier (partition key).</param>
        /// <returns>The experiment if found, null otherwise.</returns>
        Task<Experiment?> GetByIdAsync(string id, string page);

        /// <summary>
        /// Creates a new experiment.
        /// </summary>
        /// <param name="experiment">The experiment to create.</param>
        /// <returns>The created experiment.</returns>
        Task<Experiment> CreateAsync(Experiment experiment);

        /// <summary>
        /// Updates an existing experiment.
        /// </summary>
        /// <param name="experiment">The experiment to update.</param>
        /// <returns>The updated experiment.</returns>
        Task<Experiment> UpdateAsync(Experiment experiment);

        /// <summary>
        /// Deletes an experiment.
        /// </summary>
        /// <param name="id">The experiment ID.</param>
        /// <param name="page">The page identifier (partition key).</param>
        Task DeleteAsync(string id, string page);
    }
}
