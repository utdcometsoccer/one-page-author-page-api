using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using InkStainedWretchStripe;
using System.Collections.Generic;

namespace OnePageAuthor.Test.Stripe
{
    /// <summary>
    /// Unit tests for StripeHealthFunction Azure Function
    /// </summary>
    public class StripeHealthFunctionTests
    {
        private readonly Mock<ILogger<StripeHealthFunction>> _mockLogger;
        private readonly Mock<HttpRequest> _mockHttpRequest;

        public StripeHealthFunctionTests()
        {
            _mockLogger = new Mock<ILogger<StripeHealthFunction>>();
            _mockHttpRequest = new Mock<HttpRequest>();
        }

        private IConfiguration CreateConfiguration(string? stripeApiKey)
        {
            var configData = new Dictionary<string, string?>();
            if (stripeApiKey != null)
            {
                configData["STRIPE_API_KEY"] = stripeApiKey;
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var config = CreateConfiguration("sk_test_fake");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StripeHealthFunction(null!, config));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StripeHealthFunction(_mockLogger.Object, null!));
        }

        [Fact]
        public void Run_WithTestKey_ReturnsTestMode()
        {
            // Arrange
            var config = CreateConfiguration("sk_test_" + new string('x', 40));
            var function = new StripeHealthFunction(_mockLogger.Object, config);

            // Act
            var result = function.Run(_mockHttpRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<StripeHealthResponse>(okResult.Value);
            Assert.Equal("test", response.StripeMode);
            Assert.True(response.StripeConnected);
            Assert.Equal("1.0.0", response.Version);
        }

        [Fact]
        public void Run_WithLiveKey_ReturnsLiveMode()
        {
            // Arrange
            var config = CreateConfiguration("sk_live_" + new string('y', 40));
            var function = new StripeHealthFunction(_mockLogger.Object, config);

            // Act
            var result = function.Run(_mockHttpRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<StripeHealthResponse>(okResult.Value);
            Assert.Equal("live", response.StripeMode);
            Assert.True(response.StripeConnected);
            Assert.Equal("1.0.0", response.Version);
        }

        [Fact]
        public void Run_WithNoKey_ReturnsNotConnected()
        {
            // Arrange
            var config = CreateConfiguration(null);
            var function = new StripeHealthFunction(_mockLogger.Object, config);

            // Act
            var result = function.Run(_mockHttpRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<StripeHealthResponse>(okResult.Value);
            Assert.Equal("unknown", response.StripeMode);
            Assert.False(response.StripeConnected);
            Assert.Equal("1.0.0", response.Version);
        }

        [Fact]
        public void Run_WithEmptyKey_ReturnsNotConnected()
        {
            // Arrange
            var config = CreateConfiguration("");
            var function = new StripeHealthFunction(_mockLogger.Object, config);

            // Act
            var result = function.Run(_mockHttpRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<StripeHealthResponse>(okResult.Value);
            Assert.Equal("unknown", response.StripeMode);
            Assert.False(response.StripeConnected);
            Assert.Equal("1.0.0", response.Version);
        }

        [Fact]
        public void Run_WithInvalidKeyFormat_ReturnsConnectedButUnknownMode()
        {
            // Arrange
            var config = CreateConfiguration("invalid_key_format");
            var function = new StripeHealthFunction(_mockLogger.Object, config);

            // Act
            var result = function.Run(_mockHttpRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<StripeHealthResponse>(okResult.Value);
            Assert.Equal("unknown", response.StripeMode);
            Assert.True(response.StripeConnected);
            Assert.Equal("1.0.0", response.Version);
        }

        [Fact]
        public void Run_WithRestrictedApiKey_ReturnsConnectedButUnknownMode()
        {
            // Arrange - Restricted keys start with "rk_test_" or "rk_live_"
            var config = CreateConfiguration("rk_test_" + new string('z', 40));
            var function = new StripeHealthFunction(_mockLogger.Object, config);

            // Act
            var result = function.Run(_mockHttpRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<StripeHealthResponse>(okResult.Value);
            Assert.Equal("unknown", response.StripeMode);
            Assert.True(response.StripeConnected);
            Assert.Equal("1.0.0", response.Version);
        }

        [Fact]
        public void Run_LogsInformation()
        {
            // Arrange
            var config = CreateConfiguration("sk_test_fake");
            var function = new StripeHealthFunction(_mockLogger.Object, config);

            // Act
            function.Run(_mockHttpRequest.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stripe health check")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
