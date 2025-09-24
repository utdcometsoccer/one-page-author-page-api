using Microsoft.AspNetCore.Http;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices;

/// <summary>
/// Service interface for handling image upload operations.
/// </summary>
public interface IImageUploadService
{
    /// <summary>
    /// Uploads an image file with tier-based validation and storage.
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="userProfileId">The authenticated user's profile ID</param>
    /// <returns>Upload result with image metadata or error response</returns>
    Task<ImageUploadResult> UploadImageAsync(IFormFile file, string userProfileId);
}