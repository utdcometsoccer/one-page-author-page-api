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
/// Admin endpoint to update testimonials.
/// Requires authentication.
/// </summary>
public class UpdateTestimonial
{
    private readonly ILogger<UpdateTestimonial> _logger;
    private readonly ITestimonialRepository _repository;

    public UpdateTestimonial(ILogger<UpdateTestimonial> logger, ITestimonialRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// Updates an existing testimonial.
    /// </summary>
    [Function("UpdateTestimonial")]
    [Authorize]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "admin/testimonials/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Updating testimonial with ID: {TestimonialId}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Testimonial ID is required");
                return badResponse;
            }

            // Check if testimonial exists
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Testimonial with ID {id} not found");
                return notFoundResponse;
            }

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

            // Ensure ID matches
            testimonial.id = id;

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

            // Preserve CreatedAt from existing testimonial
            testimonial.CreatedAt = existing.CreatedAt;

            // Update testimonial
            var updated = await _repository.UpdateAsync(testimonial);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(updated);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating testimonial with ID: {TestimonialId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
