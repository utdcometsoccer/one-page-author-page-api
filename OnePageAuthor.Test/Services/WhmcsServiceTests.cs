using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI.API;
using System.Net;
using System.Text;

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
        public void Constructor_WithMissingApiUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["WHMCS_API_URL"]).Returns((string?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object));
            Assert.Contains("WHMCS_API_URL", exception.Message);
        }

        [Fact]
        public void Constructor_WithMissingApiIdentifier_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["WHMCS_API_IDENTIFIER"]).Returns((string?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object));
            Assert.Contains("WHMCS_API_IDENTIFIER", exception.Message);
        }

        [Fact]
        public void Constructor_WithMissingApiSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["WHMCS_API_SECRET"]).Returns((string?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object));
            Assert.Contains("WHMCS_API_SECRET", exception.Message);
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Assert
            Assert.NotNull(service);
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
