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
/// Azure Function for retrieving all images uploaded by the authenticated user.
/// Uses the UserImageService for business logic.
/// </summary>
public class User
{
    private readonly ILogger<User> _logger;
    private readonly IUserImageService _userImageService;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public User(ILogger<User> logger, IUserImageService userImageService, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _userImageService = userImageService;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    [Function("User")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        _logger.LogInformation("User images list function invoked.");

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
            _logger.LogWarning(ex, "User profile validation failed for User");
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

            // Use the user image service
            var result = await _userImageService.GetUserImagesAsync(userProfileId);

            // Convert service result to HTTP response
            if (result.IsSuccess)
            {
                return new OkObjectResult(result.Images);
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
            _logger.LogError(ex, "Failed to retrieve user images.");
            return new ObjectResult(new ErrorResponse { Error = "Internal server error occurred while retrieving images." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
