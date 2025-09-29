using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test.ImageAPI
{
    public class ImageRepositoryTests
    {
        [Fact]
        public void Constructor_ThrowsOnNullContainer()
        {
            Assert.Throws<ArgumentNullException>(() => new ImageRepository((Container)null!));
            Assert.Throws<ArgumentNullException>(() => new ImageRepository((IDataContainer)null!));
        }

        [Fact]
        public async Task GetByUserProfileIdAsync_ReturnsUserImages()
        {
            // Arrange
            var containerMock = new Mock<Container>();
            var userProfileId = "user-123";
            var images = new List<Image>
            {
                new Image
                {
                    id = Guid.NewGuid().ToString(),
                    UserProfileId = userProfileId,
                    Name = "test1.jpg",
                    Url = "https://storage.blob.core.windows.net/images/test1.jpg",
                    Size = 1024,
                    ContentType = "image/jpeg",
                    ContainerName = "images",
                    BlobName = "user-123/test1.jpg",
                    UploadedAt = DateTime.UtcNow
                },
                new Image
                {
                    id = Guid.NewGuid().ToString(),
                    UserProfileId = userProfileId,
                    Name = "test2.png",
                    Url = "https://storage.blob.core.windows.net/images/test2.png",
                    Size = 2048,
                    ContentType = "image/png",
                    ContainerName = "images",
                    BlobName = "user-123/test2.png",
                    UploadedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            var queryResponse = new Mock<FeedResponse<Image>>();
            queryResponse.Setup(x => x.GetEnumerator()).Returns(images.GetEnumerator());

            var iterator = new Mock<FeedIterator<Image>>();
            iterator.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResponse.Object);

            containerMock.Setup(x => x.GetItemQueryIterator<Image>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(iterator.Object);

            var repository = new ImageRepository(containerMock.Object);

            // Act
            var result = await repository.GetByUserProfileIdAsync(userProfileId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, img => Assert.Equal(userProfileId, img.UserProfileId));
            containerMock.Verify(x => x.GetItemQueryIterator<Image>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("WHERE c.UserProfileId = @userProfileId")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetTotalSizeByUserProfileIdAsync_ReturnsCorrectSize()
        {
            // Arrange
            var containerMock = new Mock<Container>();
            var userProfileId = "user-123";
            var totalSize = 5000L;

            var queryResponse = new Mock<FeedResponse<long?>>();
            queryResponse.Setup(x => x.GetEnumerator()).Returns(new List<long?> { totalSize }.GetEnumerator());

            var iterator = new Mock<FeedIterator<long?>>();
            iterator.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResponse.Object);

            containerMock.Setup(x => x.GetItemQueryIterator<long?>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(iterator.Object);

            var repository = new ImageRepository(containerMock.Object);

            // Act
            var result = await repository.GetTotalSizeByUserProfileIdAsync(userProfileId);

            // Assert
            Assert.Equal(totalSize, result);
            containerMock.Verify(x => x.GetItemQueryIterator<long?>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("SELECT VALUE SUM(c.Size)")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetTotalSizeByUserProfileIdAsync_ReturnsZeroWhenNull()
        {
            // Arrange
            var containerMock = new Mock<Container>();
            var userProfileId = "user-123";

            var queryResponse = new Mock<FeedResponse<long?>>();
            queryResponse.Setup(x => x.GetEnumerator()).Returns(new List<long?> { null }.GetEnumerator());

            var iterator = new Mock<FeedIterator<long?>>();
            iterator.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResponse.Object);

            containerMock.Setup(x => x.GetItemQueryIterator<long?>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(iterator.Object);

            var repository = new ImageRepository(containerMock.Object);

            // Act
            var result = await repository.GetTotalSizeByUserProfileIdAsync(userProfileId);

            // Assert
            Assert.Equal(0L, result);
        }

        [Fact]
        public async Task GetCountByUserProfileIdAsync_ReturnsCorrectCount()
        {
            // Arrange
            var containerMock = new Mock<Container>();
            var userProfileId = "user-123";
            var count = 15;

            var queryResponse = new Mock<FeedResponse<int>>();
            queryResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { count }.GetEnumerator());

            var iterator = new Mock<FeedIterator<int>>();
            iterator.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResponse.Object);

            containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(iterator.Object);

            var repository = new ImageRepository(containerMock.Object);

            // Act
            var result = await repository.GetCountByUserProfileIdAsync(userProfileId);

            // Assert
            Assert.Equal(count, result);
            containerMock.Verify(x => x.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("SELECT VALUE COUNT(1)")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }
    }
}