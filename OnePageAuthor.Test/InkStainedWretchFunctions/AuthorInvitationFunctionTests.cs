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
    /// <summary>
    /// Unit tests for AuthorInvitationFunction.
    /// </summary>
    public class AuthorInvitationFunctionTests
    {
        private readonly Mock<ILogger<AuthorInvitationFunction>> _mockLogger;
        private readonly Mock<IAuthorInvitationRepository> _mockRepository;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AuthorInvitationFunction _function;

        public AuthorInvitationFunctionTests()
        {
            _mockLogger = new Mock<ILogger<AuthorInvitationFunction>>();
            _mockRepository = new Mock<IAuthorInvitationRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _function = new AuthorInvitationFunction(
                _mockLogger.Object,
                _mockRepository.Object,
                _mockEmailService.Object);
        }

        [Fact]
        public void Constructor_InitializesWithDependencies()
        {
            // Arrange & Act
            var logger = new Mock<ILogger<AuthorInvitationFunction>>();
            var repository = new Mock<IAuthorInvitationRepository>();
            var emailService = new Mock<IEmailService>();
            var function = new AuthorInvitationFunction(logger.Object, repository.Object, emailService.Object);

            // Assert
            Assert.NotNull(function);
        }

        [Fact]
        public void Constructor_InitializesWithoutEmailService()
        {
            // Arrange & Act - Email service is optional
            var logger = new Mock<ILogger<AuthorInvitationFunction>>();
            var repository = new Mock<IAuthorInvitationRepository>();
            var function = new AuthorInvitationFunction(logger.Object, repository.Object, null);

            // Assert
            Assert.NotNull(function);
        }

        [Fact]
        public void AuthorInvitationFunction_HasCreateAuthorInvitationMethod()
        {
            // Arrange & Act
            var method = typeof(AuthorInvitationFunction).GetMethod("CreateAuthorInvitation");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsPublic);
        }

        [Fact]
        public void AuthorInvitationFunction_CreateAuthorInvitationMethod_HasFunctionAttribute()
        {
            // Arrange & Act
            var method = typeof(AuthorInvitationFunction).GetMethod("CreateAuthorInvitation");
            var attributes = method?.GetCustomAttributes(typeof(FunctionAttribute), false);

            // Assert
            Assert.NotNull(attributes);
            Assert.NotEmpty(attributes);
            var functionAttr = attributes[0] as FunctionAttribute;
            Assert.Equal("CreateAuthorInvitation", functionAttr?.Name);
        }

        [Fact]
        public void AuthorInvitationFunction_CreateAuthorInvitationMethod_HasAuthorizeAttribute()
        {
            // Arrange & Act
            var method = typeof(AuthorInvitationFunction).GetMethod("CreateAuthorInvitation");
            var attributes = method?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

            // Assert
            Assert.NotNull(attributes);
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void CreateAuthorInvitationRequest_HasRequiredProperties()
        {
            // Arrange & Act
            var request = new CreateAuthorInvitationRequest
            {
                EmailAddress = "test@example.com",
                DomainName = "example.com",
                Notes = "Test note"
            };

            // Assert
            Assert.Equal("test@example.com", request.EmailAddress);
            Assert.Equal("example.com", request.DomainName);
            Assert.Equal("Test note", request.Notes);
        }

        [Fact]
        public void CreateAuthorInvitationResponse_HasRequiredProperties()
        {
            // Arrange & Act
            var response = new CreateAuthorInvitationResponse
            {
                Id = "invite-123",
                EmailAddress = "test@example.com",
                DomainName = "example.com",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Notes = "Test note",
                EmailSent = true
            };

            // Assert
            Assert.Equal("invite-123", response.Id);
            Assert.Equal("test@example.com", response.EmailAddress);
            Assert.Equal("example.com", response.DomainName);
            Assert.Equal("Pending", response.Status);
            Assert.NotEqual(default, response.CreatedAt);
            Assert.NotEqual(default, response.ExpiresAt);
            Assert.Equal("Test note", response.Notes);
            Assert.True(response.EmailSent);
        }
    }
}
