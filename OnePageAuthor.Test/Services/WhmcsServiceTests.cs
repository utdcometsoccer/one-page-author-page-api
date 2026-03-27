using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI;
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

        [Fact]
        public async Task RegisterDomainAsync_WithHttpErrorStatus_LogsErrorWithClientIdAndAttendantInfo()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            var expectedApiIdentifier = Utility.MaskSensitiveValue("test-identifier");
            var expectedContactEmail = Utility.MaskSensitiveValue("john.doe@example.com");

            // Act
            await service.RegisterDomainAsync(registration);

            // Assert – the error log must contain the masked API identifier (client ID),
            // the registration document ID, the UPN, and the masked contact email.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains(expectedApiIdentifier) &&
                        v.ToString()!.Contains("test-id-123") &&
                        v.ToString()!.Contains("testuser@example.com") &&
                        v.ToString()!.Contains(expectedContactEmail)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithErrorResponse_LogsErrorWithClientIdAndAttendantInfo()
        {
            // Arrange
            var responseJson = "{\"result\":\"error\",\"message\":\"Domain already registered\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            var expectedApiIdentifier = Utility.MaskSensitiveValue("test-identifier");
            var expectedContactEmail = Utility.MaskSensitiveValue("john.doe@example.com");

            // Act
            await service.RegisterDomainAsync(registration);

            // Assert – the error log must contain the masked API identifier (client ID),
            // the registration document ID, the UPN, and the masked contact email.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains(expectedApiIdentifier) &&
                        v.ToString()!.Contains("test-id-123") &&
                        v.ToString()!.Contains("testuser@example.com") &&
                        v.ToString()!.Contains(expectedContactEmail)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithHttpRequestException_LogsErrorWithClientIdAndAttendantInfo()
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

            var expectedApiIdentifier = Utility.MaskSensitiveValue("test-identifier");
            var expectedContactEmail = Utility.MaskSensitiveValue("john.doe@example.com");

            // Act
            await service.RegisterDomainAsync(registration);

            // Assert – the error log must contain the masked API identifier (client ID),
            // the registration document ID, the UPN, and the masked contact email.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains(expectedApiIdentifier) &&
                        v.ToString()!.Contains("test-id-123") &&
                        v.ToString()!.Contains("testuser@example.com") &&
                        v.ToString()!.Contains(expectedContactEmail)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithInvalidJson_LogsErrorWithClientIdAndAttendantInfo()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "invalid-json");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            var expectedApiIdentifier = Utility.MaskSensitiveValue("test-identifier");
            var expectedContactEmail = Utility.MaskSensitiveValue("john.doe@example.com");

            // Act
            await service.RegisterDomainAsync(registration);

            // Assert – the JsonException path must log the masked API identifier,
            // registration document ID, UPN, and masked contact email.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains(expectedApiIdentifier) &&
                        v.ToString()!.Contains("test-id-123") &&
                        v.ToString()!.Contains("testuser@example.com") &&
                        v.ToString()!.Contains(expectedContactEmail)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterDomainAsync_WithUnexpectedException_LogsErrorWithClientIdAndAttendantInfo()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            var expectedApiIdentifier = Utility.MaskSensitiveValue("test-identifier");
            var expectedContactEmail = Utility.MaskSensitiveValue("john.doe@example.com");

            // Act
            await service.RegisterDomainAsync(registration);

            // Assert – the generic Exception path must log the masked API identifier,
            // registration document ID, UPN, and masked contact email.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains(expectedApiIdentifier) &&
                        v.ToString()!.Contains("test-id-123") &&
                        v.ToString()!.Contains("testuser@example.com") &&
                        v.ToString()!.Contains(expectedContactEmail)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region UpdateNameServersAsync Tests

        [Fact]
        public async Task UpdateNameServersAsync_WithMissingConfiguration_ReturnsFalse()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["WHMCS_API_URL"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_IDENTIFIER"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_SECRET"]).Returns((string?)null);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, mockConfig.Object);

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", new[] { "ns1.azure.com", "ns2.azure.net" });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithNullDomainName_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.UpdateNameServersAsync(null!, new[] { "ns1.azure.com", "ns2.azure.net" });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithEmptyDomainName_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.UpdateNameServersAsync("", new[] { "ns1.azure.com", "ns2.azure.net" });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithNullNameServers_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithLessThanTwoNameServers_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", new[] { "ns1.azure.com" });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithMoreThanFiveNameServers_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1.azure.com", "ns2.azure.net", "ns3.azure.org", "ns4.azure.info", "ns5.azure.biz", "ns6.azure.co" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithSuccessResponse_ReturnsTrue()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\",\"message\":\"Name servers updated successfully\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net", "ns3-04.azure-dns.org", "ns4-04.azure-dns.info" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.True(result);
            VerifyHttpRequestMade();
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithErrorResponse_ReturnsFalse()
        {
            // Arrange
            var responseJson = "{\"result\":\"error\",\"message\":\"Invalid domain\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithHttpErrorStatus_ReturnsFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "invalid-json");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_SendsCorrectFormData()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\"}";
            string? capturedFormContent = null;

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
                {
                    // Capture the form content before it's disposed
                    if (req.Content != null)
                    {
                        capturedFormContent = await req.Content.ReadAsStringAsync();
                    }

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                    };
                });

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net", "ns3-04.azure-dns.org", "ns4-04.azure-dns.info" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedFormContent);

            // Verify form-encoded body contains required WHMCS parameters
            Assert.Contains("action=DomainUpdateNameservers", capturedFormContent);
            Assert.Contains("identifier=test-identifier", capturedFormContent);
            Assert.Contains("secret=test-secret", capturedFormContent);
            Assert.Contains("domain=testdomain.com", capturedFormContent);
            Assert.Contains("responsetype=json", capturedFormContent);

            // Verify name servers are mapped correctly
            Assert.Contains("ns1=ns1-04.azure-dns.com", capturedFormContent);
            Assert.Contains("ns2=ns2-04.azure-dns.net", capturedFormContent);
            Assert.Contains("ns3=ns3-04.azure-dns.org", capturedFormContent);
            Assert.Contains("ns4=ns4-04.azure-dns.info", capturedFormContent);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithHttpRequestException_ReturnsFalse()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithTwoNameServers_ReturnsTrue()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithFiveNameServers_ReturnsTrue()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1.azure.com", "ns2.azure.net", "ns3.azure.org", "ns4.azure.info", "ns5.azure.biz" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithNullNameServerEntries_FiltersAndSucceeds()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1.azure.com", null!, "ns2.azure.net" };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithEmptyNameServerEntries_FiltersAndSucceeds()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1.azure.com", "", "ns2.azure.net", "   " };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithAllInvalidNameServers_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { null!, "", "   ", null! };

            // Act
            var result = await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithHttpErrorStatus_LogsErrorWithClientId()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net" };

            var expectedApiIdentifier = Utility.MaskSensitiveValue("test-identifier");

            // Act
            await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert – the error log must contain the masked API identifier (client ID).
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains(expectedApiIdentifier) &&
                        v.ToString()!.Contains("testdomain.com")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateNameServersAsync_WithErrorResponse_LogsErrorWithClientId()
        {
            // Arrange
            var responseJson = "{\"result\":\"error\",\"message\":\"Invalid domain\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net" };

            var expectedApiIdentifier = Utility.MaskSensitiveValue("test-identifier");

            // Act
            await service.UpdateNameServersAsync("testdomain.com", nameServers);

            // Assert – the error log must contain the masked API identifier (client ID).
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains(expectedApiIdentifier) &&
                        v.ToString()!.Contains("testdomain.com")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
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
            var responseJson = "{\"result\": \"success\", \"pricing\": {}}";
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
            var responseJson = "{\"result\": \"success\", \"pricing\": {}}";
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
            var responseJson = "{\"result\": \"error\", \"message\": \"Invalid credentials\"}";
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
            var responseJson = "{\"pricing\": {\"com\": {\"registration\": {\"1\": 8.95}}}}";
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

        #region CheckDomainAvailabilityAsync Tests

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WithMissingConfiguration_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["WHMCS_API_URL"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_IDENTIFIER"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_SECRET"]).Returns((string?)null);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, mockConfig.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CheckDomainAvailabilityAsync("test.name.ng"));
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WithNullDomainName_ThrowsArgumentException()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CheckDomainAvailabilityAsync(null!));
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WithEmptyDomainName_ThrowsArgumentException()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CheckDomainAvailabilityAsync(""));
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WhenStatusIsAvailable_ReturnsTrue()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\",\"status\":\"available\",\"whois\":\"No match for domain test.name.ng.\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.CheckDomainAvailabilityAsync("test.name.ng");

            // Assert
            Assert.True(result);
            VerifyHttpRequestMade();
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WhenStatusIsUnavailable_ReturnsFalse()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\",\"status\":\"unavailable\",\"whois\":\"Domain is registered.\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.CheckDomainAvailabilityAsync("test.name.ng");

            // Assert — only confirmed-unavailable returns false; no exception
            Assert.False(result);
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WhenResultIsError_ThrowsInvalidOperationException()
        {
            // Arrange
            var responseJson = "{\"result\":\"error\",\"message\":\"Invalid domain\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert — non-success result is a transient failure, not a confirmed unavailability
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CheckDomainAvailabilityAsync("test.name.ng"));
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WithHttpErrorStatus_ThrowsHttpRequestException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.CheckDomainAvailabilityAsync("test.name.ng"));
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WithInvalidJson_ThrowsJsonException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "invalid-json");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act & Assert
            await Assert.ThrowsAnyAsync<JsonException>(() =>
                service.CheckDomainAvailabilityAsync("test.name.ng"));
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_WithHttpRequestException_PropagatesException()
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
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.CheckDomainAvailabilityAsync("test.name.ng"));
        }

        [Fact]
        public async Task CheckDomainAvailabilityAsync_SendsCorrectFormData()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\",\"status\":\"available\",\"whois\":\"No match for domain test.name.ng.\"}";
            string? capturedFormContent = null;

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
                {
                    if (req.Content != null)
                        capturedFormContent = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                    });
                });

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.CheckDomainAvailabilityAsync("test.name.ng");

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedFormContent);
            Assert.Contains("action=DomainWhois", capturedFormContent);
            Assert.Contains("identifier=test-identifier", capturedFormContent);
            Assert.Contains("secret=test-secret", capturedFormContent);
            Assert.Contains("domain=test.name.ng", capturedFormContent);
            Assert.Contains("responsetype=json", capturedFormContent);
        }

        #endregion

        #region AddOrderAsync Tests

        [Fact]
        public async Task AddOrderAsync_WithMissingConfiguration_ReturnsFalse()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["WHMCS_API_URL"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_IDENTIFIER"]).Returns((string?)null);
            mockConfig.Setup(c => c["WHMCS_API_SECRET"]).Returns((string?)null);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, mockConfig.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.AddOrderAsync(registration, [], clientId: "181");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrderAsync_WithNullRegistration_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);

            // Act
            var result = await service.AddOrderAsync(null!, [], clientId: "181");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrderAsync_WithNullDomain_ReturnsFalse()
        {
            // Arrange
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = new DomainRegistrationEntity { id = "test-id", Domain = null! };

            // Act
            var result = await service.AddOrderAsync(registration, [], clientId: "181");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrderAsync_WithSuccessResponse_ReturnsTrue()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\",\"orderid\":264,\"domainids\":\"104\",\"invoiceid\":299}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.AddOrderAsync(registration, [], clientId: "181");

            // Assert
            Assert.True(result);
            VerifyHttpRequestMade();
        }

        [Fact]
        public async Task AddOrderAsync_WithErrorResponse_ReturnsFalse()
        {
            // Arrange
            var responseJson = "{\"result\":\"error\",\"message\":\"Insufficient funds\"}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.AddOrderAsync(registration, [], clientId: "181");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrderAsync_WithHttpErrorStatus_ReturnsFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.AddOrderAsync(registration, [], clientId: "181");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrderAsync_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "invalid-json");

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            var result = await service.AddOrderAsync(registration, [], clientId: "181");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrderAsync_WithHttpRequestException_ReturnsFalse()
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
            var result = await service.AddOrderAsync(registration, [], clientId: "181");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrderAsync_SendsCorrectFormData_WithClientIdAndNameServers()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\",\"orderid\":264,\"domainids\":\"104\",\"invoiceid\":299}";
            string? capturedFormContent = null;

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
                {
                    if (req.Content != null)
                        capturedFormContent = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                    });
                });

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();
            var nameServers = new[] { "ns1.azure.com", "ns2.azure.net", "ns3.azure.org", "ns4.azure.info" };

            // Act
            var result = await service.AddOrderAsync(registration, nameServers, clientId: "181");

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedFormContent);

            // Verify required WHMCS AddOrder parameters
            Assert.Contains("action=AddOrder", capturedFormContent);
            Assert.Contains("identifier=test-identifier", capturedFormContent);
            Assert.Contains("secret=test-secret", capturedFormContent);
            Assert.Contains("domain%5B0%5D=testdomain.com", capturedFormContent); // domain[0] URL-encoded
            Assert.Contains("domaintype%5B0%5D=register", capturedFormContent);   // domaintype[0] URL-encoded
            Assert.Contains("regperiod%5B0%5D=1", capturedFormContent);           // regperiod[0] URL-encoded
            Assert.Contains("paymentmethod=stripe", capturedFormContent);
            Assert.Contains("responsetype=json", capturedFormContent);
            Assert.Contains("clientid=181", capturedFormContent);

            // Verify name servers
            Assert.Contains("nameserver1=ns1.azure.com", capturedFormContent);
            Assert.Contains("nameserver2=ns2.azure.net", capturedFormContent);
            Assert.Contains("nameserver3=ns3.azure.org", capturedFormContent);
            Assert.Contains("nameserver4=ns4.azure.info", capturedFormContent);
        }

        [Fact]
        public async Task AddOrderAsync_WithoutClientId_ThrowsWhmcsConfigurationException()
        {
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act
            await Assert.ThrowsAsync<InkStainedWretch.OnePageAuthorAPI.API.WhmcsConfigurationException>(
                () => service.AddOrderAsync(registration, []));
        }

        [Fact]
        public async Task AddOrderAsync_WithNonNumericClientId_ThrowsWhmcsConfigurationException()
        {
            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            await Assert.ThrowsAsync<InkStainedWretch.OnePageAuthorAPI.API.WhmcsConfigurationException>(
                () => service.AddOrderAsync(registration, [], clientId: "not-an-int"));
        }

        [Fact]
        public async Task AddOrderAsync_WithNullNameServers_SucceedsWithoutNameServers()
        {
            // Arrange
            var responseJson = "{\"result\":\"success\",\"orderid\":266,\"domainids\":\"106\",\"invoiceid\":301}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var service = new WhmcsService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
            var registration = CreateTestDomainRegistration();

            // Act — passing null for nameServers should not throw
            var result = await service.AddOrderAsync(registration, null!, clientId: "181");

            // Assert
            Assert.True(result);
        }

        #endregion
    }
}
