using Moq;
using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using System.Net;

namespace OnePageAuthor.Test.DomainRegistration
{
    public class DomainRegistrationRepositoryTests
    {
        private readonly Mock<IDataContainer> _containerMock;
        private readonly DomainRegistrationRepository _repository;

        public DomainRegistrationRepositoryTests()
        {
            _containerMock = new Mock<IDataContainer>();
            _repository = new DomainRegistrationRepository(_containerMock.Object);
        }

        private static InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration CreateTestDomainRegistration(string id = "test-id")
        {
            return new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = id,
                Upn = "test@example.com",
                Domain = new Domain
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example"
                },
                ContactInformation = new ContactInformation
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Address = "123 Main St",
                    City = "Anytown",
                    State = "CA",
                    Country = "USA",
                    ZipCode = "12345",
                    EmailAddress = "john@example.com",
                    TelephoneNumber = "+1-555-123-4567"
                },
                CreatedAt = DateTime.UtcNow,
                Status = DomainRegistrationStatus.Pending
            };
        }

        [Fact]
        public async Task CreateAsync_Success_GeneratesIdWhenNull()
        {
            // Arrange
            var domainRegistration = CreateTestDomainRegistration();
            domainRegistration.id = null;

            var response = new Mock<ItemResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            var createdRegistration = CreateTestDomainRegistration("generated-id");
            response.Setup(r => r.Resource).Returns(createdRegistration);

            _containerMock.Setup(c => c.CreateItemAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(), It.IsAny<PartitionKey>()))
                         .ReturnsAsync(response.Object);

            // Act
            var result = await _repository.CreateAsync(domainRegistration);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.id);
            Assert.Equal("test@example.com", result.Upn);
            _containerMock.Verify(c => c.CreateItemAsync(It.Is<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(dr => 
                !string.IsNullOrWhiteSpace(dr.id)), It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_Success_PreservesExistingId()
        {
            // Arrange
            var domainRegistration = CreateTestDomainRegistration("existing-id");

            var response = new Mock<ItemResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            response.Setup(r => r.Resource).Returns(domainRegistration);

            _containerMock.Setup(c => c.CreateItemAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(), It.IsAny<PartitionKey>()))
                         .ReturnsAsync(response.Object);

            // Act
            var result = await _repository.CreateAsync(domainRegistration);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("existing-id", result.id);
            _containerMock.Verify(c => c.CreateItemAsync(It.Is<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(dr => 
                dr.id == "existing-id"), It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ThrowsException_MissingUpn()
        {
            // Arrange
            var domainRegistration = CreateTestDomainRegistration();
            domainRegistration.Upn = "";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.CreateAsync(domainRegistration));
        }

        [Fact]
        public async Task GetByIdAsync_Success_ReturnsRegistration()
        {
            // Arrange
            var id = "test-id";
            var upn = "test@example.com";
            var expectedRegistration = CreateTestDomainRegistration(id);

            _containerMock.Setup(c => c.ReadItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(id, new PartitionKey(upn)))
                         .ReturnsAsync(expectedRegistration);

            // Act
            var result = await _repository.GetByIdAsync(id, upn);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.id);
            Assert.Equal(upn, result.Upn);
            _containerMock.Verify(c => c.ReadItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(id, new PartitionKey(upn)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_NotFound()
        {
            // Arrange
            var id = "non-existent-id";
            var upn = "test@example.com";

            _containerMock.Setup(c => c.ReadItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(id, new PartitionKey(upn)))
                         .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

            // Act
            var result = await _repository.GetByIdAsync(id, upn);

            // Assert
            Assert.Null(result);
            _containerMock.Verify(c => c.ReadItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(id, new PartitionKey(upn)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_EmptyId()
        {
            // Arrange
            var id = "";
            var upn = "test@example.com";

            // Act
            var result = await _repository.GetByIdAsync(id, upn);

            // Assert
            Assert.Null(result);
            _containerMock.Verify(c => c.ReadItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<string>(), It.IsAny<PartitionKey>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_EmptyUpn()
        {
            // Arrange
            var id = "test-id";
            var upn = "";

            // Act
            var result = await _repository.GetByIdAsync(id, upn);

            // Assert
            Assert.Null(result);
            _containerMock.Verify(c => c.ReadItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<string>(), It.IsAny<PartitionKey>()), Times.Never);
        }

        [Fact]
        public async Task GetByUserAsync_Success_ReturnsRegistrations()
        {
            // Arrange
            var upn = "test@example.com";
            var registrations = new List<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>
            {
                CreateTestDomainRegistration("reg-1"),
                CreateTestDomainRegistration("reg-2")
            };

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            var mockResponse = new Mock<FeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();

            mockResponse.Setup(r => r.Resource).Returns(registrations);
            mockIterator.SetupSequence(i => i.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(mockResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                         .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByUserAsync(upn);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, r => Assert.Equal(upn, r.Upn));
        }

        [Fact]
        public async Task GetByUserAsync_ReturnsEmpty_EmptyUpn()
        {
            // Arrange
            var upn = "";

            // Act
            var result = await _repository.GetByUserAsync(upn);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _containerMock.Verify(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_Success_ReturnsUpdatedRegistration()
        {
            // Arrange
            var domainRegistration = CreateTestDomainRegistration();
            domainRegistration.Status = DomainRegistrationStatus.Completed;

            var response = new Mock<ItemResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            response.Setup(r => r.Resource).Returns(domainRegistration);

            _containerMock.Setup(c => c.ReplaceItemAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(), It.IsAny<string>(), It.IsAny<PartitionKey>()))
                         .ReturnsAsync(response.Object);

            // Act
            var result = await _repository.UpdateAsync(domainRegistration);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DomainRegistrationStatus.Completed, result.Status);
            _containerMock.Verify(c => c.ReplaceItemAsync(domainRegistration, domainRegistration.id!, new PartitionKey(domainRegistration.Upn)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsException_MissingId()
        {
            // Arrange
            var domainRegistration = CreateTestDomainRegistration();
            domainRegistration.id = "";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateAsync(domainRegistration));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsException_MissingUpn()
        {
            // Arrange
            var domainRegistration = CreateTestDomainRegistration();
            domainRegistration.Upn = "";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateAsync(domainRegistration));
        }

        [Fact]
        public async Task DeleteAsync_Success_ReturnsTrue()
        {
            // Arrange
            var id = "test-id";
            var upn = "test@example.com";

            _containerMock.Setup(c => c.DeleteItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(id, new PartitionKey(upn)))
                         .Returns(Task.CompletedTask);

            // Act
            var result = await _repository.DeleteAsync(id, upn);

            // Assert
            Assert.True(result);
            _containerMock.Verify(c => c.DeleteItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(id, new PartitionKey(upn)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_NotFound()
        {
            // Arrange
            var id = "non-existent-id";
            var upn = "test@example.com";

            _containerMock.Setup(c => c.DeleteItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(id, new PartitionKey(upn)))
                         .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 0));

            // Act
            var result = await _repository.DeleteAsync(id, upn);

            // Assert
            Assert.False(result);
            _containerMock.Verify(c => c.DeleteItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(id, new PartitionKey(upn)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_EmptyId()
        {
            // Arrange
            var id = "";
            var upn = "test@example.com";

            // Act
            var result = await _repository.DeleteAsync(id, upn);

            // Assert
            Assert.False(result);
            _containerMock.Verify(c => c.DeleteItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<string>(), It.IsAny<PartitionKey>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_EmptyUpn()
        {
            // Arrange
            var id = "test-id";
            var upn = "";

            // Act
            var result = await _repository.DeleteAsync(id, upn);

            // Assert
            Assert.False(result);
            _containerMock.Verify(c => c.DeleteItemAsync<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<string>(), It.IsAny<PartitionKey>()), Times.Never);
        }

        [Fact]
        public async Task GetByDomainAsync_Success_ReturnsRegistration()
        {
            // Arrange
            var topLevelDomain = "com";
            var secondLevelDomain = "example";
            var expectedRegistration = CreateTestDomainRegistration("test-id");

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            var mockResponse = new Mock<FeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();

            mockResponse.Setup(r => r.Resource).Returns(new List<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration> { expectedRegistration });
            mockIterator.SetupSequence(i => i.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(mockResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                         .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByDomainAsync(topLevelDomain, secondLevelDomain);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-id", result.id);
            Assert.Equal("com", result.Domain.TopLevelDomain);
            Assert.Equal("example", result.Domain.SecondLevelDomain);
        }

        [Fact]
        public async Task GetByDomainAsync_ReturnsNull_NotFound()
        {
            // Arrange
            var topLevelDomain = "com";
            var secondLevelDomain = "nonexistent";

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            var mockResponse = new Mock<FeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();

            mockResponse.Setup(r => r.Resource).Returns(new List<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>());
            mockIterator.SetupSequence(i => i.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(mockResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                         .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByDomainAsync(topLevelDomain, secondLevelDomain);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByDomainAsync_ReturnsNull_EmptyTopLevelDomain()
        {
            // Arrange
            var topLevelDomain = "";
            var secondLevelDomain = "example";

            // Act
            var result = await _repository.GetByDomainAsync(topLevelDomain, secondLevelDomain);

            // Assert
            Assert.Null(result);
            _containerMock.Verify(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()), Times.Never);
        }

        [Fact]
        public async Task GetByDomainAsync_ReturnsNull_EmptySecondLevelDomain()
        {
            // Arrange
            var topLevelDomain = "com";
            var secondLevelDomain = "";

            // Act
            var result = await _repository.GetByDomainAsync(topLevelDomain, secondLevelDomain);

            // Assert
            Assert.Null(result);
            _containerMock.Verify(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()), Times.Never);
        }

        [Fact]
        public async Task GetByUserAsync_HandlesNullResource_ReturnsEmpty()
        {
            // Arrange
            var upn = "test@example.com";

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            var mockResponse = new Mock<FeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();

            // Setup null Resource to test null handling
            mockResponse.Setup(r => r.Resource).Returns((IReadOnlyList<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>?)null!);
            mockIterator.SetupSequence(i => i.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(mockResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                         .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByUserAsync(upn);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByDomainAsync_HandlesNullResource_ReturnsNull()
        {
            // Arrange
            var topLevelDomain = "com";
            var secondLevelDomain = "example";

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            var mockResponse = new Mock<FeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();

            // Setup null Resource to test null handling
            mockResponse.Setup(r => r.Resource).Returns((IReadOnlyList<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>?)null!);
            mockIterator.SetupSequence(i => i.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(mockResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                         .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByDomainAsync(topLevelDomain, secondLevelDomain);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdCrossPartitionAsync_WithNullId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdCrossPartitionAsync(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdCrossPartitionAsync_WithEmptyId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdCrossPartitionAsync(string.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdCrossPartitionAsync_WhenFound_ReturnsDomainRegistration()
        {
            // Arrange
            var registration = CreateTestDomainRegistration("target-id");

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            var mockResponse = new Mock<FeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();

            mockResponse.Setup(r => r.Resource).Returns(new List<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration> { registration });
            mockIterator.SetupSequence(i => i.HasMoreResults)
                       .Returns(true)
                       .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(mockResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(
                It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                         .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByIdCrossPartitionAsync("target-id");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("target-id", result.id);
            Assert.Equal("test@example.com", result.Upn);
        }

        [Fact]
        public async Task GetByIdCrossPartitionAsync_WhenNoResults_ReturnsNull()
        {
            // Arrange
            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();

            mockIterator.Setup(i => i.HasMoreResults).Returns(false);

            _containerMock.Setup(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(
                It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                         .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByIdCrossPartitionAsync("non-existent-id");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdCrossPartitionAsync_ItemOnSecondPage_ReturnsDomainRegistration()
        {
            // Arrange – first page is empty (simulates a cross-partition gap), second page has the item
            var registration = CreateTestDomainRegistration("target-id");

            var mockIterator = new Mock<FeedIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();

            var emptyResponse = new Mock<FeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            emptyResponse.Setup(r => r.Resource)
                         .Returns(new List<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>());

            var itemResponse = new Mock<FeedResponse<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>>();
            itemResponse.Setup(r => r.Resource)
                        .Returns(new List<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration> { registration });

            // HasMoreResults: true (empty first page), true (second page with item), false (stop)
            mockIterator.SetupSequence(i => i.HasMoreResults)
                       .Returns(true)
                       .Returns(true)
                       .Returns(false);

            mockIterator.SetupSequence(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(emptyResponse.Object)
                       .ReturnsAsync(itemResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration>(
                It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                         .Returns(mockIterator.Object);

            // Act
            var result = await _repository.GetByIdCrossPartitionAsync("target-id");

            // Assert – item was found despite being on a subsequent page
            Assert.NotNull(result);
            Assert.Equal("target-id", result.id);
            Assert.Equal("test@example.com", result.Upn);
        }
    }
}