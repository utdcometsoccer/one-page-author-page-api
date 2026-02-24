using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretchFunctions;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;
using System.Security.Claims;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    /// <summary>
    /// Unit tests for GetAuthors Azure Function
    /// </summary>
    public class GetAuthorsTests
    {
        private readonly Mock<ILogger<GetAuthors>> _mockLogger;
        private readonly Mock<IAuthorDataService> _mockAuthorDataService;
        private readonly Mock<IJwtValidationService> _mockJwtValidationService;
        private readonly GetAuthors _function;

        public GetAuthorsTests()
        {
            _mockLogger = new Mock<ILogger<GetAuthors>>();
            _mockAuthorDataService = new Mock<IAuthorDataService>();
            _mockJwtValidationService = new Mock<IJwtValidationService>();
            _function = new GetAuthors(
                _mockLogger.Object,
                _mockAuthorDataService.Object,
                _mockJwtValidationService.Object);
        }

        private static HttpRequest CreateRequest(string? sld = null, string? tld = null, string? authToken = null)
        {
            var context = new DefaultHttpContext();
            var queryString = new List<string>();
            if (sld != null) queryString.Add($"secondLevelDomain={sld}");
            if (tld != null) queryString.Add($"topLevelDomain={tld}");
            if (queryString.Count > 0)
                context.Request.QueryString = new QueryString("?" + string.Join("&", queryString));
            if (authToken != null)
                context.Request.Headers["Authorization"] = $"Bearer {authToken}";
            return context.Request;
        }

        private static ClaimsPrincipal CreateUserWithScope(string scope = "Author.Read")
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", "test@example.com"),
                new Claim("scp", scope)
            }));
        }

        [Fact]
        public async Task Run_MissingBothQueryParams_ReturnsBadRequest()
        {
            var req = CreateRequest();

            var result = await _function.Run(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task Run_MissingTopLevelDomain_ReturnsBadRequest()
        {
            var req = CreateRequest(sld: "example");

            var result = await _function.Run(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_MissingSecondLevelDomain_ReturnsBadRequest()
        {
            var req = CreateRequest(tld: "com");

            var result = await _function.Run(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_MissingAuthorizationHeader_ReturnsUnauthorized()
        {
            var req = CreateRequest(sld: "example", tld: "com");

            var result = await _function.Run(req);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Run_InvalidToken_ReturnsUnauthorized()
        {
            var req = CreateRequest(sld: "example", tld: "com", authToken: "invalid-token");
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("invalid-token"))
                .ReturnsAsync((ClaimsPrincipal?)null);

            var result = await _function.Run(req);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Run_ValidTokenNoScope_ReturnsForbidden()
        {
            var req = CreateRequest(sld: "example", tld: "com", authToken: "valid-token");
            var userWithoutScope = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", "test@example.com")
            }));
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(userWithoutScope);

            var result = await _function.Run(req);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        [Fact]
        public async Task Run_AuthorsNotFound_ReturnsNotFound()
        {
            var req = CreateRequest(sld: "example", tld: "com", authToken: "valid-token");
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(CreateUserWithScope());
            _mockAuthorDataService
                .Setup(s => s.GetAuthorsByDomainAsync("com", "example"))
                .ReturnsAsync(new List<AuthorApiResponse>());

            var result = await _function.Run(req);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Run_AuthorsFound_ReturnsOk()
        {
            var req = CreateRequest(sld: "example", tld: "com", authToken: "valid-token");
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(CreateUserWithScope());
            var authors = new List<AuthorApiResponse>
            {
                new AuthorApiResponse
                {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "Test Author",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example"
                }
            };
            _mockAuthorDataService
                .Setup(s => s.GetAuthorsByDomainAsync("com", "example"))
                .ReturnsAsync(authors);

            var result = await _function.Run(req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(authors, okResult.Value);
        }
    }
}
