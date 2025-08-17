using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class ArticlesContainerManagerTests
    {
        [Fact]
        public void ThrowsOnNullArguments()
        {
           Assert.Throws<ArgumentNullException>(() => new ArticlesContainerManager((Database)null!));
        }
    }
}
