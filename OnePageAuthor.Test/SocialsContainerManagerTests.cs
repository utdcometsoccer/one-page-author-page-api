using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class SocialsContainerManagerTests
    {
        [Fact]
        public async Task EnsureContainerAsync_ReturnsContainer()
        {
            var databaseMock = new Mock<Database>();
            var containerMock = new Mock<Container>();
            var containerResponseMock = new Mock<ContainerResponse>();
            containerResponseMock.Setup(cr => cr.Container).Returns(containerMock.Object);
            databaseMock.Setup(db => db.CreateContainerIfNotExistsAsync(
                It.IsAny<string>(), It.IsAny<string>(), null, null, CancellationToken.None))
                .ReturnsAsync(containerResponseMock.Object);
            var manager = new SocialsContainerManager(databaseMock.Object);
            var container = await manager.EnsureContainerAsync();
            Assert.NotNull(container);
            Assert.Equal(containerMock.Object, container);
        }

        [Fact]
        public void ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new SocialsContainerManager((Database)null!));
        }
    }
}
