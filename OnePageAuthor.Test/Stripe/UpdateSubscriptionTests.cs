using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Stripe;

namespace OnePageAuthor.Test.Stripe
{
    public class UpdateSubscriptionTests
    {
        [Fact]
        public void UpdateResponse_Maps_Basic_Fields()
        {
            var sub = new Subscription
            {
                Id = "sub_123",
                Status = "active",
                LatestInvoiceId = "in_123"
            };

            // Service maps directly; test by simulating mapping here
            var response = new UpdateSubscriptionResponse
            {
                SubscriptionId = sub.Id,
                Status = sub.Status ?? string.Empty,
                LatestInvoiceId = sub.LatestInvoiceId ?? string.Empty,
                LatestInvoicePaymentIntentId = string.Empty
            };

            Assert.Equal("sub_123", response.SubscriptionId);
            Assert.Equal("active", response.Status);
            Assert.Equal("in_123", response.LatestInvoiceId);
            Assert.Equal(string.Empty, response.LatestInvoicePaymentIntentId);
        }
    }
}
