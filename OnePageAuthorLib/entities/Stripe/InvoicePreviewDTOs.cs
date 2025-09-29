namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    public class InvoicePreviewRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? SubscriptionId { get; set; }
        public string? SubscriptionItemId { get; set; }
        public string? PriceId { get; set; }
        public long? Quantity { get; set; }
        public string? Currency { get; set; }
        // Match Stripe.NET enum for proration; optional
        public string? ProrationBehavior { get; set; }
    }

    public class InvoicePreviewResponse
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public long AmountDue { get; set; }
        public long Subtotal { get; set; }
        public long Total { get; set; }
        public List<InvoiceLineDto> Lines { get; set; } = new();

        public decimal AmountDueDecimal => AmountDue / 100m;
        public decimal SubtotalDecimal => Subtotal / 100m;
        public decimal TotalDecimal => Total / 100m;
    }

    public class InvoiceLineDto
    {
        public string Description { get; set; } = string.Empty;
        public string PriceId { get; set; } = string.Empty;
        public long Quantity { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal AmountDecimal => Amount / 100m;
    }
}
