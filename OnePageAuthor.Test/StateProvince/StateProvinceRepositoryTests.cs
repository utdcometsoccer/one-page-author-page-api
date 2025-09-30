using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test.StateProvince
{
    public class StateProvinceRepositoryTests
    {
        private readonly Mock<Container> _containerMock;
        private readonly StateProvinceRepository _repository;

        public StateProvinceRepositoryTests()
        {
            _containerMock = new Mock<Container>();
            _repository = new StateProvinceRepository(_containerMock.Object);
        }

        private static InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince CreateTestStateProvince()
        {
            return new InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince
            {
                id = "us-ca-123",
                Code = "US-CA",
                Name = "California"
            };
        }

        [Fact]
        public async Task GetByCodeAsync_WithValidCode_ReturnsStateProvince()
        {
            // Arrange
            var testStateProvince = CreateTestStateProvince();
            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>>();
            var mockResponse = new LocalFeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>(
                new[] { testStateProvince });

            mockIterator.SetupSequence(x => x.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(x => x.ReadNextAsync(default))
                       .ReturnsAsync(mockResponse);

            _containerMock.Setup(x => x.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByCodeAsync("US-CA");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("US-CA", result!.Code);
            Assert.Equal("California", result.Name);
        }

        [Fact]
        public async Task GetByCodeAsync_WithNullOrEmptyCode_ReturnsNull()
        {
            // Act & Assert
            Assert.Null(await _repository.GetByCodeAsync(null!));
            Assert.Null(await _repository.GetByCodeAsync(""));
            Assert.Null(await _repository.GetByCodeAsync("   "));
        }

        [Fact]
        public async Task GetByNameAsync_WithValidName_ReturnsMatchingStates()
        {
            // Arrange
            var testStates = new[]
            {
                new InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince { id = "1", Code = "US-CA", Name = "California" },
                new InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince { id = "2", Code = "US-SC", Name = "South Carolina" }
            };

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>>();
            var mockResponse = new LocalFeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>(testStates);

            mockIterator.SetupSequence(x => x.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(x => x.ReadNextAsync(default))
                       .ReturnsAsync(mockResponse);

            _containerMock.Setup(x => x.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByNameAsync("Carolin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByNameAsync_WithNullOrEmptyName_ReturnsEmptyList()
        {
            // Act
            var result1 = await _repository.GetByNameAsync(null!);
            var result2 = await _repository.GetByNameAsync("");
            var result3 = await _repository.GetByNameAsync("   ");

            // Assert
            Assert.Empty(result1);
            Assert.Empty(result2);
            Assert.Empty(result3);
        }

        [Fact]
        public async Task GetByCountryAsync_WithValidCountryCode_ReturnsStatesForCountry()
        {
            // Arrange
            var testStates = new[]
            {
                new InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince { id = "1", Code = "US-CA", Name = "California" },
                new InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince { id = "2", Code = "US-TX", Name = "Texas" },
                new InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince { id = "3", Code = "US-NY", Name = "New York" }
            };

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>>();
            var mockResponse = new LocalFeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>(testStates);

            mockIterator.SetupSequence(x => x.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(x => x.ReadNextAsync(default))
                       .ReturnsAsync(mockResponse);

            _containerMock.Setup(x => x.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.StateProvince>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByCountryAsync("US");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, state => Assert.StartsWith("US-", state.Code));
        }

        [Fact]
        public async Task GetByCountryAsync_WithNullOrEmptyCountryCode_ReturnsEmptyList()
        {
            // Act
            var result1 = await _repository.GetByCountryAsync(null!);
            var result2 = await _repository.GetByCountryAsync("");
            var result3 = await _repository.GetByCountryAsync("   ");

            // Assert
            Assert.Empty(result1);
            Assert.Empty(result2);
            Assert.Empty(result3);
        }

        [Fact]
        public async Task ExistsByCodeAsync_WithExistingCode_ReturnsTrue()
        {
            // Arrange
            var mockIterator = new Mock<FeedIterator<int>>();
            var mockResponse = new LocalFeedResponse<int>(new[] { 1 });

            mockIterator.SetupSequence(x => x.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(x => x.ReadNextAsync(default))
                       .ReturnsAsync(mockResponse);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(mockIterator.Object);

            // Act
            var result = await _repository.ExistsByCodeAsync("US-CA");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsByCodeAsync_WithNonExistentCode_ReturnsFalse()
        {
            // Arrange
            var mockIterator = new Mock<FeedIterator<int>>();
            var mockResponse = new LocalFeedResponse<int>(new[] { 0 });

            mockIterator.SetupSequence(x => x.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(x => x.ReadNextAsync(default))
                       .ReturnsAsync(mockResponse);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(mockIterator.Object);

            // Act
            var result = await _repository.ExistsByCodeAsync("INVALID");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExistsByCodeAsync_WithNullOrEmptyCode_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(await _repository.ExistsByCodeAsync(null!));
            Assert.False(await _repository.ExistsByCodeAsync(""));
            Assert.False(await _repository.ExistsByCodeAsync("   "));
        }
    }
}