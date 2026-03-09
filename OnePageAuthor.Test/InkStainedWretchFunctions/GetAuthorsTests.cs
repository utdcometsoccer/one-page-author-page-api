using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretchFunctions;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
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
        private readonly Mock<IUserIdentityService> _mockUserIdentityService;
        private readonly Mock<IScopeValidationService> _mockScopeValidationService;
        private readonly GetAuthors _function;

        public GetAuthorsTests()
        {
            _mockLogger = new Mock<ILogger<GetAuthors>>();
            _mockAuthorDataService = new Mock<IAuthorDataService>();
            _mockJwtValidationService = new Mock<IJwtValidationService>();
            _mockUserIdentityService = new Mock<IUserIdentityService>();
            _mockScopeValidationService = new Mock<IScopeValidationService>();
            // Default: user has the required scope
            _mockScopeValidationService
                .Setup(s => s.HasRequiredScope(It.IsAny<ClaimsPrincipal>(), "Author.Read"))
                .Returns(true);
            _function = new GetAuthors(
                _mockLogger.Object,
                _mockAuthorDataService.Object,
                _mockJwtValidationService.Object,
                _mockUserIdentityService.Object,
                _mockScopeValidationService.Object);
        }

        private static HttpRequest CreateRequest(string? authToken = null, int? page = null)
        {
            var context = new DefaultHttpContext();
            if (authToken != null)
                context.Request.Headers["Authorization"] = $"Bearer {authToken}";
            if (page.HasValue)
                context.Request.QueryString = new QueryString($"?page={page}");
            return context.Request;
        }

        private static ClaimsPrincipal CreateUserWithScope(string userEmail = "test@example.com", string scope = "Author.Read")
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", userEmail),
                new Claim("scp", scope)
            }));
        }

        private static ClaimsPrincipal CreateUserWithoutScope(string userEmail = "test@example.com")
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", userEmail)
            }));
        }

        // --- Unauthenticated / bad token (no domain params) ---

        [Fact]
        public async Task Run_MissingAuthorizationHeader_NoDomainParams_ReturnsUnauthorized()
        {
            var req = CreateRequest();

            var result = await _function.Run(req, null, null);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Run_InvalidToken_NoDomainParams_ReturnsUnauthorized()
        {
            var req = CreateRequest(authToken: "invalid-token");
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("invalid-token"))
                .ReturnsAsync((ClaimsPrincipal?)null);

            var result = await _function.Run(req, null, null);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // --- Scenario 2: Domain parameters present — no auth required ---

        [Fact]
        public async Task Run_WithDomainParams_NoAuth_AuthorsNotFound_ReturnsNotFound()
        {
            var req = CreateRequest(); // no auth token
            _mockAuthorDataService
                .Setup(s => s.GetAuthorsByDomainAsync("com", "example"))
                .ReturnsAsync(new List<AuthorApiResponse>());

            var result = await _function.Run(req, "example", "com");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Run_WithDomainParams_NoAuth_AuthorsFound_ReturnsOk()
        {
            var req = CreateRequest(); // no auth token
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

            var result = await _function.Run(req, "example", "com");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(authors, okResult.Value);
        }

        [Fact]
        public async Task Run_WithDomainParams_WithAuth_AuthorsFound_ReturnsOk()
        {
            // Auth token present but should be ignored when domain params are provided
            var req = CreateRequest(authToken: "valid-token");
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

            var result = await _function.Run(req, "example", "com");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(authors, okResult.Value);
            // JWT validation should not be called for domain lookups
            _mockJwtValidationService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        // --- Scenario 1: Authenticated with Author.Read scope, no domain params — return all authors paged ---

        [Fact]
        public async Task Run_WithAuthorReadScope_NoDomainParams_ReturnsAllAuthorsPaged()
        {
            const string userEmail = "admin@example.com";
            var req = CreateRequest(authToken: "valid-token");
            var user = CreateUserWithScope(userEmail: userEmail);
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(user);
            var authors = new List<AuthorApiResponse>
            {
                new AuthorApiResponse { id = Guid.NewGuid().ToString(), AuthorName = "Author One" },
                new AuthorApiResponse { id = Guid.NewGuid().ToString(), AuthorName = "Author Two" }
            };
            _mockAuthorDataService
                .Setup(s => s.GetAllAuthorsPagedAsync(1, 10))
                .ReturnsAsync(authors);

            var result = await _function.Run(req, null, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(authors, okResult.Value);
            _mockAuthorDataService.Verify(s => s.GetAllAuthorsPagedAsync(1, 10), Times.Once);
        }

        [Fact]
        public async Task Run_WithAuthorReadScope_NoDomainParams_PageParam_ReturnsCorrectPage()
        {
            const string userEmail = "admin@example.com";
            var req = CreateRequest(authToken: "valid-token", page: 3);
            var user = CreateUserWithScope(userEmail: userEmail);
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(user);
            var authors = new List<AuthorApiResponse>
            {
                new AuthorApiResponse { id = Guid.NewGuid().ToString(), AuthorName = "Author On Page 3" }
            };
            _mockAuthorDataService
                .Setup(s => s.GetAllAuthorsPagedAsync(3, 10))
                .ReturnsAsync(authors);

            var result = await _function.Run(req, null, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(authors, okResult.Value);
            _mockAuthorDataService.Verify(s => s.GetAllAuthorsPagedAsync(3, 10), Times.Once);
        }

        [Fact]
        public async Task Run_WithAuthorReadScope_NoDomainParams_NoAuthors_ReturnsNotFound()
        {
            var req = CreateRequest(authToken: "valid-token");
            var user = CreateUserWithScope();
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(user);
            _mockAuthorDataService
                .Setup(s => s.GetAllAuthorsPagedAsync(1, 10))
                .ReturnsAsync(new List<AuthorApiResponse>());

            var result = await _function.Run(req, null, null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // --- Scenario 3: Authenticated without Author.Read scope, no domain params — return by email ---

        [Fact]
        public async Task Run_WithoutAuthorReadScope_NoDomainParams_ReturnsByEmail()
        {
            const string userEmail = "author@example.com";
            var req = CreateRequest(authToken: "valid-token");
            var user = CreateUserWithoutScope(userEmail: userEmail);
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(user);
            _mockScopeValidationService
                .Setup(s => s.HasRequiredScope(user, "Author.Read"))
                .Returns(false);
            _mockUserIdentityService
                .Setup(s => s.GetUserUpn(user))
                .Returns(userEmail);
            var authors = new List<AuthorApiResponse>
            {
                new AuthorApiResponse
                {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "Author One",
                    EmailAddress = userEmail
                }
            };
            _mockAuthorDataService
                .Setup(s => s.GetAuthorsByEmailAsync(userEmail))
                .ReturnsAsync(authors);

            var result = await _function.Run(req, null, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(authors, okResult.Value);
            _mockAuthorDataService.Verify(s => s.GetAuthorsByEmailAsync(userEmail), Times.Once);
        }

        [Fact]
        public async Task Run_WithoutAuthorReadScope_NoDomainParams_NoAuthors_ReturnsNotFound()
        {
            const string userEmail = "noauthor@example.com";
            var req = CreateRequest(authToken: "valid-token");
            var user = CreateUserWithoutScope(userEmail: userEmail);
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(user);
            _mockScopeValidationService
                .Setup(s => s.HasRequiredScope(user, "Author.Read"))
                .Returns(false);
            _mockUserIdentityService
                .Setup(s => s.GetUserUpn(user))
                .Returns(userEmail);
            _mockAuthorDataService
                .Setup(s => s.GetAuthorsByEmailAsync(userEmail))
                .ReturnsAsync(new List<AuthorApiResponse>());

            var result = await _function.Run(req, null, null);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Run_PartialDomainParams_WithAuthorReadScope_ReturnsAllAuthorsPaged()
        {
            // Only one route param provided — hasDomainParams is false, falls back to paged lookup
            const string userEmail = "admin@example.com";
            var req = CreateRequest(authToken: "valid-token");
            var user = CreateUserWithScope(userEmail: userEmail);
            _mockJwtValidationService
                .Setup(s => s.ValidateTokenAsync("valid-token"))
                .ReturnsAsync(user);
            _mockAuthorDataService
                .Setup(s => s.GetAllAuthorsPagedAsync(1, 10))
                .ReturnsAsync(new List<AuthorApiResponse>());

            var result = await _function.Run(req, "example", null);

            // Incomplete domain → authenticated path → Author.Read scope → paged → no authors → 404
            Assert.IsType<NotFoundObjectResult>(result);
            _mockAuthorDataService.Verify(s => s.GetAllAuthorsPagedAsync(1, 10), Times.Once);
        }
    }
}
