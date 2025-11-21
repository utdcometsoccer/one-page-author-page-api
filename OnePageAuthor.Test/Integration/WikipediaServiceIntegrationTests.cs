using InkStainedWretch.OnePageAuthorLib.API.Wikipedia;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace OnePageAuthor.Test.Integration
{
    /// <summary>
    /// Integration tests for WikipediaService - these tests call actual Wikipedia APIs.
    /// Mark as [Fact(Skip = "Integration test")] if you want to skip during normal test runs.
    /// </summary>
    public class WikipediaServiceIntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly WikipediaService _service;

        public WikipediaServiceIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<WikipediaService>();
            var httpClient = new HttpClient();
            _service = new WikipediaService(httpClient, logger);
        }

        /// <summary>
        /// This test demonstrates the service working with real Wikipedia data.
        /// It's skipped by default to avoid external dependencies in CI/CD.
        /// Run manually to verify the integration works.
        /// </summary>
        [Fact(Skip = "Integration test - requires Wikipedia API access")]
        public async Task GetPersonFactsAsync_WithRealWikipediaData_ReturnsValidData()
        {
            // Arrange
            var personName = "Albert Einstein";
            var language = "en";

            // Act
            var result = await _service.GetPersonFactsAsync(personName, language);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Title);
            Assert.NotEmpty(result.Description);
            Assert.NotEmpty(result.Extract);
            Assert.NotEmpty(result.CanonicalUrl);
            Assert.Equal(language, result.Language);

            _output.WriteLine($"Title: {result.Title}");
            _output.WriteLine($"Description: {result.Description}");
            _output.WriteLine($"Extract: {result.Extract?.Substring(0, Math.Min(100, result.Extract.Length))}...");
            _output.WriteLine($"Lead Paragraph: {result.LeadParagraph?.Substring(0, Math.Min(100, result.LeadParagraph.Length))}...");
            _output.WriteLine($"Canonical URL: {result.CanonicalUrl}");
            
            if (result.Thumbnail != null)
            {
                _output.WriteLine($"Thumbnail: {result.Thumbnail.Source} ({result.Thumbnail.Width}x{result.Thumbnail.Height})");
            }
        }

        /// <summary>
        /// Test with different languages to verify multi-language support.
        /// </summary>
        [Theory(Skip = "Integration test - requires Wikipedia API access")]
        [InlineData("Albert Einstein", "en")]
        [InlineData("Albert Einstein", "fr")]
        [InlineData("Albert Einstein", "es")]
        [InlineData("Albert Einstein", "de")]
        public async Task GetPersonFactsAsync_WithDifferentLanguages_ReturnsLocalizedData(string personName, string language)
        {
            // Act
            var result = await _service.GetPersonFactsAsync(personName, language);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(language, result.Language);
            Assert.NotEmpty(result.Title);
            
            _output.WriteLine($"[{language}] Title: {result.Title}");
            _output.WriteLine($"[{language}] Description: {result.Description}");
        }
    }
}
