using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test.Country
{
    public class CountriesContainerManagerTests
    {
        private readonly Mock<Database> _databaseMock;
        private readonly Mock<Container> _containerMock;
        private readonly CountriesContainerManager _containerManager;

        public CountriesContainerManagerTests()
        {
            _databaseMock = new Mock<Database>();
            _containerMock = new Mock<Container>();
            _containerManager = new CountriesContainerManager(_databaseMock.Object);
        }

        [Fact]
        public void Constructor_WithNullDatabase_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new CountriesContainerManager(null!));
            Assert.Equal("database", exception.ParamName);
        }

        [Fact]
        public async Task EnsureContainerAsync_CreatesContainerWithCorrectSettings()
        {
            // Arrange
            var containerResponse = new Mock<ContainerResponse>();
            containerResponse.SetupGet(x => x.Container).Returns(_containerMock.Object);
            
            _databaseMock.Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                null, 
                null, 
                CancellationToken.None))
                .ReturnsAsync(containerResponse.Object);

            // Act
            var result = await _containerManager.EnsureContainerAsync();

            // Assert
            Assert.Equal(_containerMock.Object, result);
            _databaseMock.Verify(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                null, 
                null, 
                CancellationToken.None), Times.Once);
        }
    }
}
