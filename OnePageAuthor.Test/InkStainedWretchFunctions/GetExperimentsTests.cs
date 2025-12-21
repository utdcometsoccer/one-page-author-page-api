using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretchFunctions;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    /// <summary>
    /// Unit tests for GetExperiments Azure Function
    /// </summary>
    public class GetExperimentsTests
    {
        private readonly Mock<ILogger<GetExperiments>> _mockLogger;
        private readonly Mock<IExperimentService> _mockExperimentService;
        private readonly GetExperiments _function;

        public GetExperimentsTests()
        {
            _mockLogger = new Mock<ILogger<GetExperiments>>();
            _mockExperimentService = new Mock<IExperimentService>();
            _function = new GetExperiments(_mockLogger.Object, _mockExperimentService.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GetExperiments(null!, _mockExperimentService.Object));
        }

        [Fact]
        public void Constructor_WithNullExperimentService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GetExperiments(_mockLogger.Object, null!));
        }

        [Fact]
        public async Task Run_WithoutPageParameter_ReturnsBadRequest()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Query["page"]).Returns(new Microsoft.Extensions.Primitives.StringValues());

            // Act
            var result = await _function.Run(mockRequest.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task Run_WithEmptyPageParameter_ReturnsBadRequest()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Query["page"]).Returns(new Microsoft.Extensions.Primitives.StringValues(""));

            // Act
            var result = await _function.Run(mockRequest.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task Run_WithValidPageParameter_ReturnsOkResult()
        {
            // Arrange
            var page = "landing";
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Query["page"]).Returns(new Microsoft.Extensions.Primitives.StringValues(page));
            mockRequest.Setup(r => r.Query["userId"]).Returns(new Microsoft.Extensions.Primitives.StringValues());

            var expectedResponse = new GetExperimentsResponse
            {
                SessionId = "test-session-123",
                Experiments = new List<AssignedExperiment>
                {
                    new AssignedExperiment
                    {
                        Id = "exp1",
                        Name = "Test Experiment",
                        Variant = "control",
                        Config = new Dictionary<string, object> { { "color", "blue" } }
                    }
                }
            };

            _mockExperimentService
                .Setup(s => s.GetExperimentsAsync(It.Is<GetExperimentsRequest>(r => r.Page == page)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<GetExperimentsResponse>(okResult.Value);
            Assert.Equal("test-session-123", response.SessionId);
            Assert.Single(response.Experiments);
            Assert.Equal("exp1", response.Experiments[0].Id);
        }

        [Fact]
        public async Task Run_WithUserIdParameter_PassesUserIdToService()
        {
            // Arrange
            var page = "landing";
            var userId = "user-123";
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Query["page"]).Returns(new Microsoft.Extensions.Primitives.StringValues(page));
            mockRequest.Setup(r => r.Query["userId"]).Returns(new Microsoft.Extensions.Primitives.StringValues(userId));

            var expectedResponse = new GetExperimentsResponse
            {
                SessionId = userId,
                Experiments = new List<AssignedExperiment>()
            };

            _mockExperimentService
                .Setup(s => s.GetExperimentsAsync(It.Is<GetExperimentsRequest>(
                    r => r.Page == page && r.UserId == userId)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<GetExperimentsResponse>(okResult.Value);
            Assert.Equal(userId, response.SessionId);
            _mockExperimentService.Verify(
                s => s.GetExperimentsAsync(It.Is<GetExperimentsRequest>(
                    r => r.Page == page && r.UserId == userId)),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var page = "landing";
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Query["page"]).Returns(new Microsoft.Extensions.Primitives.StringValues(page));
            mockRequest.Setup(r => r.Query["userId"]).Returns(new Microsoft.Extensions.Primitives.StringValues());

            _mockExperimentService
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ThrowsAsync(new ArgumentException("Invalid parameter"));

            // Act
            var result = await _function.Run(mockRequest.Object);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var page = "landing";
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Query["page"]).Returns(new Microsoft.Extensions.Primitives.StringValues(page));
            mockRequest.Setup(r => r.Query["userId"]).Returns(new Microsoft.Extensions.Primitives.StringValues());

            _mockExperimentService
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _function.Run(mockRequest.Object);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        [Fact]
        public async Task Run_WithMultipleExperiments_ReturnsAllAssignments()
        {
            // Arrange
            var page = "pricing";
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Query["page"]).Returns(new Microsoft.Extensions.Primitives.StringValues(page));
            mockRequest.Setup(r => r.Query["userId"]).Returns(new Microsoft.Extensions.Primitives.StringValues("user-123"));

            var expectedResponse = new GetExperimentsResponse
            {
                SessionId = "user-123",
                Experiments = new List<AssignedExperiment>
                {
                    new AssignedExperiment
                    {
                        Id = "exp1",
                        Name = "Button Color Test",
                        Variant = "variant_a",
                        Config = new Dictionary<string, object> { { "color", "red" } }
                    },
                    new AssignedExperiment
                    {
                        Id = "exp2",
                        Name = "Header Text Test",
                        Variant = "control",
                        Config = new Dictionary<string, object> { { "text", "Sign Up Now" } }
                    }
                }
            };

            _mockExperimentService
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<GetExperimentsResponse>(okResult.Value);
            Assert.Equal(2, response.Experiments.Count);
            Assert.Contains(response.Experiments, e => e.Id == "exp1");
            Assert.Contains(response.Experiments, e => e.Id == "exp2");
        }
    }
}
