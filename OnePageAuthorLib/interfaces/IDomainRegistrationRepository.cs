using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Interface for domain registration repository operations.
    /// </summary>
    public interface IDomainRegistrationRepository
    {
        /// <summary>
        /// Creates a new domain registration record in Cosmos DB.
        /// </summary>
        /// <param name="domainRegistration">The domain registration to create</param>
        /// <returns>The created domain registration with assigned id</returns>
        Task<DomainRegistration> CreateAsync(DomainRegistration domainRegistration);

        /// <summary>
        /// Gets a domain registration by id and user UPN.
        /// </summary>
        /// <param name="id">The registration id</param>
        /// <param name="upn">The user's UPN (partition key)</param>
        /// <returns>The domain registration if found, null otherwise</returns>
        Task<DomainRegistration?> GetByIdAsync(string id, string upn);

        /// <summary>
        /// Gets all domain registrations for a specific user.
        /// </summary>
        /// <param name="upn">The user's UPN (partition key)</param>
        /// <returns>List of domain registrations for the user</returns>
        Task<IEnumerable<DomainRegistration>> GetByUserAsync(string upn);

        /// <summary>
        /// Updates an existing domain registration.
        /// </summary>
        /// <param name="domainRegistration">The domain registration to update</param>
        /// <returns>The updated domain registration</returns>
        Task<DomainRegistration> UpdateAsync(DomainRegistration domainRegistration);

        /// <summary>
        /// Deletes a domain registration by id and user UPN.
        /// </summary>
        /// <param name="id">The registration id</param>
        /// <param name="upn">The user's UPN (partition key)</param>
        /// <returns>True if deleted successfully, false if not found</returns>
        Task<bool> DeleteAsync(string id, string upn);

        /// <summary>
        /// Gets a domain registration by top-level domain and second-level domain.
        /// </summary>
        /// <param name="topLevelDomain">The top-level domain (e.g., "com")</param>
        /// <param name="secondLevelDomain">The second-level domain (e.g., "example")</param>
        /// <returns>The domain registration if found, null otherwise</returns>
        Task<DomainRegistration?> GetByDomainAsync(string topLevelDomain, string secondLevelDomain);
    }
}