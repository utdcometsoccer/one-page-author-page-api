using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public static class InvoicePreviewMappers
    {
        public static InvoicePreviewResponse Map(Invoice inv)
        {
            var resp = new InvoicePreviewResponse
            {
                InvoiceId = inv.Id ?? string.Empty,
                Currency = inv.Currency ?? string.Empty,
                AmountDue = inv.AmountDue,
                Subtotal = inv.Subtotal,
                Total = inv.Total
            };

            var lines = inv.Lines?.Data ?? new List<InvoiceLineItem>();
            foreach (var l in lines)
            {
                resp.Lines.Add(new InvoiceLineDto
                {
                    Description = l.Description ?? string.Empty,
                    // Some SDK versions may not expose Price on invoice line items; leave empty
                    PriceId = string.Empty,
                    Quantity = l.Quantity ?? 0,
                    // In newer SDKs Amount is non-nullable (long)
                    Amount = l.Amount,
                    Currency = l.Currency ?? inv.Currency ?? string.Empty
                });
            }

            return resp;
        }
    }
}
