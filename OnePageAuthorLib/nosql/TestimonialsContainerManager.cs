using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for Testimonial entities.
    /// </summary>
    public class TestimonialsContainerManager : IContainerManager<Testimonial>
    {
        private readonly Database _database;
        private readonly string _containerName = "Testimonials";

        /// <summary>
        /// Initializes a new instance of the TestimonialsContainerManager class.
        /// </summary>
        /// <param name="database">The Azure Cosmos DB database.</param>
        public TestimonialsContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "TestimonialsContainerManager: The provided Database is null.");
            _database = database;
        }

        /// <summary>
        /// Ensures the Testimonials container exists, creates it if it does not.
        /// </summary>
        /// <returns>The Cosmos DB container for Testimonials.</returns>
        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/Locale"
            );
            return containerResponse.Container;
        }
    }
}
