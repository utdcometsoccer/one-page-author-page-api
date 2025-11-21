using System.Net;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.API.Wikipedia;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace OnePageAuthor.Test.Services
{
    /// <summary>
    /// Unit tests for WikipediaService.
    /// </summary>
    public class WikipediaServiceTests
    {
        private readonly Mock<ILogger<WikipediaService>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly WikipediaService _service;

        public WikipediaServiceTests()
        {
            _mockLogger = new Mock<ILogger<WikipediaService>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _service = new WikipediaService(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WikipediaService(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WikipediaService(_httpClient, null!));
        }

        [Fact]
        public async Task GetPersonFactsAsync_WithNullPersonName_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetPersonFactsAsync(null!, "en"));
        }

        [Fact]
        public async Task GetPersonFactsAsync_WithEmptyPersonName_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetPersonFactsAsync("", "en"));
        }

        [Fact]
        public async Task GetPersonFactsAsync_WithWhitespacePersonName_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetPersonFactsAsync("   ", "en"));
        }

        [Fact]
        public async Task GetPersonFactsAsync_WithNullLanguage_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetPersonFactsAsync("Albert Einstein", null!));
        }

        [Fact]
        public async Task GetPersonFactsAsync_WithEmptyLanguage_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetPersonFactsAsync("Albert Einstein", ""));
        }

        [Fact]
        public async Task GetPersonFactsAsync_WithValidParameters_ReturnsStructuredData()
        {
            // Arrange
            var summaryJson = @"{
                ""title"": ""Albert Einstein"",
                ""description"": ""German-born scientist (1879–1955)"",
                ""extract"": ""Albert Einstein was a German-born theoretical physicist."",
                ""thumbnail"": {
                    ""source"": ""https://example.com/image.jpg"",
                    ""width"": 320,
                    ""height"": 396
                },
                ""content_urls"": {
                    ""desktop"": {
                        ""page"": ""https://en.wikipedia.org/wiki/Albert_Einstein""
                    }
                }
            }";

            var extractJson = @"{
                ""query"": {
                    ""pages"": {
                        ""736"": {
                            ""extract"": ""Albert Einstein (14 March 1879 – 18 April 1955) was a German-born theoretical physicist.""
                        }
                    }
                }
            }";

            var summaryResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(summaryJson)
            };

            var extractResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(extractJson)
            };

            var requestCount = 0;
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    requestCount++;
                    return requestCount == 1 ? summaryResponse : extractResponse;
                });

            // Act
            var result = await _service.GetPersonFactsAsync("Albert Einstein", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Albert Einstein", result.Title);
            Assert.Equal("German-born scientist (1879–1955)", result.Description);
            Assert.Equal("Albert Einstein was a German-born theoretical physicist.", result.Extract);
            Assert.Equal("Albert Einstein (14 March 1879 – 18 April 1955) was a German-born theoretical physicist.", result.LeadParagraph);
            Assert.Equal("https://en.wikipedia.org/wiki/Albert_Einstein", result.CanonicalUrl);
            Assert.Equal("en", result.Language);
            Assert.NotNull(result.Thumbnail);
            Assert.Equal("https://example.com/image.jpg", result.Thumbnail.Source);
            Assert.Equal(320, result.Thumbnail.Width);
            Assert.Equal(396, result.Thumbnail.Height);
        }

        [Fact]
        public async Task GetPersonFactsAsync_WithNoThumbnail_ReturnsDataWithoutThumbnail()
        {
            // Arrange
            var summaryJson = @"{
                ""title"": ""Test Person"",
                ""description"": ""Test description"",
                ""extract"": ""Test extract"",
                ""content_urls"": {
                    ""desktop"": {
                        ""page"": ""https://en.wikipedia.org/wiki/Test_Person""
                    }
                }
            }";

            var extractJson = @"{
                ""query"": {
                    ""pages"": {
                        ""123"": {
                            ""extract"": ""Test lead paragraph.""
                        }
                    }
                }
            }";

            var summaryResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(summaryJson)
            };

            var extractResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(extractJson)
            };

            var requestCount = 0;
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    requestCount++;
                    return requestCount == 1 ? summaryResponse : extractResponse;
                });

            // Act
            var result = await _service.GetPersonFactsAsync("Test Person", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Person", result.Title);
            Assert.Null(result.Thumbnail);
        }

        [Fact]
        public async Task GetPersonFactsAsync_WithDifferentLanguage_UsesCorrectLanguageCode()
        {
            // Arrange
            var summaryJson = @"{
                ""title"": ""Albert Einstein"",
                ""description"": ""Physicien allemand (1879-1955)"",
                ""extract"": ""Albert Einstein est un physicien théoricien."",
                ""content_urls"": {
                    ""desktop"": {
                        ""page"": ""https://fr.wikipedia.org/wiki/Albert_Einstein""
                    }
                }
            }";

            var extractJson = @"{
                ""query"": {
                    ""pages"": {
                        ""736"": {
                            ""extract"": ""Albert Einstein est né le 14 mars 1879.""
                        }
                    }
                }
            }";

            var summaryResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(summaryJson)
            };

            var extractResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(extractJson)
            };

            HttpRequestMessage? capturedRequest = null;
            var requestCount = 0;
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    capturedRequest = request;
                    requestCount++;
                    return requestCount == 1 ? summaryResponse : extractResponse;
                })
                .Verifiable();

            // Act
            var result = await _service.GetPersonFactsAsync("Albert Einstein", "fr");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("fr", result.Language);
            Assert.NotNull(capturedRequest);
            Assert.Contains("fr.wikipedia.org", capturedRequest.RequestUri?.ToString() ?? "");
        }

        [Theory]
        [InlineData("EN", "en")]
        [InlineData("FR", "fr")]
        [InlineData("ES", "es")]
        public async Task GetPersonFactsAsync_NormalizesLanguageCode(string inputLanguage, string expectedLanguage)
        {
            // Arrange
            var summaryJson = @"{
                ""title"": ""Test"",
                ""description"": ""Test"",
                ""extract"": ""Test"",
                ""content_urls"": {
                    ""desktop"": {
                        ""page"": ""https://en.wikipedia.org/wiki/Test""
                    }
                }
            }";

            var extractJson = @"{
                ""query"": {
                    ""pages"": {
                        ""123"": {
                            ""extract"": ""Test extract.""
                        }
                    }
                }
            }";

            var summaryResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(summaryJson)
            };

            var extractResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(extractJson)
            };

            var requestCount = 0;
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    requestCount++;
                    return requestCount == 1 ? summaryResponse : extractResponse;
                });

            // Act
            var result = await _service.GetPersonFactsAsync("Test", inputLanguage);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedLanguage, result.Language);
        }
    }
}
