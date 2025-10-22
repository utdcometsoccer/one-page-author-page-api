using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageAPI
{
    public interface IImageStorageUsageRepository : IGenericRepository<ImageStorageUsage>
    {
        Task<ImageStorageUsage?> GetByUserProfileIdAsync(string userProfileId);
        Task<ImageStorageUsage> GetOrCreateAsync(string userProfileId);
    }
}
