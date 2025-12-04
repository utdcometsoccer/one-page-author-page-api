using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IStripeWebhookHandler
    {
        Task<WebhookResult> HandleAsync(string? payload, string? signatureHeader);
    }

    public class WebhookResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string ObjectId { get; set; } = string.Empty;
        public string PriceId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
    }

    public class StripeWebhookHandler : IStripeWebhookHandler
    {
        private readonly ILogger<StripeWebhookHandler> _logger;
        private readonly IStripeWebhookSecretProvider _secretProvider;
        private readonly IStripeTelemetryService? _telemetryService;

        public StripeWebhookHandler(ILogger<StripeWebhookHandler> logger, IStripeWebhookSecretProvider secretProvider, IStripeTelemetryService? telemetryService = null)
        {
            _logger = logger;
            _secretProvider = secretProvider;
            _telemetryService = telemetryService;
        }

        public Task<WebhookResult> HandleAsync(string? payload, string? signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return Task.FromResult(new WebhookResult { Success = false, Message = "Empty payload" });
            }
            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                return Task.FromResult(new WebhookResult { Success = false, Message = "Missing Stripe-Signature header" });
            }

            // Verify signature using webhook secret
            var secret = _secretProvider.GetWebhookSecret();
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("Stripe webhook secret is not configured.");
                return Task.FromResult(new WebhookResult { Success = false, Message = "Webhook secret not configured" });
            }
            if (!VerifyStripeSignature(signatureHeader, payload, secret, out var timestamp))
            {
                return Task.FromResult(new WebhookResult { Success = false, Message = "Invalid signature" });
            }

            try
            {
                // Parse once as both typed and raw for robust extraction
                var evt = JsonSerializer.Deserialize<StripeWebhookEvent>(payload);
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                string eventType = evt?.Type ?? string.Empty;
                string objId = evt?.Data?.Object?.Id ?? string.Empty;
                string priceId = evt?.Data?.Object?.Lines?.Data?.FirstOrDefault()?.Price?.Id ?? string.Empty;
                string customerId = evt?.Data?.Object?.GetCustomerId() ?? ExtractCustomerId(root);

                // Extract additional IDs for telemetry
                string subscriptionId = ExtractSubscriptionId(root, eventType, objId);
                string invoiceId = ExtractInvoiceId(eventType, objId);
                string paymentIntentId = ExtractPaymentIntentId(root);

                if (string.IsNullOrEmpty(eventType))
                {
                    eventType = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? string.Empty : string.Empty;
                    objId = string.IsNullOrEmpty(objId) ? ExtractObjectId(root) : objId;
                    priceId = string.IsNullOrEmpty(priceId) ? ExtractPriceIdFromLine(root) : priceId;
                }

                // Track webhook event in Application Insights
                _telemetryService?.TrackWebhookEvent(
                    eventType,
                    objId,
                    customerId,
                    subscriptionId,
                    invoiceId,
                    paymentIntentId,
                    priceId);

                switch (eventType)
                {
                    case "invoice.paid":
                        _logger.LogInformation("Stripe webhook: invoice paid {InvoiceId}", objId);
                        return Task.FromResult(new WebhookResult { Success = true, Message = $"invoice.paid: {objId}", EventType = eventType, ObjectId = objId, PriceId = priceId, CustomerId = customerId });
                    case "invoice.payment_failed":
                        _logger.LogWarning("Stripe webhook: invoice payment failed {InvoiceId}", objId);
                        return Task.FromResult(new WebhookResult { Success = true, Message = $"invoice.payment_failed: {objId}", EventType = eventType, ObjectId = objId, PriceId = priceId, CustomerId = customerId });
                    case "invoice.finalized":
                        _logger.LogInformation("Stripe webhook: invoice finalized {InvoiceId}", objId);
                        return Task.FromResult(new WebhookResult { Success = true, Message = $"invoice.finalized: {objId}", EventType = eventType, ObjectId = objId, PriceId = priceId, CustomerId = customerId });
                    case "customer.subscription.deleted":
                        _logger.LogInformation("Stripe webhook: subscription deleted {SubscriptionId}", objId);
                        return Task.FromResult(new WebhookResult { Success = true, Message = $"customer.subscription.deleted: {objId}", EventType = eventType, ObjectId = objId, PriceId = priceId, CustomerId = customerId });
                    case "customer.subscription.trial_will_end":
                        _logger.LogInformation("Stripe webhook: subscription trial will end {SubscriptionId}", objId);
                        return Task.FromResult(new WebhookResult { Success = true, Message = $"customer.subscription.trial_will_end: {objId}", EventType = eventType, ObjectId = objId, PriceId = priceId, CustomerId = customerId });
                    default:
                        _logger.LogInformation("Stripe webhook: unhandled event {EventType} for object {ObjectId}", eventType, objId);
                        return Task.FromResult(new WebhookResult { Success = true, Message = string.IsNullOrEmpty(eventType) ? "Processed" : $"Unhandled: {eventType}", EventType = eventType, ObjectId = objId, PriceId = priceId, CustomerId = customerId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook payload");
                return Task.FromResult(new WebhookResult { Success = false, Message = "Exception while processing webhook" });
            }
        }

        private static bool VerifyStripeSignature(string signatureHeader, string payload, string secret, out long timestamp)
        {
            timestamp = 0;
            // Stripe-Signature format: t=timestamp,v1=signature[,v1=retrySig...]
            var parts = signatureHeader.Split(',');
            string? tPart = parts.FirstOrDefault(p => p.StartsWith("t=", StringComparison.OrdinalIgnoreCase));
            var v1Parts = parts.Where(p => p.StartsWith("v1=", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (tPart is null || v1Parts.Length == 0) return false;

            // Parse timestamp strictly: t=1234567890
            var eqt = tPart.IndexOf('=');
            if (eqt <= 0 || !long.TryParse(tPart[(eqt + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out timestamp))
            {
                return false;
            }

            // Enforce timestamp tolerance window (e.g., 5 minutes)
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            const long toleranceSeconds = 300; // 5 minutes
            if (Math.Abs(now - timestamp) > toleranceSeconds)
            {
                return false;
            }

            var signedPayload = $"{timestamp}.{payload}";
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(payloadBytes);
            var expectedSignature = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();

            foreach (var v1 in v1Parts)
            {
                var eq = v1.IndexOf('=');
                if (eq <= 0) continue;
                var candidate = v1[(eq + 1)..].Trim();
                if (SecureEquals(candidate, expectedSignature))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SecureEquals(string a, string b)
        {
            // Constant-time comparison
            if (a.Length != b.Length) return false;
            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }

        private static string ExtractObjectId(JsonElement root)
        {
            try
            {
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("object", out var obj) &&
                    obj.TryGetProperty("id", out var id))
                {
                    return id.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // ignore parsing errors
            }
            return string.Empty;
        }

        // Optionally parse price id from raw event object when present
        private static string ExtractPriceIdFromLine(JsonElement root)
        {
            try
            {
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("object", out var obj) &&
                    obj.TryGetProperty("lines", out var lines) &&
                    lines.TryGetProperty("data", out var items) &&
                    items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("price", out var price) && price.TryGetProperty("id", out var pid))
                        {
                            return pid.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        private static string ExtractCustomerId(JsonElement root)
        {
            try
            {
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("object", out var obj))
                {
                    if (obj.TryGetProperty("customer", out var cust))
                    {
                        if (cust.ValueKind == JsonValueKind.String)
                        {
                            return cust.GetString() ?? string.Empty;
                        }
                        if (cust.ValueKind == JsonValueKind.Object && cust.TryGetProperty("id", out var cid))
                        {
                            return cid.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        private static string ExtractSubscriptionId(JsonElement root, string eventType, string objectId)
        {
            // If the event type starts with "customer.subscription", the objectId is the subscription ID
            if (eventType.StartsWith("customer.subscription", StringComparison.OrdinalIgnoreCase))
            {
                return objectId;
            }

            try
            {
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("object", out var obj) &&
                    obj.TryGetProperty("subscription", out var sub))
                {
                    if (sub.ValueKind == JsonValueKind.String)
                    {
                        return sub.GetString() ?? string.Empty;
                    }
                    if (sub.ValueKind == JsonValueKind.Object && sub.TryGetProperty("id", out var sid))
                    {
                        return sid.GetString() ?? string.Empty;
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        private static string ExtractInvoiceId(string eventType, string objectId)
        {
            // If the event type starts with "invoice.", the objectId is the invoice ID
            if (eventType.StartsWith("invoice.", StringComparison.OrdinalIgnoreCase))
            {
                return objectId;
            }
            return string.Empty;
        }

        private static string ExtractPaymentIntentId(JsonElement root)
        {
            try
            {
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("object", out var obj) &&
                    obj.TryGetProperty("payment_intent", out var pi))
                {
                    if (pi.ValueKind == JsonValueKind.String)
                    {
                        return pi.GetString() ?? string.Empty;
                    }
                    if (pi.ValueKind == JsonValueKind.Object && pi.TryGetProperty("id", out var pid))
                    {
                        return pid.GetString() ?? string.Empty;
                    }
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
