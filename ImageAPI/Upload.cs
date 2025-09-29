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
/// <remarks>
/// <para><strong>HTTP Method:</strong> POST</para>
/// <para><strong>Route:</strong> /api/images/upload</para>
/// <para><strong>Content-Type:</strong> multipart/form-data</para>
/// <para><strong>Authentication:</strong> Required - Bearer JWT token</para>
/// <para><strong>Authorization:</strong> Valid subscription tier required</para>
/// 
/// <para><strong>Service Tier Limits:</strong></para>
/// <list type="table">
/// <item>
/// <term>Starter (Free)</term>
/// <description>Storage: 5GB, Bandwidth: 25GB, Max file: 5MB, Max files: 20</description>
/// </item>
/// <item>
/// <term>Pro ($9.99/mo)</term>
/// <description>Storage: 250GB, Bandwidth: 1TB, Max file: 10MB, Max files: 500</description>
/// </item>
/// <item>
/// <term>Elite ($19.99/mo)</term>
/// <description>Storage: 2TB, Bandwidth: 10TB, Max file: 25MB, Max files: 2000</description>
/// </item>
/// </list>
/// 
/// <para><strong>TypeScript Example:</strong></para>
/// <code>
/// // Upload an image with progress tracking
/// const uploadImage = async (file: File, token: string, onProgress?: (percent: number) => void) => {
///   const formData = new FormData();
///   formData.append('file', file);
///   
///   const xhr = new XMLHttpRequest();
///   
///   return new Promise((resolve, reject) => {
///     xhr.upload.addEventListener('progress', (e) => {
///       if (e.lengthComputable &amp;&amp; onProgress) {
///         onProgress(Math.round((e.loaded * 100) / e.total));
///       }
///     });
///     
///     xhr.addEventListener('load', () => {
///       if (xhr.status === 201) {
///         resolve(JSON.parse(xhr.responseText));
///       } else {
///         const error = JSON.parse(xhr.responseText);
///         reject(new Error(error.error || 'Upload failed'));
///       }
///     });
///     
///     xhr.addEventListener('error', () => reject(new Error('Network error')));
///     
///     xhr.open('POST', '/api/images/upload');
///     xhr.setRequestHeader('Authorization', `Bearer ${token}`);
///     xhr.send(formData);
///   });
/// };
/// 
/// // Usage with React/TypeScript
/// const handleFileUpload = async (event: React.ChangeEvent&lt;HTMLInputElement&gt;) => {
///   const file = event.target.files?.[0];
///   if (!file) return;
///   
///   // Validate file type
///   if (!file.type.startsWith('image/')) {
///     alert('Please select an image file');
///     return;
///   }
///   
///   try {
///     const result = await uploadImage(file, userToken, (percent) => {
///       console.log(`Upload progress: ${percent}%`);
///     });
///     console.log('Upload successful:', result);
///   } catch (error) {
///     console.error('Upload failed:', error.message);
///   }
/// };
/// </code>
/// 
/// <para><strong>Response Codes:</strong></para>
/// <list type="bullet">
/// <item>201 Created: Image uploaded successfully - returns {id, url, name, size, uploadedAt}</item>
/// <item>400 Bad Request: Invalid file type or file too large for subscription tier</item>
/// <item>401 Unauthorized: Missing or invalid JWT token</item>
/// <item>402 Payment Required: Bandwidth limit exceeded for subscription tier</item>
/// <item>403 Forbidden: Maximum file count reached for subscription tier</item>
/// <item>507 Insufficient Storage: Storage quota exceeded for subscription tier</item>
/// <item>500 Internal Server Error: Unexpected server error</item>
/// </list>
/// 
/// <para><strong>Security Notes:</strong></para>
/// <list type="bullet">
/// <item>Only image files are accepted (validated by MIME type and file extension)</item>
/// <item>File size limits enforced based on user's subscription tier</item>
/// <item>Storage usage tracked and enforced per user</item>
/// <item>Bandwidth usage monitored and limited per tier</item>
/// </list>
/// </remarks>
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

    /// <summary>
    /// Uploads an image file to Azure Blob Storage with subscription tier validation.
    /// </summary>
    /// <param name="req">The HTTP request containing the multipart/form-data with image file</param>
    /// <returns>
    /// <list type="table">
    /// <item>
    /// <term>201 Created</term>
    /// <description>Image uploaded successfully - returns {id, url, name, size, uploadedAt}</description>
    /// </item>
    /// <item>
    /// <term>400 Bad Request</term>
    /// <description>No file provided, invalid file type, or file too large for tier</description>
    /// </item>
    /// <item>
    /// <term>401 Unauthorized</term>
    /// <description>Invalid or missing JWT token</description>
    /// </item>
    /// <item>
    /// <term>402 Payment Required</term>
    /// <description>Bandwidth limit exceeded for subscription tier</description>
    /// </item>
    /// <item>
    /// <term>403 Forbidden</term>
    /// <description>Maximum file count reached for subscription tier</description>
    /// </item>
    /// <item>
    /// <term>507 Insufficient Storage</term>
    /// <description>Storage quota exceeded for subscription tier</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <example>
    /// The request must be multipart/form-data with a file field containing the image.
    /// Supported formats: JPEG, PNG, GIF, WebP, BMP, TIFF
    /// </example>
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
