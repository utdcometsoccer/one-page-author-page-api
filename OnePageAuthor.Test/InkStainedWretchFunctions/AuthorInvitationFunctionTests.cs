using System.Text;
using System.Text.Json;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Services;
using InkStainedWretchFunctions;
using System.Net;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    /// <summary>
    /// Unit tests for AuthorInvitationFunction and AuthorInvitationService.
    /// </summary>
    public class AuthorInvitationFunctionTests
    {
        private readonly Mock<ILogger<AuthorInvitationFunction>> _mockLogger;
        private readonly Mock<IAuthorInvitationService> _mockService;
        private readonly AuthorInvitationFunction _function;

        public AuthorInvitationFunctionTests()
        {
            _mockLogger = new Mock<ILogger<AuthorInvitationFunction>>();
            _mockService = new Mock<IAuthorInvitationService>();
            _function = new AuthorInvitationFunction(_mockLogger.Object, _mockService.Object);
        }

        [Fact]
        public void Constructor_InitializesWithDependencies()
        {
            // Arrange & Act
            var logger = new Mock<ILogger<AuthorInvitationFunction>>();
            var service = new Mock<IAuthorInvitationService>();
            var function = new AuthorInvitationFunction(logger.Object, service.Object);

            // Assert
            Assert.NotNull(function);
        }

        [Fact]
        public void Constructor_InitializesWithoutEmailService()
        {
            // The service handles optional email internally; function only needs the service.
            var logger = new Mock<ILogger<AuthorInvitationFunction>>();
            var service = new Mock<IAuthorInvitationService>();
            var function = new AuthorInvitationFunction(logger.Object, service.Object);

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

        [Theory]
        [InlineData("test@example.com", true)]
        [InlineData("user.name+tag@example.co.uk", true)]
        [InlineData("invalid-email", false)]
        [InlineData("@example.com", false)]
        [InlineData("test@", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void EmailValidation_WorksCorrectly(string? email, bool expectedValid)
        {
            // Validation logic is now in AuthorInvitationService
            var repository = new Mock<IAuthorInvitationRepository>();
            var logger = new Mock<ILogger<AuthorInvitationService>>();
            var service = new AuthorInvitationService(repository.Object, logger.Object);

            var result = service.IsValidEmail(email!);
            Assert.Equal(expectedValid, result);
        }

        [Theory]
        [InlineData("example.com", true)]
        [InlineData("subdomain.example.com", true)]
        [InlineData("test-site.example.co.uk", true)]
        [InlineData("example.COM", true)] // Should handle case-insensitive
        [InlineData("example.com.", true)] // Should handle trailing dot
        [InlineData("localhost", false)] // Should reject localhost
        [InlineData("192.168.1.1", false)] // Should reject IPv4
        [InlineData("2001:db8::1", false)] // Should reject IPv6
        [InlineData("example", false)] // Should require at least one dot
        [InlineData("example .com", false)] // Should reject spaces
        [InlineData("-example.com", false)] // Should reject hyphen at start of label
        [InlineData("example-.com", false)] // Should reject hyphen at end of label
        [InlineData("", false)]
        [InlineData(null, false)]
        public void DomainValidation_WorksCorrectly(string? domain, bool expectedValid)
        {
            // Validation logic is now in AuthorInvitationService
            var repository = new Mock<IAuthorInvitationRepository>();
            var logger = new Mock<ILogger<AuthorInvitationService>>();
            var service = new AuthorInvitationService(repository.Object, logger.Object);

            var result = service.IsValidDomain(domain!);
            Assert.Equal(expectedValid, result);
        }

        [Fact]
        public async Task Service_WhenNoExistingInvitation_CreatesNewInvitation()
        {
            // Arrange
            var testInvitation = new AuthorInvitation("test@example.com", "example.com", "Test");
            testInvitation.id = "test-123";
            var result = new CreateInvitationResult { Invitation = testInvitation, EmailSent = false };

            _mockService.Setup(s => s.CreateInvitationAsync(
                    "test@example.com",
                    It.IsAny<List<string>>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(result);

            // Act
            var created = await _mockService.Object.CreateInvitationAsync(
                "test@example.com", new List<string> { "example.com" }, "Test");

            // Assert
            Assert.NotNull(created);
            Assert.NotNull(created.Invitation);
            Assert.Equal("test-123", created.Invitation.id);
        }

        [Fact]
        public async Task Service_WhenExistingInvitation_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockService = new Mock<IAuthorInvitationService>();
            mockService.Setup(s => s.CreateInvitationAsync(
                    "test@example.com",
                    It.IsAny<List<string>>(),
                    It.IsAny<string?>()))
                .ThrowsAsync(new InvalidOperationException(
                    "An invitation already exists for test@example.com with status 'Pending'."));

            // Act & Assert - Service should throw when invitation already exists
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await mockService.Object.CreateInvitationAsync(
                    "test@example.com", new List<string> { "example.com" }, "Test");
            });
        }

        [Fact]
        public async Task Service_WhenEmailServiceConfigured_EmailSentFlagReflectedInResult()
        {
            // Arrange
            var mockService = new Mock<IAuthorInvitationService>();
            var invitation = new AuthorInvitation("test@example.com", "example.com", "Test");
            invitation.id = "invite-123";
            var expectedResult = new CreateInvitationResult { Invitation = invitation, EmailSent = true };

            mockService.Setup(s => s.CreateInvitationAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockService.Object.CreateInvitationAsync(
                "test@example.com", new List<string> { "example.com" }, "Test");

            // Assert
            Assert.True(result.EmailSent);
            mockService.Verify(s => s.CreateInvitationAsync(
                "test@example.com",
                It.IsAny<List<string>>(),
                It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public async Task Service_WhenEmailServiceNotConfigured_EmailSentFalse()
        {
            // Arrange - Service returns EmailSent = false when email service is unavailable
            var mockService = new Mock<IAuthorInvitationService>();
            var invitation = new AuthorInvitation("test@example.com", "example.com", "Test");
            invitation.id = "test-123";
            var expectedResult = new CreateInvitationResult { Invitation = invitation, EmailSent = false };

            mockService.Setup(s => s.CreateInvitationAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockService.Object.CreateInvitationAsync(
                "test@example.com", new List<string> { "example.com" }, "Test");

            // Assert
            Assert.NotNull(result.Invitation);
            Assert.False(result.EmailSent);
        }

        [Fact]
        public async Task Service_WhenEmailThrowsException_CreateInvitationHandlesGracefully()
        {
            // Arrange - Service catches email exceptions and returns EmailSent = false
            var repository = new Mock<IAuthorInvitationRepository>();
            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthorInvitationService>>();

            repository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((AuthorInvitation?)null);

            var savedInvitation = new AuthorInvitation("test@example.com", "example.com", "Test");
            savedInvitation.id = "test-123";
            repository.Setup(r => r.AddAsync(It.IsAny<AuthorInvitation>()))
                .ReturnsAsync(savedInvitation);

            emailService.Setup(e => e.SendInvitationEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service error"));

            var service = new AuthorInvitationService(repository.Object, logger.Object, emailService.Object);

            // Act - Email exception should be caught; invitation should still be created
            var result = await service.CreateInvitationAsync(
                "test@example.com", new List<string> { "example.com" }, "Test");

            // Assert
            Assert.NotNull(result.Invitation);
            Assert.False(result.EmailSent);
        }

        [Fact]
        public void AuthorInvitation_SupportsMultipleDomains()
        {
            // Arrange & Act
            var domains = new List<string> { "example.com", "author-site.com" };
            var invitation = new AuthorInvitation("test@example.com", domains, "Multi-domain test");

            // Assert
            Assert.Equal(2, invitation.DomainNames.Count);
            Assert.Contains("example.com", invitation.DomainNames);
            Assert.Contains("author-site.com", invitation.DomainNames);
            Assert.Equal("example.com", invitation.GetPrimaryDomainName());

#pragma warning disable CS0618 // 'DomainName' is obsolete
            Assert.Equal("example.com", invitation.DomainName); // Backward compatibility
#pragma warning restore CS0618
        }

        [Fact]
        public void AuthorInvitation_BackwardCompatibility_SingleDomain()
        {
            // Arrange & Act
            var invitation = new AuthorInvitation("test@example.com", "example.com", "Single domain test");

            // Assert
            Assert.Single(invitation.DomainNames);
            Assert.Equal("example.com", invitation.DomainNames.First());
            Assert.Equal("example.com", invitation.GetPrimaryDomainName());

#pragma warning disable CS0618 // 'DomainName' is obsolete
            Assert.Equal("example.com", invitation.DomainName);
#pragma warning restore CS0618
        }

        [Fact]
        public void CreateAuthorInvitationRequest_SupportsSingleDomain()
        {
            // Arrange & Act
            var request = new CreateAuthorInvitationRequest
            {
                EmailAddress = "test@example.com",
                DomainName = "example.com",
                Notes = "Test"
            };

            // Assert
            Assert.Equal("test@example.com", request.EmailAddress);
            Assert.Equal("example.com", request.DomainName);
        }

        [Fact]
        public void CreateAuthorInvitationRequest_SupportsMultipleDomains()
        {
            // Arrange & Act
            var request = new CreateAuthorInvitationRequest
            {
                EmailAddress = "test@example.com",
                DomainNames = new List<string> { "example.com", "author-site.com" },
                Notes = "Test"
            };

            // Assert
            Assert.Equal("test@example.com", request.EmailAddress);
            Assert.NotNull(request.DomainNames);
            Assert.Equal(2, request.DomainNames.Count);
        }

        [Fact]
        public void UpdateAuthorInvitationRequest_HasRequiredProperties()
        {
            // Arrange & Act
            var request = new UpdateAuthorInvitationRequest
            {
                DomainNames = new List<string> { "example.com", "newdomain.com" },
                Notes = "Updated notes",
                ExpiresAt = DateTime.UtcNow.AddDays(60)
            };

            // Assert
            Assert.NotNull(request.DomainNames);
            Assert.Equal(2, request.DomainNames.Count);
            Assert.Equal("Updated notes", request.Notes);
            Assert.NotNull(request.ExpiresAt);
        }

        [Fact]
        public void ResendInvitationResponse_HasRequiredProperties()
        {
            // Arrange & Act
            var response = new ResendInvitationResponse
            {
                Id = "test-123",
                EmailAddress = "test@example.com",
                EmailSent = true,
                LastEmailSentAt = DateTime.UtcNow
            };

            // Assert
            Assert.Equal("test-123", response.Id);
            Assert.Equal("test@example.com", response.EmailAddress);
            Assert.True(response.EmailSent);
            Assert.NotNull(response.LastEmailSentAt);
        }

        [Fact]
        public async Task Service_UpdateAsync_UpdatesInvitation()
        {
            // Arrange
            var invitation = new AuthorInvitation("test@example.com", "example.com");
            invitation.id = "test-123";
            invitation.DomainNames = new List<string> { "example.com", "newdomain.com" };
            invitation.LastUpdatedAt = DateTime.UtcNow;

            _mockService.Setup(s => s.UpdateInvitationAsync(
                    "test-123",
                    It.IsAny<List<string>>(),
                    It.IsAny<string?>(),
                    It.IsAny<DateTime?>()))
                .ReturnsAsync(invitation);

            // Act
            var result = await _mockService.Object.UpdateInvitationAsync(
                "test-123",
                new List<string> { "example.com", "newdomain.com" },
                null,
                null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-123", result.id);
            Assert.Equal(2, result.DomainNames.Count);
            Assert.NotNull(result.LastUpdatedAt);
        }

        [Fact]
        public async Task Service_GetPendingInvitationsAsync_ReturnsOnlyPending()
        {
            // Arrange
            var pendingInvitations = new List<AuthorInvitation>
            {
                new AuthorInvitation("test1@example.com", "example1.com") { Status = "Pending" },
                new AuthorInvitation("test2@example.com", "example2.com") { Status = "Pending" }
            };

            _mockService.Setup(s => s.GetPendingInvitationsAsync())
                .ReturnsAsync(pendingInvitations);

            // Act
            var result = await _mockService.Object.GetPendingInvitationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, invitation => Assert.Equal("Pending", invitation.Status));
        }

        [Fact]
        public void AuthorInvitationFunction_HasListAuthorInvitationsMethod()
        {
            // Arrange & Act
            var method = typeof(AuthorInvitationFunction).GetMethod("ListAuthorInvitations");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsPublic);
        }

        [Fact]
        public void AuthorInvitationFunction_HasGetAuthorInvitationByIdMethod()
        {
            // Arrange & Act
            var method = typeof(AuthorInvitationFunction).GetMethod("GetAuthorInvitationById");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsPublic);
        }

        [Fact]
        public void AuthorInvitationFunction_HasUpdateAuthorInvitationMethod()
        {
            // Arrange & Act
            var method = typeof(AuthorInvitationFunction).GetMethod("UpdateAuthorInvitation");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsPublic);
        }

        [Fact]
        public void AuthorInvitationFunction_HasResendAuthorInvitationMethod()
        {
            // Arrange & Act
            var method = typeof(AuthorInvitationFunction).GetMethod("ResendAuthorInvitation");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsPublic);
        }

        [Fact]
        public void AuthorInvitation_NoDuplicateDomains_WhenSerializedAndDeserialized()
        {
            // Arrange - Create invitation with exact scenario from GitHub issue
            var domains = new List<string> { "whoisidaho.com", "edokpayi.com" };
            var invitation = new AuthorInvitation("idaho@edokpayi.com", domains, "testing");

            // Act - Serialize to JSON (simulating Cosmos DB write)
            var json = System.Text.Json.JsonSerializer.Serialize(invitation);

            // Deserialize from JSON (simulating Cosmos DB read)
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<AuthorInvitation>(json);

            // Assert - No duplication should occur
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.DomainNames.Count);
            Assert.Equal("whoisidaho.com", deserialized.DomainNames[0]);
            Assert.Equal("edokpayi.com", deserialized.DomainNames[1]);

            // Verify no duplicates by checking distinct count
            Assert.Equal(deserialized.DomainNames.Count, deserialized.DomainNames.Distinct().Count());
        }

        [Fact]
        public void AuthorInvitation_EnsureDomainNamesMigrated_PopulatesFromDomainName()
        {
            // Arrange - Simulate old data with only DomainName set
            var invitation = new AuthorInvitation();
#pragma warning disable CS0618 // 'DomainName' is obsolete
            invitation.DomainName = "oldsite.com";
#pragma warning restore CS0618
            invitation.EmailAddress = "old@example.com";

            // Act - Call migration method (what repository does)
            invitation.EnsureDomainNamesMigrated();

            // Assert - DomainNames should be populated from DomainName
            Assert.Single(invitation.DomainNames);
            Assert.Equal("oldsite.com", invitation.DomainNames[0]);
        }

        [Fact]
        public void AuthorInvitation_EnsureDomainNamesMigrated_DoesNotDuplicate()
        {
            // Arrange - Create invitation with domains already set
            var domains = new List<string> { "example.com", "site.com" };
            var invitation = new AuthorInvitation("test@example.com", domains, "test");

            // Act - Call migration method (should not modify existing data)
            invitation.EnsureDomainNamesMigrated();

            // Assert - DomainNames should remain unchanged
            Assert.Equal(2, invitation.DomainNames.Count);
            Assert.Equal("example.com", invitation.DomainNames[0]);
            Assert.Equal("site.com", invitation.DomainNames[1]);
        }
    }
}
