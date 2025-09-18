using System.Net.Http.Headers;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class SubscriptionsService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionsService> _logger;

        public SubscriptionsService(ILogger<SubscriptionsService> logger)
        {
            _logger = logger;
        }

        public async Task<SubscriptionCreateResponse> CreateAsync(CreateSubscriptionRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.PriceId)) throw new ArgumentException("PriceId is required", nameof(request.PriceId));
            if (string.IsNullOrWhiteSpace(request.CustomerId)) throw new ArgumentException("CustomerId is required", nameof(request.CustomerId));

            try
            {
                var svc = new SubscriptionService();
                var options = new SubscriptionCreateOptions
                {
                    // With default_incomplete, the subscription is created and requires payment confirmation
                    PaymentBehavior = "default_incomplete",
                    Items = new List<SubscriptionItemOptions>
                    {
                        new SubscriptionItemOptions { Price = request.PriceId }
                    },
                    Expand = new List<string>
                    {
                        "latest_invoice"
                    }
                };

                options.Customer = request.CustomerId;
                options.AddExpand("latest_invoice.payment_intent");

                var subscription = await svc.CreateAsync(options);

                // Retrieve client secret from the first invoice via Stripe REST API
                string clientSecret = string.Empty;
                var latestInvoiceId = subscription?.LatestInvoiceId;
                var apiKey = StripeConfiguration.ApiKey;
                if (!string.IsNullOrWhiteSpace(latestInvoiceId) && !string.IsNullOrWhiteSpace(apiKey))
                {
                    try
                    {
                        using var http = new HttpClient();
                        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                        var url = $"https://api.stripe.com/v1/invoices/{latestInvoiceId}?expand[]=payment_intent";
                        using var resp = await http.GetAsync(url);
                        resp.EnsureSuccessStatusCode();
                        var json = await resp.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("payment_intent", out var pi) &&
                            pi.ValueKind == JsonValueKind.Object)
                        {
                            string? paymentIntentId = null;
                            if (pi.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                                paymentIntentId = idProp.GetString();
                            string masked = paymentIntentId != null && paymentIntentId.Length >= 8
                                ? $"{paymentIntentId[..4]}****{paymentIntentId[^4..]}"
                                : "(set)";
                            _logger.LogInformation("Stripe payment intent for invoice {InvoiceId}: {MaskedId}", latestInvoiceId, masked);

                            if (pi.TryGetProperty("client_secret", out var cs) && cs.ValueKind == JsonValueKind.String)
                            {
                                clientSecret = cs.GetString() ?? string.Empty;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Payment intent or client secret not found in invoice {InvoiceId}", latestInvoiceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to retrieve client secret from invoice {InvoiceId}", latestInvoiceId);
                    }
                }

                return new SubscriptionCreateResponse
                {
                    SubscriptionId = subscription?.Id ?? string.Empty,
                    ClientSecret = clientSecret
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating subscription. Status={Status} Code={Code} Type={Type}",
                    ex.HttpStatusCode, ex.StripeError?.Code, ex.StripeError?.Type);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating subscription");
                throw;
            }
        }
    }
}
