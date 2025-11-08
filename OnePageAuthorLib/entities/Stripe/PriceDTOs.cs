namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    public class PriceListRequest
    {
        public PriceListRequest()
        {
            ProductId = string.Empty;
            Currency = string.Empty;
            Culture = string.Empty;
        }
        public bool? Active { get; set; }
        public string ProductId { get; set; }
        public int? Limit { get; set; } = 100;
        public string Currency { get; set; }
        public bool IncludeProductDetails { get; set; } = true;
        public string Culture { get; set; }
    }

    public class PriceListResponse
    {
        public List<PriceDto> Prices { get; set; } = new List<PriceDto>();
        public bool HasMore { get; set; }
        public string LastId { get; set; } = string.Empty;
    }

    public class PriceDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public long? UnitAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool Active { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string LookupKey { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public string RecurringInterval { get; set; } = string.Empty;
        public long? RecurringIntervalCount { get; set; }
        public decimal AmountDecimal => UnitAmount.HasValue ? (decimal)UnitAmount.Value / 100 : 0;
        public string FormattedAmount => $"{AmountDecimal:0.00} {Currency?.ToUpper()}";
        public string RecurringDescription => IsRecurring
            ? $"{RecurringIntervalCount} {RecurringInterval}{(RecurringIntervalCount > 1 ? "s" : "")}"
            : "One-time";
        public DateTime CreatedDate { get; set; }
    }
}
