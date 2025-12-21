using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using System.Net;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for Testimonial entities.
    /// </summary>
    public class TestimonialRepository : ITestimonialRepository
    {
        private readonly IDataContainer _container;

        public TestimonialRepository(Container container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "TestimonialRepository: The provided Cosmos DB container is null.");
            _container = new CosmosContainerWrapper(container);
        }

        public TestimonialRepository(IDataContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Gets a testimonial by its id.
        /// </summary>
        public async Task<Testimonial?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id cannot be null or empty.", nameof(id));

            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            using (var iterator = _container.GetItemQueryIterator<Testimonial>(query))
            {
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }
            }

            return null;
        }

        /// <summary>
        /// Gets testimonials with optional filtering.
        /// </summary>
        public async Task<(IList<Testimonial> testimonials, int total)> GetTestimonialsAsync(int limit = 5, bool? featured = null, string? locale = null)
        {
            // Enforce limits
            if (limit < 1) limit = 5;
            if (limit > 20) limit = 20;

            // Build query with filters
            var queryText = "SELECT * FROM c";
            var conditions = new List<string>();

            if (featured.HasValue && featured.Value)
            {
                conditions.Add("c.Featured = true");
            }

            if (!string.IsNullOrWhiteSpace(locale))
            {
                conditions.Add("c.Locale = @locale");
            }

            if (conditions.Count > 0)
            {
                queryText += " WHERE " + string.Join(" AND ", conditions);
            }

            queryText += " ORDER BY c.CreatedAt DESC";

            var query = new QueryDefinition(queryText);
            if (!string.IsNullOrWhiteSpace(locale))
            {
                query = query.WithParameter("@locale", locale);
            }

            var allResults = new List<Testimonial>();
            using (var iterator = _container.GetItemQueryIterator<Testimonial>(query))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    allResults.AddRange(response.Resource);
                }
            }

            var total = allResults.Count;
            var limitedResults = allResults.Take(limit).ToList();

            return (limitedResults, total);
        }

        /// <summary>
        /// Creates a new testimonial.
        /// </summary>
        public async Task<Testimonial> CreateAsync(Testimonial testimonial)
        {
            if (testimonial == null)
                throw new ArgumentNullException(nameof(testimonial));

            if (string.IsNullOrWhiteSpace(testimonial.id))
            {
                testimonial.id = Guid.NewGuid().ToString();
            }

            testimonial.CreatedAt = DateTime.UtcNow;

            var response = await _container.CreateItemAsync(testimonial);
            return response.Resource;
        }

        /// <summary>
        /// Updates an existing testimonial.
        /// </summary>
        public async Task<Testimonial> UpdateAsync(Testimonial testimonial)
        {
            if (testimonial == null)
                throw new ArgumentNullException(nameof(testimonial));

            if (string.IsNullOrWhiteSpace(testimonial.id))
                throw new InvalidOperationException("Testimonial id must not be null or empty.");

            var response = await _container.ReplaceItemAsync(
                testimonial, 
                testimonial.id, 
                new PartitionKey(testimonial.Locale));

            return response.Resource;
        }

        /// <summary>
        /// Deletes a testimonial by id.
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id cannot be null or empty.", nameof(id));

            // First, get the testimonial to find its partition key (Locale)
            var testimonial = await GetByIdAsync(id);
            if (testimonial == null)
                return false;

            try
            {
                await _container.DeleteItemAsync<Testimonial>(id, new PartitionKey(testimonial.Locale));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
