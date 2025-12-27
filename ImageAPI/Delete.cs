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
/// <remarks>
/// <para><strong>HTTP Method:</strong> DELETE</para>
/// <para><strong>Route:</strong> /api/images/{id}</para>
/// <para><strong>Authentication:</strong> Required - Bearer JWT token</para>
/// <para><strong>Authorization:</strong> Users can only delete their own images</para>
/// 
/// <para><strong>TypeScript Example:</strong></para>
/// <code>
/// // Delete an image
/// const deleteImage = async (imageId: string, token: string) => {
///   const response = await fetch(`/api/images/${imageId}`, {
///     method: 'DELETE',
///     headers: {
///       'Authorization': `Bearer ${token}`,
///       'Content-Type': 'application/json'
///     }
///   });
///   
///   if (response.ok) {
///     return { success: true, message: 'Image deleted successfully' };
///   } else if (response.status === 404) {
///     throw new Error('Image not found or not owned by user');
///   } else if (response.status === 401) {
///     throw new Error('Unauthorized - invalid or missing token');
///   }
///   
///   const error = await response.json();
///   throw new Error(error.error || 'Failed to delete image');
/// };
/// 
/// // Usage
/// try {
///   await deleteImage('image-123', userToken);
///   console.log('Image deleted successfully');
/// } catch (error) {
///   console.error('Delete failed:', error.message);
/// }
/// </code>
/// 
/// <para><strong>Response Codes:</strong></para>
/// <list type="bullet">
/// <item>200 OK: Image deleted successfully</item>
/// <item>400 Bad Request: Invalid image ID format</item>
/// <item>401 Unauthorized: Missing or invalid JWT token</item>
/// <item>404 Not Found: Image not found or not owned by user</item>
/// <item>500 Internal Server Error: Unexpected server error</item>
/// </list>
/// 
/// <para><strong>Security Notes:</strong></para>
/// <list type="bullet">
/// <item>Requires valid JWT token with proper user claims</item>
/// <item>Users can only delete images they own (enforced by user ID in claims)</item>
/// <item>Image ID can be passed as query parameter (?id=) or in URL path</item>
/// </list>
/// </remarks>
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

    /// <summary>
    /// Deletes a user's image by ID.
    /// </summary>
    /// <param name="req">The HTTP request containing the image ID to delete</param>
    /// <returns>
    /// <list type="table">
    /// <item>
    /// <term>200 OK</term>
    /// <description>Image successfully deleted - returns success message</description>
    /// </item>
    /// <item>
    /// <term>400 Bad Request</term>
    /// <description>Invalid or missing image ID</description>
    /// </item>
    /// <item>
    /// <term>401 Unauthorized</term>
    /// <description>Invalid or missing JWT token</description>
    /// </item>
    /// <item>
    /// <term>404 Not Found</term>
    /// <description>Image not found or user doesn't own the image</description>
    /// </item>
    /// <item>
    /// <term>500 Internal Server Error</term>
    /// <description>Unexpected server error during deletion</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <example>
    /// Image ID can be provided in two ways:
    /// <list type="number">
    /// <item>Query parameter: DELETE /api/Delete?id=image-123</item>
    /// <item>URL path: DELETE /api/Delete/image-123</item>
    /// </list>
    /// </example>
    [Function("Delete")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequest req)
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
