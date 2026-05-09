using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class ExperimentsContainerManagerTests
    {
        [Fact]
        public async Task EnsureContainerAsync_ReturnsContainer()
        {
            var databaseMock = new Mock<Database>();
            var containerMock = new Mock<Container>();
            var containerResponseMock = new Mock<ContainerResponse>();
            containerResponseMock.Setup(cr => cr.Container).Returns(containerMock.Object);
            databaseMock.Setup(db => db.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(), It.IsAny<int?>(), It.IsAny<RequestOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(containerResponseMock.Object);
            var manager = new ExperimentsContainerManager(databaseMock.Object);
            var container = await manager.EnsureContainerAsync();
            Assert.NotNull(container);
            Assert.Equal(containerMock.Object, container);
        }

        [Fact]
        public async Task EnsureContainerAsync_DoesNotPassThroughput()
        {
            // Serverless Cosmos DB accounts do not support throughput provisioning.
            // Verify that CreateContainerIfNotExistsAsync is called without an explicit throughput value.
            var databaseMock = new Mock<Database>();
            var containerMock = new Mock<Container>();
            var containerResponseMock = new Mock<ContainerResponse>();
            containerResponseMock.Setup(cr => cr.Container).Returns(containerMock.Object);
            databaseMock.Setup(db => db.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(), It.IsAny<int?>(), It.IsAny<RequestOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(containerResponseMock.Object);
            var manager = new ExperimentsContainerManager(databaseMock.Object);
            await manager.EnsureContainerAsync();
            databaseMock.Verify(db => db.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                It.Is<int?>(t => t == null),
                It.IsAny<RequestOptions?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void ThrowsOnNullDatabase()
        {
            Assert.Throws<ArgumentNullException>(() => new ExperimentsContainerManager(null!));
        }
    }
}
