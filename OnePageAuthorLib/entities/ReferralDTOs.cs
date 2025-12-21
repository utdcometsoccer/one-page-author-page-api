namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Request model for creating a new referral.
    /// </summary>
    public class CreateReferralRequest
    {
        /// <summary>
        /// The ID of the existing user making the referral.
        /// </summary>
        public string ReferrerId { get; set; } = string.Empty;

        /// <summary>
        /// The email address of the person being referred (new lead).
        /// </summary>
        public string ReferredEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model after successfully creating a referral.
    /// </summary>
    public class CreateReferralResponse
    {
        /// <summary>
        /// The unique referral code generated for this referral.
        /// </summary>
        public string ReferralCode { get; set; } = string.Empty;

        /// <summary>
        /// The shareable URL containing the referral code.
        /// </summary>
        public string ReferralUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistics about a user's referral activity.
    /// </summary>
    public class ReferralStats
    {
        /// <summary>
        /// Total number of referrals made by the user.
        /// </summary>
        public int TotalReferrals { get; set; }

        /// <summary>
        /// Number of referrals that converted to paid subscriptions.
        /// </summary>
        public int SuccessfulReferrals { get; set; }

        /// <summary>
        /// Number of months of credit earned but not yet redeemed.
        /// </summary>
        public int PendingCredits { get; set; }

        /// <summary>
        /// Number of months of credit already redeemed/used.
        /// </summary>
        public int RedeemedCredits { get; set; }
    }
}
