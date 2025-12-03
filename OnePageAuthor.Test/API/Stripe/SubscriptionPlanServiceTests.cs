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
        // Placeholder API key for unit tests - no actual API calls are made
        // Tests use empty ProductId to skip Stripe API calls and test mapping logic
        private const string TestApiKey = "sk_test_unit_tests_placeholder_key";
        
        private readonly Mock<ILogger<SubscriptionPlanService>> _loggerMock;
        private readonly StripeClient _stripeClient;
        private readonly SubscriptionPlanService _service;

        public SubscriptionPlanServiceTests()
        {
            _loggerMock = new Mock<ILogger<SubscriptionPlanService>>();
            _stripeClient = new StripeClient(TestApiKey);
            _service = new SubscriptionPlanService(_loggerMock.Object, _stripeClient);
        }

        /// <summary>
        /// Creates a test PriceDto with empty ProductId to skip Stripe API calls during unit testing.
        /// </summary>
        private static PriceDto CreateTestPriceDto(
            string id = "price_test",
            string productName = "Test Plan",
            string productDescription = "Test description",
            string nickname = "",
            long unitAmount = 1000,
            string currency = "usd",
            bool active = true,
            bool isRecurring = false,
            string? recurringInterval = null,
            int? recurringIntervalCount = null)
        {
            return new PriceDto
            {
                Id = id,
                ProductId = string.Empty, // Empty ProductId skips Stripe API calls
                ProductName = productName,
                ProductDescription = productDescription,
                Nickname = nickname,
                UnitAmount = unitAmount,
                Currency = currency,
                Active = active,
                IsRecurring = isRecurring,
                RecurringInterval = recurringInterval ?? string.Empty,
                RecurringIntervalCount = recurringIntervalCount,
                CreatedDate = DateTime.UtcNow
            };
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
            // Arrange
            var priceDto = new PriceDto
            {
                Id = "price_123",
                ProductId = string.Empty, // Empty ProductId skips Stripe API calls
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
            // Arrange
            var priceDtos = new List<PriceDto>
            {
                CreateTestPriceDto(
                    id: "price_123",
                    productName: "Basic Plan",
                    productDescription: "A basic subscription plan",
                    unitAmount: 999,
                    isRecurring: true,
                    recurringInterval: "month",
                    recurringIntervalCount: 1),
                CreateTestPriceDto(
                    id: "price_789",
                    productName: "Professional Plan",
                    productDescription: "A professional subscription plan",
                    unitAmount: 1999,
                    isRecurring: true,
                    recurringInterval: "month",
                    recurringIntervalCount: 1)
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
            // Arrange
            var priceDto = CreateTestPriceDto(productName: productName, productDescription: "Test plan");

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
            var priceDto = CreateTestPriceDto(productName: "Test Plan", productDescription: "Test plan");

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
            // Arrange
            var priceDtos = new List<PriceDto>
            {
                CreateTestPriceDto(
                    id: "price_valid",
                    productName: "Valid Plan",
                    productDescription: "A valid plan",
                    unitAmount: 999),
                null!, // This will cause an error but should be handled gracefully
                CreateTestPriceDto(
                    id: "price_valid2",
                    productName: "Another Valid Plan",
                    productDescription: "Another valid plan",
                    unitAmount: 1999)
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
            // Arrange
            var priceDto = CreateTestPriceDto(
                productName: productName ?? string.Empty,
                nickname: nickname ?? string.Empty);

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
            // Arrange
            var priceDto = CreateTestPriceDto(
                productName: "  Advanced   Premium   Solution  ",
                nickname: string.Empty);

            // Act
            var result = await _service.MapToSubscriptionPlanAsync(priceDto);

            // Assert
            Assert.NotNull(result.Label);
            Assert.NotEmpty(result.Label);
            Assert.Equal("Premium", result.Label); // Should extract "Premium" from the product name
        }
    }
}