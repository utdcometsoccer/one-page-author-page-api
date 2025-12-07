using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;
using Moq;
using OnePageAuthorLib.Interfaces.Stripe;
using Stripe;

namespace OnePageAuthor.Test.Stripe
{
    public class SubscriptionsServiceTests
    {
        [Fact]
        public void CreateSubscriptionRequest_DomainName_IsOptional()
        {
            // Arrange & Act
            var request = new CreateSubscriptionRequest
            {
                PriceId = "price_123",
                CustomerId = "cus_456"
            };

            // Assert
            Assert.Null(request.DomainName);
            Assert.NotEmpty(request.PriceId);
            Assert.NotEmpty(request.CustomerId);
        }

        [Fact]
        public void CreateSubscriptionRequest_WithDomainName_SetsProperty()
        {
            // Arrange & Act
            var request = new CreateSubscriptionRequest
            {
                PriceId = "price_123",
                CustomerId = "cus_456",
                DomainName = "test.example.com"
            };

            // Assert
            Assert.Equal("test.example.com", request.DomainName);
            Assert.NotEmpty(request.PriceId);
            Assert.NotEmpty(request.CustomerId);
        }

        [Fact]
        public void CreateSubscriptionRequest_WithEmptyDomainName_AcceptsEmptyString()
        {
            // Arrange & Act
            var request = new CreateSubscriptionRequest
            {
                PriceId = "price_123",
                CustomerId = "cus_456",
                DomainName = ""
            };

            // Assert
            Assert.NotNull(request.DomainName);
            Assert.Empty(request.DomainName);
        }

        [Fact]
        public void CreateSubscriptionRequest_WithWhitespaceDomainName_AcceptsWhitespace()
        {
            // Arrange & Act
            var request = new CreateSubscriptionRequest
            {
                PriceId = "price_123",
                CustomerId = "cus_456",
                DomainName = "   "
            };

            // Assert
            Assert.NotNull(request.DomainName);
            Assert.True(string.IsNullOrWhiteSpace(request.DomainName));
        }

        [Fact]
        public void CreateSubscriptionRequest_DomainName_SupportsVariousFormats()
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
                var request = new CreateSubscriptionRequest
                {
                    PriceId = "price_123",
                    CustomerId = "cus_456",
                    DomainName = domainName
                };

                // Assert
                Assert.Equal(domainName, request.DomainName);
            }
        }
    }
}
