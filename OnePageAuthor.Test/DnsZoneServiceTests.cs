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
        public void Constructor_ThrowsOnMissingSubscriptionId()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns((string?)null);
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns("test-rg");

            Assert.Throws<InvalidOperationException>(() => new DnsZoneService(logger.Object, config.Object));
        }

        [Fact]
        public void Constructor_ThrowsOnMissingResourceGroup()
        {
            var logger = new Mock<ILogger<DnsZoneService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AZURE_SUBSCRIPTION_ID"]).Returns("test-sub-id");
            config.Setup(c => c["AZURE_DNS_RESOURCE_GROUP"]).Returns((string?)null);

            Assert.Throws<InvalidOperationException>(() => new DnsZoneService(logger.Object, config.Object));
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
    }
}
