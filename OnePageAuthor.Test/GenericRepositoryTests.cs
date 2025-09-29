using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test
{
    public class GenericRepositoryTests
    {
        [Fact]
        public void ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new GenericRepository<Book>((IDataContainer)null!));
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsEntity()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var bookId = Guid.NewGuid();
            var book = new Book
            {
                id = bookId.ToString(),
                AuthorID = Guid.NewGuid().ToString(),
                Title = "Test Book",
                Description = "Desc",
                URL = new System.Uri("https://example.com"),
                Cover = new System.Uri("https://example.com/cover.jpg")
            };
            cosmosMock.Setup(c => c.ReadItemAsync<Book>(It.IsAny<string>(), It.IsAny<PartitionKey>())).Returns(Task.FromResult(book));
            var repo = new GenericRepository<Book>(cosmosMock.Object);
            var result = await repo.GetByIdAsync(bookId);
            cosmosMock.Verify(c => c.ReadItemAsync<Book>(It.IsAny<string>(), It.IsAny<PartitionKey>()), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(book.Title, result.Title);
        }

        [Fact]
        public async Task GetByAuthorIdAsync_ReturnsEntities()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var books = new List<Book> {
                new Book {
                    id = Guid.NewGuid().ToString(),
                    AuthorID = Guid.NewGuid().ToString(),
                    Title = "Book1",
                    Description = "Desc1",
                    URL = new System.Uri("https://example.com/1"),
                    Cover = new System.Uri("https://example.com/cover1.jpg")
                },
                new Book {
                    id = Guid.NewGuid().ToString(),
                    AuthorID = Guid.NewGuid().ToString(),
                    Title = "Book2",
                    Description = "Desc2",
                    URL = new System.Uri("https://example.com/2"),
                    Cover = new System.Uri("https://example.com/cover2.jpg")
                }
            };
            var iteratorMock = new Mock<FeedIterator<Book>>();
            var responseMock = new Mock<FeedResponse<Book>>();
            responseMock.Setup(r => r.Resource).Returns(books);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(responseMock.Object);
            cosmosMock.Setup(c => c.GetItemQueryIterator<Book>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new GenericRepository<Book>(cosmosMock.Object);
            var result = await repo.GetByAuthorIdAsync(new Guid(books[0].AuthorID));
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }
    }
}
