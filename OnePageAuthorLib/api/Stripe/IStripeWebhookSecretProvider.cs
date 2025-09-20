namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IStripeWebhookSecretProvider
    {
        string GetWebhookSecret();
    }

    public class StripeWebhookSecretProvider : IStripeWebhookSecretProvider
    {
        private const string EnvVarName = "STRIPE_WEBHOOK_SECRET";
        public string GetWebhookSecret()
        {
            return Environment.GetEnvironmentVariable(EnvVarName) ?? string.Empty;
        }
    }
}
