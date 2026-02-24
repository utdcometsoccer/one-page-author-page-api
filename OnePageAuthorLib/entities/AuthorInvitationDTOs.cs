namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Result model returned by the author invitation service after creating a new invitation.
    /// </summary>
    public class CreateInvitationResult
    {
        /// <summary>
        /// The newly created author invitation.
        /// </summary>
        public AuthorInvitation Invitation { get; init; } = null!;

        /// <summary>
        /// Whether the invitation email was sent successfully during creation.
        /// </summary>
        public bool EmailSent { get; init; }
    }
}
