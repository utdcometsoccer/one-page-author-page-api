using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IInvoicePreview
    {
        Task<InvoicePreviewResponse> PreviewAsync(InvoicePreviewRequest request);
    }

    public class InvoicePreviewService : IInvoicePreview
    {
        private readonly ILogger<InvoicePreviewService> _logger;
        private readonly HttpClient _http;
        private readonly IStripeApiKeyProvider _apiKeyProvider;

        public InvoicePreviewService(ILogger<InvoicePreviewService> logger, HttpClient http, IStripeApiKeyProvider apiKeyProvider)
        {
            _logger = logger;
            _http = http;
            _apiKeyProvider = apiKeyProvider;
        }

        public async Task<InvoicePreviewResponse> PreviewAsync(InvoicePreviewRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.CustomerId)) throw new ArgumentException("CustomerId is required", nameof(request.CustomerId));

            try
            {
                var apiKey = _apiKeyProvider.GetApiKey();
                var url = new StringBuilder("https://api.stripe.com/v1/invoices/upcoming");
                var query = new List<string>();
                void Add(string key, string? value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
                    }
                }

                Add("customer", request.CustomerId);
                Add("subscription", request.SubscriptionId);
                Add("currency", request.Currency);
                Add("subscription_proration_behavior", request.ProrationBehavior);
                if (!string.IsNullOrWhiteSpace(request.SubscriptionItemId))
                {
                    Add("subscription_items[0][id]", request.SubscriptionItemId);
                    Add("subscription_items[0][price]", request.PriceId);
                    if (request.Quantity.HasValue)
                    {
                        Add("subscription_items[0][quantity]", request.Quantity.Value.ToString());
                    }
                }
                else if (!string.IsNullOrWhiteSpace(request.PriceId))
                {
                    Add("subscription_items[0][price]", request.PriceId);
                    if (request.Quantity.HasValue)
                    {
                        Add("subscription_items[0][quantity]", request.Quantity.Value.ToString());
                    }
                }

                if (query.Count > 0)
                {
                    url.Append('?');
                    url.Append(string.Join('&', query));
                }

                using var reqMsg = new HttpRequestMessage(HttpMethod.Get, url.ToString());
                reqMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var resp = await _http.SendAsync(reqMsg);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                return MapFromJson(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice preview for customer {Customer}", request.CustomerId);
                throw;
            }
        }

        internal static InvoicePreviewResponse MapFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var response = new InvoicePreviewResponse
            {
                InvoiceId = root.GetProperty("id").GetString() ?? string.Empty,
                Currency = root.TryGetProperty("currency", out var cur) ? cur.GetString() ?? string.Empty : string.Empty,
                AmountDue = root.TryGetProperty("amount_due", out var ad) ? ad.GetInt64() : 0,
                Subtotal = root.TryGetProperty("subtotal", out var sub) ? sub.GetInt64() : 0,
                Total = root.TryGetProperty("total", out var tot) ? tot.GetInt64() : 0
            };

            if (root.TryGetProperty("lines", out var linesElem) && linesElem.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataElem.EnumerateArray())
                {
                    string priceId = string.Empty;
                    if (item.TryGetProperty("price", out var priceElem) && priceElem.ValueKind == JsonValueKind.Object)
                    {
                        if (priceElem.TryGetProperty("id", out var pid))
                        {
                            priceId = pid.GetString() ?? string.Empty;
                        }
                    }
                    var line = new InvoiceLineDto
                    {
                        Description = item.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty,
                        Quantity = item.TryGetProperty("quantity", out var q) && q.ValueKind != JsonValueKind.Null ? q.GetInt64() : 0,
                        Amount = item.TryGetProperty("amount", out var a) ? a.GetInt64() : 0,
                        Currency = item.TryGetProperty("currency", out var lc) ? lc.GetString() ?? string.Empty : response.Currency,
                        PriceId = priceId
                    };
                    response.Lines.Add(line);
                }
            }

            return response;
        }
    }
}
