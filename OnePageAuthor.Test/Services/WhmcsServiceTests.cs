using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI.API;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OnePageAuthor.Test.Services
{
    using DomainRegistrationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration;
    using DomainEntity = InkStainedWretch.OnePageAuthorAPI.Entities.Domain;
    using ContactInformationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.ContactInformation;
    using DomainStatus = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrationStatus;

    /// <summary>
    /// Unit tests for WhmcsService.
    /// </summary>
    public class WhmcsServiceTests
    {
        private readonly Mock<ILogger<WhmcsService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;

        public WhmcsServiceTests()
        {
            _mockLogger = new Mock<ILogger<WhmcsService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            // Setup default configuration values
            _mockConfiguration.Setup(c => c["WHMCS_API_URL"]).Returns("https://api.whmcs.test/api.php");
            _mockConfiguration.Setup(c => c["WHMCS_API_IDENTIFIER"]).Returns("test-identifier");
            _mockConfiguration.Setup(c => c["WHMCS_API_SECRET"]).Returns("test-secret");
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new WhmcsService(null!, _httpClient, _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new WhmcsService(_mockLogger.Object, null!, _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new WhmcsService(_mockLogger.Object, _httpClient, null!));
        }

        [Fact]
        public void Constructor_WithMissingConfiguration_CreatesInstanceButDisablesWhmcs()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["WHMCS_API_URL"]).Returns((string?)null);

            // Act
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithInvalidHttpsUrl_CreatesInstanceButDisablesWhmcs()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["WHMCS_API_URL"]).Returns("http://insecure.com/api.php"); // HTTP not HTTPS

            // Act
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Assert
            Assert.NotNull(service);
        }
        
        [Fact]
        public async Task RegisterDomainAsync_WithMissingConfiguration_ReturnsFalse()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["WHMCS_API_URL"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_IDENTIFIER"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_SECRET"]).Returns((string?)null);
            
            var service = new WhmcsService(_mockLogger.Object, _httpClient, mockConfig.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.RegisterDomainAsync(registration);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region RegisterDomainAsync Tests

        [Fact]
        public async Task RegisterDomainAsync_WithNullRegistration_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.RegisterDomainAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithNullDomain_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = new DomainRegistrationEntity
            {
                id = "test-id",
                Domain = null!
            };

            // Act
            var result = await service.RegisterDomainAsync(registration);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithSuccessResponse_ReturnsTrue()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\",\"message\":\"Domain registered successfully\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.RegisterDomainAsync(registration);

            // Assert
            Assert.True(result);
            VerifyHttpRequestMade();
        }

        [Fact]
        public async Task RegisterDomainAsync_WithErrorResponse_ReturnsFalse()
        {
            // Arrange
            var responseJson = "{\"result\":\"error\",\"message\":\"Domain already registered\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.RegisterDomainAsync(registration);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithHttpErrorStatus_ReturnsFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.RegisterDomainAsync(registration);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "invalid-json");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.RegisterDomainAsync(registration);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithContactInformation_SendsAllFields()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\"}";
            string? capturedFormContent = null;
            
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) => 
                {
                    // Capture the form content before it's disposed
                    if (req.Content != null)
                    {
                        capturedFormContent = await req.Content.ReadAsStringAsync();
                    }
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.RegisterDomainAsync(registration);

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedFormContent);
            
            // Verify form-encoded body contains required WHMCS parameters
            Assert.Contains("action=DomainRegister", capturedFormContent);
            Assert.Contains("identifier=test-identifier", capturedFormContent);
            Assert.Contains("secret=test-secret", capturedFormContent);
            Assert.Contains("domain=testdomain.com", capturedFormContent);
            Assert.Contains("responsetype=json", capturedFormContent);
            
            // Verify contact information is mapped
            Assert.Contains("firstname=John", capturedFormContent);
            Assert.Contains("lastname=Doe", capturedFormContent);
            Assert.Contains("email=john.doe%40example.com", capturedFormContent);
            Assert.Contains("address1=123+Test+St", capturedFormContent);
            Assert.Contains("city=Test+City", capturedFormContent);
            Assert.Contains("state=CA", capturedFormContent);
            Assert.Contains("postcode=12345", capturedFormContent);
            Assert.Contains("country=United+States", capturedFormContent);
            Assert.Contains("phonenumber=%2B1-555-123-4567", capturedFormContent);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithHttpRequestException_ReturnsFalse()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.RegisterDomainAsync(registration);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetTLDPricingAsync Tests

        [Fact]
        public async Task GetTLDPricingAsync_WithMissingConfiguration_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["WHMCS_API_URL"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_IDENTIFIER"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_SECRET"]).Returns((string?)null);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, mockConfig.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetTLDPricingAsync());
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithSuccessResponse_ReturnsJsonDocument()
        {
            // Arrange
            var responseJson = @"{
                ""result"": ""success"",
                ""pricing"": {
                    ""com"": {
                        ""registration"": { ""1"": 8.95, ""2"": 8.50 },
                        ""renewal"": { ""1"": 9.95 },
                        ""transfer"": { ""1"": 8.95 }
                    }
                }
            }";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            using var result = await service.GetTLDPricingAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("result", out var resultProp));
            Assert.Equal("success", resultProp.GetString());
            Assert.True(result.RootElement.TryGetProperty("pricing", out var pricingProp));
            Assert.True(pricingProp.TryGetProperty("com", out _));
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithClientId_IncludesInRequest()
        {
            // Arrange
            var responseJson = @"{""result"": ""success"", ""pricing"": {}}";
            string? capturedFormContent = null;

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
                {
                    if (req.Content != null)
                    {
                        capturedFormContent = await req.Content.ReadAsStringAsync();
                    }
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            using var result = await service.GetTLDPricingAsync(clientId: "12345");

            // Assert
            Assert.NotNull(capturedFormContent);
            Assert.Contains("action=GetTLDPricing", capturedFormContent);
            Assert.Contains("clientid=12345", capturedFormContent);
            Assert.Contains("identifier=test-identifier", capturedFormContent);
            Assert.Contains("secret=test-secret", capturedFormContent);
            Assert.Contains("responsetype=json", capturedFormContent);
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithCurrencyId_IncludesInRequest()
        {
            // Arrange
            var responseJson = @"{""result"": ""success"", ""pricing"": {}}";
            string? capturedFormContent = null;

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
                {
                    if (req.Content != null)
                    {
                        capturedFormContent = await req.Content.ReadAsStringAsync();
                    }
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            using var result = await service.GetTLDPricingAsync(currencyId: 2);

            // Assert
            Assert.NotNull(capturedFormContent);
            Assert.Contains("action=GetTLDPricing", capturedFormContent);
            Assert.Contains("currencyid=2", capturedFormContent);
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithErrorResponse_ThrowsInvalidOperationException()
        {
            // Arrange
            var responseJson = @"{""result"": ""error"", ""message"": ""Invalid credentials""}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetTLDPricingAsync());
            Assert.Contains("Invalid credentials", exception.Message);
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithMissingResultField_ThrowsInvalidOperationException()
        {
            // Arrange
            var responseJson = @"{""pricing"": {""com"": {""registration"": {""1"": 8.95}}}}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetTLDPricingAsync());
            Assert.Contains("missing 'result' field", exception.Message);
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithHttpErrorStatus_ThrowsHttpRequestException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetTLDPricingAsync());
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithInvalidJson_ThrowsJsonException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "invalid-json");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert - JsonDocument.Parse throws JsonReaderException (derived from JsonException)
            var exception = await Assert.ThrowsAnyAsync<JsonException>(() => service.GetTLDPricingAsync());
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task GetTLDPricingAsync_WithHttpRequestException_ThrowsHttpRequestException()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetTLDPricingAsync());
        }

        #endregion

        #region Helper Methods

        private DomainRegistrationEntity CreateTestDomainRegistration()
        {
            return new DomainRegistrationEntity
            {
                id = "test-id-123",
                Upn = "testuser@example.com",
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "testdomain"
                },
                ContactInformation = new ContactInformationEntity
                {
                    FirstName = "John",
                    LastName = "Doe",
                    EmailAddress = "john.doe@example.com",
                    Address = "123 Test St",
                    Address2 = "Apt 4",
                    City = "Test City",
                    State = "CA",
                    ZipCode = "12345",
                    Country = "United States",
                    TelephoneNumber = "+1-555-123-4567"
                },
                Status = DomainStatus.Pending
            };
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void VerifyHttpRequestMade()
        {
            _mockHttpMessageHandler.Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString() == "https://api.whmcs.test/api.php"),
                    ItExpr.IsAny<CancellationToken>());
        }

        #endregion
    }
}
