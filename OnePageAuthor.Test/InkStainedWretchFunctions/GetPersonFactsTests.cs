using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretchFunctions;
using InkStainedWretch.OnePageAuthorLib.API.Wikipedia;
using System.Threading.Tasks;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    /// <summary>
    /// Unit tests for GetPersonFacts Azure Function
    /// </summary>
    public class GetPersonFactsTests
    {
        private readonly Mock<ILogger<GetPersonFacts>> _mockLogger;
        private readonly Mock<IWikipediaService> _mockWikipediaService;
        private readonly GetPersonFacts _function;
        private readonly Mock<HttpRequest> _mockHttpRequest;

        public GetPersonFactsTests()
        {
            _mockLogger = new Mock<ILogger<GetPersonFacts>>();
            _mockWikipediaService = new Mock<IWikipediaService>();
            _mockHttpRequest = new Mock<HttpRequest>();

            _function = new GetPersonFacts(
                _mockLogger.Object,
                _mockWikipediaService.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new GetPersonFacts(null!, _mockWikipediaService.Object));
        }

        [Fact]
        public void Constructor_WithNullWikipediaService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new GetPersonFacts(_mockLogger.Object, null!));
        }

        [Fact]
        public async Task Run_WithNullLanguage_ReturnsBadRequest()
        {
            // Act
            var result = await _function.Run(_mockHttpRequest.Object, null!, "Albert Einstein");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_WithEmptyLanguage_ReturnsBadRequest()
        {
            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "", "Albert Einstein");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_WithWhitespaceLanguage_ReturnsBadRequest()
        {
            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "   ", "Albert Einstein");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_WithNullPersonName_ReturnsBadRequest()
        {
            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_WithEmptyPersonName_ReturnsBadRequest()
        {
            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", "");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_WithWhitespacePersonName_ReturnsBadRequest()
        {
            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", "   ");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var expectedFacts = new WikipediaPersonFactsResponse
            {
                Title = "Albert Einstein",
                Description = "German-born scientist (1879–1955)",
                Extract = "Albert Einstein was a German-born theoretical physicist.",
                LeadParagraph = "Albert Einstein (14 March 1879 – 18 April 1955) was a German-born theoretical physicist.",
                Thumbnail = new ThumbnailInfo
                {
                    Source = "https://example.com/image.jpg",
                    Width = 320,
                    Height = 396
                },
                CanonicalUrl = "https://en.wikipedia.org/wiki/Albert_Einstein",
                Language = "en"
            };

            _mockWikipediaService
                .Setup(s => s.GetPersonFactsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedFacts);

            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", "Albert Einstein");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var facts = Assert.IsType<WikipediaPersonFactsResponse>(okResult.Value);
            Assert.Equal("Albert Einstein", facts.Title);
            Assert.Equal("German-born scientist (1879–1955)", facts.Description);
        }

        [Fact]
        public async Task Run_WhenNoDataFound_ReturnsNotFound()
        {
            // Arrange
            var emptyFacts = new WikipediaPersonFactsResponse
            {
                Title = "",
                Description = "",
                Extract = "",
                LeadParagraph = "",
                CanonicalUrl = "",
                Language = "en"
            };

            _mockWikipediaService
                .Setup(s => s.GetPersonFactsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emptyFacts);

            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", "NonExistentPerson");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Run_WithUppercaseLanguage_NormalizesToLowercase()
        {
            // Arrange
            var expectedFacts = new WikipediaPersonFactsResponse
            {
                Title = "Test",
                Description = "Test",
                Extract = "Test",
                LeadParagraph = "Test",
                CanonicalUrl = "https://en.wikipedia.org/wiki/Test",
                Language = "en"
            };

            _mockWikipediaService
                .Setup(s => s.GetPersonFactsAsync("Test Person", "en"))
                .ReturnsAsync(expectedFacts);

            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "EN", "Test Person");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockWikipediaService.Verify(s => s.GetPersonFactsAsync("Test Person", "en"), Times.Once);
        }

        [Fact]
        public async Task Run_WithEncodedPersonName_DecodesCorrectly()
        {
            // Arrange
            var expectedFacts = new WikipediaPersonFactsResponse
            {
                Title = "Stephen Hawking",
                Description = "Test",
                Extract = "Test",
                LeadParagraph = "Test",
                CanonicalUrl = "https://en.wikipedia.org/wiki/Stephen_Hawking",
                Language = "en"
            };

            _mockWikipediaService
                .Setup(s => s.GetPersonFactsAsync("Stephen Hawking", "en"))
                .ReturnsAsync(expectedFacts);

            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", "Stephen%20Hawking");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockWikipediaService.Verify(s => s.GetPersonFactsAsync("Stephen Hawking", "en"), Times.Once);
        }

        [Fact]
        public async Task Run_WhenServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            _mockWikipediaService
                .Setup(s => s.GetPersonFactsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid parameter"));

            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", "Test");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_WhenServiceThrowsHttpRequestException_ReturnsBadGateway()
        {
            // Arrange
            _mockWikipediaService
                .Setup(s => s.GetPersonFactsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("External API error"));

            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", "Test");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
        }

        [Fact]
        public async Task Run_WhenServiceThrowsGenericException_ReturnsInternalServerError()
        {
            // Arrange
            _mockWikipediaService
                .Setup(s => s.GetPersonFactsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _function.Run(_mockHttpRequest.Object, "en", "Test");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("es")]
        [InlineData("fr")]
        [InlineData("de")]
        [InlineData("zh")]
        public async Task Run_WithDifferentLanguages_CallsServiceWithCorrectLanguage(string language)
        {
            // Arrange
            var expectedFacts = new WikipediaPersonFactsResponse
            {
                Title = "Test",
                Description = "Test",
                Extract = "Test",
                LeadParagraph = "Test",
                CanonicalUrl = $"https://{language}.wikipedia.org/wiki/Test",
                Language = language
            };

            _mockWikipediaService
                .Setup(s => s.GetPersonFactsAsync("Test Person", language))
                .ReturnsAsync(expectedFacts);

            // Act
            var result = await _function.Run(_mockHttpRequest.Object, language, "Test Person");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockWikipediaService.Verify(s => s.GetPersonFactsAsync("Test Person", language), Times.Once);
        }
    }
}
