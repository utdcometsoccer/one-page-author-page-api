using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Interface for TestimonialRepository operations.
    /// </summary>
    public interface ITestimonialRepository
    {
        /// <summary>
        /// Gets a testimonial by its id.
        /// </summary>
        Task<Testimonial?> GetByIdAsync(string id);

        /// <summary>
        /// Gets testimonials with optional filtering.
        /// </summary>
        /// <param name="limit">Maximum number of testimonials to return (default: 5, max: 20).</param>
        /// <param name="featured">Only return featured testimonials if true.</param>
        /// <param name="locale">Filter by locale if specified.</param>
        Task<(IList<Testimonial> testimonials, int total)> GetTestimonialsAsync(int limit = 5, bool? featured = null, string? locale = null);

        /// <summary>
        /// Creates a new testimonial.
        /// </summary>
        Task<Testimonial> CreateAsync(Testimonial testimonial);

        /// <summary>
        /// Updates an existing testimonial.
        /// </summary>
        Task<Testimonial> UpdateAsync(Testimonial testimonial);

        /// <summary>
        /// Deletes a testimonial by id.
        /// </summary>
        Task<bool> DeleteAsync(string id);
    }
}
