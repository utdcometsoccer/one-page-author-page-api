using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.API;
using DomainRegistrationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration;
using DomainEntity = InkStainedWretch.OnePageAuthorAPI.Entities.Domain;
using ContactInformationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.ContactInformation;

namespace OnePageAuthor.Test
{
    public class GoogleDomainsServiceTests
    {
        [Fact]
        public void Constructor_ThrowsOnNullLogger()
        {
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["GOOGLE_CLOUD_PROJECT_ID"]).Returns("test-project");
            config.Setup(c => c["GOOGLE_DOMAINS_LOCATION"]).Returns("global");

            Assert.Throws<ArgumentNullException>(() => new GoogleDomainsService(null!, config.Object));
        }

        [Fact]
        public void Constructor_ThrowsOnNullConfiguration()
        {
            var logger = new Mock<ILogger<GoogleDomainsService>>();

            Assert.Throws<ArgumentNullException>(() => new GoogleDomainsService(logger.Object, null!));
        }

        [Fact]
        public void Constructor_ThrowsOnMissingProjectId()
        {
            var logger = new Mock<ILogger<GoogleDomainsService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["GOOGLE_CLOUD_PROJECT_ID"]).Returns((string?)null);
            config.Setup(c => c["GOOGLE_DOMAINS_LOCATION"]).Returns("global");

            Assert.Throws<InvalidOperationException>(() => new GoogleDomainsService(logger.Object, config.Object));
        }

        [Fact]
        public void Constructor_UsesDefaultLocation()
        {
            var logger = new Mock<ILogger<GoogleDomainsService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["GOOGLE_CLOUD_PROJECT_ID"]).Returns("test-project");
            config.Setup(c => c["GOOGLE_DOMAINS_LOCATION"]).Returns((string?)null);

            // Should not throw - default location should be used
            var service = new GoogleDomainsService(logger.Object, config.Object);
            Assert.NotNull(service);
        }

        [Fact]
        public async Task RegisterDomainAsync_ReturnsFalseForNullDomainRegistration()
        {
            var logger = new Mock<ILogger<GoogleDomainsService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["GOOGLE_CLOUD_PROJECT_ID"]).Returns("test-project");
            config.Setup(c => c["GOOGLE_DOMAINS_LOCATION"]).Returns("global");

            var service = new GoogleDomainsService(logger.Object, config.Object);
            var result = await service.RegisterDomainAsync(null!);

            Assert.False(result);
        }

        [Fact]
        public async Task RegisterDomainAsync_ReturnsFalseForNullDomain()
        {
            var logger = new Mock<ILogger<GoogleDomainsService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["GOOGLE_CLOUD_PROJECT_ID"]).Returns("test-project");
            config.Setup(c => c["GOOGLE_DOMAINS_LOCATION"]).Returns("global");

            var service = new GoogleDomainsService(logger.Object, config.Object);
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id",
                Upn = "test@example.com",
                Domain = null!,
                ContactInformation = new ContactInformationEntity()
            };

            var result = await service.RegisterDomainAsync(domainRegistration);

            Assert.False(result);
        }

        [Fact]
        public async Task RegisterDomainAsync_ReturnsFalseForEmptyDomainName()
        {
            var logger = new Mock<ILogger<GoogleDomainsService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["GOOGLE_CLOUD_PROJECT_ID"]).Returns("test-project");
            config.Setup(c => c["GOOGLE_DOMAINS_LOCATION"]).Returns("global");

            var service = new GoogleDomainsService(logger.Object, config.Object);
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

            var result = await service.RegisterDomainAsync(domainRegistration);

            Assert.False(result);
        }

        [Fact]
        public async Task IsDomainAvailableAsync_ReturnsFalseForEmptyDomainName()
        {
            var logger = new Mock<ILogger<GoogleDomainsService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["GOOGLE_CLOUD_PROJECT_ID"]).Returns("test-project");
            config.Setup(c => c["GOOGLE_DOMAINS_LOCATION"]).Returns("global");

            var service = new GoogleDomainsService(logger.Object, config.Object);
            var result = await service.IsDomainAvailableAsync("");

            Assert.False(result);
        }

        [Fact]
        public async Task IsDomainAvailableAsync_ReturnsFalseForNullDomainName()
        {
            var logger = new Mock<ILogger<GoogleDomainsService>>();
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["GOOGLE_CLOUD_PROJECT_ID"]).Returns("test-project");
            config.Setup(c => c["GOOGLE_DOMAINS_LOCATION"]).Returns("global");

            var service = new GoogleDomainsService(logger.Object, config.Object);
            var result = await service.IsDomainAvailableAsync(null!);

            Assert.False(result);
        }
    }
}
