using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace OnePageAuthor.Test
{
    public class GetAuthorsTests
    {
        private readonly Mock<ILogger<InkStainedWretchFunctions.GetAuthors>> _loggerMock;
        private readonly Mock<IAuthorDataService> _authorDataServiceMock;
        private readonly InkStainedWretchFunctions.GetAuthors _function;

        public GetAuthorsTests()
        {
            _loggerMock = new Mock<ILogger<InkStainedWretchFunctions.GetAuthors>>();
            _authorDataServiceMock = new Mock<IAuthorDataService>();
            _function = new InkStainedWretchFunctions.GetAuthors(_loggerMock.Object, _authorDataServiceMock.Object);
        }

        [Fact]
        public async Task Run_WhenNoAuthorsFound_ReturnsNotFound()
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            
            _authorDataServiceMock.Setup(s => s.GetAuthorsByDomainAsync("com", "example"))
                .ReturnsAsync(new List<AuthorApiResponse>());
            
            var result = await _function.Run(request, "example", "com");
            
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task Run_WhenAuthorsFound_ReturnsOkWithAuthors()
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            
            var authors = new List<AuthorApiResponse>
            {
                new AuthorApiResponse
                {
                    id = "1",
                    AuthorName = "Test Author",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "US",
                    EmailAddress = "test@example.com"
                }
            };
            
            _authorDataServiceMock.Setup(s => s.GetAuthorsByDomainAsync("com", "example"))
                .ReturnsAsync(authors);
            
            var result = await _function.Run(request, "example", "com");
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var returnedAuthors = Assert.IsType<List<AuthorApiResponse>>(okResult.Value);
            Assert.Single(returnedAuthors);
            Assert.Equal("Test Author", returnedAuthors[0].AuthorName);
        }
    }
}