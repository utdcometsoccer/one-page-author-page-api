using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace OnePageAuthor.Test.Services
{
    /// <summary>
    /// Unit tests for the refactored DomainRegistrationService with validation services.
    /// </summary>
    public class RefactoredDomainRegistrationServiceTests
    {
        private readonly Mock<ILogger<DomainRegistrationService>> _mockLogger;
        private readonly Mock<IDomainRegistrationRepository> _mockRepository;
        private readonly Mock<IUserIdentityService> _mockUserIdentityService;
        private readonly Mock<IDomainValidationService> _mockDomainValidationService;
        private readonly Mock<IContactInformationValidationService> _mockContactValidationService;
        private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
        private readonly Mock<InkStainedWretch.OnePageAuthorLib.API.Stripe.IListSubscriptions> _mockListSubscriptions;
        private readonly DomainRegistrationService _service;
        private readonly ClaimsPrincipal _testUser;

        public RefactoredDomainRegistrationServiceTests()
        {
            _mockLogger = new Mock<ILogger<DomainRegistrationService>>();
            _mockRepository = new Mock<IDomainRegistrationRepository>();
            _mockUserIdentityService = new Mock<IUserIdentityService>();
            _mockDomainValidationService = new Mock<IDomainValidationService>();
            _mockContactValidationService = new Mock<IContactInformationValidationService>();
            _mockUserProfileRepository = new Mock<IUserProfileRepository>();
            _mockListSubscriptions = new Mock<InkStainedWretch.OnePageAuthorLib.API.Stripe.IListSubscriptions>();
            
            _service = new DomainRegistrationService(
                _mockLogger.Object,
                _mockRepository.Object,
                _mockUserIdentityService.Object,
                _mockDomainValidationService.Object,
                _mockContactValidationService.Object,
                _mockUserProfileRepository.Object,
                _mockListSubscriptions.Object);

            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, "test@example.com")
            }));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationService(
                null!,
                _mockRepository.Object,
                _mockUserIdentityService.Object,
                _mockDomainValidationService.Object,
                _mockContactValidationService.Object,
                _mockUserProfileRepository.Object,
                _mockListSubscriptions.Object));
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationService(
                _mockLogger.Object,
                null!,
                _mockUserIdentityService.Object,
                _mockDomainValidationService.Object,
                _mockContactValidationService.Object,
                _mockUserProfileRepository.Object,
                _mockListSubscriptions.Object));
        }

        [Fact]
        public void Constructor_WithNullUserIdentityService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationService(
                _mockLogger.Object,
                _mockRepository.Object,
                null!,
                _mockDomainValidationService.Object,
                _mockContactValidationService.Object,
                _mockUserProfileRepository.Object,
                _mockListSubscriptions.Object));
        }

        [Fact]
        public void Constructor_WithNullDomainValidationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationService(
                _mockLogger.Object,
                _mockRepository.Object,
                _mockUserIdentityService.Object,
                null!,
                _mockContactValidationService.Object,
                _mockUserProfileRepository.Object,
                _mockListSubscriptions.Object));
        }

        [Fact]
        public void Constructor_WithNullContactValidationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationService(
                _mockLogger.Object,
                _mockRepository.Object,
                _mockUserIdentityService.Object,
                _mockDomainValidationService.Object,
                null!,
                _mockUserProfileRepository.Object,
                _mockListSubscriptions.Object));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_WithValidInputs_ReturnsCreatedRegistration()
        {
            // Arrange
            var domain = CreateValidDomain();
            var contactInfo = CreateValidContactInformation();
            var expectedUpn = "test@example.com";
            var expectedRegistration = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration(expectedUpn, domain, contactInfo);

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockDomainValidationService.Setup(x => x.ValidateDomain(domain)).Returns(ValidationResult.Success());
            _mockContactValidationService.Setup(x => x.ValidateContactInformation(contactInfo)).Returns(ValidationResult.Success());
            _mockRepository.Setup(x => x.CreateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>())).ReturnsAsync(expectedRegistration);

            // Act
            var result = await _service.CreateDomainRegistrationAsync(_testUser, domain, contactInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUpn, result.Upn);
            Assert.Equal(domain.FullDomainName, result.Domain.FullDomainName);
            
            _mockDomainValidationService.Verify(x => x.ValidateDomain(domain), Times.Once);
            _mockContactValidationService.Verify(x => x.ValidateContactInformation(contactInfo), Times.Once);
            _mockRepository.Verify(x => x.CreateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()), Times.Once);
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_WithInvalidDomain_ThrowsArgumentException()
        {
            // Arrange
            var domain = CreateValidDomain();
            var contactInfo = CreateValidContactInformation();
            var expectedUpn = "test@example.com";
            var validationResult = ValidationResult.Failure("Invalid domain");

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockDomainValidationService.Setup(x => x.ValidateDomain(domain)).Returns(validationResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(_testUser, domain, contactInfo));
            
            Assert.Contains("Domain validation failed", exception.Message);
            Assert.Contains("Invalid domain", exception.Message);
            
            _mockDomainValidationService.Verify(x => x.ValidateDomain(domain), Times.Once);
            _mockContactValidationService.Verify(x => x.ValidateContactInformation(It.IsAny<ContactInformation>()), Times.Never);
            _mockRepository.Verify(x => x.CreateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()), Times.Never);
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_WithInvalidContactInfo_ThrowsArgumentException()
        {
            // Arrange
            var domain = CreateValidDomain();
            var contactInfo = CreateValidContactInformation();
            var expectedUpn = "test@example.com";
            var validationResult = ValidationResult.Failure("Invalid contact info");

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockDomainValidationService.Setup(x => x.ValidateDomain(domain)).Returns(ValidationResult.Success());
            _mockContactValidationService.Setup(x => x.ValidateContactInformation(contactInfo)).Returns(validationResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(_testUser, domain, contactInfo));
            
            Assert.Contains("Contact information validation failed", exception.Message);
            Assert.Contains("Invalid contact info", exception.Message);
            
            _mockDomainValidationService.Verify(x => x.ValidateDomain(domain), Times.Once);
            _mockContactValidationService.Verify(x => x.ValidateContactInformation(contactInfo), Times.Once);
            _mockRepository.Verify(x => x.CreateAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>()), Times.Never);
        }

        [Fact]
        public async Task GetUserDomainRegistrationsAsync_WithValidUser_ReturnsRegistrations()
        {
            // Arrange
            var expectedUpn = "test@example.com";
            var expectedRegistrations = new List<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>
            {
                new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration(expectedUpn, CreateValidDomain(), CreateValidContactInformation())
            };

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockRepository.Setup(x => x.GetByUserAsync(expectedUpn)).ReturnsAsync(expectedRegistrations);

            // Act
            var result = await _service.GetUserDomainRegistrationsAsync(_testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedUpn, result.First().Upn);
            
            _mockUserIdentityService.Verify(x => x.GetUserUpn(_testUser), Times.Once);
            _mockRepository.Verify(x => x.GetByUserAsync(expectedUpn), Times.Once);
        }

        [Fact]
        public async Task GetDomainRegistrationByIdAsync_WithValidUserAndId_ReturnsRegistration()
        {
            // Arrange
            var expectedUpn = "test@example.com";
            var registrationId = "test-id";
            var expectedRegistration = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration(expectedUpn, CreateValidDomain(), CreateValidContactInformation());

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockRepository.Setup(x => x.GetByIdAsync(registrationId, expectedUpn)).ReturnsAsync(expectedRegistration);

            // Act
            var result = await _service.GetDomainRegistrationByIdAsync(_testUser, registrationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUpn, result.Upn);
            
            _mockUserIdentityService.Verify(x => x.GetUserUpn(_testUser), Times.Once);
            _mockRepository.Verify(x => x.GetByIdAsync(registrationId, expectedUpn), Times.Once);
        }

        [Fact]
        public async Task GetDomainRegistrationByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var expectedUpn = "test@example.com";
            var registrationId = "non-existent-id";

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockRepository.Setup(x => x.GetByIdAsync(registrationId, expectedUpn)).ReturnsAsync((InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration?)null);

            // Act
            var result = await _service.GetDomainRegistrationByIdAsync(_testUser, registrationId);

            // Assert
            Assert.Null(result);
            
            _mockUserIdentityService.Verify(x => x.GetUserUpn(_testUser), Times.Once);
            _mockRepository.Verify(x => x.GetByIdAsync(registrationId, expectedUpn), Times.Once);
        }

        private static Domain CreateValidDomain()
        {
            return new Domain
            {
                SecondLevelDomain = "example",
                TopLevelDomain = "com"
            };
        }

        private static ContactInformation CreateValidContactInformation()
        {
            return new ContactInformation
            {
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john.doe@example.com",
                Address = "123 Main Street",
                City = "Anytown",
                State = "CA",
                Country = "US",
                ZipCode = "12345",
                TelephoneNumber = "1234567890"
            };
        }
    }
}