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
/// Azure Function for uploading image files to Azure Blob Storage.
/// Uses the ImageUploadService for business logic and validation.
/// </summary>
public class Upload
{
    private readonly ILogger<Upload> _logger;
    private readonly IImageUploadService _imageUploadService;

    public Upload(ILogger<Upload> logger, IImageUploadService imageUploadService)
    {
        _logger = logger;
        _imageUploadService = imageUploadService;
    }

    [Function("Upload")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Image upload function invoked.");

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

            // Check if request contains file
            if (!req.HasFormContentType || !req.Form.Files.Any())
            {
                return new BadRequestObjectResult(new ErrorResponse { Error = "No file provided in the request." });
            }

            var file = req.Form.Files[0];

            // Use the image upload service
            var result = await _imageUploadService.UploadImageAsync(file, userProfileId);

            // Convert service result to HTTP response
            if (result.IsSuccess)
            {
                return new ObjectResult(result.ImageData)
                {
                    StatusCode = result.StatusCode
                };
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
            _logger.LogError(ex, "Failed to upload image.");
            return new ObjectResult(new ErrorResponse { Error = "Internal server error occurred during upload." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
