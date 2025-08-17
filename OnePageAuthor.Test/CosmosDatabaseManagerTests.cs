using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class CosmosDatabaseManagerTests
    {
        [Fact]
        public async Task EnsureDatabaseAsync_CallsCreateIfNotExists()
        {
            var cosmosClientMock = new Mock<CosmosClient>();
            var databaseMock = new Mock<Database>();
            var databaseResponseMock = new Mock<DatabaseResponse>();
            databaseResponseMock.Setup(dr => dr.Database).Returns(databaseMock.Object);
            cosmosClientMock.Setup(c => c.CreateDatabaseIfNotExistsAsync(It.IsAny<string>(), (int?)null, null, default))
                .ReturnsAsync(databaseResponseMock.Object);
            var manager = new CosmosDatabaseManager(cosmosClientMock.Object);
            var db = await manager.EnsureDatabaseAsync("ignored", "ignored", "TestDb");
            Assert.NotNull(db);
        }
    }
}
