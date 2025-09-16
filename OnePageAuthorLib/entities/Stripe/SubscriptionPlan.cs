using System.Text.Json;
using System.Text.Json.Serialization;

namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents a subscription plan that serializes to the structure in sample-subscription-plan.json.
    /// </summary>
    public class SubscriptionPlan
    {
    /// <summary>
    /// An identifier copied from the source (e.g., Stripe Price Id) that will be serialized as "id".
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

        /// <summary>
        /// A short label for the plan, e.g., "Basic".
        /// </summary>
        [JsonPropertyName("label")]
        public required string Label { get; set; }

        /// <summary>
        /// The price amount, typically in the plan's currency units (e.g., 9.99).
        /// </summary>
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// The plan duration in months (e.g., 1 for monthly).
        /// </summary>
        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        /// <summary>
        /// The features included in this plan.
        /// </summary>
        [JsonPropertyName("features")]
        public List<string> Features { get; set; } = new();

        /// <summary>
        /// The Stripe Price identifier associated with this plan.
        /// </summary>
        [JsonPropertyName("stripePriceId")]
        public required string StripePriceId { get; set; }

        /// <summary>
        /// A human-readable name for the plan, e.g., "Basic Monthly".
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        /// <summary>
        /// A description of the plan, e.g., "Starter plan billed monthly".
        /// </summary>
        [JsonPropertyName("description")]
        public required string Description { get; set; }

        /// <summary>
        /// The three-letter ISO currency code, e.g., "USD".
        /// </summary>
        [JsonPropertyName("currency")]
        public required string Currency { get; set; }

        /// <summary>
        /// Serializes this plan into a JSON string matching the expected schema.
        /// </summary>
        public string ToJson(JsonSerializerOptions? options = null)
            => JsonSerializer.Serialize(this, options ?? DefaultJsonOptions);

        /// <summary>
        /// Deserializes a JSON string into a <see cref="SubscriptionPlan"/> instance.
        /// </summary>
        public static SubscriptionPlan? FromJson(string json, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize<SubscriptionPlan>(json, options ?? DefaultJsonOptions);

        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNamingPolicy = null, // Honor JsonPropertyName attributes exactly
            WriteIndented = false
        };
    }
}
