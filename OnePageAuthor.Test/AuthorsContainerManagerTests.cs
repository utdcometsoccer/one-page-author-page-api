using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class AuthorsContainerManagerTests
    {

        [Fact]
        public void ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthorsContainerManager( (Database)null!));
        }
    }
}
