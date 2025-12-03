namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IStripeApiKeyProvider
    {
        string GetApiKey();
    }

    public class StripeApiKeyProvider : IStripeApiKeyProvider
    {
        // Deprecated: prefer injecting StripeClient via DI and avoid static ApiKey.
        public string GetApiKey() => string.Empty;
    }
}
