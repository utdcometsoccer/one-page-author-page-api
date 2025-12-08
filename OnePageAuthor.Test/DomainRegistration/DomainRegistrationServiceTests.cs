using Moq;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using static InkStainedWretch.OnePageAuthorAPI.Interfaces.ValidationResult;

namespace OnePageAuthor.Test.DomainRegistration
{
    public class DomainRegistrationServiceTests
    {
        private readonly Mock<ILogger<DomainRegistrationService>> _loggerMock;
        private readonly Mock<IDomainRegistrationRepository> _repositoryMock;
        private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
        private readonly Mock<IDomainValidationService> _domainValidationServiceMock;
        private readonly Mock<IContactInformationValidationService> _contactValidationServiceMock;
        private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock;
        private readonly Mock<InkStainedWretch.OnePageAuthorLib.API.Stripe.IListSubscriptions> _listSubscriptionsMock;
        private readonly DomainRegistrationService _service;

        public DomainRegistrationServiceTests()
        {
            _loggerMock = new Mock<ILogger<DomainRegistrationService>>();
            _repositoryMock = new Mock<IDomainRegistrationRepository>();
            _userIdentityServiceMock = new Mock<IUserIdentityService>();
            _domainValidationServiceMock = new Mock<IDomainValidationService>();
            _contactValidationServiceMock = new Mock<IContactInformationValidationService>();
            _userProfileRepositoryMock = new Mock<IUserProfileRepository>();
            _listSubscriptionsMock = new Mock<InkStainedWretch.OnePageAuthorLib.API.Stripe.IListSubscriptions>();
            _service = new DomainRegistrationService(
                _loggerMock.Object, 
                _repositoryMock.Object, 
                _userIdentityServiceMock.Object,
                _domainValidationServiceMock.Object,
                _contactValidationServiceMock.Object,
                _userProfileRepositoryMock.Object,
                _listSubscriptionsMock.Object);

            // Setup default behavior for user identity service
            _userIdentityServiceMock.Setup(x => x.GetUserUpn(It.IsAny<ClaimsPrincipal>()))
                                   .Returns("test@example.com");

            // Setup default successful validation behavior
            _domainValidationServiceMock.Setup(x => x.ValidateDomain(It.IsAny<Domain>()))
                                       .Returns(ValidationResult.Success());
            _contactValidationServiceMock.Setup(x => x.ValidateContactInformation(It.IsAny<ContactInformation>()))
                                        .Returns(ValidationResult.Success());
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

            // Setup domain validation to fail for null domain
            _domainValidationServiceMock.Setup(x => x.ValidateDomain(null!))
                                       .Returns(ValidationResult.Failure("Domain information is required"));

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

            // Setup contact validation to fail for null contact info
            _contactValidationServiceMock.Setup(x => x.ValidateContactInformation(null!))
                                        .Returns(ValidationResult.Failure("Contact information is required"));

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

            // Setup domain validation to fail for empty second level domain
            _domainValidationServiceMock.Setup(x => x.ValidateDomain(It.Is<Domain>(d => d.SecondLevelDomain == "")))
                                       .Returns(ValidationResult.Failure("Second level domain is required"));

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

            // Setup domain validation to fail for empty top level domain
            _domainValidationServiceMock.Setup(x => x.ValidateDomain(It.Is<Domain>(d => d.TopLevelDomain == "")))
                                       .Returns(ValidationResult.Failure("Top level domain is required"));

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

            // Setup contact validation to fail for missing first name
            _contactValidationServiceMock.Setup(x => x.ValidateContactInformation(It.Is<ContactInformation>(c => c.FirstName == "")))
                                        .Returns(ValidationResult.Failure("First name is required"));

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

            // Setup contact validation to fail for missing email
            _contactValidationServiceMock.Setup(x => x.ValidateContactInformation(It.Is<ContactInformation>(c => c.EmailAddress == "")))
                                        .Returns(ValidationResult.Failure("Email address is required"));

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

            // Setup contact validation to fail for invalid email format
            _contactValidationServiceMock.Setup(x => x.ValidateContactInformation(It.Is<ContactInformation>(c => c.EmailAddress == "invalid-email")))
                                        .Returns(ValidationResult.Failure("Email address format is invalid"));

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

            _userIdentityServiceMock.Setup(x => x.GetUserUpn(It.IsAny<ClaimsPrincipal>()))
                                   .Throws(new InvalidOperationException("User is not authenticated"));

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

            _userIdentityServiceMock.Setup(x => x.GetUserUpn(It.IsAny<ClaimsPrincipal>()))
                                   .Throws(new InvalidOperationException("User UPN or email claim is required"));

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

        #region User Identity Integration Tests

        [Fact]
        public async Task CreateDomainRegistrationAsync_ThrowsException_UserIdentityServiceThrows()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = CreateTestDomain();
            var contactInfo = CreateTestContactInfo();

            _userIdentityServiceMock.Setup(x => x.GetUserUpn(It.IsAny<ClaimsPrincipal>()))
                                   .Throws(new InvalidOperationException("User is not authenticated"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateDomainRegistrationAsync(user, domain, contactInfo));

            Assert.Equal("User is not authenticated", exception.Message);
            _userIdentityServiceMock.Verify(x => x.GetUserUpn(user), Times.Once);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()), Times.Never);
        }

