using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class AuthorsContainerManagerTests
    {
        [Fact]
        public async Task EnsureContainerAsync_WhenContainerCreated_ReturnsContainer()
        {
            var databaseMock = new Mock<Database>();
            var containerMock = new Mock<Container>();
            var containerResponseMock = new Mock<ContainerResponse>();
            containerResponseMock.Setup(cr => cr.Container).Returns(containerMock.Object);
            containerResponseMock.Setup(cr => cr.StatusCode).Returns(System.Net.HttpStatusCode.Created);
            databaseMock.Setup(db => db.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(), It.IsAny<int?>(), It.IsAny<RequestOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(containerResponseMock.Object);
            var manager = new AuthorsContainerManager(databaseMock.Object);
            var container = await manager.EnsureContainerAsync();
            Assert.NotNull(container);
            Assert.Equal(containerMock.Object, container);
            containerMock.Verify(c => c.ReplaceContainerAsync(
                It.IsAny<ContainerProperties>(), It.IsAny<ContainerRequestOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EnsureContainerAsync_WhenContainerAlreadyExists_UpdatesIndexingPolicy()
        {
            var databaseMock = new Mock<Database>();
            var containerMock = new Mock<Container>();
            var createResponseMock = new Mock<ContainerResponse>();
            createResponseMock.Setup(cr => cr.Container).Returns(containerMock.Object);
            createResponseMock.Setup(cr => cr.StatusCode).Returns(System.Net.HttpStatusCode.OK);
            var replaceResponseMock = new Mock<ContainerResponse>();
            replaceResponseMock.Setup(cr => cr.Container).Returns(containerMock.Object);
            databaseMock.Setup(db => db.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(), It.IsAny<int?>(), It.IsAny<RequestOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createResponseMock.Object);
            containerMock.Setup(c => c.ReplaceContainerAsync(
                It.IsAny<ContainerProperties>(), It.IsAny<ContainerRequestOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(replaceResponseMock.Object);
            var manager = new AuthorsContainerManager(databaseMock.Object);
            var container = await manager.EnsureContainerAsync();
            Assert.NotNull(container);
            containerMock.Verify(c => c.ReplaceContainerAsync(
                It.IsAny<ContainerProperties>(), It.IsAny<ContainerRequestOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthorsContainerManager((Database)null!));
        }
    }
}
