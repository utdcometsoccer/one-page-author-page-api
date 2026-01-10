using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorLib.API;
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
    private readonly IAuthenticatedFunctionTelemetryService _telemetry;

    public DeleteTestimonial(
        ILogger<DeleteTestimonial> logger, 
        ITestimonialRepository repository,
        IAuthenticatedFunctionTelemetryService telemetry)
    {
        _logger = logger;
        _repository = repository;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Deletes a testimonial by ID.
    /// </summary>
    [Function("DeleteTestimonial")]
    [Authorize]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "testimonials/{id}")] HttpRequestData req,
        string id)
    {
        var user = req.FunctionContext.Features.Get<IHttpContextAccessor>()?.HttpContext?.User;
        var userId = AuthenticatedFunctionTelemetryService.ExtractUserId(user);
        var userEmail = AuthenticatedFunctionTelemetryService.ExtractUserEmail(user);

        _logger.LogInformation("Deleting testimonial with ID: {TestimonialId} by user {UserId}", id, userId ?? "Anonymous");

        // Track the authenticated function call
        _telemetry.TrackAuthenticatedFunctionCall(
            "DeleteTestimonial",
            userId,
            userEmail,
            new Dictionary<string, string> { { "TestimonialId", id } });

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "DeleteTestimonial",
                    userId,
                    userEmail,
                    "Testimonial ID is required",
                    "ValidationError");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Testimonial ID is required");
                return badResponse;
            }

            var deleted = await _repository.DeleteAsync(id);

            if (!deleted)
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "DeleteTestimonial",
                    userId,
                    userEmail,
                    $"Testimonial with ID {id} not found",
                    "NotFound",
                    new Dictionary<string, string> { { "TestimonialId", id } });
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Testimonial with ID {id} not found");
                return notFoundResponse;
            }

            // Track success
            _telemetry.TrackAuthenticatedFunctionSuccess(
                "DeleteTestimonial",
                userId,
                userEmail,
                new Dictionary<string, string> { { "TestimonialId", id } });

            _logger.LogInformation(
                "Successfully deleted testimonial {TestimonialId} by user {UserId}",
                id,
                userId ?? "Anonymous");

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting testimonial with ID: {TestimonialId} by user {UserId}", id, userId ?? "Anonymous");
            _telemetry.TrackAuthenticatedFunctionError(
                "DeleteTestimonial",
                userId,
                userEmail,
                ex.Message,
                ex.GetType().Name,
                new Dictionary<string, string> { { "TestimonialId", id } });
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
