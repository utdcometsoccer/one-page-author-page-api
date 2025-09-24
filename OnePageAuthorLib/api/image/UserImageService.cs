using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices;

/// <summary>
/// Service for handling user image retrieval operations.
/// </summary>
public class UserImageService : IUserImageService
{
    private readonly ILogger<UserImageService> _logger;
    private readonly IImageRepository _imageRepository;

    public UserImageService(ILogger<UserImageService> logger, IImageRepository imageRepository)
    {
        _logger = logger;
        _imageRepository = imageRepository;
    }

    public async Task<UserImagesResult> GetUserImagesAsync(string userProfileId)
    {
        try
        {
            _logger.LogInformation("Retrieving images for user {UserProfileId}", userProfileId);

            // Get all images for the user
            var images = await _imageRepository.GetByUserProfileIdAsync(userProfileId);

            // Convert to response format and sort by upload date (newest first)
            var imageResponses = images
                .OrderByDescending(img => img.UploadedAt)
                .Select(img => new UserImageResponse
                {
                    Id = img.id,
                    Url = img.Url,
                    Name = img.Name,
                    Size = img.Size,
                    UploadedAt = img.UploadedAt
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} images for user {UserProfileId}", imageResponses.Count, userProfileId);

            var result = ServiceResult.Success<UserImagesResult>(200);
            result.Images = imageResponses;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve images for user {UserProfileId}.", userProfileId);
            return ServiceResult.Failure<UserImagesResult>("Internal server error occurred while retrieving images.", 500);
        }
    }
}