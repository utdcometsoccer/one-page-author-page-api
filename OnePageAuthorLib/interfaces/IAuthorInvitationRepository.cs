using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Repository interface for managing author invitations.
    /// </summary>
    public interface IAuthorInvitationRepository
    {
        /// <summary>
        /// Gets an invitation by its ID.
        /// </summary>
        /// <param name="id">The invitation ID.</param>
        /// <returns>The invitation if found, otherwise null.</returns>
        Task<AuthorInvitation?> GetByIdAsync(string id);

        /// <summary>
        /// Gets an invitation by email address.
        /// </summary>
        /// <param name="emailAddress">The email address.</param>
        /// <returns>The invitation if found, otherwise null.</returns>
        Task<AuthorInvitation?> GetByEmailAsync(string emailAddress);

        /// <summary>
        /// Gets all invitations for a specific domain.
        /// </summary>
        /// <param name="domainName">The domain name.</param>
        /// <returns>A list of invitations for the domain.</returns>
        Task<IList<AuthorInvitation>> GetByDomainAsync(string domainName);

        /// <summary>
        /// Gets all pending invitations.
        /// </summary>
        /// <returns>A list of pending invitations.</returns>
        Task<IList<AuthorInvitation>> GetPendingInvitationsAsync();

        /// <summary>
        /// Adds a new invitation.
        /// </summary>
        /// <param name="invitation">The invitation to add.</param>
        /// <returns>The added invitation.</returns>
        Task<AuthorInvitation> AddAsync(AuthorInvitation invitation);

        /// <summary>
        /// Updates an existing invitation.
        /// </summary>
        /// <param name="invitation">The invitation to update.</param>
        /// <returns>The updated invitation.</returns>
        Task<AuthorInvitation> UpdateAsync(AuthorInvitation invitation);

        /// <summary>
        /// Deletes an invitation by its ID.
        /// </summary>
        /// <param name="id">The invitation ID.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        Task<bool> DeleteAsync(string id);
    }
}
