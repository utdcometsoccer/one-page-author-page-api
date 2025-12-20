using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace OnePageAuthor.Test
{
    public class PlatformStatsRepositoryTests
    {
        [Fact]
        public void ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new PlatformStatsRepository((IDataContainer)null!));
        }

        [Fact]
        public async Task GetCurrentStatsAsync_ReturnsStats()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var stats = new PlatformStats
            {
                id = "current",
                ActiveAuthors = 100,
                BooksPublished = 500,
                TotalRevenue = 10000,
                AverageRating = 4.5,
                CountriesServed = 25,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };
            cosmosMock.Setup(c => c.ReadItemAsync<PlatformStats>(
                It.IsAny<string>(), 
                It.IsAny<PartitionKey>()))
                .ReturnsAsync(stats);

            var repo = new PlatformStatsRepository(cosmosMock.Object);

            // Act
            var result = await repo.GetCurrentStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(stats.ActiveAuthors, result.ActiveAuthors);
            Assert.Equal(stats.BooksPublished, result.BooksPublished);
            Assert.Equal(stats.TotalRevenue, result.TotalRevenue);
            Assert.Equal(stats.AverageRating, result.AverageRating);
            Assert.Equal(stats.CountriesServed, result.CountriesServed);
            cosmosMock.Verify(c => c.ReadItemAsync<PlatformStats>(
                "current", 
                It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task GetCurrentStatsAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            cosmosMock.Setup(c => c.ReadItemAsync<PlatformStats>(
                It.IsAny<string>(), 
                It.IsAny<PartitionKey>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            var repo = new PlatformStatsRepository(cosmosMock.Object);

            // Act
            var result = await repo.GetCurrentStatsAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpsertStatsAsync_CreatesStats_WhenNotFound()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var stats = new PlatformStats
            {
                id = "current",
                ActiveAuthors = 150,
                BooksPublished = 750,
                TotalRevenue = 15000,
                AverageRating = 4.7,
                CountriesServed = 30,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            // Setup ReplaceItemAsync to throw NotFound
            cosmosMock.Setup(c => c.ReplaceItemAsync<PlatformStats>(
                It.IsAny<PlatformStats>(),
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            // Setup CreateItemAsync to succeed
            var responseMock = new Mock<ItemResponse<PlatformStats>>();
            responseMock.Setup(r => r.Resource).Returns(stats);
            cosmosMock.Setup(c => c.CreateItemAsync<PlatformStats>(
                It.IsAny<PlatformStats>(),
                It.IsAny<PartitionKey>()))
                .ReturnsAsync(responseMock.Object);

            var repo = new PlatformStatsRepository(cosmosMock.Object);

            // Act
            var result = await repo.UpsertStatsAsync(stats);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("current", result.id);
            cosmosMock.Verify(c => c.CreateItemAsync<PlatformStats>(
                It.IsAny<PlatformStats>(),
                It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task UpsertStatsAsync_UpdatesStats_WhenExists()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var stats = new PlatformStats
            {
                id = "current",
                ActiveAuthors = 200,
                BooksPublished = 1000,
                TotalRevenue = 20000,
                AverageRating = 4.8,
                CountriesServed = 35,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            var responseMock = new Mock<ItemResponse<PlatformStats>>();
            responseMock.Setup(r => r.Resource).Returns(stats);
            cosmosMock.Setup(c => c.ReplaceItemAsync<PlatformStats>(
                It.IsAny<PlatformStats>(),
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()))
                .ReturnsAsync(responseMock.Object);

            var repo = new PlatformStatsRepository(cosmosMock.Object);

            // Act
            var result = await repo.UpsertStatsAsync(stats);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(stats.ActiveAuthors, result.ActiveAuthors);
            cosmosMock.Verify(c => c.ReplaceItemAsync<PlatformStats>(
                It.IsAny<PlatformStats>(),
                "current",
                It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task UpsertStatsAsync_ThrowsOnNullStats()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var repo = new PlatformStatsRepository(cosmosMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.UpsertStatsAsync(null!));
        }

        [Fact]
        public async Task UpsertStatsAsync_SetsIdToCurrent()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var stats = new PlatformStats
            {
                id = "wrong-id", // Wrong ID
                ActiveAuthors = 100,
                BooksPublished = 500,
                TotalRevenue = 10000,
                AverageRating = 4.5,
                CountriesServed = 25,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            PlatformStats? capturedStats = null;
            var responseMock = new Mock<ItemResponse<PlatformStats>>();
            responseMock.Setup(r => r.Resource).Returns(stats);
            cosmosMock.Setup(c => c.ReplaceItemAsync<PlatformStats>(
                It.IsAny<PlatformStats>(),
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()))
                .Callback<PlatformStats, string, PartitionKey>((s, id, pk) => capturedStats = s)
                .ReturnsAsync(responseMock.Object);

            var repo = new PlatformStatsRepository(cosmosMock.Object);

            // Act
            await repo.UpsertStatsAsync(stats);

            // Assert
            Assert.NotNull(capturedStats);
            Assert.Equal("current", capturedStats.id);
        }
    }
}
