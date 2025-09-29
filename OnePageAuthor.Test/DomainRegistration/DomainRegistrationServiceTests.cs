using Moq;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace OnePageAuthor.Test.DomainRegistration
{
    public class DomainRegistrationServiceTests
    {
        private readonly Mock<ILogger<DomainRegistrationService>> _loggerMock;
        private readonly Mock<IDomainRegistrationRepository> _repositoryMock;
        private readonly DomainRegistrationService _service;

        public DomainRegistrationServiceTests()
        {
            _loggerMock = new Mock<ILogger<DomainRegistrationService>>();
            _repositoryMock = new Mock<IDomainRegistrationRepository>();
            _service = new DomainRegistrationService(_loggerMock.Object, _repositoryMock.Object);
        }

        private static ClaimsPrincipal CreateTestUser(string upn = "test@example.com", string oid = "test-oid-123")
        {
            var claims = new List<Claim>
            {
                new Claim("upn", upn),
                new Claim("oid", oid)
            };
            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }

        private static Domain CreateTestDomain()
        {
            return new Domain
            {
                TopLevelDomain = "com",
                SecondLevelDomain = "example"
            };
        }

        private static ContactInformation CreateTestContactInfo()
        {
            return new ContactInformation
            {
                FirstName = "John",
                LastName = "Doe",
                Address = "123 Main St",
                City = "Anytown",
                State = "CA",
                Country = "USA",
                ZipCode = "12345",
                EmailAddress = "john@example.com",
                TelephoneNumber = "+1-555-123-4567"
            };
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_Success_ValidInput()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = CreateTestDomain();
            var contactInfo = CreateTestContactInfo();
            var expectedRegistration = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = "test-id-123",
                Upn = "test@example.com",
                Domain = domain,
                ContactInformation = contactInfo,
                CreatedAt = DateTime.UtcNow,
                Status = DomainRegistrationStatus.Pending
            };

            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()))
                          .ReturnsAsync(expectedRegistration);

            // Act
            var result = await _service.CreateDomainRegistrationAsync(user, domain, contactInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Upn);
            Assert.Equal("com", result.Domain.TopLevelDomain);
            Assert.Equal("example", result.Domain.SecondLevelDomain);
            Assert.Equal("John", result.ContactInformation.FirstName);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()), Times.Once);
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_NullDomain()
        {
            // Arrange
            var user = CreateTestUser();
            var contactInfo = CreateTestContactInfo();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(user, null!, contactInfo));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_NullContactInfo()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = CreateTestDomain();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(user, domain, null!));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_InvalidDomain_EmptySecondLevel()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = new Domain { TopLevelDomain = "com", SecondLevelDomain = "" };
            var contactInfo = CreateTestContactInfo();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(user, domain, contactInfo));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_InvalidDomain_EmptyTopLevel()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = new Domain { TopLevelDomain = "", SecondLevelDomain = "example" };
            var contactInfo = CreateTestContactInfo();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(user, domain, contactInfo));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_InvalidContactInfo_MissingFirstName()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = CreateTestDomain();
            var contactInfo = CreateTestContactInfo();
            contactInfo.FirstName = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(user, domain, contactInfo));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_InvalidContactInfo_MissingEmail()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = CreateTestDomain();
            var contactInfo = CreateTestContactInfo();
            contactInfo.EmailAddress = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(user, domain, contactInfo));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_InvalidContactInfo_InvalidEmail()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = CreateTestDomain();
            var contactInfo = CreateTestContactInfo();
            contactInfo.EmailAddress = "invalid-email";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(user, domain, contactInfo));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_UnauthenticatedUser()
        {
            // Arrange
            var user = new ClaimsPrincipal(); // No claims, not authenticated
            var domain = CreateTestDomain();
            var contactInfo = CreateTestContactInfo();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateDomainRegistrationAsync(user, domain, contactInfo));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_UserMissingUpnClaim()
        {
            // Arrange
            var claims = new List<Claim> { new Claim("oid", "test-oid") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);
            var domain = CreateTestDomain();
            var contactInfo = CreateTestContactInfo();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateDomainRegistrationAsync(user, domain, contactInfo));
        }

        [Fact]
        public async Task GetUserDomainRegistrationsAsync_Success()
        {
            // Arrange
            var user = CreateTestUser();
            var expectedRegistrations = new List<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>
            {
                new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
                {
                    id = "reg-1",
                    Upn = "test@example.com",
                    Domain = CreateTestDomain(),
                    ContactInformation = CreateTestContactInfo()
                },
                new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
                {
                    id = "reg-2",
                    Upn = "test@example.com",
                    Domain = new Domain { TopLevelDomain = "org", SecondLevelDomain = "test" },
                    ContactInformation = CreateTestContactInfo()
                }
            };

            _repositoryMock.Setup(r => r.GetByUserAsync("test@example.com"))
                          .ReturnsAsync(expectedRegistrations);

            // Act
            var result = await _service.GetUserDomainRegistrationsAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _repositoryMock.Verify(r => r.GetByUserAsync("test@example.com"), Times.Once);
        }

        [Fact]
        public async Task GetDomainRegistrationByIdAsync_Success()
        {
            // Arrange
            var user = CreateTestUser();
            var registrationId = "test-reg-id";
            var expectedRegistration = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = registrationId,
                Upn = "test@example.com",
                Domain = CreateTestDomain(),
                ContactInformation = CreateTestContactInfo()
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(registrationId, "test@example.com"))
                          .ReturnsAsync(expectedRegistration);

            // Act
            var result = await _service.GetDomainRegistrationByIdAsync(user, registrationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(registrationId, result.id);
            Assert.Equal("test@example.com", result.Upn);
            _repositoryMock.Verify(r => r.GetByIdAsync(registrationId, "test@example.com"), Times.Once);
        }

        [Fact]
        public async Task GetDomainRegistrationByIdAsync_ReturnsNull_NotFound()
        {
            // Arrange
            var user = CreateTestUser();
            var registrationId = "non-existent-id";

            _repositoryMock.Setup(r => r.GetByIdAsync(registrationId, "test@example.com"))
                          .ReturnsAsync((InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration?)null);

            // Act
            var result = await _service.GetDomainRegistrationByIdAsync(user, registrationId);

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(registrationId, "test@example.com"), Times.Once);
        }

        [Fact]
        public async Task UpdateDomainRegistrationStatusAsync_Success()
        {
            // Arrange
            var user = CreateTestUser();
            var registrationId = "test-reg-id";
            var existingRegistration = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = registrationId,
                Upn = "test@example.com",
                Domain = CreateTestDomain(),
                ContactInformation = CreateTestContactInfo(),
                Status = DomainRegistrationStatus.Pending
            };
            var updatedRegistration = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = registrationId,
                Upn = "test@example.com",
                Domain = CreateTestDomain(),
                ContactInformation = CreateTestContactInfo(),
                Status = DomainRegistrationStatus.Completed
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(registrationId, "test@example.com"))
                          .ReturnsAsync(existingRegistration);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()))
                          .ReturnsAsync(updatedRegistration);

            // Act
            var result = await _service.UpdateDomainRegistrationStatusAsync(user, registrationId, DomainRegistrationStatus.Completed);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DomainRegistrationStatus.Completed, result.Status);
            _repositoryMock.Verify(r => r.GetByIdAsync(registrationId, "test@example.com"), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()), Times.Once);
        }

        [Fact]
        public async Task UpdateDomainRegistrationStatusAsync_ReturnsNull_NotFound()
        {
            // Arrange
            var user = CreateTestUser();
            var registrationId = "non-existent-id";

            _repositoryMock.Setup(r => r.GetByIdAsync(registrationId, "test@example.com"))
                          .ReturnsAsync((InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration?)null);

            // Act
            var result = await _service.UpdateDomainRegistrationStatusAsync(user, registrationId, DomainRegistrationStatus.Completed);

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(registrationId, "test@example.com"), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()), Times.Never);
        }
    }
}