using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageAPI
{
    public interface IImageStorageTierRepository : IGenericRepository<ImageStorageTier>
    {
        Task<IList<ImageStorageTier>> GetAllAsync();
        Task<ImageStorageTier?> GetByIdAsync(string id);
        Task<ImageStorageTier?> GetByNameAsync(string name);
    }
}
