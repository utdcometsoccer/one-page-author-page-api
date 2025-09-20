using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Stripe;

namespace OnePageAuthor.Test.Stripe
{
    public class ListSubscriptionsTests
    {
        [Fact]
        public void Mapper_MapsBasicFields_AndPagination()
        {
            // Arrange: build a minimal Stripe Subscription list
            var price = new Price
            {
                Id = "price_123",
                UnitAmount = 999,
                Currency = "usd",
                Nickname = "Basic",
                Recurring = new PriceRecurring
                {
                    Interval = "month",
                    IntervalCount = 1
                }
            };

            var item = new SubscriptionItem
            {
                Price = price,
                Quantity = 1
            };

            var invoice = new Invoice
            {
                Id = "in_123"
            };

            var sub = new Subscription
            {
                Id = "sub_123",
                Status = "active",
                CustomerId = "cus_123",
                LatestInvoice = invoice,
                LatestInvoiceId = invoice.Id,
                Items = new StripeList<SubscriptionItem> { Data = new List<SubscriptionItem> { item } }
            };

            var stripeList = new StripeList<Subscription> { Data = new List<Subscription> { sub }, HasMore = false };

            // Act
            var dto = SubscriptionMappers.Map(stripeList.Data, stripeList.HasMore);

            // Assert
            Assert.False(dto.HasMore);
            Assert.Equal("sub_123", dto.LastId);
            Assert.Single(dto.Items);

            var mapped = dto.Items[0];
            Assert.Equal("sub_123", mapped.Id);
            Assert.Equal("active", mapped.Status);
            Assert.Equal("cus_123", mapped.CustomerId);
            Assert.Equal("in_123", mapped.LatestInvoiceId);
            // Not populated by mapper in this SDK version
            Assert.Equal(string.Empty, mapped.LatestInvoicePaymentIntentId);

            Assert.Single(mapped.Items);
            var mappedItem = mapped.Items[0];
            Assert.Equal("price_123", mappedItem.PriceId);
            Assert.Equal(999, mappedItem.UnitAmount);
            Assert.Equal("usd", mappedItem.Currency);
            Assert.Equal("Basic", mappedItem.Nickname);
            Assert.Equal("month", mappedItem.RecurringInterval);
            Assert.Equal(1, mappedItem.RecurringIntervalCount);
        }

        [Fact]
        public void Mapper_HandlesNulls_Gracefully()
        {
            // Arrange: subscription with nulls
            var sub = new Subscription
            {
                Id = "sub_nulls"
            };
            var stripeList = new StripeList<Subscription> { Data = new List<Subscription> { sub }, HasMore = true };

            // Act
            var dto = SubscriptionMappers.Map(stripeList.Data, stripeList.HasMore);

            // Assert
            Assert.True(dto.HasMore);
            Assert.Equal("sub_nulls", dto.LastId);
            var mapped = dto.Items[0];
            Assert.Equal(string.Empty, mapped.Status);
            Assert.Equal(string.Empty, mapped.CustomerId);
            Assert.Equal(string.Empty, mapped.LatestInvoiceId);
            Assert.Equal(string.Empty, mapped.LatestInvoicePaymentIntentId);
            Assert.Empty(mapped.Items);
        }
    }
}
