using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices;
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

    public Delete(ILogger<Delete> logger, IImageDeleteService imageDeleteService)
    {
        _logger = logger;
        _imageDeleteService = imageDeleteService;
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

        try
        {
            // Get authenticated user
            var user = req.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return new UnauthorizedResult();
            }

            // Extract user ID from claims
            var userProfileId = user.FindFirst("oid")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
