using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using System.Security.Claims;

namespace OnePageAuthor.Test
{
    public class GetAuthorsTests
    {
        private readonly Mock<ILogger<InkStainedWretch.Function.GetAuthors>> _loggerMock;
        private readonly Mock<IAuthorDataService> _authorDataServiceMock;
        private readonly Mock<IJwtValidationService> _jwtValidationServiceMock;
        private readonly InkStainedWretch.Function.GetAuthors _function;

        public GetAuthorsTests()
        {
            _loggerMock = new Mock<ILogger<InkStainedWretch.Function.GetAuthors>>();
            _authorDataServiceMock = new Mock<IAuthorDataService>();
            _jwtValidationServiceMock = new Mock<IJwtValidationService>();
            _function = new InkStainedWretch.Function.GetAuthors(_loggerMock.Object, _authorDataServiceMock.Object, _jwtValidationServiceMock.Object);
        }

        [Fact]
        public async Task Run_WithMissingAuthorizationHeader_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            
            var result = await _function.Run(request, "example", "com");
            
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Run_WithInvalidToken_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer invalid_token";
            
            _jwtValidationServiceMock.Setup(j => j.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((ClaimsPrincipal?)null);
            
            var result = await _function.Run(httpContext.Request, "example", "com");
            
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Run_WithValidTokenButInsufficientScope_ReturnsForbidden()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer valid_token";
            
            var claims = new List<Claim>
            {
                new Claim("scp", "SomeOtherScope")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
            
            _jwtValidationServiceMock.Setup(j => j.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(principal);
            
            var result = await _function.Run(httpContext.Request, "example", "com");
            
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbiddenResult.StatusCode);
        }

        [Fact]
        public async Task Run_WithValidTokenAndScope_WhenNoAuthorsFound_ReturnsNotFound()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer valid_token";
            
            var claims = new List<Claim>
            {
                new Claim("scp", "Author.Read")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
            
            _jwtValidationServiceMock.Setup(j => j.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(principal);
            
            _authorDataServiceMock.Setup(s => s.GetAuthorsByDomainAsync("com", "example"))
                .ReturnsAsync(new List<AuthorApiResponse>());
            
            var result = await _function.Run(httpContext.Request, "example", "com");
            
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task Run_WithValidTokenAndScope_WhenAuthorsFound_ReturnsOkWithAuthors()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer valid_token";
            
            var claims = new List<Claim>
            {
                new Claim("scp", "Author.Read")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
            
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
            
            _jwtValidationServiceMock.Setup(j => j.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(principal);
            
            _authorDataServiceMock.Setup(s => s.GetAuthorsByDomainAsync("com", "example"))
                .ReturnsAsync(authors);
            
            var result = await _function.Run(httpContext.Request, "example", "com");
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var returnedAuthors = Assert.IsType<List<AuthorApiResponse>>(okResult.Value);
            Assert.Single(returnedAuthors);
            Assert.Equal("Test Author", returnedAuthors[0].AuthorName);
        }
    }
}