using System.Text;
using System.Text.Json;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretchFunctions;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    public class ReferralFunctionTests
    {
        private readonly Mock<ILogger<ReferralFunction>> _mockLogger;
        private readonly Mock<IReferralService> _mockService;
        private readonly ReferralFunction _function;

        public ReferralFunctionTests()
        {
            _mockLogger = new Mock<ILogger<ReferralFunction>>();
            _mockService = new Mock<IReferralService>();
            _function = new ReferralFunction(_mockLogger.Object, _mockService.Object);
        }

        [Fact]
        public void Constructor_InitializesWithDependencies()
        {
            // Arrange & Act
            var logger = new Mock<ILogger<ReferralFunction>>();
            var service = new Mock<IReferralService>();
            var function = new ReferralFunction(logger.Object, service.Object);

            // Assert
            Assert.NotNull(function);
        }

        [Fact]
        public void ReferralFunction_HasCreateReferralMethod()
        {
            // Arrange & Act
            var method = typeof(ReferralFunction).GetMethod("CreateReferral");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsPublic);
        }

        [Fact]
        public void ReferralFunction_HasGetReferralStatsMethod()
        {
            // Arrange & Act
            var method = typeof(ReferralFunction).GetMethod("GetReferralStats");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsPublic);
        }

        [Fact]
        public void ReferralFunction_CreateReferralMethod_HasFunctionAttribute()
        {
            // Arrange & Act
            var method = typeof(ReferralFunction).GetMethod("CreateReferral");
            var attributes = method?.GetCustomAttributes(typeof(FunctionAttribute), false);

            // Assert
            Assert.NotNull(attributes);
            Assert.NotEmpty(attributes);
            var functionAttr = attributes[0] as FunctionAttribute;
            Assert.Equal("CreateReferral", functionAttr?.Name);
        }

        [Fact]
        public void ReferralFunction_GetReferralStatsMethod_HasFunctionAttribute()
        {
            // Arrange & Act
            var method = typeof(ReferralFunction).GetMethod("GetReferralStats");
            var attributes = method?.GetCustomAttributes(typeof(FunctionAttribute), false);

            // Assert
            Assert.NotNull(attributes);
            Assert.NotEmpty(attributes);
            var functionAttr = attributes[0] as FunctionAttribute;
            Assert.Equal("GetReferralStats", functionAttr?.Name);
        }
    }
}
