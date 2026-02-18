using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Functions;
using System.Security.Claims;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    public class GetTLDPricingFunctionTests
    {
        private readonly Mock<IWhmcsService> _mockWhmcsService;
        private readonly Mock<ILogger<GetTLDPricingFunction>> _mockLogger;
        private readonly Mock<IJwtValidationService> _mockJwtValidationService;
        private readonly Mock<IUserProfileService> _mockUserProfileService;
        private readonly GetTLDPricingFunction _function;

        public GetTLDPricingFunctionTests()
        {
            _mockWhmcsService = new Mock<IWhmcsService>();
            _mockLogger = new Mock<ILogger<GetTLDPricingFunction>>();
            _mockJwtValidationService = new Mock<IJwtValidationService>();
            _mockUserProfileService = new Mock<IUserProfileService>();
            
            _function = new GetTLDPricingFunction(
                _mockWhmcsService.Object,
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                _mockUserProfileService.Object);
        }

        private HttpRequest CreateMockRequest(string? clientId = null, int? currencyId = null, bool addAuthHeader = true)
        {
            var mockRequest = new Mock<HttpRequest>();
            
            // Setup query parameters
            var queryDict = new Dictionary<string, StringValues>();
            if (clientId != null)
                queryDict["clientId"] = clientId;
            if (currencyId.HasValue)
                queryDict["currencyId"] = currencyId.ToString();
            
            var queryCollection = new QueryCollection(queryDict);
            mockRequest.Setup(r => r.Query).Returns(queryCollection);
            
            // Setup headers with Authorization
            var headers = new HeaderDictionary();
            if (addAuthHeader)
            {
                headers["Authorization"] = "Bearer valid-token";
            }
            mockRequest.Setup(r => r.Headers).Returns(headers);
            
            return mockRequest.Object;
        }

        private void SetupValidAuthentication()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user@example.com"),
                new Claim("preferred_username", "test-user@example.com")
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(claimsPrincipal);
            
            var userProfile = new InkStainedWretch.OnePageAuthorAPI.Entities.UserProfile
            {
                id = "test-user@example.com",
                Upn = "test-user@example.com"
            };
            
            _mockUserProfileService
                .Setup(s => s.EnsureUserProfileAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(userProfile);
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
        public async Task GetPricing_WithValidAuthentication_ReturnsOkWithJsonContent()
        {
            // Arrange
            SetupValidAuthentication();
            
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
                .Setup(s => s.GetTLDPricingAsync(null, null))
                .ReturnsAsync(jsonDocument);

            var httpRequest = CreateMockRequest();

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Contains("success", contentResult.Content);
            Assert.Contains("pricing", contentResult.Content);
            
            _mockWhmcsService.Verify(s => s.GetTLDPricingAsync(null, null), Times.Once);
        }

        [Fact]
        public async Task GetPricing_WithClientIdParameter_PassesClientIdToService()
        {
            // Arrange
            SetupValidAuthentication();
            
            var pricingJson = @"{""result"": ""success"", ""pricing"": {}}";
            var jsonDocument = JsonDocument.Parse(pricingJson);

            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync("12345", null))
                .ReturnsAsync(jsonDocument);

            var httpRequest = CreateMockRequest(clientId: "12345");

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            Assert.IsType<ContentResult>(result);
            _mockWhmcsService.Verify(s => s.GetTLDPricingAsync("12345", null), Times.Once);
        }

        [Fact]
        public async Task GetPricing_WithCurrencyIdParameter_PassesCurrencyIdToService()
        {
            // Arrange
            SetupValidAuthentication();
            
            var pricingJson = @"{""result"": ""success"", ""pricing"": {}}";
            var jsonDocument = JsonDocument.Parse(pricingJson);

            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync(null, 2))
                .ReturnsAsync(jsonDocument);

            var httpRequest = CreateMockRequest(currencyId: 2);

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            Assert.IsType<ContentResult>(result);
            _mockWhmcsService.Verify(s => s.GetTLDPricingAsync(null, 2), Times.Once);
        }

        [Fact]
        public async Task GetPricing_WithBothParameters_PassesBothToService()
        {
            // Arrange
            SetupValidAuthentication();
            
            var pricingJson = @"{""result"": ""success"", ""pricing"": {}}";
            var jsonDocument = JsonDocument.Parse(pricingJson);

            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync("12345", 2))
                .ReturnsAsync(jsonDocument);

            var httpRequest = CreateMockRequest(clientId: "12345", currencyId: 2);

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            Assert.IsType<ContentResult>(result);
            _mockWhmcsService.Verify(s => s.GetTLDPricingAsync("12345", 2), Times.Once);
        }

        [Fact]
        public async Task GetPricing_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((ClaimsPrincipal?)null);

            var httpRequest = CreateMockRequest(addAuthHeader: false);

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetPricing_WithInvalidUserProfile_ReturnsUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user@example.com")
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(claimsPrincipal);
            
            _mockUserProfileService
                .Setup(s => s.EnsureUserProfileAsync(It.IsAny<ClaimsPrincipal>()))
                .ThrowsAsync(new InvalidOperationException("User profile validation failed"));

            var httpRequest = CreateMockRequest();

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorObj = unauthorizedResult.Value;
            Assert.NotNull(errorObj);
        }

        [Fact]
        public async Task GetPricing_WhenWhmcsNotConfigured_Returns502()
        {
            // Arrange
            SetupValidAuthentication();
            
            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync(It.IsAny<string>(), It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("WHMCS integration is not configured"));

            var httpRequest = CreateMockRequest();

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(502, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetPricing_WhenHttpRequestFails_Returns502()
        {
            // Arrange
            SetupValidAuthentication();
            
            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync(It.IsAny<string>(), It.IsAny<int?>()))
                .ThrowsAsync(new HttpRequestException("Network error"));

            var httpRequest = CreateMockRequest();

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(502, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetPricing_WhenUnexpectedErrorOccurs_Returns500()
        {
            // Arrange
            SetupValidAuthentication();
            
            _mockWhmcsService
                .Setup(s => s.GetTLDPricingAsync(It.IsAny<string>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var httpRequest = CreateMockRequest();

            // Act
            var result = await _function.GetPricing(httpRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
