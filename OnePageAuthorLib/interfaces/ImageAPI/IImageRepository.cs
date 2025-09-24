using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageAPI
{
    public interface IImageRepository : IGenericRepository<Image>
    {
        Task<IList<Image>> GetByUserProfileIdAsync(string userProfileId);
        Task<long> GetTotalSizeByUserProfileIdAsync(string userProfileId);
        Task<int> GetCountByUserProfileIdAsync(string userProfileId);
    }
}