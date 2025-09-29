using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageAPI
{
    public interface IImageStorageTierMembershipRepository : IGenericRepository<ImageStorageTierMembership>
    {
        Task<IList<ImageStorageTierMembership>> GetByUserProfileIdAsync(string userProfileId);
        Task<ImageStorageTierMembership?> GetForUserAsync(string userProfileId);
    }
}
