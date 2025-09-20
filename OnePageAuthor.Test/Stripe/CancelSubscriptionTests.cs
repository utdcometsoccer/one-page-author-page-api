using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Stripe;

namespace OnePageAuthor.Test.Stripe
{
    public class CancelSubscriptionTests
    {
        [Fact]
        public void Mapper_Maps_Canceled_Subscription()
        {
            var sub = new Subscription
            {
                Id = "sub_123",
                Status = "canceled",
                CanceledAt = DateTime.UtcNow
            };

            var dto = CancelSubscriptionMappers.Map(sub, "sub_123");

            Assert.Equal("sub_123", dto.SubscriptionId);
            Assert.Equal("canceled", dto.Status);
            Assert.True(dto.CanceledAt.HasValue);
        }

        [Fact]
        public void Mapper_Handles_Null_Subscription()
        {
            var dto = CancelSubscriptionMappers.Map(null, "sub_fallback");
            Assert.Equal("sub_fallback", dto.SubscriptionId);
            Assert.Equal(string.Empty, dto.Status);
            Assert.False(dto.CanceledAt.HasValue);
        }
    }
}
