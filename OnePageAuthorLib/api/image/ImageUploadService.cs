using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;
using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices;

/// <summary>
/// Service for handling image upload operations with tier-based validation.
/// </summary>
public class ImageUploadService : IImageUploadService
{
    private readonly ILogger<ImageUploadService> _logger;
    private readonly IImageStorageTierService _tierService;
    private readonly IImageStorageUsageRepository _usageRepository;
    private readonly IImageRepository _imageRepository;
    private readonly BlobServiceClient _blobServiceClient;

    // Tier limits based on API documentation and seeded tier data
    private readonly Dictionary<string, (decimal MaxFileSizeMB, int MaxFiles)> _tierLimits = new()
    {
        { "Starter", (5m, 20) },
        { "Pro", (10m, 500) },
        { "Elite", (25m, 2000) }
    };

    private readonly HashSet<string> _allowedContentTypes = new()
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp", "image/tiff"
    };

    public ImageUploadService(
        ILogger<ImageUploadService> logger,
        IImageStorageTierService tierService,
        IImageStorageUsageRepository usageRepository,
        IImageRepository imageRepository,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _tierService = tierService;
        _usageRepository = usageRepository;
        _imageRepository = imageRepository;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string userProfileId, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Processing image upload for user {UserProfileId}", userProfileId);

            // Validate file
            if (file == null || file.Length == 0)
            {
                return ServiceResult.Failure<ImageUploadResult>("Empty file provided.", 400);
            }

            // Validate content type
            if (!_allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return ServiceResult.Failure<ImageUploadResult>("Invalid file type. Only image files are allowed.", 400);
            }

            // Get user's tier from Entra ID roles
            var tier = await _tierService.GetUserTierAsync(user);
            if (tier == null)
            {
                _logger.LogError("Unable to determine storage tier for user {UserProfileId}", userProfileId);
                return ServiceResult.Failure<ImageUploadResult>("Storage tier configuration error.", 500);
            }

            _logger.LogInformation("User {UserProfileId} is in tier {TierName}", userProfileId, tier.Name);

            // Get or create usage record
            var usage = await _usageRepository.GetOrCreateAsync(userProfileId);

            // Get tier limits
            if (!_tierLimits.TryGetValue(tier.Name, out var limits))
            {
                _logger.LogError("Unknown tier name: {TierName}", tier.Name);
                return ServiceResult.Failure<ImageUploadResult>("Unknown storage tier.", 500);
            }

            // Check file size limit
            var fileSizeInMB = file.Length / (1024m * 1024m);
            if (fileSizeInMB > limits.MaxFileSizeMB)
            {
                return ServiceResult.Failure<ImageUploadResult>(
                    $"File size exceeds limit for your subscription tier. Maximum: {limits.MaxFileSizeMB}MB", 400);
            }

            // Check file count limit
            var currentFileCount = await _imageRepository.GetCountByUserProfileIdAsync(userProfileId);
            if (currentFileCount >= limits.MaxFiles)
            {
                return ServiceResult.Failure<ImageUploadResult>(
                    $"Maximum number of files reached for your subscription tier. Limit: {limits.MaxFiles}", 403);
            }

            // Check storage quota
            var currentStorageUsed = usage.StorageUsedInBytes;
            var storageQuotaInBytes = (long)(tier.StorageInGB * 1024 * 1024 * 1024);
            if (currentStorageUsed + file.Length > storageQuotaInBytes)
            {
                return ServiceResult.Failure<ImageUploadResult>("Storage quota exceeded for your subscription tier.", 507);
            }

            // Check bandwidth quota
            var currentBandwidthUsed = usage.BandwidthUsedInBytes;
            var bandwidthQuotaInBytes = (long)(tier.BandwidthInGB * 1024 * 1024 * 1024);
            if (currentBandwidthUsed + file.Length > bandwidthQuotaInBytes)
            {
                return ServiceResult.Failure<ImageUploadResult>("Bandwidth limit exceeded for your subscription tier.", 402);
            }

            // Upload file to Azure Blob Storage
            var containerName = "images";
            var blobName = $"{userProfileId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            var blobHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType,
                CacheControl = "public, max-age=31536000" // Cache for 1 year
            };

            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHeaders,
                Metadata = new Dictionary<string, string>
                {
                    { "UserProfileId", userProfileId },
                    { "OriginalFileName", file.FileName },
                    { "UploadedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                }
            });

            // Create Image record
            var imageRecord = new InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI.Image
            {
                id = Guid.NewGuid().ToString(),
                UserProfileId = userProfileId,
                Name = file.FileName,
                Url = blobClient.Uri.ToString(),
                Size = file.Length,
                ContentType = file.ContentType,
                ContainerName = containerName,
                BlobName = blobName,
                UploadedAt = DateTime.UtcNow
            };

            await _imageRepository.AddAsync(imageRecord);

            // Update user's storage usage and bandwidth usage
            usage.StorageUsedInBytes += file.Length;
            usage.BandwidthUsedInBytes += file.Length; // Upload counts toward bandwidth usage
            usage.LastUpdated = DateTime.UtcNow;
            await _usageRepository.UpdateAsync(usage);

            _logger.LogInformation("Image uploaded successfully. User: {UserProfileId}, Image: {ImageId}, Size: {Size} bytes",
                userProfileId, imageRecord.id, file.Length);

            // Return success response
            var result = ServiceResult.Success<ImageUploadResult>(201);
            result.ImageData = new UploadImageResponse
            {
                Id = imageRecord.id,
                Url = imageRecord.Url,
                Name = imageRecord.Name,
                Size = imageRecord.Size
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image for user {UserProfileId}.", userProfileId);
            return ServiceResult.Failure<ImageUploadResult>("Internal server error occurred during upload.", 500);
        }
    }
}