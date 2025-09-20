using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IStripeInvoiceServiceHelper
    {
        Task<(string paymentIntentId, string clientSecret)> TryGetPaymentIntentAsync(string latestInvoiceId);
    }

    public class StripeInvoiceServiceHelper : IStripeInvoiceServiceHelper
    {
        private readonly ILogger<StripeInvoiceServiceHelper> _logger;
    private readonly HttpClient _http;
    private readonly IStripeApiKeyProvider _apiKeyProvider;

        public StripeInvoiceServiceHelper(ILogger<StripeInvoiceServiceHelper> logger, HttpClient http, IStripeApiKeyProvider apiKeyProvider)
        {
            _logger = logger;
            _http = http;
            _apiKeyProvider = apiKeyProvider;
        }

        public async Task<(string paymentIntentId, string clientSecret)> TryGetPaymentIntentAsync(string latestInvoiceId)
        {
            if (string.IsNullOrWhiteSpace(latestInvoiceId)) return (string.Empty, string.Empty);

            var apiKey = _apiKeyProvider.GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey)) return (string.Empty, string.Empty);

            try
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var url = $"https://api.stripe.com/v1/invoices/{latestInvoiceId}?expand[]=payment_intent";
                using var resp = await _http.GetAsync(url);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("payment_intent", out var pi) && pi.ValueKind == JsonValueKind.Object)
                {
                    var id = pi.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String ? idProp.GetString() ?? string.Empty : string.Empty;
                    var cs = pi.TryGetProperty("client_secret", out var csProp) && csProp.ValueKind == JsonValueKind.String ? csProp.GetString() ?? string.Empty : string.Empty;
                    return (id, cs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch payment_intent for invoice {InvoiceId}", latestInvoiceId);
            }
            return (string.Empty, string.Empty);
        }
    }
}
