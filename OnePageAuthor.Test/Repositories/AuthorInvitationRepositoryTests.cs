using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;

namespace OnePageAuthor.Test.Repositories
{
    /// <summary>
    /// Unit tests for AuthorInvitationRepository.
    /// </summary>
    public class AuthorInvitationRepositoryTests
    {
        private readonly Mock<IDataContainer> _mockContainer;
        private readonly AuthorInvitationRepository _repository;

        public AuthorInvitationRepositoryTests()
        {
            _mockContainer = new Mock<IDataContainer>();
            _repository = new AuthorInvitationRepository(_mockContainer.Object);
        }

        [Fact]
        public void Constructor_WithNullContainer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AuthorInvitationRepository((IDataContainer)null!));
        }

        [Fact]
        public async Task AddAsync_WithValidInvitation_ShouldCreateInvitation()
        {
            // Arrange
            var invitation = new AuthorInvitation("test@example.com", "example.com");
            var expectedInvitation = new AuthorInvitation("test@example.com", "example.com")
            {
                id = invitation.id
            };

            var mockResponse = new Mock<ItemResponse<AuthorInvitation>>();
            mockResponse.Setup(r => r.Resource).Returns(expectedInvitation);

            _mockContainer
                .Setup(c => c.CreateItemAsync(
                    It.IsAny<AuthorInvitation>(), 
                    It.IsAny<PartitionKey>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _repository.AddAsync(invitation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.EmailAddress);
            Assert.Equal("example.com", result.DomainName);
            Assert.Equal("Pending", result.Status);
            
            _mockContainer.Verify(c => c.CreateItemAsync(
                It.Is<AuthorInvitation>(i => i.EmailAddress == "test@example.com"),
                It.Is<PartitionKey>(pk => pk.Equals(new PartitionKey("test@example.com")))), 
                Times.Once);
        }

        [Fact]
        public async Task AddAsync_WithoutEmailAddress_ThrowsInvalidOperationException()
        {
            // Arrange
            var invitation = new AuthorInvitation
            {
                EmailAddress = string.Empty,
                DomainName = "example.com"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _repository.AddAsync(invitation));
        }

        [Fact]
        public void AuthorInvitation_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var invitation = new AuthorInvitation("test@example.com", "example.com");

            // Assert
            Assert.NotNull(invitation.id);
            Assert.NotEmpty(invitation.id);
            Assert.Equal("test@example.com", invitation.EmailAddress);
            Assert.Equal("example.com", invitation.DomainName);
            Assert.Equal("Pending", invitation.Status);
            Assert.Null(invitation.AcceptedAt);
            Assert.Null(invitation.UserOid);
            Assert.True(invitation.CreatedAt <= DateTime.UtcNow);
            Assert.True(invitation.ExpiresAt > DateTime.UtcNow);
            Assert.True((invitation.ExpiresAt - invitation.CreatedAt).TotalDays >= 29);
        }

        [Fact]
        public void AuthorInvitation_WithNotes_ShouldStoreNotes()
        {
            // Arrange & Act
            var invitation = new AuthorInvitation("test@example.com", "example.com", "Test notes");

            // Assert
            Assert.Equal("Test notes", invitation.Notes);
        }

        [Fact]
        public async Task UpdateAsync_WithoutId_ThrowsInvalidOperationException()
        {
            // Arrange
            var invitation = new AuthorInvitation("test@example.com", "example.com")
            {
                id = string.Empty
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _repository.UpdateAsync(invitation));
        }

        [Fact]
        public async Task UpdateAsync_WithoutEmailAddress_ThrowsInvalidOperationException()
        {
            // Arrange
            var invitation = new AuthorInvitation("test@example.com", "example.com")
            {
                EmailAddress = string.Empty
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _repository.UpdateAsync(invitation));
        }

        [Fact]
        public void AuthorInvitation_StatusTransitions_ShouldWork()
        {
            // Arrange
            var invitation = new AuthorInvitation("test@example.com", "example.com");
            
            // Act - Accept invitation
            invitation.Status = "Accepted";
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.UserOid = "test-oid-123";

            // Assert
            Assert.Equal("Accepted", invitation.Status);
            Assert.NotNull(invitation.AcceptedAt);
            Assert.NotNull(invitation.UserOid);
            Assert.Equal("test-oid-123", invitation.UserOid);
        }
    }
}
