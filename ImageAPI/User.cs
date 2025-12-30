using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorLib.Extensions;
using ImageAPI.Models;
using System.Security.Claims;

namespace ImageAPI;

/// <summary>
/// Azure Function for retrieving all images uploaded by the authenticated user.
/// Uses the UserImageService for business logic.
/// </summary>
/// <remarks>
/// <para><strong>HTTP Method:</strong> GET</para>
/// <para><strong>Route:</strong> /api/images/user</para>
/// <para><strong>Authentication:</strong> Required - Bearer JWT token</para>
/// <para><strong>Authorization:</strong> Users can only view their own images</para>
/// 
/// <para><strong>TypeScript Example:</strong></para>
/// <code>
/// // Interface for image response
/// interface UserImage {
///   id: string;
///   url: string;
///   name: string;
///   size: number;
///   uploadedAt: string; // ISO 8601 datetime
/// }
/// 
/// // Fetch user's images with error handling
/// const fetchUserImages = async (token: string): Promise&lt;UserImage[]&gt; => {
///   const response = await fetch('/api/images/user', {
///     method: 'GET',
///     headers: {
///       'Authorization': `Bearer ${token}`,
///       'Content-Type': 'application/json'
///     }
///   });
///   
///   if (response.ok) {
///     return await response.json();
///   } else if (response.status === 401) {
///     throw new Error('Unauthorized - invalid or missing token');
///   }
///   
///   const error = await response.json();
///   throw new Error(error.error || 'Failed to fetch images');
/// };
/// 
/// // Usage with React/TypeScript
/// const ImageGallery: React.FC = () => {
///   const [images, setImages] = useState&lt;UserImage[]&gt;([]);
///   const [loading, setLoading] = useState(true);
///   const [error, setError] = useState&lt;string | null&gt;(null);
///   
///   useEffect(() => {
///     const loadImages = async () => {
///       try {
///         setLoading(true);
///         const userImages = await fetchUserImages(userToken);
///         setImages(userImages);
///         setError(null);
///       } catch (err) {
///         setError(err.message);
///       } finally {
///         setLoading(false);
///       }
///     };
///     
///     loadImages();
///   }, [userToken]);
///   
///   if (loading) return &lt;div&gt;Loading images...&lt;/div&gt;;
///   if (error) return &lt;div&gt;Error: {error}&lt;/div&gt;;
///   
///   return (
///     &lt;div className="image-gallery"&gt;
///       {images.map(image => (
///         &lt;img key={image.id} src={image.url} alt={image.name} /&gt;
///       ))}
///     &lt;/div&gt;
///   );
/// };
/// </code>
/// 
/// <para><strong>Response Format:</strong></para>
/// <code>
/// [
///   {
///     "id": "image-uuid-123",
///     "url": "https://storage.blob.core.windows.net/images/user123/image.jpg",
///     "name": "vacation-photo.jpg",
///     "size": 2048576, // size in bytes
///     "uploadedAt": "2024-01-15T10:30:00Z"
///   }
/// ]
/// </code>
/// 
/// <para><strong>Response Codes:</strong></para>
/// <list type="bullet">
/// <item>200 OK: Returns array of user's images (empty array if no images)</item>
/// <item>401 Unauthorized: Missing or invalid JWT token</item>
/// <item>500 Internal Server Error: Unexpected server error</item>
/// </list>
/// 
/// <para><strong>Security Notes:</strong></para>
/// <list type="bullet">
/// <item>Images are sorted by upload date (newest first)</item>
/// <item>Only returns images owned by the authenticated user</item>
/// <item>Image URLs are public but contain non-guessable paths</item>
/// <item>No pagination implemented - returns all user images</item>
/// </list>
/// </remarks>
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

    /// <summary>
    /// Retrieves all images uploaded by the authenticated user.
    /// </summary>
    /// <param name="req">The HTTP request (no additional parameters required)</param>
    /// <returns>
    /// <list type="table">
    /// <item>
    /// <term>200 OK</term>
    /// <description>Array of user's images with metadata (id, url, name, size, uploadedAt)</description>
    /// </item>
    /// <item>
    /// <term>401 Unauthorized</term>
    /// <description>Invalid or missing JWT token</description>
    /// </item>
    /// <item>
    /// <term>500 Internal Server Error</term>
    /// <description>Unexpected server error during retrieval</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <example>
    /// Images are automatically sorted by upload date in descending order (newest first).
    /// Returns empty array [] if user has no uploaded images.
    /// </example>
    [Function("User")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
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
            return ErrorResponseExtensions.CreateErrorResult(
                StatusCodes.Status401Unauthorized,
                "User profile validation failed");
        }

        try
        {
            // Extract user ID from claims
            var userProfileId = authenticatedUser!.FindFirst("oid")?.Value ?? authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userProfileId))
            {
                _logger.LogWarning("User profile ID not found in claims.");
                return ErrorResponseExtensions.CreateErrorResult(
                    StatusCodes.Status401Unauthorized,
                    "User profile ID not found in claims");
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
                return ErrorResponseExtensions.CreateErrorResult(
                    result.StatusCode,
                    result.ErrorMessage ?? "Unknown error occurred.");
            }
        }
        catch (Exception ex)
        {
            return ErrorResponseExtensions.HandleException(ex, _logger);
        }
    }
}
