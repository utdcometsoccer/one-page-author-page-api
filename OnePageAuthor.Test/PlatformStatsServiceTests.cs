using Microsoft.Extensions.Logging;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Services;
using InkStainedWretch.OnePageAuthorAPI.API;
using BookEntity = InkStainedWretch.OnePageAuthorAPI.Entities.Book;

namespace OnePageAuthor.Test
{
    public class PlatformStatsServiceTests
    {
        [Fact]
        public void ThrowsOnNullArguments()
        {
            // Arrange
            var statsRepoMock = new Mock<IPlatformStatsRepository>();
            var authorRepoMock = new Mock<IAuthorRepository>();
            var bookRepoMock = new Mock<IGenericRepository<BookEntity>>();
            var countryRepoMock = new Mock<ICountryRepository>();
            var loggerMock = new Mock<ILogger<PlatformStatsService>>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PlatformStatsService(
                null!, authorRepoMock.Object, bookRepoMock.Object, countryRepoMock.Object, loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new PlatformStatsService(
                statsRepoMock.Object, null!, bookRepoMock.Object, countryRepoMock.Object, loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new PlatformStatsService(
                statsRepoMock.Object, authorRepoMock.Object, null!, countryRepoMock.Object, loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new PlatformStatsService(
                statsRepoMock.Object, authorRepoMock.Object, bookRepoMock.Object, null!, loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new PlatformStatsService(
                statsRepoMock.Object, authorRepoMock.Object, bookRepoMock.Object, countryRepoMock.Object, null!));
        }

        [Fact]
        public async Task GetPlatformStatsAsync_ReturnsStats_FromRepository()
        {
            // Arrange
            var statsRepoMock = new Mock<IPlatformStatsRepository>();
            var authorRepoMock = new Mock<IAuthorRepository>();
            var bookRepoMock = new Mock<IGenericRepository<BookEntity>>();
            var countryRepoMock = new Mock<ICountryRepository>();
            var loggerMock = new Mock<ILogger<PlatformStatsService>>();

            var expectedStats = new PlatformStats
            {
                id = "current",
                ActiveAuthors = 100,
                BooksPublished = 500,
                TotalRevenue = 10000,
                AverageRating = 4.5,
                CountriesServed = 25,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            statsRepoMock.Setup(r => r.GetCurrentStatsAsync()).ReturnsAsync(expectedStats);

            var service = new PlatformStatsService(
                statsRepoMock.Object,
                authorRepoMock.Object,
                bookRepoMock.Object,
                countryRepoMock.Object,
                loggerMock.Object);

            // Act
            var result = await service.GetPlatformStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedStats.ActiveAuthors, result.ActiveAuthors);
            Assert.Equal(expectedStats.BooksPublished, result.BooksPublished);
            Assert.Equal(expectedStats.TotalRevenue, result.TotalRevenue);
            statsRepoMock.Verify(r => r.GetCurrentStatsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPlatformStatsAsync_ReturnsDefaultStats_WhenNotFound()
        {
            // Arrange
            var statsRepoMock = new Mock<IPlatformStatsRepository>();
            var authorRepoMock = new Mock<IAuthorRepository>();
            var bookRepoMock = new Mock<IGenericRepository<BookEntity>>();
            var countryRepoMock = new Mock<ICountryRepository>();
            var loggerMock = new Mock<ILogger<PlatformStatsService>>();

            statsRepoMock.Setup(r => r.GetCurrentStatsAsync()).ReturnsAsync((PlatformStats?)null);

            var service = new PlatformStatsService(
                statsRepoMock.Object,
                authorRepoMock.Object,
                bookRepoMock.Object,
                countryRepoMock.Object,
                loggerMock.Object);

            // Act
            var result = await service.GetPlatformStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.ActiveAuthors);
            Assert.Equal(0, result.BooksPublished);
            Assert.Equal(0, result.TotalRevenue);
        }

        [Fact]
        public async Task GetPlatformStatsAsync_ReturnsCachedStats_WhenCacheValid()
        {
            // Arrange
            PlatformStatsService.ClearCache(); // Clear static cache for test isolation
            
            var statsRepoMock = new Mock<IPlatformStatsRepository>();
            var authorRepoMock = new Mock<IAuthorRepository>();
            var bookRepoMock = new Mock<IGenericRepository<BookEntity>>();
            var countryRepoMock = new Mock<ICountryRepository>();
            var loggerMock = new Mock<ILogger<PlatformStatsService>>();

            var expectedStats = new PlatformStats
            {
                id = "current",
                ActiveAuthors = 100,
                BooksPublished = 500,
                TotalRevenue = 10000,
                AverageRating = 4.5,
                CountriesServed = 25,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            statsRepoMock.Setup(r => r.GetCurrentStatsAsync()).ReturnsAsync(expectedStats);

            var service = new PlatformStatsService(
                statsRepoMock.Object,
                authorRepoMock.Object,
                bookRepoMock.Object,
                countryRepoMock.Object,
                loggerMock.Object);

            // Act
            var result1 = await service.GetPlatformStatsAsync(); // First call - cache miss
            var result2 = await service.GetPlatformStatsAsync(); // Second call - cache hit

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.ActiveAuthors, result2.ActiveAuthors);
            // Repository should only be called once due to caching
            statsRepoMock.Verify(r => r.GetCurrentStatsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPlatformStatsAsync_ReturnsDefaultOnError()
        {
            // Arrange
            PlatformStatsService.ClearCache(); // Clear static cache for test isolation
            
            var statsRepoMock = new Mock<IPlatformStatsRepository>();
            var authorRepoMock = new Mock<IAuthorRepository>();
            var bookRepoMock = new Mock<IGenericRepository<BookEntity>>();
            var countryRepoMock = new Mock<ICountryRepository>();
            var loggerMock = new Mock<ILogger<PlatformStatsService>>();

            statsRepoMock.Setup(r => r.GetCurrentStatsAsync()).ThrowsAsync(new Exception("Database error"));

            var service = new PlatformStatsService(
                statsRepoMock.Object,
                authorRepoMock.Object,
                bookRepoMock.Object,
                countryRepoMock.Object,
                loggerMock.Object);

            // Act
            var result = await service.GetPlatformStatsAsync();

            // Assert
            Assert.NotNull(result);
            // Verify the repository was called
            statsRepoMock.Verify(r => r.GetCurrentStatsAsync(), Times.Once);
            
            // Verify error was logged
            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching platform stats")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ComputeAndUpdateStatsAsync_ComputesAndSavesStats()
        {
            // Arrange
            var statsRepoMock = new Mock<IPlatformStatsRepository>();
            var authorRepoMock = new Mock<IAuthorRepository>();
            var bookRepoMock = new Mock<IGenericRepository<BookEntity>>();
            var countryRepoMock = new Mock<ICountryRepository>();
            var loggerMock = new Mock<ILogger<PlatformStatsService>>();

            var expectedStats = new PlatformStats
            {
                id = "current",
                ActiveAuthors = 0, // Will be computed
                BooksPublished = 0, // Will be computed
                TotalRevenue = 0,
                AverageRating = 4.8,
                CountriesServed = 0, // Will be computed
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            statsRepoMock.Setup(r => r.UpsertStatsAsync(It.IsAny<PlatformStats>())).ReturnsAsync(expectedStats);

            var service = new PlatformStatsService(
                statsRepoMock.Object,
                authorRepoMock.Object,
                bookRepoMock.Object,
                countryRepoMock.Object,
                loggerMock.Object);

            // Act
            var result = await service.ComputeAndUpdateStatsAsync();

            // Assert
            Assert.NotNull(result);
            statsRepoMock.Verify(r => r.UpsertStatsAsync(It.IsAny<PlatformStats>()), Times.Once);
        }
    }
}
