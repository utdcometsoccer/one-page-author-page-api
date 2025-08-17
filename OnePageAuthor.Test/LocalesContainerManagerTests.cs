using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class LocalesContainerManagerTests
    {
        [Fact]
        public void ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new LocalesContainerManager((Database)null!));
        }
    }
}
