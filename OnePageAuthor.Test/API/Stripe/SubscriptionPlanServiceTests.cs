using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Stripe;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthor.Test.API.Stripe
{
    public class SubscriptionPlanServiceTests
    {
        private readonly Mock<ILogger<SubscriptionPlanService>> _loggerMock;
        private readonly StripeClient _stripeClient;
        private readonly SubscriptionPlanService _service;

        public SubscriptionPlanServiceTests()
        {
            _loggerMock = new Mock<ILogger<SubscriptionPlanService>>();
            // Use a test API key for the StripeClient - API calls will fail but fallback logic will handle it
            _stripeClient = new StripeClient("sk_test_placeholder_for_unit_tests");
            _service = new SubscriptionPlanService(_loggerMock.Object, _stripeClient);
        }

        [Fact]
        public async Task MapToSubscriptionPlanAsync_NullPriceDto_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.MapToSubscriptionPlanAsync(null!));
        }

        [Fact]
        public async Task MapToSubscriptionPlanAsync_ValidPriceDto_ReturnsSubscriptionPlan()
        {
            // Arrange - Use empty ProductId to skip Stripe API calls in unit tests
            var priceDto = new PriceDto
            {
                Id = "price_123",
                ProductId = "", // Empty ProductId to use fallback logic and avoid Stripe API calls
                ProductName = "Professional Plan",
                ProductDescription = "A professional subscription plan",
                UnitAmount = 1999,
                Currency = "usd",
                Active = true,
                Nickname = "Pro",
                LookupKey = "pro-monthly",
                Type = "recurring",
                IsRecurring = true,
                RecurringInterval = "month",
                RecurringIntervalCount = 1,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var result = await _service.MapToSubscriptionPlanAsync(priceDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(priceDto.Id, result.Id);
            Assert.Equal(priceDto.Id, result.StripePriceId);
            Assert.Equal(priceDto.ProductName, result.Name);
            Assert.Equal(priceDto.ProductDescription, result.Description);
            Assert.Equal(priceDto.AmountDecimal, result.Price);
            Assert.Equal(priceDto.Currency.ToUpperInvariant(), result.Currency);
            Assert.NotNull(result.Features);
            Assert.IsType<List<string>>(result.Features);
        }

        [Fact]
        public async Task MapToSubscriptionPlansAsync_NullPriceDtos_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.MapToSubscriptionPlansAsync(null!));
        }

        [Fact]
        public async Task MapToSubscriptionPlansAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var priceDtos = new List<PriceDto>();

            // Act
            var result = await _service.MapToSubscriptionPlansAsync(priceDtos);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task MapToSubscriptionPlansAsync_ValidPriceDtos_ReturnsSubscriptionPlans()
        {
            // Arrange - Use empty ProductId to skip Stripe API calls in unit tests
            var priceDtos = new List<PriceDto>
            {
                new PriceDto
                {
                    Id = "price_123",
                    ProductId = "", // Empty ProductId to use fallback logic
                    ProductName = "Basic Plan",
                    ProductDescription = "A basic subscription plan",
                    UnitAmount = 999,
                    Currency = "usd",
                    Active = true,
                    IsRecurring = true,
                    RecurringInterval = "month",
                    RecurringIntervalCount = 1,
                    CreatedDate = DateTime.UtcNow
                },
                new PriceDto
                {
                    Id = "price_789",
                    ProductId = "", // Empty ProductId to use fallback logic
                    ProductName = "Professional Plan",
                    ProductDescription = "A professional subscription plan",
                    UnitAmount = 1999,
                    Currency = "usd",
                    Active = true,
                    IsRecurring = true,
                    RecurringInterval = "month",
                    RecurringIntervalCount = 1,
                    CreatedDate = DateTime.UtcNow
                }
            };

            // Act
            var result = await _service.MapToSubscriptionPlansAsync(priceDtos);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("price_123", result[0].Id);
            Assert.Equal("price_789", result[1].Id);
        }

        [Theory]
        [InlineData("Basic Starter", new[] { "Basic author profile", "Single book listing", "Contact form", "Basic social media links" })]
        [InlineData("Professional Plan", new[] { "Professional author profile", "Unlimited book listings", "Custom domain support", "Advanced social media integration", "Analytics dashboard", "Custom themes", "Priority support" })]
        [InlineData("Enterprise Solution", new[] { "Enterprise author profile", "Unlimited everything", "Custom domain with SSL", "Full social media suite", "Advanced analytics", "Custom branding", "API access", "24/7 support", "Custom integrations" })]
        [InlineData("Custom Plan", new[] { "Author profile", "Book listings", "Contact information", "Social media links" })]
        public async Task MapToSubscriptionPlanAsync_DefaultFeatures_ReturnsExpectedFeatures(string productName, string[] expectedFeatures)
        {
            // Arrange - Use empty ProductId to skip Stripe API calls and test fallback logic
            var priceDto = new PriceDto
            {
                Id = "price_test",
                ProductId = "", // Empty ProductId to use fallback logic and avoid Stripe API calls
                ProductName = productName,
                ProductDescription = "Test plan",
                UnitAmount = 1000,
                Currency = "usd",
                Active = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var result = await _service.MapToSubscriptionPlanAsync(priceDto);

            // Assert
            Assert.NotNull(result.Features);
            foreach (var expectedFeature in expectedFeatures)
            {
                Assert.Contains(expectedFeature, result.Features);
            }
        }

        [Fact]
        public async Task MapToSubscriptionPlanAsync_EmptyProductId_ReturnsDefaultFeatures()
        {
            // Arrange
            var priceDto = new PriceDto
            {
                Id = "price_test",
                ProductId = "",
                ProductName = "Test Plan",
                ProductDescription = "Test plan",
                UnitAmount = 1000,
                Currency = "usd",
                Active = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var result = await _service.MapToSubscriptionPlanAsync(priceDto);

            // Assert
            Assert.NotNull(result.Features);
            Assert.NotEmpty(result.Features); // Should get default features based on product name
            // Should contain default features since product name doesn't match known patterns
            Assert.Contains("Author profile", result.Features);
            Assert.Contains("Book listings", result.Features);
        }

        [Fact]
        public async Task MapToSubscriptionPlansAsync_WithInvalidPriceDto_ContinuesProcessingOthers()
        {
            // Arrange - Use empty ProductId to skip Stripe API calls in unit tests
            var priceDtos = new List<PriceDto>
            {
                new PriceDto
                {
                    Id = "price_valid",
                    ProductId = "", // Empty ProductId to use fallback logic
                    ProductName = "Valid Plan",
                    ProductDescription = "A valid plan",
                    UnitAmount = 999,
                    Currency = "usd",
                    Active = true,
                    CreatedDate = DateTime.UtcNow
                },
                null!, // This will cause an error but should be handled gracefully
                new PriceDto
                {
                    Id = "price_valid2",
                    ProductId = "", // Empty ProductId to use fallback logic
                    ProductName = "Another Valid Plan",
                    ProductDescription = "Another valid plan",
                    UnitAmount = 1999,
                    Currency = "usd",
                    Active = true,
                    CreatedDate = DateTime.UtcNow
                }
            };

            // Act
            var result = await _service.MapToSubscriptionPlansAsync(priceDtos);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Should have 2 valid plans, null one skipped
            Assert.Equal("price_valid", result[0].Id);
            Assert.Equal("price_valid2", result[1].Id);
        }

        [Theory]
        [InlineData("Pro Monthly", "Pro Monthly", "Pro Monthly")] // Valid nickname should be used
        [InlineData("", "Professional Plan", "Pro")] // Empty nickname should fall back to product name extraction
        [InlineData(null, "Basic Starter Plan", "Basic")] // Null nickname should fall back to product name extraction
        [InlineData("   ", "Enterprise Solution", "Enterprise")] // Whitespace nickname should fall back to product name extraction
        [InlineData(null, "", "Plan")] // Both null/empty should result in default "Plan"
        [InlineData("", null, "Plan")] // Both empty/null should result in default "Plan"
        [InlineData(null, "   ", "Plan")] // Both null/whitespace should result in default "Plan"
        [InlineData("Valid Nickname", "", "Valid Nickname")] // Valid nickname with empty product name
        [InlineData("My Custom Label", "Some Product", "My Custom Label")] // Valid nickname should take precedence
        public async Task MapToSubscriptionPlanAsync_Label_AlwaysHasValidValue(string? nickname, string? productName, string expectedLabel)
        {
            // Arrange - Use empty ProductId to skip Stripe API calls in unit tests
            var priceDto = new PriceDto
            {
                Id = "price_test",
                ProductId = "", // Empty ProductId to use fallback logic and avoid Stripe API calls
                ProductName = productName ?? string.Empty,
                ProductDescription = "Test description",
                Nickname = nickname ?? string.Empty,
                UnitAmount = 1000,
                Currency = "usd",
                Active = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var result = await _service.MapToSubscriptionPlanAsync(priceDto);

            // Assert
            Assert.NotNull(result.Label);
            Assert.NotEmpty(result.Label);
            Assert.Equal(expectedLabel, result.Label);
            Assert.False(string.IsNullOrWhiteSpace(result.Label));
        }

        [Fact]
        public async Task MapToSubscriptionPlanAsync_Label_HandlesComplexProductNames()
        {
            // Arrange - Use empty ProductId to skip Stripe API calls in unit tests
            var priceDto = new PriceDto
            {
                Id = "price_test",
                ProductId = "", // Empty ProductId to use fallback logic and avoid Stripe API calls
                ProductName = "  Advanced   Premium   Solution  ",
                ProductDescription = "Test description",
                Nickname = string.Empty,
                UnitAmount = 1000,
                Currency = "usd",
                Active = true,
                CreatedDate = DateTime.UtcNow
            };

            // Act
            var result = await _service.MapToSubscriptionPlanAsync(priceDto);

            // Assert
            Assert.NotNull(result.Label);
            Assert.NotEmpty(result.Label);
            Assert.Equal("Premium", result.Label); // Should extract "Premium" from the product name
        }
    }
}