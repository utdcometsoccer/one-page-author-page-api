using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using InkStainedWretch.Function;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using ApiBook = InkStainedWretch.OnePageAuthorAPI.API.Book;
using ApiArticle = InkStainedWretch.OnePageAuthorAPI.API.Article;

namespace OnePageAuthor.Test.FunctionApp
{
    /// <summary>
    /// Tests for <see cref="GetAuthorData"/> covering the homepage hero experiment
    /// acceptance criteria (experiment assignment, featured book propagation, defaults).
    /// </summary>
    public class GetAuthorDataTests
    {
        private readonly Mock<ILogger<GetAuthorData>> _loggerMock;
        private readonly Mock<IAuthorDataService> _authorDataServiceMock;
        private readonly Mock<IExperimentService> _experimentServiceMock;

        private static readonly AuthorResponse BaseResponse = new AuthorResponse
        {
            Name = "Jane Author",
            Welcome = "Welcome!",
            AboutMe = "About me.",
            Headshot = "https://example.com/headshot.jpg",
            Books = new List<ApiBook>(),
            Copyright = "© 2025",
            Social = new List<SocialLink>(),
            Email = "jane@example.com",
            Articles = new List<ApiArticle>()
        };

        private static readonly AuthorResponse ResponseWithFeaturedBook = new AuthorResponse
        {
            Name = "Jane Author",
            Welcome = "Welcome!",
            AboutMe = "About me.",
            Headshot = "https://example.com/headshot.jpg",
            Books = new List<ApiBook>(),
            Copyright = "© 2025",
            Social = new List<SocialLink>(),
            Email = "jane@example.com",
            Articles = new List<ApiArticle>(),
            FeaturedBook = new FeaturedBookDto
            {
                Title = "The Featured Novel",
                AuthorName = "Jane Author",
                Description = "A great book.",
                CoverImageUrl = "https://covers.example.com/featured.jpg",
                CoverImageAlt = "The Featured Novel",
                PrimaryCtaLabel = "Buy Now",
                PrimaryCtaUrl = "https://buy.example.com/featured"
            }
        };

        public GetAuthorDataTests()
        {
            _loggerMock = new Mock<ILogger<GetAuthorData>>();
            _authorDataServiceMock = new Mock<IAuthorDataService>();
            _experimentServiceMock = new Mock<IExperimentService>();
        }

        private static Mock<HttpRequest> CreateMockRequest(string? sessionId = null)
        {
            var mockRequest = new Mock<HttpRequest>();
            var headers = new HeaderDictionary();
            if (sessionId != null)
                headers["X-Session-Id"] = new StringValues(sessionId);
            mockRequest.Setup(r => r.Headers).Returns(headers);
            return mockRequest;
        }

        private GetExperimentsResponse BuildExperimentResponse(string variant) =>
            new GetExperimentsResponse
            {
                SessionId = "test-session",
                Experiments = new List<AssignedExperiment>
                {
                    new AssignedExperiment
                    {
                        Id = "homepage-hero-exp",
                        Name = "homepage-hero",
                        Variant = variant,
                        Config = new Dictionary<string, object>()
                    }
                }
            };

        // -------------------------------------------------------------------------
        // Test: Author not found → 404
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_AuthorNotFound_Returns404()
        {
            // Arrange
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync("com", "notfound", "en", null))
                .ReturnsAsync((AuthorResponse?)null);

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object);
            var mockRequest = CreateMockRequest();

