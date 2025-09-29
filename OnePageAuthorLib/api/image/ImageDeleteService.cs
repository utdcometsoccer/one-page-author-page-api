using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices;

/// <summary>
/// Service for handling image deletion operations.
/// </summary>
public class ImageDeleteService : IImageDeleteService
{
    private readonly ILogger<ImageDeleteService> _logger;
    private readonly IImageRepository _imageRepository;
    private readonly IImageStorageTierMembershipRepository _membershipRepository;
    private readonly BlobServiceClient _blobServiceClient;

    public ImageDeleteService(
        ILogger<ImageDeleteService> logger,
        IImageRepository imageRepository,
        IImageStorageTierMembershipRepository membershipRepository,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _imageRepository = imageRepository;
        _membershipRepository = membershipRepository;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<ImageDeleteResult> DeleteImageAsync(string imageId, string userProfileId)
    {
        try
        {
            _logger.LogInformation("Processing image deletion for user {UserProfileId}, image {ImageId}", userProfileId, imageId);

            // Validate image ID
            if (string.IsNullOrWhiteSpace(imageId))
            {
                return ServiceResult.Failure<ImageDeleteResult>("Image ID is required.", 400);
            }

            // Get the image record
            var images = await _imageRepository.GetByUserProfileIdAsync(userProfileId);
            var image = images.FirstOrDefault(img => img.id == imageId);

            if (image == null)
            {
                return ServiceResult.Failure<ImageDeleteResult>("Image not found.", 404);
            }

            // Verify the image belongs to the authenticated user
            if (image.UserProfileId != userProfileId)
            {
                _logger.LogWarning("User {UserProfileId} attempted to delete image {ImageId} owned by {OwnerProfileId}",
                    userProfileId, imageId, image.UserProfileId);
                return ServiceResult.Failure<ImageDeleteResult>("Image not found.", 404);
            }

            // Delete from Azure Blob Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(image.ContainerName);
            var blobClient = containerClient.GetBlobClient(image.BlobName);

            var deleteResult = await blobClient.DeleteIfExistsAsync();
            if (!deleteResult.Value)
            {
                _logger.LogWarning("Blob {BlobName} in container {ContainerName} was not found during deletion",
                    image.BlobName, image.ContainerName);
            }

            // Delete from database
            await _imageRepository.DeleteAsync(Guid.Parse(image.id));

            // Update user's storage usage
            var membership = await _membershipRepository.GetForUserAsync(userProfileId);
            if (membership != null)
            {
                membership.StorageUsedInBytes = Math.Max(0, membership.StorageUsedInBytes - image.Size);
                await _membershipRepository.UpdateAsync(membership);
            }

            _logger.LogInformation("Image deleted successfully. User: {UserProfileId}, Image: {ImageId}, Size: {Size} bytes",
                userProfileId, image.id, image.Size);

            var result = ServiceResult.Success<ImageDeleteResult>(200);
            result.Message = "Image deleted successfully.";
            return result;
        }
        catch (FormatException)
        {
            return ServiceResult.Failure<ImageDeleteResult>("Invalid image ID format.", 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image {ImageId} for user {UserProfileId}", imageId, userProfileId);
            return ServiceResult.Failure<ImageDeleteResult>("Internal server error occurred during deletion.", 500);
        }
    }
}