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
/// Admin endpoint to create testimonials.
/// Requires authentication.
/// </summary>
public class CreateTestimonial
{
    private readonly ILogger<CreateTestimonial> _logger;
    private readonly ITestimonialRepository _repository;
    private readonly IAuthenticatedFunctionTelemetryService _telemetry;

    public CreateTestimonial(
        ILogger<CreateTestimonial> logger, 
        ITestimonialRepository repository,
        IAuthenticatedFunctionTelemetryService telemetry)
    {
        _logger = logger;
        _repository = repository;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Creates a new testimonial.
    /// </summary>
    [Function("CreateTestimonial")]
    [Authorize]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "testimonials")] HttpRequestData req)
    {
        var user = req.FunctionContext.Features.Get<IHttpContextAccessor>()?.HttpContext?.User;
        var userId = AuthenticatedFunctionTelemetryService.ExtractUserId(user);
        var userEmail = AuthenticatedFunctionTelemetryService.ExtractUserEmail(user);

        _logger.LogInformation("Creating new testimonial by user {UserId}", userId ?? "Anonymous");

        // Track the authenticated function call
        _telemetry.TrackAuthenticatedFunctionCall(
            "CreateTestimonial",
            userId,
            userEmail);

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
                _telemetry.TrackAuthenticatedFunctionError(
                    "CreateTestimonial",
                    userId,
                    userEmail,
                    "Invalid testimonial data",
                    "ValidationError");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid testimonial data");
                return badResponse;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(testimonial.AuthorName))
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "CreateTestimonial",
                    userId,
                    userEmail,
                    "AuthorName is required",
                    "ValidationError");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("AuthorName is required");
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(testimonial.Quote))
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "CreateTestimonial",
                    userId,
                    userEmail,
                    "Quote is required",
                    "ValidationError");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Quote is required");
                return badResponse;
            }

            // Validate rating
            if (testimonial.Rating < 1 || testimonial.Rating > 5)
            {
                _telemetry.TrackAuthenticatedFunctionError(
                    "CreateTestimonial",
                    userId,
                    userEmail,
                    "Rating must be between 1 and 5",
                    "ValidationError");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Rating must be between 1 and 5");
                return badResponse;
            }

            // Create testimonial
            var created = await _repository.CreateAsync(testimonial);

            // Track success with additional context
            var successProperties = new Dictionary<string, string>
            {
                { "TestimonialId", created.id ?? "unknown" },
                { "AuthorName", created.AuthorName },
                { "Rating", created.Rating.ToString() }
            };

            _telemetry.TrackAuthenticatedFunctionSuccess(
                "CreateTestimonial",
                userId,
                userEmail,
                successProperties);

            _logger.LogInformation(
                "Successfully created testimonial {TestimonialId} by user {UserId}",
                created.id,
                userId ?? "Anonymous");

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(created);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating testimonial by user {UserId}", userId ?? "Anonymous");
            _telemetry.TrackAuthenticatedFunctionError(
                "CreateTestimonial",
                userId,
                userEmail,
                ex.Message,
                ex.GetType().Name);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
