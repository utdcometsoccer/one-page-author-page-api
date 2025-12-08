using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DomainRegistrationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration;

namespace OnePageAuthor.Test.Services
{
    /// <summary>
    /// Tests for subscription validation in domain registration.
    /// </summary>
    public class DomainRegistrationSubscriptionValidationTests
    {
        private readonly Mock<ILogger<DomainRegistrationService>> _mockLogger;
        private readonly Mock<IDomainRegistrationRepository> _mockRepository;
        private readonly Mock<IUserIdentityService> _mockUserIdentityService;
        private readonly Mock<IDomainValidationService> _mockDomainValidationService;
        private readonly Mock<IContactInformationValidationService> _mockContactValidationService;
        private readonly Mock<ISubscriptionValidationService> _mockSubscriptionValidationService;
        private readonly DomainRegistrationService _service;
        private readonly ClaimsPrincipal _testUser;

        public DomainRegistrationSubscriptionValidationTests()
        {
            _mockLogger = new Mock<ILogger<DomainRegistrationService>>();
            _mockRepository = new Mock<IDomainRegistrationRepository>();
            _mockUserIdentityService = new Mock<IUserIdentityService>();
            _mockDomainValidationService = new Mock<IDomainValidationService>();
            _mockContactValidationService = new Mock<IContactInformationValidationService>();
            _mockSubscriptionValidationService = new Mock<ISubscriptionValidationService>();
            
            _service = new DomainRegistrationService(
                _mockLogger.Object,
                _mockRepository.Object,
                _mockUserIdentityService.Object,
                _mockDomainValidationService.Object,
                _mockContactValidationService.Object,
                _mockSubscriptionValidationService.Object);

            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, "test@example.com")
            }));
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_WithValidSubscription_CreatesRegistration()
        {
            // Arrange
            var domain = CreateValidDomain();
            var contactInfo = CreateValidContactInformation();
            var expectedUpn = "test@example.com";
            var expectedRegistration = new DomainRegistrationEntity(expectedUpn, domain, contactInfo);

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockSubscriptionValidationService.Setup(x => x.HasValidSubscriptionAsync(_testUser, It.IsAny<string>())).ReturnsAsync(true);
            _mockDomainValidationService.Setup(x => x.ValidateDomain(domain)).Returns(ValidationResult.Success());
            _mockContactValidationService.Setup(x => x.ValidateContactInformation(contactInfo)).Returns(ValidationResult.Success());
            _mockRepository.Setup(x => x.CreateAsync(It.IsAny<DomainRegistrationEntity>())).ReturnsAsync(expectedRegistration);

            // Act
            var result = await _service.CreateDomainRegistrationAsync(_testUser, domain, contactInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUpn, result.Upn);
            
            // Verify subscription validation was called
            _mockSubscriptionValidationService.Verify(x => x.HasValidSubscriptionAsync(_testUser, It.IsAny<string>()), Times.Once);
            _mockRepository.Verify(x => x.CreateAsync(It.IsAny<DomainRegistrationEntity>()), Times.Once);
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_WithNoValidSubscription_ThrowsInvalidOperationException()
        {
            // Arrange
            var domain = CreateValidDomain();
            var contactInfo = CreateValidContactInformation();
            var expectedUpn = "test@example.com";

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockDomainValidationService.Setup(x => x.ValidateDomain(domain)).Returns(ValidationResult.Success());
            _mockSubscriptionValidationService.Setup(x => x.HasValidSubscriptionAsync(_testUser, domain.FullDomainName)).ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateDomainRegistrationAsync(_testUser, domain, contactInfo));
            
            Assert.Contains("valid subscription is required", exception.Message);
            Assert.Contains(domain.FullDomainName, exception.Message);
            
            // Verify domain validation was called first
            _mockDomainValidationService.Verify(x => x.ValidateDomain(domain), Times.Once);
            // Verify subscription validation was called
            _mockSubscriptionValidationService.Verify(x => x.HasValidSubscriptionAsync(_testUser, domain.FullDomainName), Times.Once);
            
            // Verify contact validation and repository were NOT called
            _mockContactValidationService.Verify(x => x.ValidateContactInformation(It.IsAny<ContactInformation>()), Times.Never);
            _mockRepository.Verify(x => x.CreateAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        [Fact]
        public async Task CreateDomainRegistrationAsync_DomainValidationCheckedBeforeSubscriptionValidation()
        {
            // Arrange
            var domain = CreateValidDomain();
            var contactInfo = CreateValidContactInformation();
            var expectedUpn = "test@example.com";

            _mockUserIdentityService.Setup(x => x.GetUserUpn(_testUser)).Returns(expectedUpn);
            _mockDomainValidationService.Setup(x => x.ValidateDomain(domain)).Returns(ValidationResult.Failure("Invalid domain"));
            _mockSubscriptionValidationService.Setup(x => x.HasValidSubscriptionAsync(_testUser, domain.FullDomainName)).ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateDomainRegistrationAsync(_testUser, domain, contactInfo));
            
            // Verify domain validation failed first
            Assert.Contains("Domain validation failed", exception.Message);
            
            // Verify subscription validation was NOT called because domain check failed first
            _mockSubscriptionValidationService.Verify(x => x.HasValidSubscriptionAsync(_testUser, It.IsAny<string>()), Times.Never);
        }

        private static Domain CreateValidDomain()
        {
            return new Domain
            {
                TopLevelDomain = "com",
                SecondLevelDomain = "example"
            };
        }

        private static ContactInformation CreateValidContactInformation()
        {
            return new ContactInformation
            {
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john@example.com",
                Address = "123 Main St",
                City = "Anytown",
                State = "CA",
                Country = "USA",
                ZipCode = "12345",
                TelephoneNumber = "+1-555-123-4567"
            };
        }
    }
}
