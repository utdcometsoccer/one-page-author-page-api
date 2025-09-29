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
    }
}
