namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IStripeApiKeyProvider
    {
        string GetApiKey();
    }

    public class StripeApiKeyProvider : IStripeApiKeyProvider
    {
        public string GetApiKey() => global::Stripe.StripeConfiguration.ApiKey ?? string.Empty;
    }
}
