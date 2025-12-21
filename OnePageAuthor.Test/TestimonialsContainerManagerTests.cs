using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test
{
    public class TestimonialsContainerManagerTests
    {
        [Fact]
        public async Task EnsureContainerAsync_CreatesContainer()
        {
            // Arrange
            var databaseMock = new Mock<Database>();
            var containerMock = new Mock<Container>();
            var containerResponseMock = new Mock<ContainerResponse>();

            containerResponseMock.Setup(r => r.Container).Returns(containerMock.Object);
            databaseMock.Setup(d => d.CreateContainerIfNotExistsAsync(
                It.Is<string>(id => id == "Testimonials"),
                It.Is<string>(path => path == "/Locale"),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(containerResponseMock.Object);

            var manager = new TestimonialsContainerManager(databaseMock.Object);

            // Act
            var container = await manager.EnsureContainerAsync();

            // Assert
            Assert.NotNull(container);
            databaseMock.Verify(d => d.CreateContainerIfNotExistsAsync(
                "Testimonials",
                "/Locale",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_ThrowsException_WhenDatabaseIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new TestimonialsContainerManager(null!));
            Assert.Contains("TestimonialsContainerManager: The provided Database is null", exception.Message);
        }
    }
}
