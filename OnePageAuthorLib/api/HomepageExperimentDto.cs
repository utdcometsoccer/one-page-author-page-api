namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents the homepage A/B experiment assignment for the current request.
    /// Defaults to <c>"control"</c> to preserve existing homepage behavior.
    /// </summary>
    public class HomepageExperimentDto
    {
        /// <summary>
        /// The assigned variant for the homepage hero experiment.
        /// <list type="bullet">
        ///   <item><description><c>"control"</c> — render the standard author-first homepage.</description></item>
        ///   <item><description><c>"featured-book-hero"</c> — render the book-first homepage hero (requires <see cref="AuthorResponse.FeaturedBook"/> to be non-null).</description></item>
        /// </list>
        /// Defaults to <c>"control"</c> when no experiment is active or assignment fails.
        /// </summary>
        public string HomepageHeroVariant { get; set; } = "control";
    }
}
