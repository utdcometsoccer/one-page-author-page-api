using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.Function;
using DomainRegistrationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace OnePageAuthor.Test.FunctionApp
{
    public class GetSitemapTests
    {
        private readonly Mock<ILogger<GetSitemap>> _loggerMock;
        private readonly Mock<IDomainRegistrationRepository> _repositoryMock;
        private readonly GetSitemap _function;

        public GetSitemapTests()
        {
            _loggerMock = new Mock<ILogger<GetSitemap>>();
            _repositoryMock = new Mock<IDomainRegistrationRepository>();
            _function = new GetSitemap(_loggerMock.Object, _repositoryMock.Object);
        }

        private static DomainRegistrationEntity CreateTestDomainRegistration()
        {
            return new DomainRegistrationEntity
            {
                id = "test-id",
                Upn = "test@example.com",
                Domain = new Domain
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example"
                },
                ContactInformation = new ContactInformation
                {
                    FirstName = "John",
                    LastName = "Doe",
                    EmailAddress = "john@example.com"
                },
                LastUpdatedAt = new DateTime(2023, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = DomainRegistrationStatus.Completed
            };
        }

        [Fact]
        public async Task Run_Success_ReturnsXmlSitemap()
        {
            // Arrange
            var topLevelDomain = "com";
            var secondLevelDomain = "example";
            var domainRegistration = CreateTestDomainRegistration();

            _repositoryMock.Setup(r => r.GetByDomainAsync(topLevelDomain, secondLevelDomain))
                          .ReturnsAsync(domainRegistration);

            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _function.Run(mockRequest.Object, topLevelDomain, secondLevelDomain);

            // Assert
            Assert.IsType<ContentResult>(result);
            var contentResult = result as ContentResult;
            Assert.NotNull(contentResult);
            Assert.Equal("application/xml", contentResult.ContentType);
            Assert.Equal(200, contentResult.StatusCode);
            Assert.Contains("https://example.com", contentResult.Content);
            Assert.Contains("<lastmod>2023-12-01</lastmod>", contentResult.Content);
            Assert.Contains("http://www.sitemaps.org/schemas/sitemap/0.9", contentResult.Content);
            Assert.Contains("<changefreq>weekly</changefreq>", contentResult.Content);
            Assert.Contains("<priority>1.0</priority>", contentResult.Content);
        }

        [Fact]
        public async Task Run_DomainNotFound_ReturnsNotFound()
        {
            // Arrange
            var topLevelDomain = "com";
            var secondLevelDomain = "nonexistent";

            _repositoryMock.Setup(r => r.GetByDomainAsync(topLevelDomain, secondLevelDomain))
                          .ReturnsAsync((DomainRegistrationEntity?)null);

            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _function.Run(mockRequest.Object, topLevelDomain, secondLevelDomain);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            var notFoundResult = result as NotFoundObjectResult;
            Assert.NotNull(notFoundResult);
            Assert.Contains("Domain registration not found", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task Run_RepositoryThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var topLevelDomain = "com";
            var secondLevelDomain = "example";

            _repositoryMock.Setup(r => r.GetByDomainAsync(topLevelDomain, secondLevelDomain))
                          .ThrowsAsync(new Exception("Database error"));

            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _function.Run(mockRequest.Object, topLevelDomain, secondLevelDomain);

            // Assert
            Assert.IsType<StatusCodeResult>(result);
            var statusCodeResult = result as StatusCodeResult;
            Assert.NotNull(statusCodeResult);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Run_ValidatesXmlStructure()
        {
            // Arrange
            var topLevelDomain = "org";
            var secondLevelDomain = "testsite";
            var domainRegistration = CreateTestDomainRegistration();
            domainRegistration.Domain.TopLevelDomain = "org";
            domainRegistration.Domain.SecondLevelDomain = "testsite";
            domainRegistration.LastUpdatedAt = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);

            _repositoryMock.Setup(r => r.GetByDomainAsync(topLevelDomain, secondLevelDomain))
                          .ReturnsAsync(domainRegistration);

            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _function.Run(mockRequest.Object, topLevelDomain, secondLevelDomain);

            // Assert
            Assert.IsType<ContentResult>(result);
            var contentResult = result as ContentResult;
            Assert.NotNull(contentResult);
            
            // Verify XML declaration
            Assert.Contains("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", contentResult.Content);
            
            // Verify correct domain URL
            Assert.Contains("https://testsite.org", contentResult.Content);
            
            // Verify lastmod format (should be yyyy-MM-dd)
            Assert.Contains("<lastmod>2024-06-15</lastmod>", contentResult.Content);
            
            // Verify all required sitemap elements
            Assert.Contains("<url>", contentResult.Content);
            Assert.Contains("<loc>", contentResult.Content);
            Assert.Contains("</url>", contentResult.Content);
            Assert.Contains("</urlset>", contentResult.Content);
        }
    }
}
