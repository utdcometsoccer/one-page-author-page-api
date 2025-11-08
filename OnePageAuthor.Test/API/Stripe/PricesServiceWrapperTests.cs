using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthor.Test.API.Stripe
{
    public class PricesServiceWrapperTests
    {
        private readonly Mock<IPriceService> _innerServiceMock;
        private readonly Mock<ISubscriptionPlanService> _subscriptionPlanServiceMock;
        private readonly Mock<ILogger<PricesServiceWrapper>> _loggerMock;
        private readonly PricesServiceWrapper _wrapper;

        public PricesServiceWrapperTests()
        {
            _innerServiceMock = new Mock<IPriceService>();
            _subscriptionPlanServiceMock = new Mock<ISubscriptionPlanService>();
            _loggerMock = new Mock<ILogger<PricesServiceWrapper>>();
            _wrapper = new PricesServiceWrapper(_innerServiceMock.Object, _subscriptionPlanServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetPricesAsync_ValidRequest_ReturnsSubscriptionPlanListResponse()
        {
            // Arrange
            var request = new PriceListRequest { Active = true, Limit = 10 };
            var priceDtos = new List<PriceDto>
            {
                new PriceDto { Id = "price_1", ProductName = "Basic Plan" },
                new PriceDto { Id = "price_2", ProductName = "Pro Plan" }
            };
            var priceListResponse = new PriceListResponse
            {
                Prices = priceDtos,
                HasMore = false,
                LastId = "price_2"
            };
            var subscriptionPlans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan 
                { 
                    Id = "price_1", 
                    StripePriceId = "price_1", 
                    Label = "Basic", 
                    Name = "Basic Plan", 
                    Description = "Basic plan description", 
                    Currency = "USD", 
                    Features = new List<string> { "Basic features" } 
                },
                new SubscriptionPlan 
                { 
                    Id = "price_2", 
                    StripePriceId = "price_2", 
                    Label = "Pro", 
                    Name = "Pro Plan", 
                    Description = "Pro plan description", 
                    Currency = "USD", 
                    Features = new List<string> { "Pro features" } 
                }
            };

            _innerServiceMock.Setup(x => x.GetPricesAsync(request))
                .ReturnsAsync(priceListResponse);
            _subscriptionPlanServiceMock.Setup(x => x.MapToSubscriptionPlansAsync(priceDtos))
                .ReturnsAsync(subscriptionPlans);

            // Act
            var result = await _wrapper.GetPricesAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(priceListResponse.LastId, result.LastId);
            Assert.Equal(priceListResponse.HasMore, result.HasMore);
            Assert.Equal(2, result.Plans.Count);
            Assert.Equal("price_1", result.Plans[0].Id);
            Assert.Equal("price_2", result.Plans[1].Id);
            Assert.Contains("Basic features", result.Plans[0].Features);
            Assert.Contains("Pro features", result.Plans[1].Features);

            _innerServiceMock.Verify(x => x.GetPricesAsync(request), Times.Once);
            _subscriptionPlanServiceMock.Verify(x => x.MapToSubscriptionPlansAsync(priceDtos), Times.Once);
        }

        [Fact]
        public async Task GetPriceByIdAsync_ValidPriceId_ReturnsSubscriptionPlan()
        {
            // Arrange
            var priceId = "price_123";
            var priceDto = new PriceDto 
            { 
                Id = priceId, 
                ProductName = "Test Plan",
                UnitAmount = 1999,
                Currency = "usd"
            };
            var subscriptionPlan = new SubscriptionPlan 
            { 
                Id = priceId, 
                StripePriceId = priceId,
                Label = "Test",
                Name = "Test Plan", 
                Description = "Test plan description",
                Price = 19.99m,
                Currency = "USD",
                Features = new List<string> { "Test features" } 
            };

            _innerServiceMock.Setup(x => x.GetPriceByIdAsync(priceId))
                .ReturnsAsync(priceDto);
            _subscriptionPlanServiceMock.Setup(x => x.MapToSubscriptionPlanAsync(priceDto))
                .ReturnsAsync(subscriptionPlan);

            // Act
            var result = await _wrapper.GetPriceByIdAsync(priceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(priceId, result.Id);
            Assert.Equal("Test Plan", result.Name);
            Assert.Equal(19.99m, result.Price);
            Assert.Equal("USD", result.Currency);
            Assert.Contains("Test features", result.Features);

            _innerServiceMock.Verify(x => x.GetPriceByIdAsync(priceId), Times.Once);
            _subscriptionPlanServiceMock.Verify(x => x.MapToSubscriptionPlanAsync(priceDto), Times.Once);
        }

        [Fact]
        public async Task GetPriceByIdAsync_PriceNotFound_ReturnsNull()
        {
            // Arrange
            var priceId = "price_nonexistent";

            _innerServiceMock.Setup(x => x.GetPriceByIdAsync(priceId))
                .ReturnsAsync((PriceDto?)null);

            // Act
            var result = await _wrapper.GetPriceByIdAsync(priceId);

            // Assert
            Assert.Null(result);

            _innerServiceMock.Verify(x => x.GetPriceByIdAsync(priceId), Times.Once);
            _subscriptionPlanServiceMock.Verify(x => x.MapToSubscriptionPlanAsync(It.IsAny<PriceDto>()), Times.Never);
        }

        [Fact]
        public async Task GetPricesAsync_EmptyPriceList_ReturnsEmptySubscriptionPlanList()
        {
            // Arrange
            var request = new PriceListRequest { Active = true };
            var priceListResponse = new PriceListResponse
            {
                Prices = new List<PriceDto>(),
                HasMore = false,
                LastId = ""
            };
            var subscriptionPlans = new List<SubscriptionPlan>();

            _innerServiceMock.Setup(x => x.GetPricesAsync(request))
                .ReturnsAsync(priceListResponse);
            _subscriptionPlanServiceMock.Setup(x => x.MapToSubscriptionPlansAsync(It.IsAny<IEnumerable<PriceDto>>()))
                .ReturnsAsync(subscriptionPlans);

            // Act
            var result = await _wrapper.GetPricesAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Plans);
            Assert.False(result.HasMore);
            Assert.Equal("", result.LastId);

            _innerServiceMock.Verify(x => x.GetPricesAsync(request), Times.Once);
            _subscriptionPlanServiceMock.Verify(x => x.MapToSubscriptionPlansAsync(It.IsAny<IEnumerable<PriceDto>>()), Times.Once);
        }

        [Fact]
        public void Constructor_ValidDependencies_DoesNotThrow()
        {
            // Act & Assert
            var wrapper = new PricesServiceWrapper(_innerServiceMock.Object, _subscriptionPlanServiceMock.Object, _loggerMock.Object);
            Assert.NotNull(wrapper);
        }

        [Fact]
        public async Task GetPricesAsync_LogsDebugMessage()
        {
            // Arrange
            var request = new PriceListRequest();
            var priceListResponse = new PriceListResponse
            {
                Prices = new List<PriceDto>(),
                HasMore = false,
                LastId = ""
            };

            _innerServiceMock.Setup(x => x.GetPricesAsync(request))
                .ReturnsAsync(priceListResponse);
            _subscriptionPlanServiceMock.Setup(x => x.MapToSubscriptionPlansAsync(It.IsAny<IEnumerable<PriceDto>>()))
                .ReturnsAsync(new List<SubscriptionPlan>());

            // Act
            await _wrapper.GetPricesAsync(request);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Wrapper forwarding GetPricesAsync")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetPriceByIdAsync_LogsDebugMessage()
        {
            // Arrange
            var priceId = "price_test";
            _innerServiceMock.Setup(x => x.GetPriceByIdAsync(priceId))
                .ReturnsAsync((PriceDto?)null);

            // Act
            await _wrapper.GetPriceByIdAsync(priceId);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Wrapper forwarding GetPriceByIdAsync") && v.ToString()!.Contains(priceId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}