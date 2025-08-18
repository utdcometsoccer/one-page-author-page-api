using Microsoft.Azure.Cosmos;

namespace OnePageAuthor.Test
{
    public partial class LocaleRepositoryTests
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
    }
}
