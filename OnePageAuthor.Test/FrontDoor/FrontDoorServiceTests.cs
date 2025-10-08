using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test.FrontDoor
{
    /// <summary>
    /// Unit tests for FrontDoorService.
    /// Note: These tests validate input validation and basic logic.
    /// Integration tests with actual Azure resources are not included.
    /// </summary>
    public class FrontDoorServiceTests
    {
        private readonly Mock<ILogger<FrontDoorService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public FrontDoorServiceTests()
        {
            _loggerMock = new Mock<ILogger<FrontDoorService>>();
            _configurationMock = new Mock<IConfiguration>();
        }

        private FrontDoorService CreateServiceWithConfig(
            string? subscriptionId = "test-subscription-id",
            string? resourceGroupName = "test-resource-group",
            string? frontDoorProfileName = "test-frontdoor-profile")
        {
            _configurationMock.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns(subscriptionId);
            _configurationMock.Setup(c => c["AZURE_RESOURCE_GROUP_NAME"]).Returns(resourceGroupName);
            _configurationMock.Setup(c => c["AZURE_FRONTDOOR_PROFILE_NAME"]).Returns(frontDoorProfileName);

            return new FrontDoorService(_loggerMock.Object, _configurationMock.Object);
        }

        private static InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration CreateTestDomainRegistration(string secondLevel = "example", string topLevel = "com")
        {
            return new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = "test-id-123",
                Upn = "test@example.com",
                Domain = new Domain
                {
                    SecondLevelDomain = secondLevel,
                    TopLevelDomain = topLevel
                },
                ContactInformation = new ContactInformation
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
                },
                Status = DomainRegistrationStatus.Pending
            };
        }

        [Fact]
        public void Constructor_ThrowsException_NullLogger()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new FrontDoorService(null!, _configurationMock.Object));
        }

        [Fact]
        public void Constructor_ThrowsException_NullConfiguration()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new FrontDoorService(_loggerMock.Object, null!));
        }

        [Fact]
        public void Constructor_ThrowsException_MissingSubscriptionId()
        {
            // Arrange & Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                CreateServiceWithConfig(subscriptionId: null));
            
            Assert.Contains("AZURE_SUBSCRIPTION_ID", ex.Message);
        }

        [Fact]
        public void Constructor_ThrowsException_MissingResourceGroupName()
        {
            // Arrange & Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                CreateServiceWithConfig(resourceGroupName: null));
            
            Assert.Contains("AZURE_RESOURCE_GROUP_NAME", ex.Message);
        }

        [Fact]
        public void Constructor_ThrowsException_MissingFrontDoorProfileName()
        {
            // Arrange & Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                CreateServiceWithConfig(frontDoorProfileName: null));
            
            Assert.Contains("AZURE_FRONTDOOR_PROFILE_NAME", ex.Message);
        }

        [Fact]
        public void Constructor_Success_AllConfigurationPresent()
        {
            // Arrange & Act
            var service = CreateServiceWithConfig();

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task DomainExistsAsync_ThrowsException_NullDomainName()
        {
            // Arrange
            var service = CreateServiceWithConfig();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.DomainExistsAsync(null!));
        }

        [Fact]
        public async Task DomainExistsAsync_ThrowsException_EmptyDomainName()
        {
            // Arrange
            var service = CreateServiceWithConfig();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.DomainExistsAsync(string.Empty));
        }

        [Fact]
        public async Task DomainExistsAsync_ThrowsException_WhitespaceDomainName()
        {
            // Arrange
            var service = CreateServiceWithConfig();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.DomainExistsAsync("   "));
        }

        [Fact]
        public async Task AddDomainToFrontDoorAsync_ThrowsException_NullDomainRegistration()
        {
            // Arrange
            var service = CreateServiceWithConfig();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.AddDomainToFrontDoorAsync(null!));
        }

        [Fact]
        public async Task AddDomainToFrontDoorAsync_ThrowsException_NullDomain()
        {
            // Arrange
            var service = CreateServiceWithConfig();
            var registration = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = "test-id",
                Upn = "test@example.com",
                Domain = null!,
                ContactInformation = new ContactInformation()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.AddDomainToFrontDoorAsync(registration));
        }

        [Fact]
        public void DomainRegistration_FullDomainName_IsCorrect()
        {
            // Arrange
            var registration = CreateTestDomainRegistration("myblog", "com");

            // Act
            var fullDomain = registration.Domain.FullDomainName;

            // Assert
            Assert.Equal("myblog.com", fullDomain);
        }

        [Fact]
        public void DomainRegistration_FullDomainName_WithDifferentTLD()
        {
            // Arrange
            var registration = CreateTestDomainRegistration("example", "org");

            // Act
            var fullDomain = registration.Domain.FullDomainName;

            // Assert
            Assert.Equal("example.org", fullDomain);
        }
    }
}
