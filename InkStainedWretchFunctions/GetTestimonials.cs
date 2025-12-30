using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretchFunctions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function HTTP endpoint that returns testimonials for the landing page.
/// Public endpoint with optional filtering and caching.
/// </summary>
public class GetTestimonials
{
    private readonly ILogger<GetTestimonials> _logger;
    private readonly ITestimonialRepository _repository;

    /// <summary>
    /// Creates a new <see cref="GetTestimonials"/> function handler.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="repository">Testimonial repository service.</param>
    public GetTestimonials(ILogger<GetTestimonials> logger, ITestimonialRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// Handles HTTP GET requests for testimonials.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <returns>200 with JSON payload of testimonials; standardized error response on failure.</returns>
    [Function("GetTestimonials")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "testimonials")] HttpRequestData req)
    {
        _logger.LogInformation("Received request for testimonials");

        try
        {
            // Parse query parameters
            var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            
            int limit = 5;
            if (queryParams["limit"] != null && int.TryParse(queryParams["limit"], out var parsedLimit))
            {
                limit = parsedLimit;
            }

            bool? featured = null;
            if (queryParams["featured"] != null && bool.TryParse(queryParams["featured"], out var parsedFeatured))
            {
                featured = parsedFeatured;
            }

            string? locale = queryParams["locale"];

            // Get testimonials
            var (testimonials, total) = await _repository.GetTestimonialsAsync(limit, featured, locale);

            var result = new
            {
                testimonials = testimonials,
                total = total
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            
            // Add caching header (15 minutes)
            response.Headers.Add("Cache-Control", "public, max-age=900");
            
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
        }
    }
}
