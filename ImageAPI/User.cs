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
/// Azure Function for retrieving all images uploaded by the authenticated user.
/// Uses the UserImageService for business logic.
/// </summary>
public class User
{
    private readonly ILogger<User> _logger;
    private readonly IUserImageService _userImageService;

    public User(ILogger<User> logger, IUserImageService userImageService)
    {
        _logger = logger;
        _userImageService = userImageService;
    }

    [Function("User")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        _logger.LogInformation("User images list function invoked.");

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