            // Act
            var result = await function.Run(mockRequest.Object, "com", "notfound", "en", null);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // -------------------------------------------------------------------------
        // Test: No featured book — experiment defaults to control
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_NoFeaturedBook_NoExperimentService_ExperimentIsControl()
        {
            // Arrange — no experiment service injected (null)
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync("com", "example", "en", null))
                .ReturnsAsync(BaseResponse);

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object, experimentService: null);
            var mockRequest = CreateMockRequest();

            // Act
            var result = await function.Run(mockRequest.Object, "com", "example", "en", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthorResponse>(okResult.Value);
            Assert.Null(response.FeaturedBook);
            Assert.NotNull(response.Experiment);
            Assert.Equal("control", response.Experiment.HomepageHeroVariant);
        }

        // -------------------------------------------------------------------------
        // Test: Featured book + control assignment
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_FeaturedBook_ControlAssignment_ReturnsFeaturedBookAndControl()
        {
            // Arrange
            var responseClone = new AuthorResponse
            {
                Name = ResponseWithFeaturedBook.Name,
                Books = ResponseWithFeaturedBook.Books,
                FeaturedBook = ResponseWithFeaturedBook.FeaturedBook
            };
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync("com", "example", "en", null))
                .ReturnsAsync(responseClone);
            _experimentServiceMock
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ReturnsAsync(BuildExperimentResponse("control"));

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object, _experimentServiceMock.Object);
            var mockRequest = CreateMockRequest("session-abc");

            // Act
            var result = await function.Run(mockRequest.Object, "com", "example", "en", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthorResponse>(okResult.Value);
            Assert.NotNull(response.FeaturedBook);
            Assert.Equal("The Featured Novel", response.FeaturedBook.Title);
            Assert.NotNull(response.Experiment);
            Assert.Equal("control", response.Experiment.HomepageHeroVariant);
        }

        // -------------------------------------------------------------------------
        // Test: Featured book + variant assignment
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_FeaturedBook_VariantAssignment_ReturnsFeaturedBookAndVariant()
        {
            // Arrange
            var responseClone = new AuthorResponse
            {
                Name = ResponseWithFeaturedBook.Name,
                Books = ResponseWithFeaturedBook.Books,
                FeaturedBook = ResponseWithFeaturedBook.FeaturedBook
            };
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync("com", "example", "en", null))
                .ReturnsAsync(responseClone);
            _experimentServiceMock
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ReturnsAsync(BuildExperimentResponse("featured-book-hero"));

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object, _experimentServiceMock.Object);
            var mockRequest = CreateMockRequest("session-xyz");

            // Act
            var result = await function.Run(mockRequest.Object, "com", "example", "en", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthorResponse>(okResult.Value);
            Assert.NotNull(response.FeaturedBook);
            Assert.NotNull(response.Experiment);
            Assert.Equal("featured-book-hero", response.Experiment.HomepageHeroVariant);
        }

        // -------------------------------------------------------------------------
        // Test: Experiment service unavailable / throws → defaults to control
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_ExperimentServiceThrows_DefaultsToControl()
        {
            // Arrange
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync("com", "example", "en", null))
                .ReturnsAsync(BaseResponse);
            _experimentServiceMock
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ThrowsAsync(new Exception("Cosmos unavailable"));

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object, _experimentServiceMock.Object);
            var mockRequest = CreateMockRequest();

            // Act
            var result = await function.Run(mockRequest.Object, "com", "example", "en", null);

            // Assert — should not throw; should return control
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthorResponse>(okResult.Value);
            Assert.NotNull(response.Experiment);
            Assert.Equal("control", response.Experiment.HomepageHeroVariant);
        }

        // -------------------------------------------------------------------------
        // Test: Experiment service returns no assignments → defaults to control
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_ExperimentServiceReturnsNoAssignments_DefaultsToControl()
        {
            // Arrange
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync("com", "example", "en", null))
                .ReturnsAsync(BaseResponse);
            _experimentServiceMock
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ReturnsAsync(new GetExperimentsResponse
                {
                    SessionId = "test",
                    Experiments = new List<AssignedExperiment>()
                });

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object, _experimentServiceMock.Object);
            var mockRequest = CreateMockRequest();

            // Act
            var result = await function.Run(mockRequest.Object, "com", "example", "en", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthorResponse>(okResult.Value);
            Assert.NotNull(response.Experiment);
            Assert.Equal("control", response.Experiment.HomepageHeroVariant);
        }

        // -------------------------------------------------------------------------
        // Test: Unknown variant returned → treated as control
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_UnknownVariantAssigned_TreatedAsControl()
        {
            // Arrange
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync("com", "example", "en", null))
                .ReturnsAsync(ResponseWithFeaturedBook);
            _experimentServiceMock
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ReturnsAsync(BuildExperimentResponse("some-unknown-variant"));

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object, _experimentServiceMock.Object);
            var mockRequest = CreateMockRequest();

            // Act
            var result = await function.Run(mockRequest.Object, "com", "example", "en", null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthorResponse>(okResult.Value);
            Assert.Equal("control", response.Experiment!.HomepageHeroVariant);
        }

        // -------------------------------------------------------------------------
        // Test: Session ID from header is forwarded to experiment service
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_SessionIdHeader_ForwardedToBucketingKey()
        {
            // Arrange
            const string expectedSessionId = "user-session-12345";
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(BaseResponse);
            _experimentServiceMock
                .Setup(s => s.GetExperimentsAsync(It.Is<GetExperimentsRequest>(r => r.UserId == expectedSessionId)))
                .ReturnsAsync(BuildExperimentResponse("control"))
                .Verifiable();

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object, _experimentServiceMock.Object);
            var mockRequest = CreateMockRequest(expectedSessionId);

            // Act
            await function.Run(mockRequest.Object, "com", "example", "en", null);

            // Assert
            _experimentServiceMock.Verify();
        }

        // -------------------------------------------------------------------------
        // Test: Existing homepage data contract is preserved
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Run_AuthorFound_ExistingFieldsPreserved()
        {
            // Arrange
            _authorDataServiceMock
                .Setup(s => s.GetHomepageDataAsync("com", "example", "en", null))
                .ReturnsAsync(BaseResponse);
            _experimentServiceMock
                .Setup(s => s.GetExperimentsAsync(It.IsAny<GetExperimentsRequest>()))
                .ReturnsAsync(BuildExperimentResponse("control"));

            var function = new GetAuthorData(_loggerMock.Object, _authorDataServiceMock.Object, _experimentServiceMock.Object);
            var mockRequest = CreateMockRequest();

            // Act
            var result = await function.Run(mockRequest.Object, "com", "example", "en", null);

            // Assert — all original fields unchanged
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthorResponse>(okResult.Value);
            Assert.Equal("Jane Author", response.Name);
            Assert.Equal("Welcome!", response.Welcome);
            Assert.Equal("jane@example.com", response.Email);
        }
    }
}
