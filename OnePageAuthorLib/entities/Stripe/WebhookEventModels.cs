using System.Text.Json;
using System.Text.Json.Serialization;

namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    // Minimal typed models to map the event JSON parsed in WebhookHandler
    public class StripeWebhookEvent
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("data")] public StripeWebhookEventData? Data { get; set; }
    }

    public class StripeWebhookEventData
    {
        [JsonPropertyName("object")] public StripeWebhookObject? Object { get; set; }
    }

    public class StripeWebhookObject
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("lines")] public StripeWebhookLines? Lines { get; set; }
        // 'customer' may be a string id or an expanded object; keep as raw JsonElement
        [JsonPropertyName("customer")] public JsonElement Customer { get; set; }

        public string? GetCustomerId()
        {
            if (Customer.ValueKind == JsonValueKind.String)
            {
                return Customer.GetString();
            }
            if (Customer.ValueKind == JsonValueKind.Object && Customer.TryGetProperty("id", out var cid))
            {
                return cid.GetString();
            }
            return null;
        }
    }

    public class StripeWebhookLines
    {
        [JsonPropertyName("data")] public List<StripeWebhookLineItem>? Data { get; set; }
    }

    public class StripeWebhookLineItem
    {
        [JsonPropertyName("price")] public StripeWebhookPrice? Price { get; set; }
    }

    public class StripeWebhookPrice
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }
}
