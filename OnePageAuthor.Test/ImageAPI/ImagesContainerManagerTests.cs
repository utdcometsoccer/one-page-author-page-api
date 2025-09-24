using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL.ImageAPI;

namespace OnePageAuthor.Test.ImageAPI
{
    public class ImagesContainerManagerTests
    {
        [Fact]
        public void Constructor_ThrowsOnNullDatabase()
        {
            Assert.Throws<ArgumentNullException>(() => new ImagesContainerManager(null!));
        }

        [Fact]
        public async Task EnsureContainerAsync_CreatesContainerWithCorrectSettings()
        {
            // Arrange
            var containerResponse = new Mock<ContainerResponse>();
            var container = new Mock<Container>();
            containerResponse.Setup(x => x.Container).Returns(container.Object);

            var database = new Mock<Database>();
            database.Setup(x => x.CreateContainerIfNotExistsAsync(
                "Images",
                "/UserProfileId",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(containerResponse.Object);

            var manager = new ImagesContainerManager(database.Object);

            // Act
            var result = await manager.EnsureContainerAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Same(container.Object, result);
            database.Verify(x => x.CreateContainerIfNotExistsAsync(
                "Images",
                "/UserProfileId",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}