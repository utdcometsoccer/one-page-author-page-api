using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using ImageAPI.Models;
using System.Security.Claims;

namespace ImageAPI;

/// <summary>
/// Azure Function for deleting an image by its ID.
/// Uses the ImageDeleteService for business logic.
/// </summary>
public class Delete
{
    private readonly ILogger<Delete> _logger;
    private readonly IImageDeleteService _imageDeleteService;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public Delete(ILogger<Delete> logger, IImageDeleteService imageDeleteService, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _imageDeleteService = imageDeleteService;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    [Function("Delete")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequest req)
    {
        // Extract image ID from query parameters or route
        var id = req.Query["id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id))
        {
            // Try to extract from path (e.g., /api/Delete/image-id)
            var pathSegments = req.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments?.Length > 1)
            {
                id = pathSegments.Last();
            }
        }

        _logger.LogInformation("Image delete function invoked for image ID: {ImageId}", id);

        // Validate JWT token and get authenticated user
        var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (authError != null)
        {
            return authError;
        }

        try
        {
            // Ensure user profile exists
            await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User profile validation failed for Delete");
            return new UnauthorizedObjectResult(new ErrorResponse { Error = "User profile validation failed" });
        }

        try
        {
            // Extract user ID from claims
            var userProfileId = authenticatedUser!.FindFirst("oid")?.Value ?? authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userProfileId))
            {
                _logger.LogWarning("User profile ID not found in claims.");
                return new UnauthorizedResult();
            }

            // Use the image delete service
            var result = await _imageDeleteService.DeleteImageAsync(id ?? string.Empty, userProfileId);

            // Convert service result to HTTP response
            if (result.IsSuccess)
            {
                return new OkObjectResult(new { message = result.Message });
            }
            else
            {
                return new ObjectResult(new ErrorResponse { Error = result.ErrorMessage ?? "Unknown error occurred." })
                {
                    StatusCode = result.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image {ImageId}", id);
            return new ObjectResult(new ErrorResponse { Error = "Internal server error occurred during deletion." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
