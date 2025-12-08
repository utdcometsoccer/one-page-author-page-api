using InkStainedWretch.OnePageAuthorAPI.Entities;
using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Interface for domain registration service operations.
    /// </summary>
    public interface IDomainRegistrationService
    {
        /// <summary>
        /// Creates a new domain registration for an authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="domain">Domain information</param>
        /// <param name="contactInformation">Contact information for registration</param>
        /// <returns>The created domain registration</returns>
        Task<DomainRegistration> CreateDomainRegistrationAsync(
            ClaimsPrincipal user, 
            Domain domain, 
            ContactInformation contactInformation);

        /// <summary>
        /// Gets all domain registrations for an authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <returns>List of domain registrations for the user</returns>
        Task<IEnumerable<DomainRegistration>> GetUserDomainRegistrationsAsync(ClaimsPrincipal user);

        /// <summary>
        /// Gets a specific domain registration by id for an authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="registrationId">The registration id</param>
        /// <returns>The domain registration if found and belongs to the user, null otherwise</returns>
        Task<DomainRegistration?> GetDomainRegistrationByIdAsync(ClaimsPrincipal user, string registrationId);

        /// <summary>
        /// Updates a domain registration status for an authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="registrationId">The registration id</param>
        /// <param name="status">The new status</param>
        /// <returns>The updated domain registration if found and belongs to the user, null otherwise</returns>
        Task<DomainRegistration?> UpdateDomainRegistrationStatusAsync(
            ClaimsPrincipal user, 
            string registrationId, 
            DomainRegistrationStatus status);

        /// <summary>
        /// Updates a domain registration for an authenticated user.
        /// Validates that the user has an active subscription before allowing updates.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="registrationId">The registration id</param>
        /// <param name="domain">Optional domain information to update</param>
        /// <param name="contactInformation">Optional contact information to update</param>
        /// <param name="status">Optional status to update</param>
        /// <returns>The updated domain registration if found and belongs to the user, null otherwise</returns>
        /// <exception cref="InvalidOperationException">Thrown if user doesn't have an active subscription</exception>
        Task<DomainRegistration?> UpdateDomainRegistrationAsync(
            ClaimsPrincipal user,
            string registrationId,
            Domain? domain = null,
            ContactInformation? contactInformation = null,
            DomainRegistrationStatus? status = null);
    }
}