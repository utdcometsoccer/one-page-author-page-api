using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Repository interface for Referral entities.
    /// Provides data access methods for referral management.
    /// </summary>
    public interface IReferralRepository
    {
        /// <summary>
        /// Gets a referral by its unique ID.
        /// </summary>
        /// <param name="id">The referral ID.</param>
        /// <param name="referrerId">The referrer ID (partition key).</param>
        /// <returns>The referral if found, null otherwise.</returns>
        Task<Referral?> GetByIdAsync(string id, string referrerId);

        /// <summary>
        /// Gets all referrals made by a specific user.
        /// </summary>
        /// <param name="referrerId">The referrer's user ID.</param>
        /// <returns>List of referrals made by the user.</returns>
        Task<IList<Referral>> GetByReferrerIdAsync(string referrerId);

        /// <summary>
        /// Gets a referral by its unique referral code.
        /// </summary>
        /// <param name="referralCode">The referral code.</param>
        /// <returns>The referral if found, null otherwise.</returns>
        Task<Referral?> GetByReferralCodeAsync(string referralCode);

        /// <summary>
        /// Checks if a referred email already exists for a given referrer.
        /// </summary>
        /// <param name="referrerId">The referrer's user ID.</param>
        /// <param name="referredEmail">The email being checked.</param>
        /// <returns>True if the email was already referred by this user.</returns>
        Task<bool> ExistsByReferrerAndEmailAsync(string referrerId, string referredEmail);

        /// <summary>
        /// Creates a new referral.
        /// </summary>
        /// <param name="referral">The referral to create.</param>
        /// <returns>The created referral with generated ID.</returns>
        Task<Referral> AddAsync(Referral referral);

        /// <summary>
        /// Updates an existing referral.
        /// </summary>
        /// <param name="referral">The referral to update.</param>
        /// <returns>The updated referral.</returns>
        Task<Referral> UpdateAsync(Referral referral);

        /// <summary>
        /// Deletes a referral by ID.
        /// </summary>
        /// <param name="id">The referral ID.</param>
        /// <param name="referrerId">The referrer ID (partition key).</param>
        /// <returns>True if deleted, false if not found.</returns>
        Task<bool> DeleteAsync(string id, string referrerId);
    }
}
