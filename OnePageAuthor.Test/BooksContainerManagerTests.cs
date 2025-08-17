using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class BooksContainerManagerTests
    {
        [Fact]
        public void BooksContainerManager_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new BooksContainerManager((CosmosClient)null!, new Mock<Database>().Object));
            Assert.Throws<ArgumentNullException>(() => new BooksContainerManager(new Mock<CosmosClient>().Object, (Database)null!));
        }

        [Fact]
        public async Task EnsureContainerAsync_ReturnsContainer()
        {
            var cosmosClientMock = new Mock<CosmosClient>();
            var databaseMock = new Mock<Database>();
            var containerMock = new Mock<Container>();
            var containerResponseMock = new Mock<ContainerResponse>();
            containerResponseMock.Setup(cr => cr.Container).Returns(containerMock.Object);
            databaseMock.Setup(db => db.CreateContainerIfNotExistsAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                null, 
                null, 
                CancellationToken.None))
                .ReturnsAsync(containerResponseMock.Object);
            var manager = new BooksContainerManager(cosmosClientMock.Object, databaseMock.Object);
            var container = await manager.EnsureContainerAsync();
            Assert.NotNull(container);
            Assert.Equal(containerMock.Object, container);
        }

        [Fact]
        public void Properties_AreInitializedCorrectly()
        {
            var cosmosClientMock = new Mock<CosmosClient>().Object;
            var databaseMock = new Mock<Database>().Object;
            var manager = new BooksContainerManager(cosmosClientMock, databaseMock);
            Assert.NotNull(manager);
        }
    }
}
