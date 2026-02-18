using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Interface for DNS zone management operations.
    /// </summary>
    public interface IDnsZoneService
    {
        /// <summary>
        /// Creates a DNS zone for the specified domain if it does not already exist.
        /// </summary>
        /// <param name="domainRegistration">The domain registration containing the domain information</param>
        /// <returns>True if the DNS zone was created or already exists, false otherwise</returns>
        Task<bool> EnsureDnsZoneExistsAsync(DomainRegistration domainRegistration);

        /// <summary>
        /// Checks if a DNS zone exists for the specified domain.
        /// </summary>
        /// <param name="domainName">The fully qualified domain name</param>
        /// <returns>True if the DNS zone exists, false otherwise</returns>
        Task<bool> DnsZoneExistsAsync(string domainName);

        /// <summary>
        /// Retrieves the Azure DNS name servers for the specified domain.
        /// </summary>
        /// <param name="domainName">The fully qualified domain name</param>
        /// <returns>Array of name server hostnames, or null if DNS zone does not exist</returns>
        Task<string[]?> GetNameServersAsync(string domainName);
    }
}
