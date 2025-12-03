namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IStripeApiKeyProvider
    {
        string GetApiKey();
    }

    public class StripeApiKeyProvider : IStripeApiKeyProvider
    {
        // Deprecated: prefer injecting StripeClient via DI and avoid static ApiKey.
        public string GetApiKey()
        {
            throw new NotSupportedException("IStripeApiKeyProvider is deprecated. Use StripeClient injection via DI instead. See MIGRATION_GUIDE_ENTRA_ID_ROLES.md for migration steps.");
        }
    }
}
