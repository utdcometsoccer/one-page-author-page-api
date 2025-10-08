using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Interface for Azure Front Door domain management operations.
    /// </summary>
    public interface IFrontDoorService
    {
        /// <summary>
        /// Checks if a domain already exists in Azure Front Door.
        /// </summary>
        /// <param name="domainName">The full domain name (e.g., "example.com")</param>
        /// <returns>True if the domain exists, false otherwise</returns>
        Task<bool> DomainExistsAsync(string domainName);

        /// <summary>
        /// Adds a new domain to Azure Front Door if it doesn't already exist.
        /// </summary>
        /// <param name="domainRegistration">The domain registration information</param>
        /// <returns>True if the domain was added or already exists, false on failure</returns>
        Task<bool> AddDomainToFrontDoorAsync(DomainRegistration domainRegistration);
    }
}
