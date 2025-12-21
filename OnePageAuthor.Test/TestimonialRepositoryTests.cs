using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test
{
    public class TestimonialRepositoryTests
    {
        private class LocalFeedIterator<T> : FeedIterator<T>
        {
            private readonly List<T> _items;
            private int _currentIndex = 0;
            private readonly int _pageSize;
            public LocalFeedIterator(List<T> items, int pageSize = 100)
            {
                _items = items;
                _pageSize = pageSize;
            }
            public override bool HasMoreResults => _currentIndex < _items.Count;
            public override Task<FeedResponse<T>> ReadNextAsync(CancellationToken cancellationToken = default)
            {
                var page = _items.Skip(_currentIndex).Take(_pageSize).ToList();
                _currentIndex += page.Count;
                FeedResponse<T> response = new LocalFeedResponse<T>(page);
                return Task.FromResult(response);
            }
        }

        private class TestGetByIdAsyncDataContainer : IDataContainer
        {
            private readonly List<Testimonial> _testimonials;
            private readonly string _idToReturn;
            public TestGetByIdAsyncDataContainer(List<Testimonial> testimonials, string idToReturn)
            {
                _testimonials = testimonials;
                _idToReturn = idToReturn;
            }
            public FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition query, string? continuationToken = null, QueryRequestOptions? requestOptions = null)
            {
                var filtered = _testimonials.Where(t => t.id == _idToReturn).Cast<T>().ToList();
                return new LocalFeedIterator<T>(filtered);
            }
            public Task<T> ReadItemAsync<T>(string id, PartitionKey partitionKey)
            {
                var item = _testimonials.Where(t => t.id == _idToReturn).Cast<T>().FirstOrDefault();
                return Task.FromResult((T)item!);
            }
            public Task<ItemResponse<T>> CreateItemAsync<T>(T item) => throw new NotImplementedException();
            public Task<ItemResponse<T>> CreateItemAsync<T>(T item, PartitionKey partitionKey) => throw new NotImplementedException();
            public Task<ItemResponse<T>> ReplaceItemAsync<T>(T item, string id, PartitionKey partitionKey) => throw new NotImplementedException();
            public Task DeleteItemAsync<T>(string id, PartitionKey partitionKey) => throw new NotImplementedException();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsTestimonial()
        {
            var testimonialId = Guid.NewGuid().ToString();
            var testimonial1 = new Testimonial 
            { 
                id = testimonialId, 
                AuthorName = "John Doe",
                Quote = "Great service!",
                Rating = 5,
                Locale = "en-US"
            };
            var testimonial2 = new Testimonial 
            { 
                id = Guid.NewGuid().ToString(), 
                AuthorName = "Jane Smith",
                Quote = "Amazing!",
                Rating = 4,
                Locale = "en-US"
            };

            var dataContainer = new TestGetByIdAsyncDataContainer(new List<Testimonial> { testimonial1, testimonial2 }, testimonial1.id);
            var repo = new TestimonialRepository(dataContainer);
            var result = await repo.GetByIdAsync(testimonial1.id);

            Assert.NotNull(result);
            Assert.Equal(testimonial1.AuthorName, result.AuthorName);
            Assert.Equal(testimonial1.Quote, result.Quote);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            var testimonials = new List<Testimonial>();
            var dataContainer = new TestGetByIdAsyncDataContainer(testimonials, "nonexistent-id");
            var repo = new TestimonialRepository(dataContainer);

            var result = await repo.GetByIdAsync("nonexistent-id");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetTestimonialsAsync_ReturnsAllTestimonials()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var testimonials = new List<Testimonial>
            {
                new Testimonial { AuthorName = "John Doe", Quote = "Great!", Rating = 5, Locale = "en-US", Featured = true },
                new Testimonial { AuthorName = "Jane Smith", Quote = "Amazing!", Rating = 4, Locale = "en-US", Featured = false }
            };

            var iteratorMock = new Mock<FeedIterator<Testimonial>>();
            var responseMock = new Mock<FeedResponse<Testimonial>>();
            responseMock.Setup(r => r.Resource).Returns(testimonials);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);

            cosmosMock.Setup(c => c.GetItemQueryIterator<Testimonial>(It.IsAny<QueryDefinition>(), null, null))
                .Returns(iteratorMock.Object);

            var repo = new TestimonialRepository(cosmosMock.Object);
            var (results, total) = await repo.GetTestimonialsAsync(10);

            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal(2, total);
        }

        [Fact]
        public async Task GetTestimonialsAsync_FiltersByFeatured()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var testimonials = new List<Testimonial>
            {
                new Testimonial { AuthorName = "John Doe", Quote = "Great!", Rating = 5, Locale = "en-US", Featured = true }
            };

            var iteratorMock = new Mock<FeedIterator<Testimonial>>();
            var responseMock = new Mock<FeedResponse<Testimonial>>();
            responseMock.Setup(r => r.Resource).Returns(testimonials);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);

            cosmosMock.Setup(c => c.GetItemQueryIterator<Testimonial>(It.IsAny<QueryDefinition>(), null, null))
                .Returns(iteratorMock.Object);

            var repo = new TestimonialRepository(cosmosMock.Object);
            var (results, total) = await repo.GetTestimonialsAsync(10, featured: true);

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.True(results[0].Featured);
        }

        [Fact]
        public async Task GetTestimonialsAsync_FiltersByLocale()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var testimonials = new List<Testimonial>
            {
                new Testimonial { AuthorName = "Juan Pérez", Quote = "Excelente!", Rating = 5, Locale = "es-ES", Featured = false }
            };

            var iteratorMock = new Mock<FeedIterator<Testimonial>>();
            var responseMock = new Mock<FeedResponse<Testimonial>>();
            responseMock.Setup(r => r.Resource).Returns(testimonials);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);

            cosmosMock.Setup(c => c.GetItemQueryIterator<Testimonial>(It.IsAny<QueryDefinition>(), null, null))
                .Returns(iteratorMock.Object);

            var repo = new TestimonialRepository(cosmosMock.Object);
            var (results, total) = await repo.GetTestimonialsAsync(10, locale: "es-ES");

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal("es-ES", results[0].Locale);
        }

        [Fact]
        public async Task GetTestimonialsAsync_RespectsLimit()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var testimonials = new List<Testimonial>
            {
                new Testimonial { AuthorName = "User 1", Quote = "Quote 1", Rating = 5, Locale = "en-US" },
                new Testimonial { AuthorName = "User 2", Quote = "Quote 2", Rating = 5, Locale = "en-US" },
                new Testimonial { AuthorName = "User 3", Quote = "Quote 3", Rating = 5, Locale = "en-US" }
            };

            var iteratorMock = new Mock<FeedIterator<Testimonial>>();
            var responseMock = new Mock<FeedResponse<Testimonial>>();
            responseMock.Setup(r => r.Resource).Returns(testimonials);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);

            cosmosMock.Setup(c => c.GetItemQueryIterator<Testimonial>(It.IsAny<QueryDefinition>(), null, null))
                .Returns(iteratorMock.Object);

            var repo = new TestimonialRepository(cosmosMock.Object);
            var (results, total) = await repo.GetTestimonialsAsync(2);

            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, total);
        }

        [Fact]
        public async Task CreateAsync_CreatesTestimonial()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var testimonial = new Testimonial
            {
                AuthorName = "John Doe",
                Quote = "Great service!",
                Rating = 5,
                Locale = "en-US"
            };

            var responseMock = new Mock<ItemResponse<Testimonial>>();
            responseMock.Setup(r => r.Resource).Returns(testimonial);

            cosmosMock.Setup(c => c.CreateItemAsync(It.IsAny<Testimonial>()))
                .ReturnsAsync(responseMock.Object);

            var repo = new TestimonialRepository(cosmosMock.Object);
            var result = await repo.CreateAsync(testimonial);

            Assert.NotNull(result);
            Assert.Equal(testimonial.AuthorName, result.AuthorName);
            cosmosMock.Verify(c => c.CreateItemAsync(It.IsAny<Testimonial>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesTestimonial()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var testimonial = new Testimonial
            {
                id = Guid.NewGuid().ToString(),
                AuthorName = "John Doe",
                Quote = "Updated quote",
                Rating = 4,
                Locale = "en-US"
            };

            var responseMock = new Mock<ItemResponse<Testimonial>>();
            responseMock.Setup(r => r.Resource).Returns(testimonial);

            cosmosMock.Setup(c => c.ReplaceItemAsync(It.IsAny<Testimonial>(), It.IsAny<string>(), It.IsAny<PartitionKey>()))
                .ReturnsAsync(responseMock.Object);

            var repo = new TestimonialRepository(cosmosMock.Object);
            var result = await repo.UpdateAsync(testimonial);

            Assert.NotNull(result);
            Assert.Equal("Updated quote", result.Quote);
            cosmosMock.Verify(c => c.ReplaceItemAsync(It.IsAny<Testimonial>(), It.IsAny<string>(), It.IsAny<PartitionKey>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_DeletesTestimonial()
        {
            var testimonialId = Guid.NewGuid().ToString();
            var testimonial = new Testimonial
            {
                id = testimonialId,
                AuthorName = "John Doe",
                Quote = "Great!",
                Rating = 5,
                Locale = "en-US"
            };

            // Setup for GetByIdAsync call
            var dataContainer = new TestGetByIdAsyncDataContainer(new List<Testimonial> { testimonial }, testimonialId);
            
            // Setup mock for delete
            var cosmosMock = new Mock<IDataContainer>();
            cosmosMock.Setup(c => c.GetItemQueryIterator<Testimonial>(It.IsAny<QueryDefinition>(), null, null))
                .Returns(dataContainer.GetItemQueryIterator<Testimonial>(new QueryDefinition("SELECT * FROM c"), null, null));

            var responseMock = new Mock<ItemResponse<Testimonial>>();
            cosmosMock.Setup(c => c.DeleteItemAsync<Testimonial>(It.IsAny<string>(), It.IsAny<PartitionKey>()))
                .Returns(Task.CompletedTask);

            var repo = new TestimonialRepository(cosmosMock.Object);
            var result = await repo.DeleteAsync(testimonialId);

            Assert.True(result);
            cosmosMock.Verify(c => c.DeleteItemAsync<Testimonial>(It.IsAny<string>(), It.IsAny<PartitionKey>()), Times.Once);
        }
    }
}
