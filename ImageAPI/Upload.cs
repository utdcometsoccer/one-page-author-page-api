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
/// Azure Function for uploading image files to Azure Blob Storage.
/// Uses the ImageUploadService for business logic and validation.
/// </summary>
public class Upload
{
    private readonly ILogger<Upload> _logger;
    private readonly IImageUploadService _imageUploadService;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public Upload(ILogger<Upload> logger, IImageUploadService imageUploadService, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _imageUploadService = imageUploadService;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    [Function("Upload")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Image upload function invoked.");

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
            _logger.LogWarning(ex, "User profile validation failed for Upload");
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
