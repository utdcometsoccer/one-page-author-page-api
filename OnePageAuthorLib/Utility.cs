using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorAPI
{
    public static class Utility
    {
        public static string GetDataRoot()
        {
            string path = "data";
            return GetAbsolutePath(path);
        }

        public static string GetAbsolutePath(string path)
        {
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", path));
        }

        /// <summary>
        /// Maps a Stripe price DTO into a SubscriptionPlan suitable for serialization.
        /// </summary>
        /// <param name="price">The Stripe price DTO to map.</param>
        /// <returns>A populated <see cref="SubscriptionPlan"/> derived from the Stripe price.</returns>
        [Obsolete("Use ISubscriptionPlanService.MapToSubscriptionPlanAsync instead for dependency injection and Stripe feature retrieval.")]
        public static SubscriptionPlan MapToSubscriptionPlan(PriceDto price)
        {
            if (price == null) throw new ArgumentNullException(nameof(price));

            // Determine duration in YEARS based on recurring interval
            // For month-based intervals, we round up to the nearest year.
            int durationYears = 1;
            if (price.IsRecurring)
            {
                var count = (int)(price.RecurringIntervalCount ?? 1);
                durationYears = price.RecurringInterval?.ToLowerInvariant() switch
                {
                    "month" => (int)Math.Ceiling(count / 12.0),
                    "year" => count,
                    _ => 1
                };
            }

            string label = !string.IsNullOrWhiteSpace(price.Nickname)
                ? price.Nickname
                : (!string.IsNullOrWhiteSpace(price.ProductName) ? price.ProductName : "Plan");

            string cadence = price.IsRecurring
                ? (price.RecurringInterval?.ToLowerInvariant() == "year" ? "Yearly" : "Monthly")
                : "One-time";

            string name = !string.IsNullOrWhiteSpace(price.ProductName)
                ? $"{price.ProductName} {cadence}"
                : $"{label} {cadence}";

            var plan = new SubscriptionPlan
            {
                Id = price.Id,
                Label = label,
                Price = price.AmountDecimal,
                Duration = durationYears,
                Features = new List<string>(),
                StripePriceId = price.Id,
                Name = name,
                Description = price.ProductDescription ?? string.Empty,
                Currency = (price.Currency ?? "").ToUpperInvariant(),
            };

            return plan;
        }

        /// <summary>
        /// Masks sensitive configuration values for safe logging.
        /// Shows first 4 and last 4 characters with asterisks in between.
        /// </summary>
        /// <param name="value">The sensitive value to mask</param>
        /// <param name="notSetText">Text to show when value is null or empty</param>
        /// <returns>Masked string safe for logging</returns>
        public static string MaskSensitiveValue(string? value, string notSetText = "(not set)")
        {
            if (string.IsNullOrWhiteSpace(value)) return notSetText;
            if (value.Length < 8) return "(set)";
            return $"{value[..4]}****{value[^4..]}";
        }

        /// <summary>
        /// Masks URLs and endpoints for safe logging.
        /// Shows first 8 and last 4 characters with asterisks in between to provide more context for debugging.
        /// </summary>
        /// <param name="value">The URL or endpoint to mask</param>
        /// <param name="notSetText">Text to show when value is null or empty</param>
        /// <returns>Masked string safe for logging</returns>
        public static string MaskUrl(string? value, string notSetText = "(not set)")
        {
            if (string.IsNullOrWhiteSpace(value)) return notSetText;
            if (value.Length < 12) return "(set)";
            return $"{value[..8]}****{value[^4..]}";
        }

        /// <summary>
        /// Parses a comma-separated list of JWT issuer URLs into a distinct array.
        /// Trims whitespace, removes trailing slashes, filters empty values, and ensures case-insensitive uniqueness.
        /// </summary>
        /// <param name="validIssuersRaw">Comma-separated string of issuer URLs (e.g., "https://login.microsoftonline.com/tenant1/v2.0, https://login.microsoftonline.com/tenant2/v2.0")</param>
        /// <returns>Array of normalized issuer URLs, or null if the input is null/empty or results in no valid issuers</returns>
        public static string[]? ParseValidIssuers(string? validIssuersRaw)
        {
            if (string.IsNullOrWhiteSpace(validIssuersRaw))
            {
                return null;
            }

            var issuers = validIssuersRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(i => i.TrimEnd('/'))
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return issuers.Length > 0 ? issuers : null;
        }
    }
}
