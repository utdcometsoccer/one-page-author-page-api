using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service interface for A/B testing experiment management and variant assignment.
    /// </summary>
    public interface IExperimentService
    {
        /// <summary>
        /// Gets experiment assignments for a user/session on a specific page.
        /// Uses consistent hashing to ensure the same user/session always gets the same variant.
        /// </summary>
        /// <param name="request">Request containing userId (optional), page, and other parameters.</param>
        /// <returns>Response containing assigned experiments and sessionId for tracking.</returns>
        Task<GetExperimentsResponse> GetExperimentsAsync(GetExperimentsRequest request);

        /// <summary>
        /// Assigns a user/session to a variant for a specific experiment.
        /// Uses consistent hashing based on experimentId + userId/sessionId.
        /// </summary>
        /// <param name="experiment">The experiment configuration.</param>
        /// <param name="bucketingKey">The key to use for bucketing (userId or sessionId).</param>
        /// <returns>The assigned variant.</returns>
        ExperimentVariant AssignVariant(Experiment experiment, string bucketingKey);
    }
}
