using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace OnePageAuthor.Test.Stripe
{
    internal static class InvoicePreviewServiceTestAccessor
    {
        public static InvoicePreviewResponse MapFromJson_ForTest(string json) => InvoicePreviewService.MapFromJson(json);
    }
}
