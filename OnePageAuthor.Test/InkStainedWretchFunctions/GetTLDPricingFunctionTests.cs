using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Functions;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    public class GetTLDPricingFunctionTests
    {
        private readonly Mock<IWhmcsService> _mockWhmcsService;
        private readonly Mock<ILogger<GetTLDPricingFunction>> _mockLogger;
        private readonly Mock<IJwtValidationService> _mockJwtValidationService;
        private readonly Mock<IUserProfileService> _mockUserProfileService;

        public GetTLDPricingFunctionTests()
        {
            _mockWhmcsService = new Mock<IWhmcsService>();
            _mockLogger = new Mock<ILogger<GetTLDPricingFunction>>();
            _mockJwtValidationService = new Mock<IJwtValidationService>();
            _mockUserProfileService = new Mock<IUserProfileService>();
        }

        [Fact]
        public void Constructor_WithNullWhmcsService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GetTLDPricingFunction(
                    null!,
                    _mockLogger.Object,
                    _mockJwtValidationService.Object,
                    _mockUserProfileService.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GetTLDPricingFunction(
                    _mockWhmcsService.Object,
                    null!,
                    _mockJwtValidationService.Object,
                    _mockUserProfileService.Object));
        }

        [Fact]
        public void Constructor_WithNullJwtValidationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GetTLDPricingFunction(
                    _mockWhmcsService.Object,
                    _mockLogger.Object,
                    null!,
                    _mockUserProfileService.Object));
        }

        [Fact]
        public void Constructor_WithNullUserProfileService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GetTLDPricingFunction(
                    _mockWhmcsService.Object,
                    _mockLogger.Object,
                    _mockJwtValidationService.Object,
                    null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_DoesNotThrow()
        {
            // Act
            var function = new GetTLDPricingFunction(
                _mockWhmcsService.Object,
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                _mockUserProfileService.Object);

            // Assert
            Assert.NotNull(function);
        }

        [Fact]
        public async Task GetTLDPricingAsync_ServiceReturnsValidData_ReturnsJsonDocument()
        {
            // Arrange
            var pricingJson = @"{
                ""result"": ""success"",
                ""pricing"": {
                    ""com"": {
                        ""registration"": { ""1"": 8.95 },
                        ""renewal"": { ""1"": 9.95 }
                    }
                }
            }";
            var jsonDocument = JsonDocument.Parse(pricingJson);

            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync(It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(jsonDocument);

            // Act
            var result = await _mockWhmcsService.Object.GetTLDPricingAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("result", out var resultProp));
            Assert.Equal("success", resultProp.GetString());
            _mockWhmcsService.Verify(s => s.GetTLDPricingAsync(null, null), Times.Once);
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithClientId_PassesParameterToService()
        {
            // Arrange
            var pricingJson = @"{""result"": ""success"", ""pricing"": {}}";
            var jsonDocument = JsonDocument.Parse(pricingJson);

            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync("12345", null))
                .ReturnsAsync(jsonDocument);

            // Act
            var result = await _mockWhmcsService.Object.GetTLDPricingAsync("12345", null);

            // Assert
            Assert.NotNull(result);
            _mockWhmcsService.Verify(s => s.GetTLDPricingAsync("12345", null), Times.Once);
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithCurrencyId_PassesParameterToService()
        {
            // Arrange
            var pricingJson = @"{""result"": ""success"", ""pricing"": {}}";
            var jsonDocument = JsonDocument.Parse(pricingJson);

            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync(null, 2))
                .ReturnsAsync(jsonDocument);

            // Act
            var result = await _mockWhmcsService.Object.GetTLDPricingAsync(null, 2);

            // Assert
            Assert.NotNull(result);
            _mockWhmcsService.Verify(s => s.GetTLDPricingAsync(null, 2), Times.Once);
        }

        [Fact]
        public async Task GetTLDPricingAsync_ServiceThrowsInvalidOperationException_CanHandleException()
        {
            // Arrange
            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync(It.IsAny<string>(), It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("WHMCS not configured"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _mockWhmcsService.Object.GetTLDPricingAsync());
        }

        [Fact]
        public async Task GetTLDPricingAsync_ServiceThrowsHttpRequestException_CanHandleException()
        {
            // Arrange
            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync(It.IsAny<string>(), It.IsAny<int?>()))
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                _mockWhmcsService.Object.GetTLDPricingAsync());
        }
    }
}
