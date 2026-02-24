using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.API;
using DomainRegistrationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration;
using DomainEntity = InkStainedWretch.OnePageAuthorAPI.Entities.Domain;
using ContactInformationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.ContactInformation;

namespace OnePageAuthor.Test
{
    public class DnsZoneServiceTests
    {
        [Fact]
        public void Constructor_ThrowsOnNullLogger()
        {
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            Assert.Throws<ArgumentNullException>(() => new DnsZoneService(null!, config.Object));
        }

        [Fact]
        public void Constructor_ThrowsOnNullConfiguration()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();

            Assert.Throws<ArgumentNullException>(() => new DnsZoneService(logger.Object, null!));
        }

        [Fact]
        public void Constructor_LogsWarning_MissingSubscriptionId()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns((string?)null);
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            // Should NOT throw; service constructs with _isConfigured = false
            var service = new DnsZoneService(logger.Object, config.Object);
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_LogsWarning_MissingResourceGroup()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns((string?)null);

            // Should NOT throw; service constructs with _isConfigured = false
            var service = new DnsZoneService(logger.Object, config.Object);
            Assert.NotNull(service);
        }

        [Fact]
        public async Task EnsureDnsZoneExistsAsync_ReturnsFalseForNullDomainRegistration()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            var service = new DnsZoneService(logger.Object, config.Object);
            var result = await service.EnsureDnsZoneExistsAsync(null!);

            Assert.False(result);
        }

        [Fact]
        public async Task EnsureDnsZoneExistsAsync_ReturnsFalseForNullDomain()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            var service = new DnsZoneService(logger.Object, config.Object);
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id",
                Upn = "test@example.com",
                Domain = null!,
                ContactInformation = new ContactInformationEntity()
            };

            var result = await service.EnsureDnsZoneExistsAsync(domainRegistration);

            Assert.False(result);
        }

        [Fact]
        public async Task EnsureDnsZoneExistsAsync_ReturnsFalseForEmptyDomainName()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            var service = new DnsZoneService(logger.Object, config.Object);
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id",
                Upn = "test@example.com",
                Domain = new DomainEntity
                {
                    SecondLevelDomain = "",
                    TopLevelDomain = ""
                },
                ContactInformation = new ContactInformationEntity()
            };

            var result = await service.EnsureDnsZoneExistsAsync(domainRegistration);

            Assert.False(result);
        }

        [Fact]
        public async Task DnsZoneExistsAsync_ReturnsFalseForEmptyDomainName()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            var service = new DnsZoneService(logger.Object, config.Object);
            var result = await service.DnsZoneExistsAsync("");

            Assert.False(result);
        }

        [Fact]
        public async Task DnsZoneExistsAsync_ReturnsFalseForNullDomainName()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            var service = new DnsZoneService(logger.Object, config.Object);
            var result = await service.DnsZoneExistsAsync(null!);

            Assert.False(result);
        }

        [Fact]
        public async Task GetNameServersAsync_ReturnsNullForEmptyDomainName()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            var service = new DnsZoneService(logger.Object, config.Object);
            var result = await service.GetNameServersAsync("");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetNameServersAsync_ReturnsNullForNullDomainName()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            var service = new DnsZoneService(logger.Object, config.Object);
            var result = await service.GetNameServersAsync(null!);

            Assert.Null(result);
        }

        [Fact]
        public async Task EnsureDnsZoneExistsAsync_ReturnsFalse_WhenNotConfigured()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns((string?)null);
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns((string?)null);

            var service = new DnsZoneService(logger.Object, config.Object);
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id",
                Upn = "test@example.com",
                Domain = new DomainEntity { SecondLevelDomain = "example", TopLevelDomain = "com" },
                ContactInformation = new ContactInformationEntity()
            };

            var result = await service.EnsureDnsZoneExistsAsync(domainRegistration);

            Assert.False(result);
        }

        [Fact]
        public async Task DnsZoneExistsAsync_ReturnsFalse_WhenNotConfigured()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns((string?)null);
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns((string?)null);

            var service = new DnsZoneService(logger.Object, config.Object);
            var result = await service.DnsZoneExistsAsync("example.com");

            Assert.False(result);
        }

        [Fact]
        public async Task GetNameServersAsync_ReturnsNull_WhenNotConfigured()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns((string?)null);
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns((string?)null);

            var service = new DnsZoneService(logger.Object, config.Object);
            var result = await service.GetNameServersAsync("example.com");

            Assert.Null(result);
        }
    }
}