        [Fact]
        public async Task GetUserDomainRegistrationsAsync_ThrowsException_UserIdentityServiceThrows()
        {
            // Arrange
            var user = CreateTestUser();

            _userIdentityServiceMock.Setup(x => x.GetUserUpn(It.IsAny<ClaimsPrincipal>()))
                                   .Throws(new InvalidOperationException("User UPN or email claim is required"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetUserDomainRegistrationsAsync(user));

            Assert.Equal("User UPN or email claim is required", exception.Message);
            _userIdentityServiceMock.Verify(x => x.GetUserUpn(user), Times.Once);
            _repositoryMock.Verify(r => r.GetByUserAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetDomainRegistrationByIdAsync_ThrowsException_UserIdentityServiceThrows()
        {
            // Arrange
            var user = CreateTestUser();
            var registrationId = "test-id-123";

            _userIdentityServiceMock.Setup(x => x.GetUserUpn(It.IsAny<ClaimsPrincipal>()))
                                   .Throws(new InvalidOperationException("User is not authenticated"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetDomainRegistrationByIdAsync(user, registrationId));

            Assert.Equal("User is not authenticated", exception.Message);
            _userIdentityServiceMock.Verify(x => x.GetUserUpn(user), Times.Once);
            _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateDomainRegistrationStatusAsync_ThrowsException_UserIdentityServiceThrows()
        {
            // Arrange
            var user = CreateTestUser();
            var registrationId = "test-id-123";
            var status = DomainRegistrationStatus.Completed;

            _userIdentityServiceMock.Setup(x => x.GetUserUpn(It.IsAny<ClaimsPrincipal>()))
                                   .Throws(new InvalidOperationException("User is not authenticated"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateDomainRegistrationStatusAsync(user, registrationId, status));

            Assert.Equal("User is not authenticated", exception.Message);
            _userIdentityServiceMock.Verify(x => x.GetUserUpn(user), Times.Once);
            _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_UsesCorrectUpnFromIdentityService()
        {
            // Arrange
            var user = CreateTestUser();
            var domain = CreateTestDomain();
            var contactInfo = CreateTestContactInfo();
            var expectedUpn = "different@domain.com";

            _userIdentityServiceMock.Setup(x => x.GetUserUpn(It.IsAny<ClaimsPrincipal>()))
                                   .Returns(expectedUpn);

            var expectedRegistration = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = "test-id-123",
                Upn = expectedUpn,
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
            Assert.Equal(expectedUpn, result.Upn);
            _userIdentityServiceMock.Verify(x => x.GetUserUpn(user), Times.Once);
            _repositoryMock.Verify(r => r.CreateAsync(It.Is<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(
                dr => dr.Upn == expectedUpn)), Times.Once);
        }

        #endregion
    }
}