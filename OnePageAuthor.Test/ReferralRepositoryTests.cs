using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace OnePageAuthor.Test
{
    public class ReferralRepositoryTests
    {
        [Fact]
        public void Constructor_ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new ReferralRepository((IDataContainer)null!));
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsReferral()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referralId = Guid.NewGuid().ToString();
            var referrerId = "user-123";
            var referral = new Referral
            {
                id = referralId,
                ReferrerId = referrerId,
                ReferredEmail = "test@example.com",
                ReferralCode = "ABC12345",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            cosmosMock.Setup(c => c.ReadItemAsync<Referral>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()))
                .ReturnsAsync(referral);

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.GetByIdAsync(referralId, referrerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(referralId, result.id);
            Assert.Equal(referrerId, result.ReferrerId);
            Assert.Equal("test@example.com", result.ReferredEmail);
            cosmosMock.Verify(c => c.ReadItemAsync<Referral>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referralId = Guid.NewGuid().ToString();
            var referrerId = "user-123";

            cosmosMock.Setup(c => c.ReadItemAsync<Referral>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.GetByIdAsync(referralId, referrerId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByReferrerIdAsync_ReturnsReferrals()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referrerId = "user-123";
            var referrals = new List<Referral>
            {
                new Referral(referrerId, "test1@example.com", "CODE1"),
                new Referral(referrerId, "test2@example.com", "CODE2")
            };

            var iteratorMock = new Mock<FeedIterator<Referral>>();
            var responseMock = new Mock<FeedResponse<Referral>>();
            responseMock.Setup(r => r.Resource).Returns(referrals);

            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);

            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);

            cosmosMock.Setup(c => c.GetItemQueryIterator<Referral>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(iteratorMock.Object);

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.GetByReferrerIdAsync(referrerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(referrerId, r.ReferrerId));
        }

        [Fact]
        public async Task GetByReferralCodeAsync_ReturnsReferral()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referralCode = "ABC12345";
            var referral = new Referral("user-123", "test@example.com", referralCode);

            var iteratorMock = new Mock<FeedIterator<Referral>>();
            var responseMock = new Mock<FeedResponse<Referral>>();
            responseMock.Setup(r => r.Resource).Returns(new List<Referral> { referral });

            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true);

            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);

            cosmosMock.Setup(c => c.GetItemQueryIterator<Referral>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(iteratorMock.Object);

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.GetByReferralCodeAsync(referralCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(referralCode, result.ReferralCode);
        }

        [Fact]
        public async Task ExistsByReferrerAndEmailAsync_ReturnsTrue_WhenExists()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referrerId = "user-123";
            var email = "test@example.com";

            var iteratorMock = new Mock<FeedIterator<int>>();
            var responseMock = new Mock<FeedResponse<int>>();
            responseMock.Setup(r => r.Resource).Returns(new List<int> { 1 });

            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true);

            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);

            cosmosMock.Setup(c => c.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(iteratorMock.Object);

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.ExistsByReferrerAndEmailAsync(referrerId, email);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsByReferrerAndEmailAsync_ReturnsFalse_WhenNotExists()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referrerId = "user-123";
            var email = "test@example.com";

            var iteratorMock = new Mock<FeedIterator<int>>();
            var responseMock = new Mock<FeedResponse<int>>();
            responseMock.Setup(r => r.Resource).Returns(new List<int> { 0 });

            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true);

            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);

            cosmosMock.Setup(c => c.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), null, null))
                .Returns(iteratorMock.Object);

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.ExistsByReferrerAndEmailAsync(referrerId, email);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddAsync_CreatesReferral()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referral = new Referral("user-123", "test@example.com", "ABC12345");

            var mockResponse = new Mock<ItemResponse<Referral>>();
            mockResponse.Setup(r => r.Resource).Returns(referral);

            cosmosMock.Setup(c => c.CreateItemAsync<Referral>(
                It.IsAny<Referral>(),
                It.IsAny<PartitionKey>()))
                .ReturnsAsync(mockResponse.Object);

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.AddAsync(referral);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.id);
            cosmosMock.Verify(c => c.CreateItemAsync<Referral>(
                It.IsAny<Referral>(),
                It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ThrowsWhenReferrerIdMissing()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referral = new Referral
            {
                ReferrerId = "", // Empty referrerId
                ReferredEmail = "test@example.com",
                ReferralCode = "ABC12345"
            };

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddAsync(referral));
        }

        [Fact]
        public async Task UpdateAsync_UpdatesReferral()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referral = new Referral("user-123", "test@example.com", "ABC12345")
            {
                id = Guid.NewGuid().ToString(),
                Status = "Converted"
            };

            var mockResponse = new Mock<ItemResponse<Referral>>();
            mockResponse.Setup(r => r.Resource).Returns(referral);

            cosmosMock.Setup(c => c.ReplaceItemAsync<Referral>(
                It.IsAny<Referral>(),
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()))
                .ReturnsAsync(mockResponse.Object);

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.UpdateAsync(referral);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Converted", result.Status);
            cosmosMock.Verify(c => c.ReplaceItemAsync<Referral>(
                It.IsAny<Referral>(),
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_DeletesReferral()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referralId = Guid.NewGuid().ToString();
            var referrerId = "user-123";

            cosmosMock.Setup(c => c.DeleteItemAsync<Referral>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()))
                .Returns(Task.CompletedTask);

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.DeleteAsync(referralId, referrerId);

            // Assert
            Assert.True(result);
            cosmosMock.Verify(c => c.DeleteItemAsync<Referral>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            var cosmosMock = new Mock<IDataContainer>();
            var referralId = Guid.NewGuid().ToString();
            var referrerId = "user-123";

            cosmosMock.Setup(c => c.DeleteItemAsync<Referral>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            var repo = new ReferralRepository(cosmosMock.Object);

            // Act
            var result = await repo.DeleteAsync(referralId, referrerId);

            // Assert
            Assert.False(result);
        }
    }
}
