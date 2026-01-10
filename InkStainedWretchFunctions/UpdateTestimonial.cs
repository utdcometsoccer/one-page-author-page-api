using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorLib.API;
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
    private readonly IAuthenticatedFunctionTelemetryService _telemetry;

    public UpdateTestimonial(
        ILogger<UpdateTestimonial> logger, 
        ITestimonialRepository repository,
        IAuthenticatedFunctionTelemetryService telemetry)
    {
        _logger = logger;
        _repository = repository;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Updates an existing testimonial.
    /// </summary>
    [Function("UpdateTestimonial")]
    [Authorize]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "testimonials/{id}")] HttpRequestData req,
        string id)
    {
        var user = req.FunctionContext.Features.Get<IHttpContextAccessor>()?.HttpContext?.User;
        var userId = AuthenticatedFunctionTelemetryService.ExtractUserId(user);
        var userEmail = AuthenticatedFunctionTelemetryService.ExtractUserEmail(user);

        _logger.LogInformation("Updating testimonial with ID: {TestimonialId} by user {UserId}", id, userId ?? "Anonymous");

        // Track the authenticated function call
        _telemetry.TrackAuthenticatedFunctionCall(
            "UpdateTestimonial",
            userId,
            userEmail,
            new Dictionary<string, string> { { "TestimonialId", id } });

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "UpdateTestimonial",
                    userId,
                    userEmail,
                    "Testimonial ID is required",
                    "ValidationError");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Testimonial ID is required");
                return badResponse;
            }

            // Check if testimonial exists
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "UpdateTestimonial",
                    userId,
                    userEmail,
                    $"Testimonial with ID {id} not found",
                    "NotFound",
                    new Dictionary<string, string> { { "TestimonialId", id } });
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
                _telemetry.TrackAuthenticatedFunctionError(
                    "UpdateTestimonial",
                    userId,
                    userEmail,
                    "Invalid testimonial data",
                    "ValidationError",
                    new Dictionary<string, string> { { "TestimonialId", id } });
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid testimonial data");
                return badResponse;
            }

            // Ensure ID matches
            testimonial.id = id;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(testimonial.AuthorName))
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "UpdateTestimonial",
                    userId,
                    userEmail,
                    "AuthorName is required",
                    "ValidationError",
                    new Dictionary<string, string> { { "TestimonialId", id } });
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("AuthorName is required");
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(testimonial.Quote))
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "UpdateTestimonial",
                    userId,
                    userEmail,
                    "Quote is required",
                    "ValidationError",
                    new Dictionary<string, string> { { "TestimonialId", id } });
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Quote is required");
                return badResponse;
            }

            // Validate rating
            if (testimonial.Rating < 1 || testimonial.Rating > 5)
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "UpdateTestimonial",
                    userId,
                    userEmail,
                    "Rating must be between 1 and 5",
                    "ValidationError",
                    new Dictionary<string, string> { { "TestimonialId", id } });
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Rating must be between 1 and 5");
                return badResponse;
            }

            // Preserve CreatedAt from existing testimonial
            testimonial.CreatedAt = existing.CreatedAt;

            // Update testimonial
            var updated = await _repository.UpdateAsync(testimonial);

            // Track success with additional context
            var successProperties = new Dictionary<string, string>
            {
                { "TestimonialId", id },
                { "AuthorName", updated.AuthorName },
                { "Rating", updated.Rating.ToString() }
            };

            _telemetry.TrackAuthenticatedFunctionSuccess(
                "UpdateTestimonial",
                userId,
                userEmail,
                successProperties);

            _logger.LogInformation(
                "Successfully updated testimonial {TestimonialId} by user {UserId}",
                id,
                userId ?? "Anonymous");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(updated);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating testimonial with ID: {TestimonialId} by user {UserId}", id, userId ?? "Anonymous");
            _telemetry.TrackAuthenticatedFunctionError(
                "UpdateTestimonial",
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
