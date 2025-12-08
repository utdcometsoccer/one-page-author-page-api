namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service interface for sending email notifications.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an invitation email to an author.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="domainName">The domain name that will be linked to the author's account.</param>
        /// <param name="invitationId">The unique invitation ID.</param>
        /// <returns>True if the email was sent successfully, false otherwise.</returns>
        Task<bool> SendInvitationEmailAsync(string toEmail, string domainName, string invitationId);
    }
}
