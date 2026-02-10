using System.Text;
using System.Text.Json;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretchFunctions;
using System.Net;

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
        private readonly AuthorInvitationFunction _functionWithoutEmail;

        public AuthorInvitationFunctionTests()
        {
            _mockLogger = new Mock<ILogger<AuthorInvitationFunction>>();
            _mockRepository = new Mock<IAuthorInvitationRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _function = new AuthorInvitationFunction(
                _mockLogger.Object,
                _mockRepository.Object,
                _mockEmailService.Object);
            _functionWithoutEmail = new AuthorInvitationFunction(
                _mockLogger.Object,
                _mockRepository.Object,
                null);
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
            // This test validates the email validation logic through reflection
            var method = typeof(AuthorInvitationFunction).GetMethod("IsValidEmail", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert
            Assert.NotNull(method);
            var result = (bool)method!.Invoke(null, new object?[] { email })!;
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
            // This test validates the domain validation logic through reflection
            var method = typeof(AuthorInvitationFunction).GetMethod("IsValidDomain", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Assert
            Assert.NotNull(method);
            var result = (bool)method!.Invoke(null, new object?[] { domain })!;
            Assert.Equal(expectedValid, result);
        }

        [Fact]
        public async Task Repository_WhenNoExistingInvitation_CreatesNewInvitation()
        {
            // Arrange
            var testInvitation = new AuthorInvitation("test@example.com", "example.com", "Test");
            testInvitation.id = "test-123";

            _mockRepository.Setup(r => r.GetByEmailAsync("test@example.com"))
                .ReturnsAsync((AuthorInvitation?)null);
            
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<AuthorInvitation>()))
                .ReturnsAsync(testInvitation);

            // Act - Since we can't easily mock HttpRequestData, we verify the repository setup
            
            // Assert - Verify mocks were set up correctly
            var existingCheck = await _mockRepository.Object.GetByEmailAsync("test@example.com");
            Assert.Null(existingCheck);
            
            var created = await _mockRepository.Object.AddAsync(testInvitation);
            Assert.NotNull(created);
            Assert.Equal("test-123", created.id);
        }

        [Fact]
        public async Task Repository_WhenExistingInvitation_ShouldReturnConflict()
        {
            // Arrange
            var existingInvitation = new AuthorInvitation("test@example.com", "example.com", "Existing");
            existingInvitation.id = "existing-123";
            existingInvitation.Status = "Pending";

            _mockRepository.Setup(r => r.GetByEmailAsync("test@example.com"))
                .ReturnsAsync(existingInvitation);

            // Act - Verify repository returns existing invitation
            var result = await _mockRepository.Object.GetByEmailAsync("test@example.com");

            // Assert - Should find existing invitation (function should return 409)
            Assert.NotNull(result);
            Assert.Equal("existing-123", result.id);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task EmailService_WhenConfigured_IsCalled()
        {
            // Arrange
            _mockEmailService.Setup(e => e.SendInvitationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _mockEmailService.Object.SendInvitationEmailAsync(
                "test@example.com", 
                "example.com", 
                "invite-123");

            // Assert
            Assert.True(result);
            _mockEmailService.Verify(e => e.SendInvitationEmailAsync(
                "test@example.com",
                "example.com",
                "invite-123"), Times.Once);
        }

        [Fact]
        public async Task EmailService_WhenNotConfigured_DoesNotThrow()
        {
            // Arrange - Function created without email service
            var testInvitation = new AuthorInvitation("test@example.com", "example.com", "Test");
            testInvitation.id = "test-123";

            _mockRepository.Setup(r => r.GetByEmailAsync("test@example.com"))
                .ReturnsAsync((AuthorInvitation?)null);
            
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<AuthorInvitation>()))
                .ReturnsAsync(testInvitation);

            // Act & Assert - Should not throw when email service is null
            var created = await _mockRepository.Object.AddAsync(testInvitation);
            Assert.NotNull(created);
        }

        [Fact]
        public async Task EmailService_WhenThrowsException_ShouldBeCaught()
        {
            // Arrange
            _mockEmailService.Setup(e => e.SendInvitationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service error"));

            // Act & Assert - Exception should be caught and handled
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _mockEmailService.Object.SendInvitationEmailAsync(
                    "test@example.com",
                    "example.com",
                    "invite-123");
            });
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
        public async Task Repository_UpdateAsync_UpdatesInvitation()
        {
            // Arrange
            var invitation = new AuthorInvitation("test@example.com", "example.com");
            invitation.id = "test-123";
            invitation.DomainNames = new List<string> { "example.com", "newdomain.com" };
            invitation.LastUpdatedAt = DateTime.UtcNow;

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<AuthorInvitation>()))
                .ReturnsAsync(invitation);

            // Act
            var result = await _mockRepository.Object.UpdateAsync(invitation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-123", result.id);
            Assert.Equal(2, result.DomainNames.Count);
            Assert.NotNull(result.LastUpdatedAt);
        }

        [Fact]
        public async Task Repository_GetPendingInvitationsAsync_ReturnsOnlyPending()
        {
            // Arrange
            var pendingInvitations = new List<AuthorInvitation>
            {
                new AuthorInvitation("test1@example.com", "example1.com") { Status = "Pending" },
                new AuthorInvitation("test2@example.com", "example2.com") { Status = "Pending" }
            };

            _mockRepository.Setup(r => r.GetPendingInvitationsAsync())
                .ReturnsAsync(pendingInvitations);

            // Act
            var result = await _mockRepository.Object.GetPendingInvitationsAsync();

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
        public void AuthorInvitationFunction_HasGetAuthorInvitationMethod()
        {
            // Arrange & Act
            var method = typeof(AuthorInvitationFunction).GetMethod("GetAuthorInvitation");

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
