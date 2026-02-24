using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service interface for author invitation business logic, including creation,
    /// retrieval, update, and email delivery.
    /// </summary>
    public interface IAuthorInvitationService
    {
        /// <summary>
        /// Gets an author invitation by its unique ID.
        /// </summary>
        /// <param name="id">The invitation ID.</param>
        /// <returns>The invitation if found; otherwise <see langword="null"/>.</returns>
        Task<AuthorInvitation?> GetByIdAsync(string id);

        /// <summary>
        /// Gets all pending author invitations.
        /// </summary>
        /// <returns>A list of pending invitations.</returns>
        Task<IList<AuthorInvitation>> GetPendingInvitationsAsync();

        /// <summary>
        /// Creates a new author invitation, optionally sending an invitation email.
        /// Throws <see cref="ArgumentException"/> when email or domain validation fails.
        /// Throws <see cref="InvalidOperationException"/> when an invitation already exists
        /// for the supplied email address.
        /// </summary>
        /// <param name="email">The invitee's email address.</param>
        /// <param name="domainNames">One or more domain names to link to the invitation.</param>
        /// <param name="notes">Optional notes about the invitation.</param>
        /// <returns>
        /// A <see cref="CreateInvitationResult"/> containing the saved invitation and a flag
        /// indicating whether the invitation email was sent.
        /// </returns>
        Task<CreateInvitationResult> CreateInvitationAsync(string email, List<string> domainNames, string? notes);

        /// <summary>
        /// Updates a pending author invitation.
        /// Throws <see cref="InvalidOperationException"/> when the invitation is not found or
        /// is not in <c>Pending</c> status.
        /// Throws <see cref="ArgumentException"/> when a supplied domain name is invalid.
        /// </summary>
        /// <param name="id">The invitation ID.</param>
        /// <param name="domainNames">Replacement domain names, or <see langword="null"/> to leave unchanged.</param>
        /// <param name="notes">Replacement notes, or <see langword="null"/> to leave unchanged.</param>
        /// <param name="expiresAt">Replacement expiry, or <see langword="null"/> to leave unchanged.</param>
        /// <returns>The updated invitation.</returns>
        Task<AuthorInvitation> UpdateInvitationAsync(string id, List<string>? domainNames, string? notes, DateTime? expiresAt);

        /// <summary>
        /// Resends the invitation email for a pending author invitation.
        /// Throws <see cref="InvalidOperationException"/> when the invitation is not found,
        /// is not in <c>Pending</c> status, or when the email service is not configured.
        /// </summary>
        /// <param name="id">The invitation ID.</param>
        /// <returns>The invitation with an updated <c>LastEmailSentAt</c> timestamp.</returns>
        Task<AuthorInvitation> ResendInvitationEmailAsync(string id);

        /// <summary>
        /// Validates an email address format.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns><see langword="true"/> if the format is valid; otherwise <see langword="false"/>.</returns>
        bool IsValidEmail(string email);

        /// <summary>
        /// Validates a domain name format.
        /// </summary>
        /// <param name="domain">The domain name to validate.</param>
        /// <returns><see langword="true"/> if the format is valid; otherwise <see langword="false"/>.</returns>
        bool IsValidDomain(string domain);
    }
}
