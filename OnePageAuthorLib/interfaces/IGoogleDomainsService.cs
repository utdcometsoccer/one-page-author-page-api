using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Interface for Google Domains API integration to register and manage domains.
    /// </summary>
    public interface IGoogleDomainsService
    {
        /// <summary>
        /// Registers a domain using the Google Domains API.
        /// </summary>
        /// <param name="domainRegistration">The domain registration information</param>
        /// <returns>True if registration was successful, false otherwise</returns>
        Task<bool> RegisterDomainAsync(DomainRegistration domainRegistration);

        /// <summary>
        /// Checks if a domain is available for registration.
        /// </summary>
        /// <param name="domainName">The full domain name to check</param>
        /// <returns>True if the domain is available, false otherwise</returns>
        Task<bool> IsDomainAvailableAsync(string domainName);
    }
}
