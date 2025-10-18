using System.Net;
using InkStainedWretch.OnePageAuthorLib.API.Amazon;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace OnePageAuthor.Test.API
{
    [Trait("Category", "AmazonProduct")]
    public class AmazonProductServiceTests
    {
        private readonly Mock<IAmazonProductConfig> _configMock;
        private readonly Mock<ILogger<AmazonProductService>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly AmazonProductService _service;

        public AmazonProductServiceTests()
        {
            _configMock = new Mock<IAmazonProductConfig>();
            _loggerMock = new Mock<ILogger<AmazonProductService>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

            // Setup default config values
            _configMock.Setup(c => c.AccessKey).Returns("test-access-key");
            _configMock.Setup(c => c.SecretKey).Returns("test-secret-key");
            _configMock.Setup(c => c.PartnerTag).Returns("testtag-20");
            _configMock.Setup(c => c.Region).Returns("us-east-1");
            _configMock.Setup(c => c.Marketplace).Returns("www.amazon.com");
            _configMock.Setup(c => c.ApiEndpoint).Returns("https://webservices.amazon.com/paapi5/searchitems");

            _service = new AmazonProductService(_httpClient, _configMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_ValidAuthor_ReturnsJsonDocument()
        {
            // Arrange
            var authorName = "Stephen King";
            var responseJson = @"{
                ""SearchResult"": {
                    ""Items"": [
                        {
                            ""ASIN"": ""B001"",
                            ""ItemInfo"": {
                                ""Title"": {
                                    ""DisplayValue"": ""The Shining""
                                }
                            }
                        }
                    ]
                }
            }";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _service.SearchBooksByAuthorAsync(authorName);

            // Assert
            Assert.NotNull(result);
            var rootElement = result.RootElement;
            Assert.True(rootElement.TryGetProperty("SearchResult", out var searchResult));
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_WithPageNumber_ReturnsJsonDocument()
        {
            // Arrange
            var authorName = "Stephen King";
            var itemPage = 2;
            var responseJson = @"{""SearchResult"": {""Items"": []}}";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _service.SearchBooksByAuthorAsync(authorName, itemPage);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_NullAuthorName_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SearchBooksByAuthorAsync(null!));
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_EmptyAuthorName_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SearchBooksByAuthorAsync(string.Empty));
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_InvalidPageNumber_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SearchBooksByAuthorAsync("Stephen King", 0));
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_HttpError_ThrowsInvalidOperationException()
        {
            // Arrange
            var authorName = "Stephen King";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Bad Request")
                });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.SearchBooksByAuthorAsync(authorName));
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_InvalidJson_ThrowsInvalidOperationException()
        {
            // Arrange
            var authorName = "Stephen King";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Not valid JSON")
                });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.SearchBooksByAuthorAsync(authorName));
        }

        [Fact]
        public void Constructor_NullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new AmazonProductService(null!, _configMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_NullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new AmazonProductService(_httpClient, null!, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new AmazonProductService(_httpClient, _configMock.Object, null!));
        }
    }
}
