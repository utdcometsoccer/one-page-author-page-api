using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace OnePageAuthor.Test
{
    public class SocialsContainerManagerTests
    {
        [Fact]
        public void ThrowsOnNullArguments()
        {            
            Assert.Throws<ArgumentNullException>(() => new SocialsContainerManager((Database)null!));
        }
    }
}
