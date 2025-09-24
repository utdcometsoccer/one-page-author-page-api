using InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices;

/// <summary>
/// Service interface for handling user image operations.
/// </summary>
public interface IUserImageService
{
    /// <summary>
    /// Retrieves all images for a specific user.
    /// </summary>
    /// <param name="userProfileId">The user's profile ID</param>
    /// <returns>List of user images or error response</returns>
    Task<UserImagesResult> GetUserImagesAsync(string userProfileId);
}