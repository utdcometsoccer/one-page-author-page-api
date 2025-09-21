namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents the authenticated user's identity details and linkage to Stripe.
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Cosmos DB id (case-sensitive). If not provided on create, it will be generated.
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// The user's User Principal Name (e.g., email or login name). Optional depending on identity provider.
        /// </summary>
        public string? Upn { get; set; }

        /// <summary>
        /// The user's Object ID (OID) from Entra ID ("oid" claim). This is typically stable per tenant.
        /// </summary>
        public string? Oid { get; set; }

        /// <summary>
        /// The Stripe Customer ID associated with this user (e.g., "cus_..."). Optional.
        /// </summary>
        public string? StripeCustomerId { get; set; }

        /// <summary>
        /// Initializes an empty user profile.
        /// </summary>
        public UserProfile() { }

        /// <summary>
        /// Initializes a user profile with optional UPN, OID and Stripe customer id.
        /// </summary>
        public UserProfile(string? upn, string? oid, string? stripeCustomerId = null)
        {
            id = null;
            Upn = upn;
            Oid = oid;
            StripeCustomerId = stripeCustomerId;
        }
    }
}
