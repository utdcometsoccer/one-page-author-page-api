using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service interface for referral program business logic.
    /// </summary>
    public interface IReferralService
    {
        /// <summary>
        /// Creates a new referral for a user.
        /// </summary>
        /// <param name="request">The referral creation request.</param>
        /// <returns>Response containing the referral code and URL.</returns>
        Task<CreateReferralResponse> CreateReferralAsync(CreateReferralRequest request);

        /// <summary>
        /// Gets referral statistics for a specific user.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <returns>Statistics about the user's referrals.</returns>
        Task<ReferralStats> GetReferralStatsAsync(string userId);

        /// <summary>
        /// Generates a unique referral code.
        /// </summary>
        /// <returns>A unique referral code string.</returns>
        string GenerateReferralCode();

        /// <summary>
        /// Generates a shareable referral URL with the given code.
        /// </summary>
        /// <param name="referralCode">The referral code to include in the URL.</param>
        /// <returns>A complete referral URL.</returns>
        string GenerateReferralUrl(string referralCode);
    }
}
