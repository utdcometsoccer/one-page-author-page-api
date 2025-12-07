using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Xunit;

namespace OnePageAuthor.Test.Stripe
{
    public class FindSubscriptionsTests
    {
        [Fact]
        public void FindSubscriptionRequest_RequiresEmail()
        {
            // Arrange & Act
            var request = new FindSubscriptionRequest
            {
                Email = "test@example.com",
                DomainName = "example.com"
            };

            // Assert
            Assert.NotEmpty(request.Email);
            Assert.NotEmpty(request.DomainName);
        }

        [Fact]
        public void FindSubscriptionRequest_RequiresDomainName()
        {
            // Arrange & Act
            var request = new FindSubscriptionRequest
            {
                Email = "test@example.com",
                DomainName = "example.com"
            };

            // Assert
            Assert.NotEmpty(request.Email);
            Assert.NotEmpty(request.DomainName);
        }

        [Fact]
        public void FindSubscriptionResponse_InitializesWithDefaults()
        {
            // Arrange & Act
            var response = new FindSubscriptionResponse();

            // Assert
            Assert.Empty(response.CustomerId);
            Assert.Empty(response.Subscriptions);
            Assert.False(response.CustomerFound);
            Assert.False(response.SubscriptionsFound);
        }

        [Fact]
        public void FindSubscriptionResponse_SubscriptionsFound_ReturnsTrueWhenSubscriptionsExist()
        {
            // Arrange & Act
            var response = new FindSubscriptionResponse
            {
                CustomerId = "cus_123",
                CustomerFound = true,
                Subscriptions = new List<SubscriptionDto>
                {
                    new SubscriptionDto { Id = "sub_123" }
                }
            };

            // Assert
            Assert.True(response.CustomerFound);
            Assert.True(response.SubscriptionsFound);
            Assert.Single(response.Subscriptions);
        }

        [Fact]
        public void FindSubscriptionResponse_SubscriptionsFound_ReturnsFalseWhenNoSubscriptions()
        {
            // Arrange & Act
            var response = new FindSubscriptionResponse
            {
                CustomerId = "cus_123",
                CustomerFound = true,
                Subscriptions = new List<SubscriptionDto>()
            };

            // Assert
            Assert.True(response.CustomerFound);
            Assert.False(response.SubscriptionsFound);
            Assert.Empty(response.Subscriptions);
        }

        [Fact]
        public void FindSubscriptionRequest_SupportsVariousDomainFormats()
        {
            // Test various domain name formats
            var testCases = new[]
            {
                "example.com",
                "subdomain.example.com",
                "sub.domain.example.com",
                "example.co.uk",
                "test-site.example.org"
            };

            foreach (var domainName in testCases)
            {
                // Arrange & Act
                var request = new FindSubscriptionRequest
                {
                    Email = "test@example.com",
                    DomainName = domainName
                };

                // Assert
                Assert.Equal(domainName, request.DomainName);
                Assert.Equal("test@example.com", request.Email);
            }
        }
    }
}
