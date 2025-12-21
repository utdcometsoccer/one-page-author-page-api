using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace InkStainedWretchFunctions;

/// <summary>
/// Admin endpoint to create testimonials.
/// Requires authentication.
/// </summary>
public class CreateTestimonial
{
    private readonly ILogger<CreateTestimonial> _logger;
    private readonly ITestimonialRepository _repository;

    public CreateTestimonial(ILogger<CreateTestimonial> logger, ITestimonialRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// Creates a new testimonial.
    /// </summary>
    [Function("CreateTestimonial")]
    [Authorize]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/testimonials")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new testimonial");

        try
        {
            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var testimonial = JsonSerializer.Deserialize<Testimonial>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (testimonial == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid testimonial data");
                return badResponse;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(testimonial.AuthorName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("AuthorName is required");
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(testimonial.Quote))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Quote is required");
                return badResponse;
            }

            // Validate rating
            if (testimonial.Rating < 1 || testimonial.Rating > 5)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Rating must be between 1 and 5");
                return badResponse;
            }

            // Create testimonial
            var created = await _repository.CreateAsync(testimonial);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(created);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating testimonial");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
