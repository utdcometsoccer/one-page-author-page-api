using InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices;

/// <summary>
/// Service interface for handling image deletion operations.
/// </summary>
public interface IImageDeleteService
{
    /// <summary>
    /// Deletes an image for a specific user.
    /// </summary>
    /// <param name="imageId">The image ID to delete</param>
    /// <param name="userProfileId">The user's profile ID</param>
    /// <returns>Deletion result or error response</returns>
    Task<ImageDeleteResult> DeleteImageAsync(string imageId, string userProfileId);
}