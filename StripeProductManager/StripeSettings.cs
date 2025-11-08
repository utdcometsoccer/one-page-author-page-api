namespace InkStainedWretch.StripeProductManager
{
    public class StripeSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public List<StripeProductSettings> Products { get; set; } = new();
    }

    public class StripeProductSettings
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long PriceInCents { get; set; }
        public string PriceNickname { get; set; } = string.Empty;
        public int IntervalCount { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public List<string> SupportedCultures { get; set; } = new();
        public string PrimaryCulture { get; set; } = "en-US";
        public Dictionary<string, ProductCultureInfo> CultureSpecificInfo { get; set; } = new();
    }

    public class ProductCultureInfo
    {
        public string LocalizedName { get; set; } = string.Empty;
        public string LocalizedDescription { get; set; } = string.Empty;
        public string LocalizedNickname { get; set; } = string.Empty;
    }
}