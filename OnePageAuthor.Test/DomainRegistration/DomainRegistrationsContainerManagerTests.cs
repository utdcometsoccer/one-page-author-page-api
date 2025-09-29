using Moq;
using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test.DomainRegistration
{
    public class DomainRegistrationsContainerManagerTests
    {
        private readonly Mock<Database> _databaseMock;
        private readonly DomainRegistrationsContainerManager _containerManager;

        public DomainRegistrationsContainerManagerTests()
        {
            _databaseMock = new Mock<Database>();
            _containerManager = new DomainRegistrationsContainerManager(_databaseMock.Object);
        }

        [Fact]
        public async Task EnsureContainerAsync_CreatesContainerWithCorrectSettings()
        {
            // Arrange
            var mockContainer = new Mock<Container>();
            var mockResponse = new Mock<ContainerResponse>();
            
            mockResponse.Setup(r => r.Container).Returns(mockContainer.Object);
            
            _databaseMock.Setup(d => d.CreateContainerIfNotExistsAsync(
                "DomainRegistrations",
                "/upn",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _containerManager.EnsureContainerAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mockContainer.Object, result);
            
            _databaseMock.Verify(d => d.CreateContainerIfNotExistsAsync(
                "DomainRegistrations",
                "/upn",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_ThrowsException_NullDatabase()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationsContainerManager(null!));
        }
    }
}