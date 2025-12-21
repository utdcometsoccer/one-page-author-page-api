using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretchFunctions;
using System.Text;
using System.Text.Json;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    public class LeadCaptureFunctionTests
    {
        private readonly Mock<ILogger<LeadCaptureFunction>> _mockLogger;
        private readonly Mock<ILeadService> _mockLeadService;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly LeadCaptureFunction _function;

        public LeadCaptureFunctionTests()
        {
            _mockLogger = new Mock<ILogger<LeadCaptureFunction>>();
            _mockLeadService = new Mock<ILeadService>();
            _mockRateLimitService = new Mock<IRateLimitService>();

            _function = new LeadCaptureFunction(
                _mockLogger.Object,
                _mockLeadService.Object,
                _mockRateLimitService.Object);
        }

        private HttpRequest CreateMockRequest(CreateLeadRequest request, string ipAddress = "192.168.1.1")
        {
            var json = JsonSerializer.Serialize(request);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Body).Returns(stream);
            
            var mockConnection = new Mock<ConnectionInfo>();
            mockConnection.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Parse(ipAddress));

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
            
            mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);
            mockRequest.Setup(r => r.Headers).Returns(new HeaderDictionary());

            return mockRequest.Object;
        }

        [Fact]
        public async Task Run_WithValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "test@example.com",
                FirstName = "John",
                Source = "landing_page",
                Locale = "en-US",
                ConsentGiven = true
            };

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockLeadService
                .Setup(s => s.IsValidEmail(It.IsAny<string>()))
                .Returns(true);

            _mockLeadService
                .Setup(s => s.CreateLeadAsync(It.IsAny<CreateLeadRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new CreateLeadResponse
                {
                    Id = "lead-123",
                    Status = LeadCreationStatus.Created,
                    Message = "Lead successfully created"
                });

            var httpRequest = CreateMockRequest(request);

            // Act
            var result = await _function.Run(httpRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

            var response = Assert.IsType<CreateLeadResponse>(objectResult.Value);
            Assert.Equal("lead-123", response.Id);
            Assert.Equal(LeadCreationStatus.Created, response.Status);

            _mockRateLimitService.Verify(r => r.IsRequestAllowedAsync(It.IsAny<string>(), "leads"), Times.Once);
            _mockRateLimitService.Verify(r => r.RecordRequestAsync(It.IsAny<string>(), "leads"), Times.Once);
            _mockLeadService.Verify(s => s.CreateLeadAsync(It.IsAny<CreateLeadRequest>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Run_WithExistingEmail_ReturnsOkResult()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "existing@example.com",
                Source = "blog",
                Locale = "en-US"
            };

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockLeadService
                .Setup(s => s.IsValidEmail(It.IsAny<string>()))
                .Returns(true);

            _mockLeadService
                .Setup(s => s.CreateLeadAsync(It.IsAny<CreateLeadRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new CreateLeadResponse
                {
                    Id = "existing-lead-123",
                    Status = LeadCreationStatus.Existing,
                    Message = "Email already registered"
                });

            var httpRequest = CreateMockRequest(request);

            // Act
            var result = await _function.Run(httpRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);

            var response = Assert.IsType<CreateLeadResponse>(objectResult.Value);
            Assert.Equal("existing-lead-123", response.Id);
            Assert.Equal(LeadCreationStatus.Existing, response.Status);
        }

        [Fact]
        public async Task Run_RateLimitExceeded_Returns429()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "test@example.com",
                Source = "landing_page",
                Locale = "en-US"
            };

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var httpRequest = CreateMockRequest(request);

            // Act
            var result = await _function.Run(httpRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);

            _mockRateLimitService.Verify(r => r.RecordRequestAsync(It.IsAny<string>(), "leads"), Times.Never);
            _mockLeadService.Verify(s => s.CreateLeadAsync(It.IsAny<CreateLeadRequest>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Run_InvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "invalid-email",
                Source = "landing_page",
                Locale = "en-US"
            };

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockLeadService
                .Setup(s => s.IsValidEmail(It.IsAny<string>()))
                .Returns(false);

            var httpRequest = CreateMockRequest(request);

            // Act
            var result = await _function.Run(httpRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            _mockLeadService.Verify(s => s.CreateLeadAsync(It.IsAny<CreateLeadRequest>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Run_InvalidSource_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "test@example.com",
                Source = "invalid_source",
                Locale = "en-US"
            };

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockLeadService
                .Setup(s => s.IsValidEmail(It.IsAny<string>()))
                .Returns(true);

            var httpRequest = CreateMockRequest(request);

            // Act
            var result = await _function.Run(httpRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            _mockLeadService.Verify(s => s.CreateLeadAsync(It.IsAny<CreateLeadRequest>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Run_MissingRequiredFields_ReturnsBadRequest()
        {
            // Arrange - Email is missing
            var request = new CreateLeadRequest
            {
                Source = "landing_page",
                Locale = "en-US"
            };

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var httpRequest = CreateMockRequest(request);

            // Act
            var result = await _function.Run(httpRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task Run_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var invalidJson = "{ invalid json }";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Body).Returns(stream);
            mockRequest.Setup(r => r.Headers).Returns(new HeaderDictionary());

            var mockConnection = new Mock<ConnectionInfo>();
            mockConnection.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.1"));

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _function.Run(mockRequest.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task Run_ServiceThrowsException_Returns500()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "test@example.com",
                Source = "landing_page",
                Locale = "en-US"
            };

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockLeadService
                .Setup(s => s.IsValidEmail(It.IsAny<string>()))
                .Returns(true);

            _mockLeadService
                .Setup(s => s.CreateLeadAsync(It.IsAny<CreateLeadRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            var httpRequest = CreateMockRequest(request);

            // Act
            var result = await _function.Run(httpRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        [Fact]
        public async Task Run_WithAllOptionalFields_ProcessesSuccessfully()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "test@example.com",
                FirstName = "Jane",
                Source = "exit_intent",
                LeadMagnet = "marketing-guide",
                UtmSource = "facebook",
                UtmMedium = "social",
                UtmCampaign = "winter-2024",
                Referrer = "https://facebook.com",
                Locale = "es-MX",
                ConsentGiven = true
            };

            _mockRateLimitService
                .Setup(r => r.IsRequestAllowedAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockLeadService
                .Setup(s => s.IsValidEmail(It.IsAny<string>()))
                .Returns(true);

            _mockLeadService
                .Setup(s => s.CreateLeadAsync(It.IsAny<CreateLeadRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new CreateLeadResponse
                {
                    Id = "lead-456",
                    Status = LeadCreationStatus.Created,
                    Message = "Lead successfully created"
                });

            var httpRequest = CreateMockRequest(request);

            // Act
            var result = await _function.Run(httpRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

            _mockLeadService.Verify(s => s.CreateLeadAsync(
                It.Is<CreateLeadRequest>(r => 
                    r.Email == "test@example.com" &&
                    r.FirstName == "Jane" &&
                    r.Source == "exit_intent" &&
                    r.LeadMagnet == "marketing-guide" &&
                    r.UtmSource == "facebook" &&
                    r.UtmMedium == "social" &&
                    r.UtmCampaign == "winter-2024" &&
                    r.Referrer == "https://facebook.com" &&
                    r.Locale == "es-MX" &&
                    r.ConsentGiven == true),
                It.IsAny<string>()), 
                Times.Once);
        }
    }
}
