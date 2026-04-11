using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretchFunctions;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using StateProvinceEntity = InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    /// <summary>
    /// Unit tests for GetStateProvinces Azure Function
    /// </summary>
    public class GetStateProvincesTests
    {
        private readonly Mock<ILogger<GetStateProvinces>> _mockLogger;
        private readonly Mock<IStateProvinceService> _mockStateProvinceService;
        private readonly GetStateProvinces _function;

        public GetStateProvincesTests()
        {
            _mockLogger = new Mock<ILogger<GetStateProvinces>>();
            _mockStateProvinceService = new Mock<IStateProvinceService>();
            _function = new GetStateProvinces(_mockLogger.Object, _mockStateProvinceService.Object);
        }

        private static HttpRequest CreateRequest()
        {
            var context = new DefaultHttpContext();
            return context.Request;
        }

        private static IList<StateProvinceEntity> CreateStateProvinces(string culture = "en-US")
        {
            return new List<StateProvinceEntity>
            {
                new StateProvinceEntity { id = "us-ca", Code = "US-CA", Name = "California", Country = "US", Culture = culture },
                new StateProvinceEntity { id = "us-tx", Code = "US-TX", Name = "Texas",      Country = "US", Culture = culture }
            };
        }

        // --- Constructor validation ---

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GetStateProvinces(null!, _mockStateProvinceService.Object));
        }

        [Fact]
        public void Constructor_WithNullStateProvinceService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GetStateProvinces(_mockLogger.Object, null!));
        }

        // --- Anonymous access (no Authorization header) ---

        [Fact]
        public async Task Run_AnonymousRequest_ValidCulture_WithResults_ReturnsOk()
        {
            // No Authorization header — endpoint is fully anonymous
            var req = CreateRequest();
            var stateProvinces = CreateStateProvinces("en-US");
            _mockStateProvinceService
                .Setup(s => s.GetStateProvincesByCultureAsync("en-US"))
                .ReturnsAsync(stateProvinces);

            var result = await _function.Run(req, "en-US");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task Run_AnonymousRequest_ValidCulture_ServiceNotCalled_WithNoResults_ReturnsNotFound()
        {
            var req = CreateRequest();
            _mockStateProvinceService
                .Setup(s => s.GetStateProvincesByCultureAsync("en-US"))
                .ReturnsAsync(new List<StateProvinceEntity>());

            var result = await _function.Run(req, "en-US");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // --- Culture validation ---

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Run_NullOrEmptyCulture_ReturnsBadRequest(string? culture)
        {
            var req = CreateRequest();

            var result = await _function.Run(req, culture!);

            Assert.IsType<BadRequestObjectResult>(result);
            _mockStateProvinceService.Verify(s => s.GetStateProvincesByCultureAsync(It.IsAny<string>()), Times.Never);
        }

        // --- Response shape ---

        [Fact]
        public async Task Run_ValidCulture_ResponseContainsTotalCountAndData()
        {
            var req = CreateRequest();
            var stateProvinces = CreateStateProvinces("en-US");
            _mockStateProvinceService
                .Setup(s => s.GetStateProvincesByCultureAsync("en-US"))
                .ReturnsAsync(stateProvinces);

            var result = await _function.Run(req, "en-US");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value!;
            var totalCount = (int)value.GetType().GetProperty("TotalCount")!.GetValue(value)!;
            var culture = (string)value.GetType().GetProperty("Culture")!.GetValue(value)!;
            Assert.Equal(2, totalCount);
            Assert.Equal("en-US", culture);
        }

        // --- Exception handling ---

        [Fact]
        public async Task Run_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            var req = CreateRequest();
            _mockStateProvinceService
                .Setup(s => s.GetStateProvincesByCultureAsync("bad-culture"))
                .ThrowsAsync(new ArgumentException("Invalid culture"));

            var result = await _function.Run(req, "bad-culture");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_ServiceThrowsException_ReturnsInternalServerError()
        {
            var req = CreateRequest();
            _mockStateProvinceService
                .Setup(s => s.GetStateProvincesByCultureAsync("en-US"))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _function.Run(req, "en-US");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }
    }
}
