using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace InkStainedWretchFunctions;

/// <summary>
/// Admin endpoint to delete testimonials.
/// Requires authentication.
/// </summary>
public class DeleteTestimonial
{
    private readonly ILogger<DeleteTestimonial> _logger;
    private readonly ITestimonialRepository _repository;

    public DeleteTestimonial(ILogger<DeleteTestimonial> logger, ITestimonialRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// Deletes a testimonial by ID.
    /// </summary>
    [Function("DeleteTestimonial")]
    [Authorize]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "admin/testimonials/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Deleting testimonial with ID: {TestimonialId}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Testimonial ID is required");
                return badResponse;
            }

            var deleted = await _repository.DeleteAsync(id);

            if (!deleted)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Testimonial with ID {id} not found");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting testimonial with ID: {TestimonialId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
