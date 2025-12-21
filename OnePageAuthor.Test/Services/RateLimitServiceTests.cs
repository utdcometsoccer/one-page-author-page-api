using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Services;

namespace OnePageAuthor.Test.Services
{
    public class RateLimitServiceTests
    {
        private readonly Mock<ILogger<RateLimitService>> _mockLogger;
        private readonly RateLimitService _rateLimitService;

        public RateLimitServiceTests()
        {
            _mockLogger = new Mock<ILogger<RateLimitService>>();
            _rateLimitService = new RateLimitService(_mockLogger.Object, maxRequestsPerMinute: 10);
        }

        [Fact]
        public async Task IsRequestAllowedAsync_FirstRequest_ReturnsTrue()
        {
            // Arrange
            var ipAddress = "192.168.1.1";
            var endpoint = "leads";

            // Act
            var result = await _rateLimitService.IsRequestAllowedAsync(ipAddress, endpoint);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsRequestAllowedAsync_WithinLimit_ReturnsTrue()
        {
            // Arrange
            var ipAddress = "192.168.1.2";
            var endpoint = "leads";

            // Act - Make 9 requests (within limit of 10)
            for (int i = 0; i < 9; i++)
            {
                await _rateLimitService.RecordRequestAsync(ipAddress, endpoint);
            }

            var result = await _rateLimitService.IsRequestAllowedAsync(ipAddress, endpoint);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsRequestAllowedAsync_ExceedsLimit_ReturnsFalse()
        {
            // Arrange
            var ipAddress = "192.168.1.3";
            var endpoint = "leads";

            // Act - Make 10 requests (at limit)
            for (int i = 0; i < 10; i++)
            {
                await _rateLimitService.RecordRequestAsync(ipAddress, endpoint);
            }

            var result = await _rateLimitService.IsRequestAllowedAsync(ipAddress, endpoint);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsRequestAllowedAsync_DifferentIPs_TrackedSeparately()
        {
            // Arrange
            var ipAddress1 = "192.168.1.4";
            var ipAddress2 = "192.168.1.5";
            var endpoint = "leads";

            // Act - Max out requests for IP 1
            for (int i = 0; i < 10; i++)
            {
                await _rateLimitService.RecordRequestAsync(ipAddress1, endpoint);
            }

            var result1 = await _rateLimitService.IsRequestAllowedAsync(ipAddress1, endpoint);
            var result2 = await _rateLimitService.IsRequestAllowedAsync(ipAddress2, endpoint);

            // Assert
            Assert.False(result1); // IP 1 is at limit
            Assert.True(result2);  // IP 2 has no requests
        }

        [Fact]
        public async Task IsRequestAllowedAsync_DifferentEndpoints_TrackedSeparately()
        {
            // Arrange
            var ipAddress = "192.168.1.6";
            var endpoint1 = "leads";
            var endpoint2 = "authors";

            // Act - Max out requests for endpoint1
            for (int i = 0; i < 10; i++)
            {
                await _rateLimitService.RecordRequestAsync(ipAddress, endpoint1);
            }

            var result1 = await _rateLimitService.IsRequestAllowedAsync(ipAddress, endpoint1);
            var result2 = await _rateLimitService.IsRequestAllowedAsync(ipAddress, endpoint2);

            // Assert
            Assert.False(result1); // endpoint1 is at limit
            Assert.True(result2);  // endpoint2 has no requests
        }

        [Fact]
        public async Task IsRequestAllowedAsync_NullIpAddress_ReturnsTrue()
        {
            // Arrange
            string? ipAddress = null;
            var endpoint = "leads";

            // Act
            var result = await _rateLimitService.IsRequestAllowedAsync(ipAddress!, endpoint);

            // Assert
            Assert.True(result); // Should allow if IP is unknown
        }

        [Fact]
        public async Task GetRemainingRequestsAsync_InitialState_ReturnsMaxRequests()
        {
            // Arrange
            var ipAddress = "192.168.1.7";
            var endpoint = "leads";

            // Act
            var remaining = await _rateLimitService.GetRemainingRequestsAsync(ipAddress, endpoint);

            // Assert
            Assert.Equal(10, remaining);
        }

        [Fact]
        public async Task GetRemainingRequestsAsync_AfterRequests_ReturnsCorrectCount()
        {
            // Arrange
            var ipAddress = "192.168.1.8";
            var endpoint = "leads";

            // Act - Make 3 requests
            for (int i = 0; i < 3; i++)
            {
                await _rateLimitService.RecordRequestAsync(ipAddress, endpoint);
            }

            var remaining = await _rateLimitService.GetRemainingRequestsAsync(ipAddress, endpoint);

            // Assert
            Assert.Equal(7, remaining); // 10 - 3 = 7
        }

        [Fact]
        public async Task GetRemainingRequestsAsync_AtLimit_ReturnsZero()
        {
            // Arrange
            var ipAddress = "192.168.1.9";
            var endpoint = "leads";

            // Act - Make 10 requests
            for (int i = 0; i < 10; i++)
            {
                await _rateLimitService.RecordRequestAsync(ipAddress, endpoint);
            }

            var remaining = await _rateLimitService.GetRemainingRequestsAsync(ipAddress, endpoint);

            // Assert
            Assert.Equal(0, remaining);
        }

        [Fact]
        public async Task GetRemainingRequestsAsync_ExceedsLimit_ReturnsZero()
        {
            // Arrange
            var ipAddress = "192.168.1.10";
            var endpoint = "leads";

            // Act - Make 12 requests (over limit)
            for (int i = 0; i < 12; i++)
            {
                await _rateLimitService.RecordRequestAsync(ipAddress, endpoint);
            }

            var remaining = await _rateLimitService.GetRemainingRequestsAsync(ipAddress, endpoint);

            // Assert
            Assert.Equal(0, remaining); // Can't go negative
        }

        [Fact]
        public async Task RecordRequestAsync_NullIpAddress_DoesNotThrow()
        {
            // Arrange
            string? ipAddress = null;
            var endpoint = "leads";

            // Act & Assert - Should not throw
            await _rateLimitService.RecordRequestAsync(ipAddress!, endpoint);
        }

        [Fact]
        public async Task RateLimitService_WithCustomMaxRequests_EnforcesCorrectLimit()
        {
            // Arrange
            var customService = new RateLimitService(_mockLogger.Object, maxRequestsPerMinute: 5);
            var ipAddress = "192.168.1.11";
            var endpoint = "leads";

            // Act - Make 5 requests
            for (int i = 0; i < 5; i++)
            {
                await customService.RecordRequestAsync(ipAddress, endpoint);
            }

            var isAllowed = await customService.IsRequestAllowedAsync(ipAddress, endpoint);
            var remaining = await customService.GetRemainingRequestsAsync(ipAddress, endpoint);

            // Assert
            Assert.False(isAllowed);
            Assert.Equal(0, remaining);
        }
    }
}
