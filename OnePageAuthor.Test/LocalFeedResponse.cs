using Microsoft.Azure.Cosmos;
using System.Collections.ObjectModel;
using System.Net;

namespace OnePageAuthor.Test
{
    public class LocalFeedResponse<T> : FeedResponse<T>
    {
        private readonly IReadOnlyList<T> _resource;

        public LocalFeedResponse(IEnumerable<T> items)
        {
            _resource = new ReadOnlyCollection<T>(items is IList<T> list ? list : new List<T>(items));
        }

        public override IReadOnlyList<T> Resource => _resource;
    public override string ContinuationToken => string.Empty;
        public override double RequestCharge => 0;
        public override Headers Headers => new Headers();
        public override int Count => _resource.Count;

        public override string IndexMetrics => throw new NotImplementedException();

        public override HttpStatusCode StatusCode => throw new NotImplementedException();

        public override CosmosDiagnostics Diagnostics => throw new NotImplementedException();

        public override IEnumerator<T> GetEnumerator() => _resource.GetEnumerator();
    }
}
