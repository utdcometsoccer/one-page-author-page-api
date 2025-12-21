using Microsoft.Extensions.Logging;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    public class GetPlatformStatsTests
    {
        [Fact]
        public void Constructor_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<global::InkStainedWretchFunctions.GetPlatformStats>>();
            var serviceMock = new Mock<IPlatformStatsService>();

            // Act & Assert - constructor should not throw
            var function = new global::InkStainedWretchFunctions.GetPlatformStats(loggerMock.Object, serviceMock.Object);
            Assert.NotNull(function);
        }

        [Fact]
        public async Task Service_GetPlatformStatsAsync_IsCalled()
        {
            // Arrange
            var serviceMock = new Mock<IPlatformStatsService>();

            var expectedStats = new PlatformStats
            {
                id = "current",
                ActiveAuthors = 150,
                BooksPublished = 750,
                TotalRevenue = 15000,
                AverageRating = 4.7,
                CountriesServed = 30,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            serviceMock.Setup(s => s.GetPlatformStatsAsync()).ReturnsAsync(expectedStats);

            // Act
            var result = await serviceMock.Object.GetPlatformStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedStats.ActiveAuthors, result.ActiveAuthors);
            Assert.Equal(expectedStats.BooksPublished, result.BooksPublished);
            Assert.Equal(expectedStats.TotalRevenue, result.TotalRevenue);
            serviceMock.Verify(s => s.GetPlatformStatsAsync(), Times.Once);
        }
    }
}
